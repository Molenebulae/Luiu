<script setup>
import { onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { googleLoginApi } from '@/api/auth'

const route = useRoute()

onMounted(async () => {
  // 1. 從小視窗的網址 Query 中，摘下 Google 賜予的臨時通行證 (code)
  const code = route.query.code

  if (code) {
    try {
      const response = await googleLoginApi({ code: code })
      console.log('googleCallBack')
      console.log(response)
      console.log(response.data)

      //  當後端回傳成功
      window.opener.postMessage(
        { type: 'GOOGLE_AUTH_SUCCESS', data: response.data },
        window.location.origin,
      )
    } catch (err) {
      // 失敗了，跳 alert
      const errorMsg = err.message || 'Google 登入失敗'
      window.opener.postMessage(
        { type: 'GOOGLE_AUTH_FAILED', message: errorMsg },
        window.location.origin,
      )
    } finally {
      // 小視窗在正式關閉！
      window.close()
    }
  } else {
    // 如果沒有 code，代表出了問題，也直接關閉視窗
    window.close()
  }
})
</script>

<template>
  <div class="d-flex flex-column align-items-center justify-content-center vh-100 bg-white">
    <div class="spinner-border text-primary" role="status"></div>
    <h5 class="mt-3 text-secondary">Google 安全驗證中，請勿關閉視窗...</h5>
  </div>
</template>
