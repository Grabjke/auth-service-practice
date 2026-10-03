import { useState } from 'react'
import { request, type ApiResult } from './api/client'

export default function App() {
  const [result, setResult] = useState<ApiResult | null>(null)

  // Публичный запрос: GET /api/ping (через прокси Vite / nginx уходит на бэк)
  const ping = async () => setResult(await request('/api/ping'))

  return (
    <main>
      <h1>Auth Practice</h1>
      <button onClick={ping}>GET /api/ping</button>
      {result && (
        <pre>
          {`HTTP ${result.status}\n`}
          {JSON.stringify(result.body, null, 2)}
        </pre>
      )}
    </main>
  )
}
