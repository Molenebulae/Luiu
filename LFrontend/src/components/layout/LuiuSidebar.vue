<script setup>
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useUserStore } from '@/stores/user';
import { luiuNotify, toast } from '@/utils/sweetAlert';
import { logoutApi } from '@/api/auth';

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

// 動態產生「行程規劃」的路由物件，需要登入者的 userId
const planRoute = computed(() => {
  // 優先使用 userId (字串) 給 Plan API
  const uid = userStore.userInfo?.userId || userStore.userInfo?.memberId
  return uid ? { name: 'PlanList', params: { userId: uid } } : null
})

const menuItems = computed(() => [
  { name: '首頁', path: '/', icon: 'ri-home-4-line' },
  { name: '旅遊回憶', path: '/memory', icon: 'ri-camera-line' },
  // 行程規劃：需登入才會顯示
  ...(planRoute.value
    ? [{ name: '行程規劃', routeConfig: planRoute.value, icon: 'ri-calendar-todo-line' }]
    : []),
])

const navigateTo = (target) => {
  if (typeof target === 'string') {
    router.push(target)
  } else {
    router.push(target)
  }
}

// 登出功能
const handleLogout = async () => {
  const result = await luiuNotify.logout()

  // 沒有要登出
  if (!result.isConfirmed) return

  try {
    await logoutApi('/auth/logout')
  } catch (error) {
    // 防禦性處理：就算後端通知失敗（例如網路斷線或 Token 本來就過期了）
    // 前端依然要強行把本機資料清乾淨，避免畫面卡死
    console.error('後端通知登出失敗，執行強制本機登出:', error)
  } finally {
    // 清除登入紀錄
    userStore.logout()

    // 提示使用者
    toast('已安全登出', 'success')

    // 跳轉回登入頁面
    router.push({ name: 'MemberAuth', params: { mode: 'login' } })
  }
};
</script>

<template>
  <aside class="luiu-sidebar bg-white border-end d-flex flex-column flex-shrink-0 shadow-sm vh-100">
    <!-- Logo 區塊 -->
    <!-- Logo 區塊 -->
    <RouterLink class="navbar-brand p-0" :to="{ name: 'Home' }">
      <div class="logo-wrapper">
        <img src="@/assets/Images/logo.png" alt="Luiu Logo" class="logo-img" />
        <span class="logo-text">Luiu</span>
      </div>
    </RouterLink>

    <!-- 導航選單區塊 -->
    <nav class="sidebar-nav d-flex flex-column gap-2 flex-grow-1 px-3">
      <!-- 對於「行程規劃」：判斷路徑是否有 userId 前置跟當前已登入用戶匹配 -->
      <a v-for="item in menuItems" :key="item.name"
        class="nav-link d-flex align-items-center gap-3 px-3 py-3 rounded-4 fw-bold transition-all cursor-pointer"
        :class="(item.path &&
          (route.path === item.path ||
            (item.path !== '/' && route.path.startsWith(item.path)))) ||
          (item.routeConfig && route.name === item.routeConfig.name)
          ? 'bg-primary-subtle text-primary'
          : 'text-muted luiu-hover-link'
          " @click="navigateTo(item.routeConfig || item.path)">
        <i :class="[item.icon, 'fs-5']"></i>
        <span class="nav-text">{{ item.name }}</span>
      </a>

      <hr class="sidebar-divider my-3 border-secondary opacity-25" />

      <!-- 未登入才顯示登入按鈕 -->
      <a v-if="!userStore.isLoggedIn"
        class="nav-link d-flex align-items-center gap-3 px-3 py-3 rounded-4 fw-bold text-muted luiu-hover-link cursor-pointer"
        @click="navigateTo('/auth/login')">
        <i class="ri-user-line fs-5"></i> <span class="nav-text">登入 / 註冊</span>
      </a>
      <!-- 已登入則顯示會員中心 -->
      <a v-else
        class="nav-link d-flex align-items-center gap-3 px-3 py-3 rounded-4 fw-bold text-muted luiu-hover-link cursor-pointer"
        @click="
          navigateTo(
            `/member/profile/${userStore.userInfo?.memberId || userStore.userInfo?.userId}`,
          )
          ">
        <i class="ri-user-3-line fs-5"></i> <span class="nav-text">我的個人中心</span>
      </a>
    </nav>

    <!-- 已登入才顯示底部用戶區塊 -->
    <div v-if="userStore.isLoggedIn" class="sidebar-user-block mt-auto p-3">
      <div class="rounded-4 p-3 d-flex flex-column gap-2" style="background-color: #f0f4ff; border: 1px solid #dce5fb;">

        <!-- 用戶資訊區 -->
        <div class="d-flex align-items-center gap-2 mb-1">
          <div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0 overflow-hidden"
            style="width: 36px; height: 36px; background-color: #1a2b4c;">
            <img v-if="userStore.userInfo?.avatarUrl" :src="$img(userStore.userInfo.avatarUrl)"
              style="width: 100%; height: 100%; object-fit: cover;" alt="avatar">
            <i v-else class="ri-user-line text-white" style="font-size: 1rem;"></i>
          </div>
          <div class="overflow-hidden">
            <p class="mb-0 fw-bold small text-truncate" style="color: #1a2b4c; max-width: 160px;">
              {{ userStore.userInfo?.name || '旅人' }}
            </p>
          </div>
        </div>

        <hr class="my-1 border-secondary opacity-25">

        <!-- 設定按鈕 -->
        <button class="btn btn-sm w-100 text-start fw-bold transition-all"
          style="color: #1a2b4c; background: transparent; border: none; padding: 4px 4px;"
          @click="navigateTo('/member/settings')">
          <i class="ri-settings-3-line me-2"></i>帳號設定
        </button>

        <!-- 登出按鈕 -->
        <button class="btn btn-sm w-100 text-start fw-bold text-danger transition-all"
          style="background: transparent; border: none; padding: 4px 4px;" @click="handleLogout">
          <i class="ri-logout-box-r-line me-2"></i>登出
        </button>
      </div>
    </div>
  </aside>
