import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Адрес бэка для dev-прокси (локально — `dotnet run`, профиль http)
const apiTarget = process.env.API_URL ?? 'http://localhost:5285'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Все запросы /api/* Vite перенаправляет на бэк — CORS не нужен
    proxy: { '/api': { target: apiTarget, changeOrigin: true } },
  },
})
