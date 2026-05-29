import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev proxy avoids browser CORS errors: React calls /api/* on :5173, Vite forwards to :5016.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5016',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
