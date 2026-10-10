// Ответ от API в удобном для отображения виде
export type ApiResult = {
  status: number;
  body: unknown;
};

// Как аутентифицируемся на бэке:
// - cookie: браузер сам шлёт HttpOnly cookie "auth" (тот же origin через прокси) — веб-фронт
// - jwt: токен в заголовке Authorization: Bearer <token> — так ходят mobile / другие сервисы
export type AuthMode = "cookie" | "jwt";

// Конверт ответа бэка (Envelope из SharedKernel)
export type Envelope<T> = {
  result: T | null;
  errors: unknown;
  isError: boolean;
  timeGenerated: string;
};

// Обёртка над fetch: всегда возвращает статус и тело (даже при 401/403)
export async function request(
  path: string,
  init?: RequestInit,
): Promise<ApiResult> {
  const res = await fetch(path, init);
  const text = await res.text();
  let body: unknown = text;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    // не JSON — оставляем текст как есть
  }
  return { status: res.status, body };
}

// POST с JSON-телом. Cookie (auth) браузер шлёт/сохраняет сам: запросы идут на тот же origin через прокси
export function postJson(
  path: string,
  data: unknown,
  init?: RequestInit,
): Promise<ApiResult> {
  return request(path, {
    ...init,
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}
