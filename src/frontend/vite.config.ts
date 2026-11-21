import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// API URL used by the dev server proxy (the backend "https" launch profile).
const apiTarget = process.env.VITE_API_PROXY_TARGET ?? 'https://localhost:7144'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true, secure: false },
    },
  },
  build: {
    // The backend serves the compiled SPA from its wwwroot folder.
    outDir: fileURLToPath(new URL('../backend/Pharmacy.Api/wwwroot', import.meta.url)),
    emptyOutDir: true,
  },
})
