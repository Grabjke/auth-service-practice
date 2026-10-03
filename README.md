# Auth Practice

Песочница для практики аутентификации в ASP.NET Core + React.

## Структура

```
src/
  AuthPractice.Domain          — сущности/правила предметной области (ни от чего не зависит)
  AuthPractice.Contracts       — DTO запросов/ответов API
  AuthPractice.Core            — фичи: endpoints + handlers (Domain, Contracts)
  AuthPractice.Infrastructure  — внешние вещи: аутентификация, БД, интеграции (Core)
  AuthPractice.Web             — хост: DI + middleware pipeline (Core, Infrastructure)
client/                        — React (Vite + TS)
```

## Ветки

- `main` — каркас слоёв, публичный `GET /api/ping`, Docker.
- `practice/auth-handler` — самописные `AuthenticationHandler` + `Options` + регистрация в DI и защищённый endpoint.

## Запуск в Docker

```bash
docker compose up --build
```

- UI: http://localhost:3000
- Swagger: http://localhost:8080/swagger

## Запуск для дебага (рекомендуется)

1. Бэк: открыть `AuthPractice.sln` в Rider/VS и запустить профиль `http` под дебагом
   (или `dotnet run --project src/AuthPractice.Web`) — http://localhost:5285
2. Клиент: `cd client && npm install && npm run dev` — http://localhost:5173
   (Vite проксирует `/api` на `localhost:5285`)
3. Ставить брейкпоинты и жать кнопки в UI.
