<script setup>
import { ref, reactive } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { resetSendCodeApi, resetConfirmCodeApi } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const formData = reactive({
  email: '',
  code: '',
})
const errorMessage = ref('')
const isCodeSent = ref(false)
const countdown = ref(0)
const authStore = useAuthStore()

const handleSendCode = async () => {
  errorMessage.value = ''

  // Email 欄位檢查
  if (!formData.email) {
    errorMessage.value = '請輸入 Email 才能獲取驗證碼'
    return
  }

  // Email 格式檢查
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!emailRegex.test(formData.email)) {
    errorMessage.value = 'Email 格式不正確'
    return
  }

  try {
    await resetSendCodeApi({
      email: formData.email,
    })

    isCodeSent.value = true
    countdown.value = 60

    const timer = setInterval(() => {
      countdown.value--
      if (countdown.value <= 0) {
        clearInterval(timer) // 時間到清除計時器
      }
    }, 1000)
  } catch (error) {
    errorMessage.value = error.message || '驗證碼發送錯誤'
  }
}

const handleVerify = async () => {
  errorMessage.value = ''

  if (!formData.email || !formData.code) {
    errorMessage.value = '請確認 Email 與驗證碼已填寫'
    return
  }

  const codeRegex = /^\d{6}$/
  if (!codeRegex.test(formData.code)) {
    errorMessage.value = '驗證碼格式錯誤，請輸入 6 位數字'
    return
  }

  // 檢查驗證碼
  try {
    await resetConfirmCodeApi(formData)

    // 紀錄Email跟驗證碼
    authStore.setResetInfo(formData.email, formData.code)
    router.push({
      name: 'MemberAuth',
      params: { mode: 'reset' },
    })
  } catch (error) {
    errorMessage.value = error.message || '驗證碼確認錯誤'
  }
}
</script>

<template>
  <h2>忘記密碼</h2>

  <div v-if="errorMessage" class="alert alert-danger py-2 mb-3">
    {{ errorMessage }}
  </div>

  <form @submit.prevent="handleVerify">
    <div class="form-item">
      <label>Step 1. 輸入註冊Email</label>
      <div class="input-group">
        <input
          v-model="formData.email"
          type="email"
          class="form-control"
          placeholder="輸入Email"
          required
        />
        <button
          class="btn btn-outline-primary"
          type="button"
          :disabled="countdown > 0"
          @click="handleSendCode"
        >
          {{ countdown > 0 ? `${countdown} 秒後重新發送` : isCodeSent ? '重新發送' : '獲取驗證碼' }}
        </button>
      </div>
    </div>

    <div class="form-item">
      <label class="form-label">Step 2. 輸入 6 位數驗證碼</label>
      <input
        v-model="formData.code"
        type="text"
        maxlength="6"
        name=""
        class="form-control"
        :disabled="!isCodeSent"
      />
    </div>

    <div class="form-item">
      <button class="btn btn-primary w-100" :disabled="!isCodeSent" type="submit">重設密碼</button>
    </div>
  </form>

  <div class="text-center mt-3">
    <RouterLink :to="{ name: 'MemberAuth', params: { mode: 'login' } }">返回登入</RouterLink>
  </div>
</template>
