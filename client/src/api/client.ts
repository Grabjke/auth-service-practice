// Ответ от API в удобном для отображения виде
export type ApiResult = {
  status: number
  body: unknown
}

// Обёртка над fetch: всегда возвращает статус и тело (даже при 401/403)
export async function request(path: string, init?: RequestInit): Promise<ApiResult> {
  const res = await fetch(path, init)
  const text = await res.text()
  let body: unknown = text
  try {
    body = text ? JSON.parse(text) : null
  } catch {
    // не JSON — оставляем текст как есть
  }
  return { status: res.status, body }
}
