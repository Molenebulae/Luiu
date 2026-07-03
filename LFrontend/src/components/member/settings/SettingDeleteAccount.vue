<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { luiuNotify, toast } from '@/utils/sweetAlert'
import { deleteAccountApi } from '@/api/member'

const router = useRouter()
const userStore = useUserStore()

// 防禦性輸入變數
const confirmText = ref('')
const isSubmitting = ref(false)

// 強制核對字串：使用者必須一字不差地輸入「確認刪除」，按鈕才允許點擊
const isUnlockDisabled = computed(() => {
  return confirmText.value !== '確認刪除'
})

// 實作完全刪除帳號操作
const handleDeleteAccount = async () => {
  if (isUnlockDisabled.value) return

  // 呼叫全域通用的高風險刪除確認框（會顯示危險紅色樣式）
  const result = await luiuNotify.confirmDelete('確定要刪除帳號嗎？', '注意！此動作永久無法還原！')

  if (result.isConfirmed) {
    try {
      isSubmitting.value = true
      await deleteAccountApi()

      // 執行清理全域狀態機制（清除 Token 與使用者快取資訊）
      userStore.clearUserData()

      // 對齊狀態碼 200 後續處理，彈出成功提示並強制踢回登入頁或首頁
      toast('您的帳號已刪除', 'success')
      router.push({
        name: 'MemberAuth',
        params: {
          mode: 'login',
        },
      })
    } catch (error) {
      console.error('刪除帳號執行失敗：', error?.message)
      toast(error?.message || '刪除失敗，請稍後再試', 'error')
    } finally {
      isSubmitting.value = false
    }
  }
}
</script>

<template>
  <div>
    <div class="d-flex align-items-center mb-5 settings-title-section">
      <div class="title-indicator bg-danger"></div>
      <h3 class="mb-0 fw-bold ms-3 text-danger">危險區域：刪除帳號</h3>
    </div>

    <div
      class="card border-danger-subtle bg-danger-subtle-light mb-5 rounded-3 max-width-container"
    >
      <div class="card-body p-4">
        <h5 class="fw-bold text-danger mb-2">請務必詳閱以下帳號註銷條款：</h5>
        <ul class="text-secondary small mb-0 ps-3 squad-list">
          <li class="mb-1">帳號註銷後，您的帳號將立即關閉，您將無法再登入系統或存取任何服務。</li>
          <li class="mb-0">此操作一旦完成，您的帳號狀態將變更為永久註銷，無法復原。</li>
        </ul>
      </div>
    </div>

    <div class="row g-4 max-width-container">
      <div class="col-12 col-md-8">
        <label class="form-label fw-bold text-secondary small mb-2">
          請在下方輸入 <span class="text-danger fw-black">確認刪除</span> 以解鎖此操作：
        </label>
        <input
          v-model="confirmText"
          type="text"
          class="form-control custom-input"
          placeholder="請手動輸入：確認刪除"
          :disabled="isSubmitting"
          required
        />
      </div>
    </div>

    <div class="mt-5 border-top pt-4 text-end action-button-group">
      <button
        type="button"
        class="luiu-btn-danger"
        :disabled="isUnlockDisabled || isSubmitting"
        @click="handleDeleteAccount"
      >
        <span
          v-if="isSubmitting"
          class="spinner-border spinner-border-sm me-2"
          role="status"
        ></span>
        永久完全刪除帳號
      </button>
    </div>
  </div>
</template>

<!-- <style scoped>
.max-width-container {
  max-width: 720px;
}

/* 輕量危險警告背景色 */
.bg-danger-subtle-light {
  background-color: rgba(220, 53, 69, 0.03);
}

.squad-list li {
  line-height: 1.6;
}

/* 整合專案自訂輸入框焦點樣式（危險區改為紅色焦點保護） */
.custom-input {
  padding: 0.75rem 1rem;
  border-radius: 8px;
  border: 1px solid #dee2e6;
  transition:
    border-color 0.2s,
    box-shadow 0.2s;
}

.custom-input:focus {
  border-color: #dc3545; /* 核心危險色 */
  box-shadow: 0 0 0 0.25rem rgba(220, 53, 69, 0.15);
  outline: none;
}
</style> -->
<style scoped lang="scss">
.max-width-container {
  max-width: 720px;
}

.bg-danger-subtle-light {
  background-color: rgba($danger, 0.03);
}

.squad-list li {
  line-height: 1.6;
}

.settings-title-section {
  .title-indicator {
    width: 4px;
    height: 24px;
    background-color: $danger;
    border-radius: 2px;
    margin-right: 12px;
  }
}

.custom-input {
  padding: 0.75rem 1rem;
  border-radius: 8px;
  border: 1px solid $input-border;
  transition: $luiu-transition;

  &:focus {
    border-color: $danger;
    box-shadow: 0 0 0 0.25rem rgba($danger, 0.15);
    outline: none;
  }
}
</style>
