<script setup>
// 本分頁目前後端暫無實作欄位，採純視覺 Coming Soon 遮罩呈現，無需任何響應式變數與 API 呼叫
</script>

<template>
  <div class="notifications-tab-container">
    <div class="d-flex align-items-center mb-5 settings-title-section">
      <div class="title-indicator"></div>
      <h3 class="mb-0 fw-bold ms-3">通知推播設定</h3>
    </div>

    <div
      class="coming-soon-wrapper text-center d-flex flex-column align-items-center justify-content-center"
    >
      <div class="icon-placeholder mb-4">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="64"
          height="64"
          fill="currentColor"
          class="text-muted-light"
          viewBox="0 0 16 16"
        >
          <path
            d="M8 16a2 2 0 0 0 2-2H6a2 2 0 0 0 2 2M8 1.918l-.797.161A4 4 0 0 0 4 6c0 .628-.134 2.197-.459 3.742-.16.767-.376 1.566-.663 2.258h10.244c-.287-.692-.502-1.49-.663-2.258C12.134 8.197 12 6.628 12 6a4 4 0 0 0-3.203-3.92zM14.22 12c.223.447.481.801.78 1H1c.299-.199.557-.553.78-1C2.68 10.2 3 6.88 3 6c0-2.42 1.72-4.44 4.005-4.901a1 1 0 1 1 1.99 0A5 5 0 0 1 13 6c0 .88.32 4.2 1.22 6"
          />
        </svg>
      </div>

      <h4 class="fw-bold text-dark mb-2">功能即將推出</h4>
      <p class="text-secondary text-center max-width-text mb-0">
        個人化通知與團隊專案變更推播功能正在全力開發中。<br />
        未來上線後，您將可以自由控制系統公告與音訊功能的推播狀態。
      </p>
    </div>
  </div>
</template>

<!-- <style scoped>
.notifications-tab-container {
  min-height: 400px;
}

/* 遮罩置中與微調樣式 */
.coming-soon-wrapper {
  padding: 5rem 2rem;
  background-color: #fafafa;
  border-radius: 12px;
  border: 1px dashed #dee2e6;
}

/* 低強度灰色圖示 */
.text-muted-light {
  color: #ced4da;
}

.max-width-text {
  max-width: 420px;
  line-height: 1.6;
  font-size: 0.95rem;
}
</style> -->
<style scoped lang="scss">
.notifications-tab-container {
  min-height: 400px;
}

// 標題樣式保持與其他頁面一致
.settings-title-section {
  .title-indicator {
    width: 4px;
    height: 24px;
    background-color: $primary;
    border-radius: 2px;
    margin-right: 12px;
  }
}

.coming-soon-wrapper {
  padding: 5rem 2rem;
  background-color: $luiu-bg-light;
  border-radius: $luiu-card-radius;
  border: 1px dashed $input-border;
}

// 確保文字顏色與全站主體文字統一
h4.text-dark {
  color: $luiu-text-main !important;
}

.text-muted-light {
  color: $input-border;
}

.max-width-text {
  max-width: 420px;
  line-height: 1.6;
  font-size: 0.95rem;
  color: $text-muted;
}
</style>
