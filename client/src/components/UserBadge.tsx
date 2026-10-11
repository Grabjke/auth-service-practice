import { useNavigate } from "react-router";
import {
  useAuthMode,
  useAuthStore,
  useCurrentUser,
  useHasRole,
  useIsRefreshing,
} from "../auth/authStore";

// Любой компонент берёт пользователя из стора — без props и без запроса к /me
export function UserBadge() {
  const mode = useAuthMode();
  const user = useCurrentUser();
  const isAdmin = useHasRole("admin");
  const refreshing = useIsRefreshing();
  const logout = useAuthStore((s) => s.logout);
  const navigate = useNavigate();

  if (mode === "cookie") return null;

  // Тихое обновление (в т.ч. восстановление сессии после перезагрузки) — лёгкий лоадер
  if (refreshing && !user) return <span className="muted spinner">Восстанавливаем сессию…</span>;
  if (!user) return <span className="muted">JWT: не вошли</span>;

  return (
    <span className="row">
      {refreshing && <span className="spinner" aria-label="Обновляем сессию" />}
      <span className="badge">
        {user.userName} · {user.roles.join(", ") || "без ролей"}
        {isAdmin && " ★"}
      </span>
      <button
        onClick={async () => {
          await logout();
          navigate("/login");
        }}
      >
        Выйти
      </button>
    </span>
  );
}
