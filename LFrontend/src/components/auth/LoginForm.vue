<script setup>
import { RouterLink, useRouter, useRoute } from 'vue-router'
import { ref, reactive } from 'vue'
import { demoLoginApi, loginApi } from '@/api/auth'
import { useUserStore } from '@/stores/user'
import { useAuthManager } from '@/composables/AuthManager'
import LuiuSeparator from '@/components/base/LuiuSeparator.vue'
import googleIcon from '@/assets/icons/google_48x48.svg'
import facebookIcon from '@/assets/icons/facebook_48x48.svg'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const errorMessage = ref('')
const authManager = useAuthManager()
const loginForm = reactive({
  email: '',
  password: '',
})

// 使用第三方登入
const loginWithOAuth = async (providerName) => {
  try {
    await authManager.login(providerName)
  } catch (err) {
    errorMessage.value = err
  }
}

// 一般登入
const login = async () => {
  errorMessage.value = ''

  if (!loginForm.email || !loginForm.password) {
    errorMessage.value = 'Email 跟 密碼都要寫'
    return
  }

  try {
    loading.value = true
    const result = await loginApi(loginForm)
    const userStore = useUserStore()
    userStore.login(result.data)

    const fromPath = route.query.redirect

    if (fromPath) {
      router.push(fromPath)
    } else {
      router.push({ name: 'Home' })
    }
  } catch (error) {
    errorMessage.value = error?.message || '登入發生錯誤'
  } finally {
    loading.value = false
  }
}

const loginDemo = async () => {
  errorMessage.value = ''

  try {
    loading.value = true
    const result = await demoLoginApi()
    const userStore = useUserStore()
    userStore.login(result.data)

    const fromPath = route.query.redirect
    if (fromPath) {
      router.push(fromPath)
    } else {
      router.push({ name: 'Home' })
    }
  } catch (error) {
    errorMessage.value = error?.message || 'Demo 登入發生錯誤'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <h2>歡迎回來</h2>

  <form @submit.prevent="login">
    <div class="form-item">
      <label class="form-label">Email</label>
      <input
        v-model="loginForm.email"
        type="email"
        class="form-control"
        placeholder="example@example.com"
      />
    </div>

    <div class="form-item">
      <label class="form-label">Password</label>
      <input v-model="loginForm.password" type="password" class="form-control" />
      <div class="text-end">
        <RouterLink :to="{ name: 'MemberAuth', params: { mode: 'forgot' } }">忘記密碼</RouterLink>
      </div>
    </div>

    <div v-if="errorMessage" class="alert alert-danger m-0 py-1">
      {{ errorMessage }}
    </div>

    <div class="form-item">
      <button class="btn btn-primary w-100 my-2" :disabled="loading">
        {{ loading ? '驗證中...' : '登入' }}
      </button>
      <button type="button" class="btn btn-outline-primary w-100" :disabled="loading" @click="loginDemo">
        {{ loading ? '驗證中...' : '使用 Demo 帳號' }}
      </button>
    </div>
  </form>

  <div class="my-3">
    <LuiuSeparator>或使用以下方式</LuiuSeparator>
  </div>

  <div class="row g-2 my-3">
    <div class="col-6 oauth-item">
      <a
        href="#"
        class="btn btn-light w-100 d-flex align-items-center justify-content-center gap-2"
        :class="{ disabled: authManager.isAnyProviderLoading.value }"
        @click.prevent="loginWithOAuth('google')"
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
    <span class="text-black">沒有帳戶?</span>
    <RouterLink
      :to="{ name: 'MemberAuth', params: { mode: 'register' } }"
      class="text-decoration-none fw-bold ms-2"
      >立即註冊</RouterLink
    >
  </div>
</template>

<style></style>
