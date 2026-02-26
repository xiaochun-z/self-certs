import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
    define: {
    '__APP_VER_TAG__': JSON.stringify(process.env.VITE_APP_VERSION || '0.0.1')
  },
  server: {
    host: true,
    proxy: {
      '/api': {
        target: 'http://host.docker.internal:5000',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
