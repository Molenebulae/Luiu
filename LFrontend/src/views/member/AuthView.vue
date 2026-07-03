<script setup>
import { useRouter } from 'vue-router'
import { computed, ref } from 'vue'
import LuiuNavbar from '@/components/layout/LuiuNavbar.vue'
import LoginForm from '@/components/auth/LoginForm.vue'
import RegisterForm from '@/components/auth/RegisterForm.vue'
import VerifyForm from '@/components/auth/VerifyForm.vue'
import ForgotPasswordForm from '@/components/auth/ForgotPasswordForm.vue'
import ResetPasswordForm from '@/components/auth/ResetPasswordForm.vue'

const props = defineProps({
  mode: { type: String, required: true },
})
const router = useRouter()
const activeField = ref('')
const authMap = {
  login: LoginForm,
  register: RegisterForm,
  verify: VerifyForm,
  forgot: ForgotPasswordForm,
  reset: ResetPasswordForm,
}

const handleFocusChange = (field) => {
  activeField.value = field
}

const handleModeSwitch = (targetMode) => {
  router.push({
    name: 'MemberAuth',
    params: {
      mode: targetMode,
    },
  })
}

const currentForm = computed(() => {
  const component = authMap[props.mode]

  if (!component) {
    router.replace({ name: 'MemberAuth', params: { mode: 'login' } })
    return LoginForm
  }

  return component
})
</script>

<template>
  <div class="auth-page-root d-flex flex-column min-vh-100">
    <header>
      <LuiuNavbar />
    </header>

    <main class="flex-grow-1 d-flex align-items-center justify-content-center bg-light">
      <div class="container-fluid">
        <div class="card auth-card shadow-sm border-1 overflow-hidden mx-auto">
          <div class="row">
            <!-- <div class="col-5">
              <button class="btn btn-primary">123</button>
            </div> -->

            <div class="col-12 d-flex">
              <div class="card-body p-5">
                <div class="auth-container">
                  <component :is="currentForm" />
                  <!-- <component
                :is="currentForm"
                @update-focus="handleFocusChange"
                @switch-mode="handleModeSwitch"
              /> -->
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<style lang="scss">
@import '@/assets/scss/pages/auth';
</style>
