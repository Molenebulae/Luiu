<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { registerConfirmCodeApi } from '@/api/auth'
import { useUserStore } from '@/stores/user'
import { luiuNotify } from '@/utils/sweetAlert'

const route = useRoute()
const router = useRouter()

const verifyCode = ref('')
const errorMessage = ref('')
const email = route.query.email

const handleVerify = async () => {
  console.log(email)
  console.log(verifyCode.value)
  // 判斷驗證碼是否正確
  try {
    const result = await registerConfirmCodeApi({
      email: email,
      code: verifyCode.value,
    })

    // 正確直接登入
    const userStore = useUserStore()
    userStore.login(result.data)

    luiuNotify.success('註冊成功')

    router.push({ name: 'Home' })
  } catch (error) {
    errorMessage.value = error.message || '驗證發生錯誤'
  }
}
</script>

<template>
  <h2>信箱驗證</h2>
  <p>驗證碼已發至：{{ email }}</p>

  <form @submit.prevent="handleVerify">
    <div class="form-item">
      <label class="form-label">請輸入6位數驗證碼</label>
      <input v-model="verifyCode" type="text" name="" class="form-control" />
    </div>

    <div v-if="errorMessage" class="alert alert-danger m-0 py-1">
      {{ errorMessage }}
    </div>

    <div class="form-item">
      <button class="btn btn-primary w-100">完成註冊</button>
    </div>
  </form>
</template>

<style></style>
