<script setup>
import { RouterLink, useRouter } from 'vue-router'
import { ref } from 'vue'
import { registerSendCodeApi } from '@/api/auth'
import LuiuSeparator from '@/components/base/LuiuSeparator.vue'
import googleIcon from '@/assets/icons/google_48x48.svg'
import facebookIcon from '@/assets/icons/facebook_48x48.svg'

const router = useRouter()
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const errorMessage = ref('')
const isLoading = ref(false)

const handleSubmit = async () => {
  console.log('開始註冊流程')

  // 1. 檢查密碼一致
  // TODO: 強密碼檢查
  if (password.value !== confirmPassword.value) {
    errorMessage.value = '兩次密碼輸入不一致！'
    return
  }

  isLoading.value = true
  try {
    // 發送驗證碼
    await registerSendCodeApi({
      email: email.value,
      password: password.value,
    })

    router.push({
      name: 'MemberAuth',
      params: { mode: 'verify' },
      query: { email: email.value },
    })
  } catch (error) {
    errorMessage.value = error.message || '註冊發生錯誤'
    isLoading.value = false
  }
}
</script>

<template>
  <h2>註冊帳號</h2>

  <form @submit.prevent="handleSubmit">
    <div class="form-item">
      <label class="form-label">Email</label>
      <input v-model="email" type="email" class="form-control" required />
    </div>

    <div class="form-item">
      <label class="form-label">密碼</label>
      <input v-model="password" type="password" class="form-control" required />
    </div>

    <div class="form-item">
      <label class="form-label">確認密碼</label>
      <input v-model="confirmPassword" type="password" class="form-control" required />
    </div>

    <div v-if="errorMessage" class="alert alert-danger m-0 py-1">
      {{ errorMessage }}
    </div>

    <div class="form-item">
      <button class="btn btn-primary w-100" :disabled="isLoading">
        <span v-if="isLoading">
          <span
            class="spinner-border spinner-border-sm me-2"
            role="status"
            aria-hidden="true"
          ></span>
          帳號建立中，請稍候...
        </span>
        <span v-else>建立帳號</span>
      </button>
    </div>
  </form>

  <div class="my-3">
    <LuiuSeparator>或使用以下方式</LuiuSeparator>
  </div>

  <div class="row g-2 mb-3">
    <div class="col-6 oauth-item">
      <a
        href="#"
        class="btn btn-light w-100 d-flex align-items-center justify-content-center gap-2"
      >
        <img :src="googleIcon" alt="Google" height="24" width="24" />
        <span>Google</span>
      </a>
    </div>
    <div class="col-6 oauth-item">
      <a href="#" class="btn btn-light d-flex align-items-center justify-content-center gap-2">
        <img :src="facebookIcon" alt="Facebook" height="24" width="24" />
        <span>Facebook</span>
      </a>
    </div>
  </div>

  <div class="text-center">
    <span class="text-black">已經有帳戶了? </span>
    <RouterLink :to="{ name: 'MemberAuth', params: { mode: 'login' } }" class="luiu-a-primary">
      立即登入</RouterLink
    >
  </div>
</template>

<style></style>
