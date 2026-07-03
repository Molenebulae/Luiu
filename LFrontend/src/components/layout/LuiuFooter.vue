<script setup>
import { computed } from 'vue'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()

const profileTarget = computed(() => {
  return userStore.isLoggedIn
    ? { name: 'MemberProfile', params: { userId: userStore.userInfo.userId } }
    : { name: 'MemberAuth', params: { mode: 'login' } }
})

const PlansTarget = computed(() => {
  return userStore.isLoggedIn
    ? { name: 'PlanList', params: { userId: userStore.userInfo.userId } }
    : { name: 'MemberAuth', params: { mode: 'login' } }
})
</script>

<template>
  <div class="container-fluid footer py-5">
    <div class="container py-4">
      <div class="row g-5">
        <div class="col-md-6 col-lg-4">
          <div class="footer-item d-flex flex-column gap-2">
            <h3 class="mb-3 footer-brand fw-bold tracking-wide">Luiu</h3>
            <p class="text-muted-dark footer-desc mb-3">
              專為旅人打造的智慧行程規劃平台。輕鬆探索官方精選路線、記錄專屬的旅遊回憶，讓下一段旅程化繁為簡。
            </p>
            <div class="contact-info d-flex align-items-center text-muted-dark mb-2">
              <i class="bi bi-envelope-fill me-2 text-primary"></i>
              <span>support@luiu.com</span>
            </div>

            <div class="d-flex align-items-center mt-2">
              <a class="btn-square btn rounded-circle me-2" href="#" title="Facebook">
                <i class="bi bi-facebook"></i>
              </a>
              <a class="btn-square btn rounded-circle me-2" href="#" title="Instagram">
                <i class="bi bi-instagram"></i>
              </a>
              <a class="btn-square btn rounded-circle" href="#" title="Youtube">
                <i class="bi bi-youtube"></i>
              </a>
            </div>
          </div>
        </div>

        <div class="col-md-6 col-lg-2 offset-lg-1">
          <div class="footer-item d-flex flex-column footer-links">
            <h5 class="mb-4 footer-title fw-bold">探索行程</h5>
            <RouterLink :to="{ name: 'Home' }"
              ><i class="bi bi-chevron-right me-1"></i>首頁精選</RouterLink
            >
            <RouterLink :to="{ name: 'Home' }"
              ><i class="bi bi-chevron-right me-1"></i>推薦行程</RouterLink
            >
            <RouterLink :to="{ name: 'Home' }"
              ><i class="bi bi-chevron-right me-1"></i>旅人回憶</RouterLink
            >
          </div>
        </div>

        <div class="col-md-6 col-lg-2">
          <div class="footer-item d-flex flex-column footer-links">
            <h5 class="mb-4 footer-title fw-bold">旅人服務</h5>
            <RouterLink :to="profileTarget"
              ><i class="bi bi-chevron-right me-1"></i>個人檔案</RouterLink
            >
            <RouterLink :to="PlansTarget"
              ><i class="bi bi-chevron-right me-1"></i>我的行程</RouterLink
            >
            <RouterLink :to="{ name: 'MemberSetting' }"
              ><i class="bi bi-chevron-right me-1"></i>帳號設定</RouterLink
            >
          </div>
        </div>

        <div class="col-md-6 col-lg-3">
          <div class="footer-item d-flex flex-column gap-2">
            <h5 class="mb-4 footer-title fw-bold">專案資訊</h5>
            <div class="contact-info d-flex align-items-start text-muted-dark">
              <i class="bi bi-building me-2 mt-1 text-primary"></i>
              <span>指導單位：iSpan 財團法人資訊工業策進會</span>
            </div>
            <div class="contact-info d-flex align-items-start text-muted-dark mt-1">
              <i class="bi bi-people-fill me-2 mt-1 text-primary"></i>
              <div>
                <span class="d-block mb-1">開發團隊：Luiu 專題小組 (3人團隊)</span>
                <span class="text-secondary-muted small">Vue 3 + .NET 10 核心建構</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <hr class="border-light-gray my-4" />

      <div class="row">
        <div class="col-12 text-center">
          <span class="small text-secondary-muted">
            &copy; 2026 Luiu Project. 本網站僅供學術成果發表與技術交流使用。
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
// 套用變數
.footer {
  background-color: $footer-bg;
}

.footer-brand {
  color: $footer-title-color;
}

.footer-title {
  color: $footer-title-color;
}

.text-primary {
  color: $primary !important; // 使用全域橘色變數
}

.text-muted-dark {
  color: $footer-text-main;
}

.text-secondary-muted {
  color: $footer-text-muted;
}

.tracking-wide {
  letter-spacing: 1.5px;
}

.footer-desc {
  font-size: 14px;
  line-height: 1.6;
}

.contact-info {
  font-size: 14px;
}

// 導覽連結設定
.footer-links {
  :deep(a) {
    color: $footer-text-main;
    text-decoration: none;
    margin-bottom: 12px;
    font-size: 14px;
    transition:
      color 0.2s ease,
      padding-left 0.2s ease;
    display: inline-block;

    &:hover {
      color: $primary !important; // Hover 時觸發專案主色橘色
      padding-left: 4px;
    }
  }
}

// 社群按鈕設定
.btn-square {
  width: 34px;
  height: 34px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border-color: transparent;
  background-color: $social-btn-bg;
  color: $social-btn-icon;
  transition: all 0.2s ease;

  &:hover {
    background-color: $primary !important; // Hover 時背景變為主色橘色
    color: #ffffff !important; // 圖示變回白色
    transform: translateY(-2px);
  }

  i {
    font-size: 16px;
  }
}

// 分隔線設定
.border-light-gray {
  border-color: $footer-border-color !important;
}
</style>
