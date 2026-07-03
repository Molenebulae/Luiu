<script setup>
import { ref, computed } from 'vue'
import { changePasswordApi } from '@/api/member'
import { luiuNotify, toast } from '@/utils/sweetAlert'
import { validatePassword, ValidateConfirmPassword } from '@/utils/validators'

// 宣告表單響應式變數
const form = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

// 表單提交載入狀態
const isSubmitting = ref(false)

// 利用計算屬性，動態向 validator 索取新密碼的強度錯誤訊息
const passwordError = computed(() => {
  if (!form.value.newPassword) return ''
  const result = validatePassword(form.value.newPassword)
  return result.isValid ? '' : result.message
})

// 利用計算屬性，動態拿取確認密碼的一致性錯誤訊息
const confirmPasswordError = computed(() => {
  if (!form.value.confirmPassword) return ''
  const result = ValidateConfirmPassword(form.value.newPassword, form.value.confirmPassword)
  return result.isValid ? '' : result.message
})

// 實作取消修改（重設表單並跳出確認彈窗）
const resetForm = async () => {
  // 如果所有欄位都是空的，代表使用者未曾輸入，直接清空即可，不需要干擾使用者
  if (!form.value.currentPassword && !form.value.newPassword && !form.value.confirmPassword) {
    clearFields()
    return
  }

  // 呼叫全域通用的表單放棄修改確認視窗
  const result = await luiuNotify.confirmCancel()
  if (result.isConfirmed) {
    clearFields()
  }
}

// 輔助方法：清空所有輸入框欄位
const clearFields = () => {
  form.value.currentPassword = ''
  form.value.newPassword = ''
  form.value.confirmPassword = ''
}

// 實作變更密碼表單提交處理
const handleUpdatePassword = async () => {
  // 前端第一道安全防線：再次嚴格校對新密碼強度
  const pwdCheck = validatePassword(form.value.newPassword)
  if (!pwdCheck.isValid) {
    toast(pwdCheck.message, 'error')
    return
  }

  // 前端第二道安全防線：雙重確認新密碼一致性
  const confirmCheck = ValidateConfirmPassword(form.value.newPassword, form.value.confirmPassword)
  if (!confirmCheck.isValid) {
    toast(confirmCheck.message, 'error')
    return
  }

  // 前端第三道安全防線：目前密碼不可為空
  if (!form.value.currentPassword) {
    toast('請輸入目前密碼', 'error')
    return
  }

  try {
    isSubmitting.value = true

    // 打包傳輸模型：依照規範，僅抽取舊密碼與新密碼，不傳送確認密碼給後端
    const submitData = {
      currentPassword: form.value.currentPassword,
      newPassword: form.value.newPassword,
    }

    // 更新密碼
    await changePasswordApi(submitData)

    // 假定非同步 API 執行成功後的邏輯處理
    toast('密碼已成功變更，請牢記新密碼', 'success')
    clearFields()
  } catch (error) {
    // 捕捉並攔截後端回傳的錯誤訊息（例如：目前密碼輸入錯誤、新舊密碼不准相同等商業邏輯阻擋）
    console.error('帳戶安全密碼更新失敗：', error?.message)
    toast(error?.message || '密碼變更失敗，請檢查輸入內容', 'error')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div>
    <div class="d-flex align-items-center mb-5 settings-title-section">
      <div class="title-indicator"></div>
      <h3 class="mb-0 fw-bold ms-3">帳戶安全設定</h3>
    </div>

    <form @submit.prevent="handleUpdatePassword">
      <div class="row g-4 max-width-container">
        <div class="col-12 col-md-8">
          <label class="form-label fw-bold text-secondary small">目前密碼</label>
          <input
            v-model="form.currentPassword"
            type="password"
            class="form-control custom-input"
            placeholder="請輸入您目前使用的密碼"
            autocomplete="current-password"
            required
          />
        </div>

        <div class="col-12 col-md-8">
          <label class="form-label fw-bold text-secondary small">新密碼</label>
          <input
            v-model="form.newPassword"
            type="password"
            class="form-control custom-input"
            :class="{ 'is-invalid': passwordError }"
            placeholder="請輸入新密碼（8-20碼，需包含英數）"
            autocomplete="new-password"
            required
          />
          <div v-if="passwordError" class="invalid-feedback fw-bold">
            {{ passwordError }}
          </div>
        </div>

        <div class="col-12 col-md-8">
          <label class="form-label fw-bold text-secondary small">確認新密碼</label>
          <input
            v-model="form.confirmPassword"
            type="password"
            class="form-control custom-input"
            :class="{ 'is-invalid': confirmPasswordError }"
            placeholder="請再次輸入新密碼以進行確認"
            autocomplete="new-password"
            required
          />
          <div v-if="confirmPasswordError" class="invalid-feedback fw-bold">
            {{ confirmPasswordError }}
          </div>
        </div>
      </div>

      <div class="mt-5 border-top pt-4 text-end action-button-group">
        <button
          type="button"
          class="luiu-btn-light me-3"
          :disabled="isSubmitting"
          @click="resetForm"
        >
          取消修改
        </button>
        <button
          type="submit"
          class="luiu-btn-primary"
          :disabled="isSubmitting || !!passwordError || !!confirmPasswordError"
        >
          <span
            v-if="isSubmitting"
            class="spinner-border spinner-border-sm me-2"
            role="status"
          ></span>
          變更密碼
        </button>
      </div>
    </form>
  </div>
</template>

<style scoped lang="scss">
.max-width-container {
  max-width: 720px;
}

// 統一表單標籤顏色
.form-label {
  color: $text-muted; // 使用全域定義的靜音色變數
}
.settings-title-section {
  .title-indicator {
    width: 4px;
    height: 24px;
    background-color: $primary;
    border-radius: 2px;
    margin-right: 12px;
  }
}

// 統一輸入框樣式
.custom-input {
  padding: 0.75rem 1rem;
  border-radius: 8px;
  border: 1.5px solid $input-border; // 調整為 1.5px 更穩定
  background-color: $white;
  color: $luiu-text-main;
  transition: $luiu-transition;

  &::placeholder {
    color: $text-muted;
  }

  &:focus {
    border-color: $primary;
    box-shadow: 0 0 0 0.25rem rgba($primary, 0.15);
    outline: none;
  }

  &.is-invalid {
    border-color: $danger;
    &:focus {
      box-shadow: 0 0 0 0.25rem rgba($danger, 0.15);
    }
  }
}

// 錯誤訊息樣式
.invalid-feedback {
  color: $danger;
  font-size: 0.85rem;
  margin-top: 0.5rem;
}
</style>
