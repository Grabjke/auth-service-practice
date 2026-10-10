import { useState, type FormEvent } from "react";
import { postJson, request, type ApiResult } from "../api/client";
import { authGet } from "../auth/authFetch";
import { useAuthMode, useAuthStore } from "../auth/authStore";
import { JwtSessionPanel } from "../components/JwtSessionPanel";

export function HomePage() {
  const [userName, setUserName] = useState("alice");
  const [email, setEmail] = useState("alice@example.com");
  const [password, setPassword] = useState("secret123");
  const mode = useAuthMode(); // переключатель — в шапке (App), общий для всех страниц
  const [result, setResult] = useState<ApiResult | null>(null);
  // JWT-сессия живёт в zustand-сторе (auth/authStore.ts) — доступна из любого компонента
  const loginWithJwt = useAuthStore((s) => s.loginWithJwt);

  // Регистрация: создаёт пользователя с ролью user, ни cookie, ни токен не выдаёт
  const register = async (e: FormEvent) => {
    e.preventDefault();
    setResult(
      await postJson("/api/auth/register", { userName, email, password }),
    );
  };

  // cookie: бэк ставит HttpOnly cookie "auth" (в JS её не видно — смотри DevTools → Application)
  // jwt: бэк возвращает accessToken + expiresAt в теле, без Set-Cookie
  const login = async (e: FormEvent) => {
    e.preventDefault();

    if (mode === "cookie") {
      setResult(await postJson("/api/auth/login", { email, password }));
      return;
    }

    setResult(await loginWithJwt(email, password));
  };

  const get = async (path: string) => setResult(await authGet(path));

  return (
    <>
      <form className="card" onSubmit={register}>
        <h2>Регистрация</h2>
        <input
          value={userName}
          onChange={(e) => setUserName(e.target.value)}
          placeholder="username"
          autoComplete="username"
        />
        <input
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="email"
          type="email"
          autoComplete="email"
        />
        <input
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="password"
          type="password"
          autoComplete="new-password"
        />
        <button type="submit">POST /api/auth/register</button>
      </form>

      <form className="card" onSubmit={login}>
        <h2>Логин {mode === "cookie" ? "(cookie)" : "(JWT)"}</h2>
        <input
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="email"
          type="email"
          autoComplete="email"
        />
        <input
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="password"
          type="password"
          autoComplete="current-password"
        />
        <button type="submit">
          {mode === "cookie"
            ? "POST /api/auth/login"
            : "POST /api/auth/jwt/login"}
        </button>
      </form>

      {mode === "jwt" && <JwtSessionPanel />}

      <div className="row">
        {/* cookie или Bearer — обе схемы; ни того ни другого → 401 */}
        <button onClick={() => get("/api/auth/me")}>GET /api/auth/me</button>
        {/* нужна роль admin: обычный пользователь → 403 */}
        <button onClick={() => get("/api/auth/admin")}>
          GET /api/auth/admin
        </button>
        <button onClick={() => request("/api/ping").then(setResult)}>
          GET /api/ping
        </button>
      </div>

      {result && (
        <pre>
          {`HTTP ${result.status}\n`}
          {JSON.stringify(result.body, null, 2)}
        </pre>
      )}
    </>
  );
}
