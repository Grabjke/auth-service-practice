import { request, type Envelope } from "../api/client";
import { useAuthStore, type JwtLoginResponse } from "./authStore";

// Single-flight: пока идёт /refresh, все желающие получают ОДИН и тот же промис.
// Пять параллельных 401 → один запрос к /refresh, остальные ждут его результата
let refreshInFlight: Promise<boolean> | null = null;

export function refreshAccessToken(): Promise<boolean> {
  refreshInFlight ??= doRefresh().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

// Если обновление уже идёт — новый запрос сначала дождётся его и уйдёт со свежим access
export function waitForRefresh(): Promise<unknown> {
  return refreshInFlight ?? Promise.resolve();
}

async function doRefresh(): Promise<boolean> {
  const store = useAuthStore.getState();
  store.setRefreshing(true);
  try {
    // 409 = другая вкладка только что ротировала этот refresh. Браузер уже получил новую cookie
    // из её ответа (cookie общие на вкладки) — повторяем один раз
    for (let attempt = 0; attempt < 2; attempt++) {
      store.countRefresh();
      // Refresh ходит только cookie (HttpOnly, path=/api/auth/jwt) — в теле и заголовках его нет
      const res = await request("/api/auth/jwt/refresh", {
        method: "POST",
        credentials: "same-origin",
      });

      if (res.status === 200) {
        store.setSession((res.body as Envelope<JwtLoginResponse>).result!);
        return true;
      }
      if (res.status === 409 && attempt === 0) {
        await new Promise((r) => setTimeout(r, 300));
        continue;
      }
      break;
    }

    // 401: сессии нет / истекла / отозвана (logout, reuse detection) — только заново логиниться
    store.clearSession();
    return false;
  } finally {
    store.setRefreshing(false);
  }
}
