import { request, type ApiResult } from "../api/client";
import { getAccessToken, getAuthMode } from "./authStore";

// GET выбранной в сторе схемой. Токен и режим берём прямо из стора — компонентам ничего передавать не нужно.
// В режиме jwt cookie НЕ шлём (credentials: "omit"), чтобы бэк точно проверял только Bearer
export function authGet(path: string): Promise<ApiResult> {
  if (getAuthMode() === "cookie") return request(path);

  const token = getAccessToken();
  return request(path, {
    credentials: "omit",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
}
