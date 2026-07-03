import { defineConfig, globalIgnores } from 'eslint/config'
import globals from 'globals'
import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import pluginOxlint from 'eslint-plugin-oxlint'
import skipFormatting from 'eslint-config-prettier/flat'

export default defineConfig([
  {
    name: 'app/files-to-lint',
    files: ['**/*.{vue,js,mjs,jsx}'],
  },

  globalIgnores(['**/dist/**', '**/dist-ssr/**', '**/coverage/**']),

  {
    languageOptions: {
      globals: {
        ...globals.browser,
      },
    },
  },

  js.configs.recommended,
  ...pluginVue.configs['flat/essential'],

  ...pluginOxlint.buildFromOxlintConfigFile('.oxlintrc.json'),

  skipFormatting,
  {
    name: 'luiu-rules',
    rules: {
      // 沒使用的變數給警告就好
      'no-unused-vars': 'warn',

      // 強制使用單引號
      // 'quotes': ['error', 'single'],

      'vue/require-v-for-key': 'error',
      'vue/no-v-html': 'warn',                // 警告使用 v-html，防止 XSS 攻擊
      'vue/component-api-style': ['error', ['script-setup']], // 強制使用 script setup 語法，保持團隊風格統一
      'no-duplicate-imports': 'error',
      'vue/block-order': ['error', {
        'order': ['script', 'template', 'style']
      }],
      // 強制 defineProps 必須在 defineEmits 之前
      'vue/define-macros-order': ['error', {
        'order': ['defineProps', 'defineEmits']
      }],

      // 強制屬性的排序 (例如 v-if 放在前面，v-for 放在後面，接著是屬性，最後是事件)
      'vue/attributes-order': ['warn', {
        'order': [
          'DEFINITION',      // is, v-is
          'LIST_RENDERING',   // v-for
          'CONDITIONALS',     // v-if, v-else-if, v-else
          'RENDER_MODIFIERS', // v-once, v-pre
          'GLOBAL',           // id
          'UNIQUE',           // ref, key
          'TWO_WAY_BINDING',  // v-model
          'OTHER_ATTR',       // 一般屬性
          'EVENTS',           // @click
          'CONTENT'           // v-text, v-html
        ],
        'alphabetical': false // 是否要按字母順序排列（通常不建議，按功能排較直觀）
      }],
    },
  },
])
