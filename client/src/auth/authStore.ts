import { create } from "zustand";
import {
  postJson,
  type ApiResult,
  type AuthMode,
  type Envelope,
} from "../api/client";
import { decodeJwtPayload, tamperSignature, type JwtPayload } from "./jwt";

// Ответ POST /api/auth/jwt/login
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

  loginWithJwt: (email: string, password: string) => Promise<ApiResult>;
  setMode: (mode: AuthMode) => void;
  logout: () => void;
  tamperToken: () => void;
};

const emptySession = {
  accessToken: null,
  expiresAt: null,
  user: null,
  payload: null,
};

// Сессия JWT. Только в памяти (без persist): localStorage прочитает любой XSS,
// поэтому после перезагрузки страницы нужно залогиниться заново.
// Компоненты читают через хуки ниже, не-React код (api/client) — через useAuthStore.getState()
export const useAuthStore = create<AuthState>()((set, get) => ({
  mode: "cookie",
  ...emptySession,

  setMode: (mode) => set({ mode }),

  // Бэк возвращает accessToken + expiresAt в теле, без Set-Cookie
  loginWithJwt: async (email, password) => {
    const res = await postJson(
      "/api/auth/jwt/login",
      { email, password },
      { credentials: "omit" },
    );
    if (res.status === 200) {
      const { result } = res.body as Envelope<JwtLoginResponse>;
      set(toSession(result!.accessToken, Date.parse(result!.expiresAt)));
    }
    return res;
  },

  logout: () => set(emptySession),

  // Один символ подписи → HMAC не сойдётся → бэк ответит 401. Claims при этом те же
  tamperToken: () => {
    const { accessToken } = get();
    if (accessToken) set({ accessToken: tamperSignature(accessToken) });
  },
}));

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

// Для запросов вне React (fetch-обёртки)
export const getAccessToken = () => useAuthStore.getState().accessToken;
export const getAuthMode = () => useAuthStore.getState().mode;
