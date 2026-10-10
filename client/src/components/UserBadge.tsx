import { useCurrentUser, useHasRole } from "../auth/authStore";

// Пример: любой компонент берёт пользователя из стора — без props и без запроса к /me
export function UserBadge() {
  const user = useCurrentUser();
  const isAdmin = useHasRole("admin");

  if (!user) return <span className="muted">JWT: не вошли</span>;

  return (
    <span className="badge">
      {user.userName} · {user.email} · {user.roles.join(", ") || "без ролей"}
      {isAdmin && " ★"}
    </span>
  );
}
