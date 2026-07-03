<script setup>
import LuiuAvator from '@/components/base/LuiuAvator.vue'
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { luiuNotify, toast } from '@/utils/sweetAlert'
import { logoutApi } from '@/api/auth'

const router = useRouter()
const userStore = useUserStore()
const isDropdownOpen = ref(false)

const loginLink = { name: 'MemberAuth', params: { mode: 'login' } }
const currentUserId = computed(() => userStore.userInfo?.userId)
const memberProfileLink = computed(() =>
  currentUserId.value
    ? { name: 'MemberProfile', params: { userId: currentUserId.value } }
    : loginLink,
)
const planListLink = computed(() =>
  currentUserId.value ? { name: 'PlanList', params: { userId: currentUserId.value } } : loginLink,
)

const handleLogout = async () => {
  const result = await luiuNotify.logout()
  if (!result.isConfirmed) return // 沒有要登出

  try {
    await logoutApi('/auth/logout')
  } catch (error) {
    // 防禦性處理：就算後端通知失敗（例如網路斷線或 Token 本來就過期了）
    // 前端依然要強行把本機資料清乾淨，避免畫面卡死
    console.error('後端通知登出失敗，執行強制本機登出:', error)
  } finally {
    // 清除登入紀錄
    userStore.logout()
    isDropdownOpen.value = false
    toast('已安全登出', 'success') // 提示使用者
    router.push({ name: 'MemberAuth', params: { mode: 'login' } }) // 跳轉回登入頁面
  }
}

const toggleDropdown = () => {
  isDropdownOpen.value = !isDropdownOpen.value
}

// 點擊選單外部時自動關閉選单
const closeDropdown = (e) => {
  if (!e.target.closest('.nav-item.dropdown')) {
    isDropdownOpen.value = false
  }
}

onMounted(() => {
  window.addEventListener('click', closeDropdown)
})

onUnmounted(() => {
  window.removeEventListener('click', closeDropdown)
})
</script>

<template>
  <nav class="navbar navbar-expand-lg navbar-light fixed-top shadow-sm px-4 px-lg-5 py-3 py-lg-0">
    <RouterLink class="navbar-brand p-0" :to="{ name: 'Home' }">
      <div class="logo-wrapper">
        <img src="@/assets/Images/logo.png" alt="Luiu Logo" class="logo-img" />
        <span class="logo-text">Luiu</span>
      </div>
    </RouterLink>

    <button
      class="navbar-toggler"
      type="button"
      data-bs-toggle="collapse"
      data-bs-target="#navbarCollapse"
    >
      <span class="fa fa-bars"></span>
    </button>
    <div id="navbarCollapse" class="collapse navbar-collapse">
      <div class="navbar-nav ms-auto py-0">
        <RouterLink class="nav-item nav-link" :to="planListLink">行程規劃</RouterLink>
        <RouterLink class="nav-item nav-link" :to="{ name: 'Memory' }">回憶紀錄</RouterLink>
      </div>

      <div>
        <div v-if="userStore.isLoggedIn" class="nav-item dropdown">
          <a href="#" class="avatar-link" @click.prevent="toggleDropdown">
            <LuiuAvator :avatar="$img(userStore.userInfo?.avatarUrl || '')" size="sm" />
          </a>

          <div
            class="dropdown-menu dropdown-menu-end shadow-sm border-0 mt-2"
            :class="{ show: isDropdownOpen }"
          >
            <div class="px-3 py-2 text-muted small border-bottom">
              {{ userStore.userInfo?.displayName || '會員' }}
            </div>

            <RouterLink
              :to="memberProfileLink"
              class="dropdown-item py-2"
              @click="isDropdownOpen = false"
            >
              <i class="bi bi-person me-2"></i>個人頁面
            </RouterLink>

            <RouterLink
              :to="{ name: 'MemberSetting' }"
              class="dropdown-item py-2"
              @click="isDropdownOpen = false"
            >
              <i class="bi bi-gear me-2"></i>帳戶設定
            </RouterLink>

            <div class="dropdown-divider"></div>

            <a href="#" class="dropdown-item py-2 text-danger" @click.prevent="handleLogout">
              <i class="bi bi-box-arrow-right me-2"></i>登出
            </a>
          </div>
        </div>
        <div v-else class="auth-buttons">
          <RouterLink
            :to="{ name: 'MemberAuth', params: { mode: 'login' } }"
            class="luiu-btn-outline-primary luiu-btn-sm"
            >登入</RouterLink
          >
        </div>
      </div>
    </div>
  </nav>
</template>

<style lang="scss" scoped>
@import '@/assets/scss/layout/navbar';
</style>
