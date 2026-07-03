<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { validatePassword, ValidateConfirmPassword } from '@/utils/validators'
import { resetPasswordApi } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const errorMessage = ref('')
const password = ref('')
const confirmPassword = ref('')

const handleReset = async () => {
  errorMessage.value = ''

  const passwordCheck = validatePassword(password.value)
  if (!passwordCheck.isValid) {
    errorMessage.value = passwordCheck.message
    return
  }

  const confirmCheck = ValidateConfirmPassword(password.value, confirmPassword.value)
  if (!confirmCheck.isValid) {
    errorMessage.value = confirmCheck.message
    return
  }

  // TODO: 如果驗證碼過期要導回輸入信箱那頁
  // 修改密碼
  try {
    await resetPasswordApi({
      email: authStore.resetEmail,
      code: authStore.resetCode,
      password: password.value,
    })

    // 刪除Email跟驗證碼
    authStore.clearResetInfo()

    alert('密碼修改成功! 請使用新密碼登入')
    router.push({
      name: 'MemberAuth',
      params: { mode: 'login' },
    })
  } catch (error) {
    errorMessage.value = error.message || '修改密碼發生錯誤'
  }
}
</script>

<template>
  <h2>重設密碼</h2>
  <div v-if="errorMessage" class="alert alert-danger py-2 mb-3">
    {{ errorMessage }}
  </div>

  <form @submit.prevent="handleReset">
    <div class="form-item">
      <label class="form-label">設定新密碼</label>
      <input v-model="password" type="password" class="form-control" required />
    </div>
    <div class="form-item">
      <label class="form-label">確認密碼</label>
      <input v-model="confirmPassword" type="password" class="form-control" required />
    </div>

    <div class="form-item">
      <button class="btn btn-primary w-100">確認修改</button>
    </div>
  </form>
</template>
