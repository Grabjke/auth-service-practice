import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { postJson, type ApiResult } from "../api/client";
import { useAuthMode, useAuthStore } from "../auth/authStore";

// Регистрация + вход выбранной схемой. Сюда же ведёт неудачный тихий refresh
export function LoginPage() {
  const [userName, setUserName] = useState("alice");
  const [email, setEmail] = useState("alice@example.com");
  const [password, setPassword] = useState("secret123");
  const [result, setResult] = useState<ApiResult | null>(null);
  const mode = useAuthMode(); // переключатель — в шапке (App), общий для всех страниц
  const loginWithJwt = useAuthStore((s) => s.loginWithJwt);
  const navigate = useNavigate();

  // Регистрация: создаёт пользователя с ролью user, ни cookie, ни токен не выдаёт
  const register = async (e: FormEvent) => {
    e.preventDefault();
    setResult(
      await postJson("/api/auth/register", { userName, email, password }),
    );
  };

  // cookie: бэк ставит HttpOnly cookie "auth"
  // jwt: access в теле ответа (→ память), refresh в HttpOnly-cookie (→ браузер)
  const login = async (e: FormEvent) => {
    e.preventDefault();
    const res =
      mode === "cookie"
        ? await postJson("/api/auth/login", { email, password })
        : await loginWithJwt(email, password);

    setResult(res);
    if (res.status === 200) navigate("/");
  };

  return (
    <>
      <form className="card" onSubmit={login}>
        <h2>Вход {mode === "cookie" ? "(cookie)" : "(JWT)"}</h2>
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

      {result && (
        <pre>
          {`HTTP ${result.status}\n`}
          {JSON.stringify(result.body, null, 2)}
        </pre>
      )}
    </>
  );
}
