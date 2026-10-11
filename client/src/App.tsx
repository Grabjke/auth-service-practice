import { useEffect } from "react";
import { NavLink, Route, Routes, useNavigate } from "react-router";
import { setSessionExpiredHandler } from "./api/http";
import { useAuthMode, useAuthStore } from "./auth/authStore";
import { refreshAccessToken } from "./auth/refresh";
import { UserBadge } from "./components/UserBadge";
import { HomePage } from "./pages/HomePage";
import { LoginPage } from "./pages/LoginPage";
import { ProfilePage } from "./pages/ProfilePage";

// Каркас: шапка (навигация, пользователь, схема) + страницы
export default function App() {
  const mode = useAuthMode();
  const setMode = useAuthStore((s) => s.setMode);
  const navigate = useNavigate();

  // Тихий refresh не удался (сессия истекла / отозвана) → на логин
  useEffect(() => {
    setSessionExpiredHandler(() => navigate("/login"));
  }, [navigate]);

  // Hard reload: access в памяти пропал, refresh-cookie осталась → восстанавливаем access без пароля.
  // Не вышло — просто аноним, без редиректа. Запросы страниц, ушедшие параллельно, дождутся этого же refresh
  useEffect(() => {
    if (mode === "jwt" && !useAuthStore.getState().accessToken) {
      void refreshAccessToken();
    }
  }, [mode]);

  return (
    <main>
      <header className="row header">
        <h1>Auth Practice</h1>
        <UserBadge />
      </header>

      <nav className="row nav">
        <NavLink to="/" end>
          Главная
        </NavLink>
        <NavLink to="/profile">Профиль</NavLink>
        <NavLink to="/login">Вход</NavLink>
      </nav>

      {/* Схема хранится в сторе (и переживает перезагрузку): страницы ходят на бэк тем же способом */}
      <div className="row tabs" role="tablist">
        <span>Схема:</span>
        <button
          role="tab"
          aria-selected={mode === "cookie"}
          onClick={() => setMode("cookie")}
        >
          Cookie (веб)
        </button>
        <button
          role="tab"
          aria-selected={mode === "jwt"}
          onClick={() => setMode("jwt")}
        >
          JWT Bearer (API-клиент)
        </button>
      </div>

      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/profile" element={<ProfilePage />} />
      </Routes>
    </main>
  );
}
