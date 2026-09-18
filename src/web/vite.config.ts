import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Same-origin from the browser's perspective, so the BFF's session
      // cookie is set for this origin and no CORS policy is needed.
      '/bff': {
        target: 'http://localhost:5180',
        changeOrigin: true,
      },
    },
  },
})
