import { useEffect, useState, type ReactNode } from "react";
import { Link } from "react-router";
import { getUser, type GetUserResult } from "../api/users";
import { useAuthMode, useAuthStore } from "../auth/authStore";

// Профиль текущего пользователя — GET /api/users/me
export function ProfilePage() {
  const mode = useAuthMode();
  const accessToken = useAuthStore((s) => s.accessToken);
  const [reloadKey, setReloadKey] = useState(0);
  // Ответ запоминаем вместе с ключом запроса: «загрузка» = ответ ещё не для текущего ключа
  const [loaded, setLoaded] = useState<{ key: string; res: GetUserResult } | null>(null);
  const requestKey = `${mode}|${accessToken}|${reloadKey}`;
  const loading = loaded?.key !== requestKey;
  const state = loaded?.res ?? null;

  // Перезапрашиваем при смене схемы / токена (новый вход, «испорченная» подпись, выход) и по кнопке
  useEffect(() => {
    let cancelled = false;
    getUser().then((res) => {
      if (!cancelled) setLoaded({ key: requestKey, res });
    });
    return () => {
      cancelled = true;
    };
  }, [requestKey]);

  const profile = state?.profile;

  return (
    <>
      <div className="row header">
        <h2>Профиль</h2>
        <button onClick={() => setReloadKey((k) => k + 1)} disabled={loading}>
          {loading ? "Загрузка…" : "Обновить"}
        </button>
      </div>

      {!loading && !profile && (
        <section className="card">
          <p>
            {state?.status === 401
              ? "Вы не вошли в аккаунт"
              : "Не удалось загрузить профиль"}
          </p>
          <Link to="/">Войти →</Link>
        </section>
      )}

      {profile && (
        <section className="card">
          <h3>{profile.userName}</h3>
          <dl className="props">
            <Prop name="Email">
              {profile.email}{" "}
              <span className={profile.emailConfirmed ? "ok" : "warn"}>
                {profile.emailConfirmed ? "подтверждён" : "не подтверждён"}
              </span>
            </Prop>
            <Prop name="Телефон">{profile.phoneNumber ?? "не указан"}</Prop>
            <Prop name="Роли">{profile.roles.join(", ") || "—"}</Prop>
            <Prop name="Двухфакторная защита">
              {profile.twoFactorEnabled ? "включена" : "выключена"}
            </Prop>
          </dl>
        </section>
      )}
    </>
  );
}

function Prop({ name, children }: { name: string; children: ReactNode }) {
  return (
    <>
      <dt>{name}</dt>
      <dd>{children}</dd>
    </>
  );
}
