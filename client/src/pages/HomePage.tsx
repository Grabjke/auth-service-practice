import { useState } from "react";
import { Link } from "react-router";
import { request, type ApiResult } from "../api/client";
import { apiFetch } from "../api/http";
import { useAuthMode, useIsAuthenticated } from "../auth/authStore";
import { JwtSessionPanel } from "../components/JwtSessionPanel";

const PARALLEL_PATHS = ["/api/auth/me", "/api/users/me", "/api/auth/admin"];

// Песочница запросов. В режиме jwt все запросы идут через apiFetch (перехватчик 401 → refresh → повтор)
export function HomePage() {
  const mode = useAuthMode();
  const isJwtUser = useIsAuthenticated();
  const [result, setResult] = useState<unknown>(null);

  const get = async (path: string) => setResult(await apiFetch(path));

  // Испорть подпись / сбрось access и жми: три 401 → ОДИН /refresh → три повтора
  const parallel = async () => {
    const responses = await Promise.all(PARALLEL_PATHS.map((p) => apiFetch(p)));
    setResult(
      Object.fromEntries(
        PARALLEL_PATHS.map((p, i) => [p, `HTTP ${responses[i].status}`]),
      ),
    );
  };

  return (
    <>
      {mode === "jwt" && (isJwtUser ? <JwtSessionPanel /> : <NotLoggedIn />)}

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
        {mode === "jwt" && (
          <button onClick={parallel}>3 запроса параллельно</button>
        )}
      </div>

      {result !== null && <ResultView result={result} />}
    </>
  );
}

function NotLoggedIn() {
  return (
    <section className="card">
      <p className="muted">
        Нет JWT-сессии. <Link to="/login">Войти →</Link>
      </p>
    </section>
  );
}

function ResultView({ result }: { result: unknown }) {
  const api = result as ApiResult;
  return (
    <pre>
      {typeof api?.status === "number"
        ? `HTTP ${api.status}\n${JSON.stringify(api.body, null, 2)}`
        : JSON.stringify(result, null, 2)}
    </pre>
  );
}
