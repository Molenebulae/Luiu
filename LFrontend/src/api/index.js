import axios from 'axios'
import { useUserStore } from '@/stores/user'
import router from '@/router'

const buildApiUrl = (path) => {
  if (!path) return import.meta.env.VITE_BASE_URL || ''
  if (/^https?:\/\//i.test(path)) return path

  const baseUrl = import.meta.env.VITE_BASE_URL || ''
  const normalizedBase = baseUrl.replace(/\/$/, '')
  const normalizedPath = path.startsWith('/') ? path : `/${path}`

  return `${normalizedBase}${normalizedPath}`
}

const service = axios.create({
  baseURL: buildApiUrl(import.meta.env.VITE_API_BASE_URL),
  timeout: 60000,
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
})

// 請求攔截器
service.interceptors.request.use(
  (config) => {
    if (config.version === 'v2') {
      config.baseURL = buildApiUrl(import.meta.env.VITE_API_V2)
    }

    let member = null
    try {
      member = JSON.parse(localStorage.getItem('Luiu_Member') || 'null')
    } catch {
      localStorage.removeItem('Luiu_Member')
    }

    const token = member?.token
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  (error) => Promise.reject(error),
)

// 回應攔截器
service.interceptors.response.use(
  (response) => {
    const res = response.data

    if (res.success) {
      return res
    } else {
      console.error(res.message || '系統錯誤')
      return Promise.reject(new Error(res.message || 'Error'))
    }
  },
  (error) => {
    const apiResponse = error.response?.data
    const status = error.response?.status

    const errorMsg = apiResponse?.message || error.message || '發生未知錯誤'

    console.error('狀態碼', status)
    console.error('前端攔截到錯誤:', errorMsg)
    // alert(errorMsg);

    if (status === 401) {
      const userStore = useUserStore()

      // 清除登入
      userStore.logout(true)
    }

    if (status === 429) {
      console.warn('[Interceptor] 偵測到 429 限流，攔截器決定放行路由，不觸發登出。')
      alert('刷新次數過於頻繁，請稍後再試')

      // 這裡改用 resolve 或是拋出一個特定字串，我們這裡拋出一個識別字串
      return Promise.reject('RATE_LIMIT_HIT')
    }

    return Promise.reject(apiResponse || error)
  },
)
console.log('API baseURL:', buildApiUrl(import.meta.env.VITE_API_BASE_URL))
export default service
