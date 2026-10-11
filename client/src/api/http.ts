import {
  getAccessToken,
  getAuthMode,
} from "../auth/authStore";
import { refreshAccessToken, waitForRefresh } from "../auth/refresh";
import { request, type ApiResult } from "./client";

// Куда деваться, если обновить сессию не вышло. Регистрирует App (через react-router navigate)
let onSessionExpired: () => void = () => {};
export function setSessionExpiredHandler(handler: () => void) {
  onSessionExpired = handler;
}

// fetch для защищённых API. В режиме jwt — перехватчик ответов:
// 401 → тихий /refresh (один на всех) → повтор исходного запроса с новым access.
// Пользователь видит только чуть более долгий запрос, без ошибки и без повторного логина
export async function apiFetch(
  path: string,
  init: RequestInit = {},
): Promise<ApiResult> {
  if (getAuthMode() === "cookie") return request(path, init);

  // Обновление уже идёт → не плодим заведомые 401, ждём свежий access
  await waitForRefresh();

  const tokenUsed = getAccessToken();
  const res = await request(path, withBearer(init, tokenUsed));
  if (res.status !== 401) return res;

  // Пока запрос летел, access уже обновил кто-то другой — просто повторяем с ним
  const refreshed =
    (getAccessToken() !== null && getAccessToken() !== tokenUsed) ||
    (await refreshAccessToken());

  if (!refreshed) {
    onSessionExpired(); // ошибку самого /refresh не рефрешим — выход на /login
    return res;
  }

  // Повтор — ровно один раз: следующий 401 возвращаем как есть, иначе бесконечная петля
  return request(path, withBearer(init, getAccessToken()));
}

// В режиме jwt cookie НЕ шлём (credentials: "omit"), чтобы бэк проверял только Bearer
function withBearer(init: RequestInit, token: string | null): RequestInit {
  const headers = new Headers(init.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);
  return { ...init, credentials: "omit", headers };
}
