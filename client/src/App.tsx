import { NavLink, Route, Routes } from "react-router";
import { useAuthMode, useAuthStore } from "./auth/authStore";
import { UserBadge } from "./components/UserBadge";
import { HomePage } from "./pages/HomePage";
import { ProfilePage } from "./pages/ProfilePage";

// Каркас: шапка (навигация, пользователь, схема) + страницы
export default function App() {
  const mode = useAuthMode();
  const setMode = useAuthStore((s) => s.setMode);

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
      </nav>

      {/* Схема хранится в сторе: страницы ходят на бэк тем же способом */}
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
        <Route path="/profile" element={<ProfilePage />} />
      </Routes>
    </main>
  );
}
