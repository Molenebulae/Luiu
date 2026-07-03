// 1. 載入 jQuery
import jQuery from 'jquery'
window.$ = window.jQuery = jQuery

const initPlugins = async () => {
  await import('owl.carousel')
  // 如果還有其他依賴 jQuery 的插件，也放在這裡
}
initPlugins()

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

// 載入bootstrap css跟js
import './assets/scss/main.scss'

// 插件
import FontAwesomeIcon from './plugins/fontawesome'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
// import './assets/js/main.js';

import 'sweetalert2/dist/sweetalert2.min.css'

const app = createApp(App)

import { getImageUrl } from './utils/pathHelper'
app.config.globalProperties.$img = getImageUrl

// 註冊全域組件
app.component('font-awesome-icon', FontAwesomeIcon)

// 使用插件
app.use(createPinia())
app.use(router)

app.mount('#app')
