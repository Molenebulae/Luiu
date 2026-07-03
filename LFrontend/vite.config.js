import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import basicSsl from '@vitejs/plugin-basic-ssl'
import process from 'node:process'

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, process.cwd());

  const plugins = [vue(), vueDevTools()];
  if (command === 'serve') {
    plugins.push(basicSsl());
  }
  return {
    plugins: plugins,
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      },
    },
    //基於家裡sass版本太新，讓我放個靜音
    css: {
      preprocessorOptions: {
        scss: {
          // 這一行可以過濾掉那些煩人的 Deprecation Warning
          silenceDeprecations: ["import", "global-builtin", "color-functions", "if-function"],
          additionalData: `
          // 載入必要函數
          @import "bootstrap/scss/functions";

          // 載入自訂變數
          @import "@/assets/scss/_variable.scss";

          // 載入剩下的變數
          @import "bootstrap/scss/variables";
          @import "bootstrap/scss/maps";
          @import "bootstrap/scss/mixins";

          // 載入寫好的樣式
          @import "@/assets/scss/components/buttons";
        `
        },
      },
    },
    server: {
      port: 3030,
      strictPort: true,
      https: false,
      open: true,
      // Proxy 設定
      proxy: {
        '/api': {
          target: env.VITE_BASE_URL,
          changeOrigin: true,
          secure: false
        },
        '/uploads': {
          target: env.VITE_BASE_URL,
          changeOrigin: true,
          secure: false
        }
      }
    },

    preview: {
      port: 4173,
      strictPort: true,
      https: false,
      open: true,
      // Proxy 設定
      proxy: {
        '/api': {
          target: env.VITE_BASE_URL,
          changeOrigin: true,
          secure: false
        },

      }
    }
  }

})
