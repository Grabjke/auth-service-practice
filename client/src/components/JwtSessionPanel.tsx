import { useEffect, useState } from "react";
import { useAuthStore } from "../auth/authStore";

// Токен, обратный отсчёт до exp и claims — всё из стора, без props
export function JwtSessionPanel() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const expiresAt = useAuthStore((s) => s.expiresAt);
  const payload = useAuthStore((s) => s.payload);
  const tamperToken = useAuthStore((s) => s.tamperToken);
  const logout = useAuthStore((s) => s.logout);
  const now = useNow();

  if (!accessToken || !expiresAt) {
    return (
      <section className="card">
        <h2>Access-токен</h2>
        <p className="muted">
          Нет токена — запросы уйдут без заголовка Authorization (→ 401)
        </p>
      </section>
    );
  }

  const secondsLeft = Math.max(0, Math.round((expiresAt - now) / 1000));

  return (
    <section className="card">
      <h2>Access-токен</h2>
      <p className="muted">
        {secondsLeft > 0
          ? `Истекает через ${formatSeconds(secondsLeft)} (${new Date(expiresAt).toLocaleTimeString()})`
          : "Истёк — бэк ответит 401 (ClockSkew = 0)"}
      </p>
      <code className="token">{accessToken}</code>
      {/* Подпись НЕ проверяется на клиенте: claims видны всем, как на jwt.io */}
      <pre>{JSON.stringify(payload, null, 2)}</pre>
      <div className="row">
        <button onClick={tamperToken}>Испортить подпись</button>
        <button onClick={logout}>Забыть токен</button>
      </div>
    </section>
  );
}

// Текущее время, обновляется раз в секунду — для обратного отсчёта до exp
function useNow() {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  return now;
}

function formatSeconds(total: number) {
  const m = Math.floor(total / 60);
  const s = String(total % 60).padStart(2, "0");
  return `${m}:${s}`;
}
