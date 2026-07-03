<script setup>
import { ref } from 'vue'
import { useUserStore } from '@/stores/user'
import { updateMemberSettingsApi } from '@/api/member'
import { uploadFileApi } from '@/api/file'
import { luiuNotify } from '@/utils/sweetAlert'
import LuiuAvator from '@/components/base/LuiuAvator.vue'
import LuiuDatePicker from '@/components/base/LuiuDatePicker.vue'

const userStore = useUserStore()

const fileInput = ref(null)
const selectedFile = ref(null) // 暫存待上傳的原始檔案實體
const avatarPreview = ref(userStore.userInfo?.avatarUrl || '') // 控制頭像預覽畫面的網址

// 綁定表單變數
const user = ref({
  displayName: userStore.userInfo?.name || '',
  email: userStore.userInfo?.email || '',
  phone: userStore.userInfo?.phone || '',
  birthday: userStore.userInfo?.birthday || '',
  gender: userStore.userInfo?.gender ?? 0,
})

const today = new Date()
today.setHours(23, 59, 59, 999)
const birthdayOptions = {
  maxDate: today,
}

// 當使用者選取新圖片時觸發（不立刻呼叫 API，只做本機預覽與暫存）
const handleFieldUpload = (event) => {
  const file = event.target.files[0]
  if (!file) return

  selectedFile.value = file
  console.log(selectedFile)
  // 建立暫時性的 Blob 網址，讓畫面能立刻看到更換後的圖片
  avatarPreview.value = URL.createObjectURL(file)
}

const resetForm = async () => {
  // 跳出確認視窗，防範使用者誤觸
  const result = await luiuNotify.confirmCancel()

  // 不取消
  if (!result.isConfirmed) return

  // 還原資料
  user.value = {
    displayName: userStore.userInfo?.name || '',
    email: userStore.userInfo?.email || '',
    phone: userStore.userInfo?.phone || '',
    birthday: userStore.userInfo?.birthday || '',
    gender: userStore.userInfo?.gender ?? 0,
  }

  // 同步還原大頭貼預覽畫面為原本的圖片
  avatarPreview.value = userStore.userInfo?.avatarUrl || ''

  // 清空暫存的待上傳檔案
  selectedFile.value = null
  if (fileInput.value) {
    fileInput.value.value = '' // 清空 HTML input file 的暫存值
  }
}

// 實作儲存變更點擊事件
const saveSettings = async () => {
  let formattedBirthday = null

  // 核心防禦：如果使用者有填寫生日，必須將斜線 "2026/05/12" 取代為橫線 "2026-05-12"
  if (user.value.birthday && user.value.birthday.trim() !== '') {
    formattedBirthday = user.value.birthday.replace(/\//g, '-')
  }
  try {
    let finalAvatarUrl = userStore.userInfo?.avatarUrl || ''

    // 流程控制：如果使用者有選擇新圖片，必須攔截並優先執行圖片上傳 API
    if (selectedFile.value) {
      const formData = new FormData()
      formData.append('file', selectedFile.value)
      formData.append('type', 'avatar')

      // 呼叫上傳 API，依據你的攔截器規範，成功直接拿到回傳的路徑字串
      const fileRes = await uploadFileApi(formData)
      finalAvatarUrl = fileRes.data
      console.log('設定頁面大頭貼上傳成功，新路徑為：', finalAvatarUrl)
    }

    // 封裝最終要送給隱私設定 API 的資料，將新圖片路徑補進去
    const submitData = {
      name: user.value.displayName,
      phone: user.value.phone,
      birthday: formattedBirthday,
      gender: user.value.gender,
      avatarUrl: finalAvatarUrl,
    }

    // 呼叫設定更新 API
    const response = await updateMemberSettingsApi(submitData)

    // 依據 API 返回結果整包覆蓋更新前端表單變數
    user.value = {
      displayName: response.data.name,
      email: user.value.email,
      phone: response.data.phone || '',
      birthday: response.data.birthday || '',
      gender: response.data.gender ?? 0,
    }

    // 同步更新全域 Store 狀態，確保導覽列的大頭貼與暱稱同步變更
    userStore.updateUserInfo(response.data)

    // 清空暫存檔案
    selectedFile.value = null

    luiuNotify.success('更新成功')
  } catch (error) {
    console.error('執行帳戶設定更新時發生錯誤：', error?.message)
  }
}
</script>

<template>
  <div>
    <div class="d-flex align-items-center mb-5 settings-title-section">
      <div class="title-indicator"></div>
      <h3 class="mb-0 fw-bold ms-3">帳戶資料編輯</h3>
    </div>

    <form @submit.prevent="saveSettings">
      <div class="mb-5 d-flex align-items-center p-4 avatar-edit-wrapper shadow-sm rounded-3">
        <div class="position-relative">
          <LuiuAvator size="xl" :avatar="$img(avatarPreview)" />
          <input
            ref="fileInput"
            type="file"
            class="d-none"
            accept="image/*"
            @change="handleFieldUpload"
          />
          <button
            type="button"
            class="btn btn-primary btn-sm rounded-circle position-absolute bottom-0 end-0 p-2 btn-camera"
            @click="fileInput.click()"
          >
            <i class="bi bi-camera-fill text-white"></i>
          </button>
        </div>
        <div class="ms-4">
          <h6 class="mb-1 fw-bold">你的頭像</h6>
          <p class="mb-0 text-muted small">建議尺寸 400x400 px，支援 JPG, PNG</p>
        </div>
      </div>

      <div class="row g-4">
        <div class="col-md-6">
          <label class="form-label fw-bold text-secondary small">顯示名稱</label>
          <input v-model="user.displayName" type="text" class="form-control custom-input" />
        </div>

        <div class="col-md-6">
          <label class="form-label fw-bold text-secondary small">電子信箱</label>
          <input v-model="user.email" type="email" class="form-control custom-input" disabled />
        </div>

        <div class="col-md-6">
          <label class="form-label fw-bold text-secondary small">電話號碼</label>
          <input v-model="user.phone" type="tel" class="form-control custom-input" maxLength="10" />
        </div>

        <div class="col-md-6">
          <label class="form-label fw-bold text-secondary small">出生日期</label>
          <LuiuDatePicker
            v-model="user.birthday"
            placeholder="請選擇您的出生日期"
            :options="birthdayOptions"
          />
        </div>

        <div class="col-md-6">
          <label class="form-label fw-bold text-secondary small">性別</label>
          <select v-model="user.gender" class="form-select custom-input">
            <option :value="0">不透露</option>
            <option :value="1">男</option>
            <option :value="2">女</option>
          </select>
        </div>
      </div>

      <div class="mt-5 border-top pt-4 text-end action-button-group">
        <button type="button" class="luiu-btn-light me-3" @click="resetForm">取消修改</button>

        <button type="submit" class="luiu-btn-primary">儲存變更</button>
      </div>
    </form>
  </div>
</template>

<style scoped lang="scss">
.settings-container {
  .settings-title-section {
    .title-indicator {
      width: 4px;
      height: 24px;
      background-color: $primary;
      border-radius: 2px;
      margin-right: 12px;
    }
  }

  .custom-input {
    // 僅保留表單特有樣式，其餘按鈕樣式由 _luiu-buttons.scss 處理
    border: 1.5px solid $input-border;
    border-radius: 8px;
    padding: 0.6rem 1rem;
    &:focus {
      border-color: $primary;
      box-shadow: 0 0 0 0.25rem rgba($primary, 0.15);
    }
  }
}
</style>