</template>

<style lang="scss" scoped>
.navbar-brand {
  padding: 1.5rem 1rem !important;
  display: block;
  text-decoration: none;

  .logo-wrapper {
    display: flex;
    align-items: center;
    gap: 0.7rem; // 圖片與文字間距
    padding-left: 0.5rem;
  }

  .logo-img {
    height: 40px; // 強制設定高度，與 Navbar 視覺統一
    width: auto;
    object-fit: contain;
  }

  .logo-text {
    font-size: 1.75rem; // 放大字體，強調品牌識別
    font-weight: 800;
    color: $primary; // 原本的側邊欄主題色
    letter-spacing: -0.5px;
  }
}

.cursor-pointer {
  cursor: pointer;
}

.fw-black {
  font-weight: 900;
}

.transition-all {
  transition: all 0.2s ease-in-out;
}

.luiu-hover-link:hover {
  background-color: #f8f9fa;
  color: #1a2b4c !important;
}

.luiu-hover-blue:hover {
  color: #2b5fe8 !important;
}

.bg-primary-subtle {
  background-color: #e8f0fe !important;
}

/* --- RWD 響應式側邊欄/底部導航列 --- */

/* 預設：電腦版與平板版 */
.luiu-sidebar {
  width: 260px;
  z-index: 1000;
}

/* 手機版：寬度小於 768px 時變為底部導航列 */
@media (max-width: 767.98px) {
  .luiu-sidebar {
    position: fixed !important;
    bottom: 0;
    left: 0;
    width: 100vw !important;
    height: 65px !important;
    flex-direction: row !important;
    border-right: none !important;
    border-top: 1px solid #e0e0e0;
    z-index: 1050 !important;
    background-color: rgba(255, 255, 255, 0.95) !important;
    backdrop-filter: blur(10px);
    /* 毛玻璃質感 */
  }

  /* 隱藏不需要的區塊 */
  .sidebar-logo,
  .sidebar-user-block,
  .sidebar-divider {
    display: none !important;
  }

  /* 導航列變成橫向排開 */
  .sidebar-nav {
    flex-direction: row !important;
    width: 100%;
    padding: 0 10px !important;
    justify-content: space-around;
    align-items: center;
  }

  /* 選單按鈕變更形狀 */
  .nav-link {
    flex-direction: column !important;
    padding: 8px 16px !important;
    gap: 2px !important;
    border-radius: 12px !important;
    background-color: transparent !important;
    /* 移除藍色背景 */
  }

  /* 隱藏文字，只留 Icon */
  .nav-text {
    display: none !important;
  }

  .nav-link i {
    font-size: 1.6rem !important;
  }
}

/* 平板版：寬度在 768px ~ 991px 之間時，變成迷你側邊欄 (只顯示 Icon) */
@media (min-width: 768px) and (max-width: 991.98px) {
  .logo-text {
    display: none !important; // 平板只顯示 Logo 圖示
  }

  .luiu-sidebar {
    width: 80px !important;
    align-items: center;
  }

  .sidebar-logo h2 {
    display: none;
  }

  .sidebar-logo::after {
    content: 'L';
    font-weight: 900;
    font-size: 1.5rem;
    color: #1a2b4c;
  }

  .nav-text {
    display: none !important;
  }

  .nav-link {
    padding: 12px !important;
    justify-content: center;
  }

  .nav-link i {
    margin: 0 !important;
  }

  .sidebar-user-block {
    display: none !important;
    /* 平板隱藏下方設定區，保持畫面乾淨 */
  }
}
</style>
