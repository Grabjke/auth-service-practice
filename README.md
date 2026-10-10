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
- `practice/auth-handler` — ASP.NET Identity + cookie-схема: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`.
  Клаймы собираются вручную (`User.ToClaimsPrincipal`, имена — `AuthClaims`), хендлеры пишут в БД через `ITransactionManager`.
  Самописный `TestAuthenticationHandler` — в коммите `0030242`.
- JWT Bearer — вторая, именованная схема рядом с cookie (для mobile / сервисов):
  `POST /api/auth/jwt/login` → `{ accessToken, expiresAt }` (без Set-Cookie), дальше `Authorization: Bearer <token>`.
  Cookie остаётся default-схемой: `RequireAuthorization()` без схем — только cookie;
  `/api/auth/me` и политика `AdminOnly` пускают обе (`AuthSchemes.CookieOrBearer`).
  HS256, `ExpireMinutes` ≤ 30, `ClockSkew = 0`, claims: `sub, iss, aud, exp, iat, nbf, jti, name, email, security_stamp, role`.
  Refresh-токенов пока нет — отозвать JWT до `exp` нельзя.
- `UserScopeData` (Core/Auth, Scoped) — текущий пользователь запроса для хендлеров через DI.
  Заполняет `UserScopeDataMiddleware` (между `UseAuthentication` и `UseAuthorization`): cookie из `HttpContext.User`,
  иначе явно `AuthenticateAsync(Bearer)`. `HttpContext.User` не подменяется. `GET /api/users/me` — профиль
  (id, имя, email, телефон, роли, 2FA — из БД). На фронте — страница `/profile`.

## Пакеты

Общие библиотеки из [Grabjke/shared](https://github.com/Grabjke/shared) (GitHub Packages):
`SharedKernel` (Domain), `Core` + `Framework` (Core-слой: `IEndpoint`, `IQueryHandler`, `ResultResponse`, `ExceptionMiddleware`).

Нужен PAT с `read:packages` в пользовательском конфиге (не в репо):

```bash
dotnet nuget update source github \
  --username Grabjke --password <PAT> --store-password-in-clear-text \
  --configfile ~/.nuget/NuGet/NuGet.Config
```

## Настройки JWT

Options pattern: секция `Jwt` из `appsettings*.json` → `IOptions<JwtOptions>`.
`Issuer` / `Audience` / `ExpireMinutes` — в `appsettings.json`, dev-ключ `SigningKey` — в `appsettings.Development.json`.
Невалидные настройки (нет ключа, ключ < 32 символов, `ExpireMinutes` > 30) → бэк падает на старте (`OptionsValidationException`).
Переменная окружения `Jwt__SigningKey` перекрывает значение из JSON (так задаётся ключ вне Development).

## Запуск в Docker

```bash
docker compose up --build
```

- UI: http://localhost:3000
- Swagger: http://localhost:8080/swagger
- Postgres: localhost:5432 (`postgres`/`postgres`, БД `auth_practice`, схема `identity`).
  Миграции `AppIdentityDbContext` накатываются при старте бэка.

Новая миграция:

```bash
dotnet ef migrations add <Name> --project src/AuthPractice.Infrastructure \
  --startup-project src/AuthPractice.Web --output-dir Database/Migrations
```

## Запуск для дебага (рекомендуется)

0. БД: `docker compose up -d postgres`
1. Бэк: открыть `AuthPractice.sln` в Rider/VS и запустить профиль `http` под дебагом
   (или `dotnet run --project src/AuthPractice.Web`) — http://localhost:5285
2. Клиент: `cd client && npm install && npm run dev` — http://localhost:5173
   (Vite проксирует `/api` на `localhost:5285`)
3. Ставить брейкпоинты и жать кнопки в UI. Переключатель «Cookie / JWT Bearer» выбирает схему:
   в режиме JWT токен хранится в zustand-сторе (`client/src/auth/authStore.ts`, только в памяти) и уходит в `Authorization`,
   cookie не отправляется. Пользователь из claims доступен в любом компоненте: `useCurrentUser()`, `useHasRole("admin")`.
   Там же — декодированный payload, обратный отсчёт до `exp` и кнопка «Испортить подпись» (→ 401).
