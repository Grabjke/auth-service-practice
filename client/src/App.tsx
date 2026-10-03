import { useState } from 'react'
import { request, type ApiResult } from './api/client'

// Должно совпадать с TestAuthentication:HeaderName в appsettings.json бэка
const TOKEN_HEADER = 'X-Test-Token'

// Токены из appsettings.json (+ один неверный, чтобы увидеть AuthenticateResult.Fail)
const PRESETS = ['', 'user-token', 'admin-token', 'wrong-token']

export default function App() {
  const [token, setToken] = useState('user-token')
  const [result, setResult] = useState<ApiResult | null>(null)

  // Отправляем запрос; если токен задан — кладём его в заголовок
  const send = async (path: string) => {
    const headers: HeadersInit = token ? { [TOKEN_HEADER]: token } : {}
    setResult(await request(path, { headers }))
  }

  return (
    <main>
      <h1>Auth Practice</h1>

      <div className="row">
        <label>
          {TOKEN_HEADER}:{' '}
          <input value={token} onChange={(e) => setToken(e.target.value)} placeholder="без токена" />
        </label>
      </div>
      <div className="row">
        {PRESETS.map((p) => (
          <button key={p || 'none'} onClick={() => setToken(p)}>
            {p || 'без токена'}
          </button>
        ))}
      </div>

      <div className="row">
        {/* публичный — handler вернёт NoResult, но endpoint всё равно ответит 200 */}
        <button onClick={() => send('/api/ping')}>GET /api/ping</button>
        {/* нужен любой валидный токен, иначе 401 */}
        <button onClick={() => send('/api/auth/me')}>GET /api/auth/me</button>
        {/* нужна роль admin: user-token → 403 */}
        <button onClick={() => send('/api/auth/admin')}>GET /api/auth/admin</button>
      </div>

      {result && (
        <pre>
          {`HTTP ${result.status}\n`}
          {JSON.stringify(result.body, null, 2)}
        </pre>
      )}
    </main>
  )
}
