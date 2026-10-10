// Payload JWT (вторая часть header.payload.signature) — это просто base64url-JSON.
// Подпись тут НЕ проверяется: читать claims может кто угодно, подделать — нет
export type JwtPayload = {
  sub: string;
  name: string;
  email: string;
  security_stamp: string;
  role?: string | string[]; // одна роль — строка, несколько — массив
  iss: string;
  aud: string;
  exp: number;
  iat: number;
  jti: string;
};

export function decodeJwtPayload(token: string): JwtPayload | null {
  try {
    const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const json = new TextDecoder().decode(
      Uint8Array.from(atob(base64), (c) => c.charCodeAt(0)),
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}

// Меняем один символ в НАЧАЛЕ подписи (последний символ base64url может кодировать
// только "лишние" биты и после декодирования дать ту же подпись)
export function tamperSignature(token: string): string {
  const [header, payload, signature] = token.split(".");
  const swapped = signature[0] === "A" ? "B" : "A";
  return `${header}.${payload}.${swapped}${signature.slice(1)}`;
}
