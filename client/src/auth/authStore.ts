import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import {
  postJson,
  type ApiResult,
  type AuthMode,
  type Envelope,
} from "../api/client";
import { decodeJwtPayload, tamperSignature, type JwtPayload } from "./jwt";

// Ответ POST /api/auth/jwt/login и /api/auth/jwt/refresh
export type JwtLoginResponse = {
  accessToken: string;
  expiresAt: string;
};

// Пользователь из claims токена — те же поля, что отдаёт GET /api/auth/me
export type CurrentUser = {
  id: string;
  userName: string;
  email: string;
  roles: string[];
  securityStamp: string;
};

type AuthState = {
  // Какой схемой ходим на бэк — общий для всех страниц
  mode: AuthMode;
  accessToken: string | null;
  expiresAt: number | null; // ms since epoch
  user: CurrentUser | null;
  payload: JwtPayload | null;
  // Идёт тихий /refresh — UI показывает лоадер, запросы ждут
  refreshing: boolean;
  // Сколько раз реально сходили в /refresh (для наглядности single-flight)
  refreshCount: number;

  setMode: (mode: AuthMode) => void;
  loginWithJwt: (email: string, password: string) => Promise<ApiResult>;
  logout: () => Promise<void>;
  setSession: (response: JwtLoginResponse) => void;
  clearSession: () => void;
  setRefreshing: (refreshing: boolean) => void;
  countRefresh: () => void;
  tamperToken: () => void;
  dropAccessToken: () => void;
};

const emptySession = {
  accessToken: null,
  expiresAt: null,
  user: null,
  payload: null,
};

// Access — ТОЛЬКО в памяти: localStorage прочитает любой XSS.
// После перезагрузки access восстанавливается тихим /refresh (refresh лежит в HttpOnly-cookie, JS её не видит).
// persist сохраняет лишь выбранный режим — это не секрет.
// Компоненты читают через хуки ниже, не-React код (api/http) — через useAuthStore.getState()
export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      mode: "cookie",
      ...emptySession,
      refreshing: false,
      refreshCount: 0,

      setMode: (mode) => set({ mode }),

      // Access — в теле ответа, refresh — в Set-Cookie (HttpOnly).
      // credentials: "same-origin" — иначе браузер проигнорирует Set-Cookie
      loginWithJwt: async (email, password) => {
        const res = await postJson(
          "/api/auth/jwt/login",
          { email, password },
          { credentials: "same-origin" },
        );
        if (res.status === 200) {
          get().setSession((res.body as Envelope<JwtLoginResponse>).result!);
        }
        return res;
      },

      // Сервер отзывает refresh-сессию и чистит cookie. Access доживёт свои минуты, но мы его забываем
      logout: async () => {
        await postJson("/api/auth/jwt/logout", {}, { credentials: "same-origin" });
        set(emptySession);
      },

      setSession: ({ accessToken, expiresAt }) =>
        set(toSession(accessToken, Date.parse(expiresAt))),

      clearSession: () => set(emptySession),

      setRefreshing: (refreshing) => set({ refreshing }),

      countRefresh: () => set((s) => ({ refreshCount: s.refreshCount + 1 })),

      // Один символ подписи → HMAC не сойдётся → 401 → перехватчик сделает refresh
      tamperToken: () => {
        const { accessToken } = get();
        if (accessToken) set({ accessToken: tamperSignature(accessToken) });
      },

      // Как после перезагрузки страницы: access пропал, refresh-cookie осталась
      dropAccessToken: () => set(emptySession),
    }),
    {
      name: "auth-practice",
      storage: createJSONStorage(() => localStorage),
      partialize: (s) => ({ mode: s.mode }),
    },
  ),
);

function toSession(accessToken: string, expiresAt: number) {
  const payload = decodeJwtPayload(accessToken);
  if (!payload) return emptySession;

  const user: CurrentUser = {
    id: payload.sub,
    userName: payload.name,
    email: payload.email,
    roles: payload.role ? [payload.role].flat() : [],
    securityStamp: payload.security_stamp,
  };
  return { accessToken, expiresAt, user, payload };
}

// --- Хуки для компонентов: подписка только на нужный срез, лишних ререндеров нет ---

export const useCurrentUser = () => useAuthStore((s) => s.user);

export const useIsAuthenticated = () => useAuthStore((s) => s.user !== null);

export const useHasRole = (role: string) =>
  useAuthStore((s) => s.user?.roles.includes(role) ?? false);

export const useAuthMode = () => useAuthStore((s) => s.mode);

export const useIsRefreshing = () => useAuthStore((s) => s.refreshing);

// Для запросов вне React (fetch-обёртки)
export const getAccessToken = () => useAuthStore.getState().accessToken;
export const getAuthMode = () => useAuthStore.getState().mode;
