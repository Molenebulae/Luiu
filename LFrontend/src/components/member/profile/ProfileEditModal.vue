<script setup>
import { ref, reactive } from 'vue'
import LuiuAvator from '@/components/base/LuiuAvator.vue'

const props = defineProps({
  initialData: {
    type: Object,
    default: () => ({
      avatarUrl: '',
      name: '',
      bio: '',
      socialLinks: {
        // 結構化
        google: '',
        facebook: '',
      },
    }),
  },
})

const emit = defineEmits(['close', 'save'])

// 分離 Tab 狀態
const activeTab = ref('basic')

// 建立響應式表單資料
const form = reactive({
  userId: props.initialData?.userId || '',
  avatarUrl: props.initialData?.avatarUrl || undefined,
  name: props.initialData?.name || '',
  bio: props.initialData?.bio || '',
  socialLinks: {
    google: props.initialData?.socialLinks?.google || '',
    facebook: props.initialData?.socialLinks?.facebook || '',
  },
})

// TODO: 檢查網址
const socialPlatforms = [
  { key: 'google', label: 'Google', icon: 'bi-google', placeholder: 'Google 連結代碼' },
  { key: 'facebook', label: 'Facebook', icon: 'bi-facebook', placeholder: 'Facebook 連結' },
  // 以後要加平台，只要在這裡加一行就好
  // { key: 'github', label: 'GitHub', icon: 'bi-github', placeholder: 'GitHub 連結' },
]

// 處理頭像上傳預覽
const onFileChange = (e) => {
  const file = e.target.files[0]
  if (file) {
    form.avatarUrl = URL.createObjectURL(file)
    form.rawFile = file
  }
}

const handleSave = () => {
  emit('save', { ...form })
}
</script>

<template>
  <div class="edit-modal-container">
    <div class="modal fade show d-block" tabindex="-1">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 modal-custom-shadow">
          <div class="modal-header border-0 flex-column align-items-start pb-0">
            <div class="d-flex justify-content-between w-100 mb-2">
              <h5 class="fw-bold m-0">編輯個人檔案</h5>
              <button type="button" class="btn-close shadow-none" @click="$emit('close')"></button>
            </div>

            <ul class="nav nav-underline">
              <li class="nav-item">
                <button
                  class="nav-link"
                  :class="{ active: activeTab === 'basic' }"
                  @click="activeTab = 'basic'"
                >
                  基本資料
                </button>
              </li>
              <li class="nav-item">
                <button
                  class="nav-link"
                  :class="{ active: activeTab === 'social' }"
                  @click="activeTab = 'social'"
                >
                  社群連結
                </button>
              </li>
            </ul>
          </div>

          <div class="modal-body p-4">
            <div v-if="activeTab === 'basic'">
              <div class="d-flex flex-column align-items-center mb-4">
                <div class="position-relative avatar-edit-group">
                  <LuiuAvator :avatar="$img(form.avatarUrl)" size="xl" />
                  <label
                    class="btn btn-light position-absolute bottom-0 end-0 rounded-circle shadow-sm border upload-btn"
                  >
                    <i class="bi bi-camera-fill"></i>
                    <input type="file" hidden accept="image/*" @change="onFileChange" />
                  </label>
                </div>
                <div class="small text-muted mt-2">更換大頭照</div>
              </div>

              <div class="mb-3">
                <label class="form-label small fw-bold text-secondary">使用者 ID</label>
                <input
                  v-model="form.userId"
                  type="text"
                  class="form-control shadow-none"
                  placeholder="輸入您想要改的id"
                />
              </div>

              <div class="mb-3">
                <label class="form-label small fw-bold text-secondary">名稱</label>
                <input
                  v-model="form.name"
                  type="text"
                  class="form-control shadow-none"
                  placeholder="輸入您的名稱"
                />
              </div>

              <div class="mb-3">
                <label class="form-label small fw-bold text-secondary">個人簡介</label>
                <textarea
                  v-model="form.bio"
                  class="form-control shadow-none"
                  rows="4"
                  placeholder="介紹一下你自己..."
                ></textarea>
              </div>
            </div>

            <div v-if="activeTab === 'social'">
              <div v-for="platform in socialPlatforms" :key="platform.key" class="mb-3">
                <label class="form-label small fw-bold text-secondary">{{ platform.label }}</label>
                <div class="input-group">
                  <span class="input-group-text bg-transparent border-end-0">
                    <i :class="['bi', platform.icon]"></i>
                  </span>
                  <input
                    v-model="form.socialLinks[platform.key]"
                    type="text"
                    class="form-control border-start-0 shadow-none"
                    :placeholder="platform.placeholder"
                  />
                </div>
              </div>
            </div>
          </div>

          <div class="modal-footer border-0 pt-0">
            <button
              type="button"
              class="luiu-btn-outline-primary px-4 me-2"
              @click="$emit('close')"
            >
              取消
            </button>
            <button type="button" class="luiu-btn-primary px-4" @click="handleSave">
              儲存設定
            </button>
          </div>
        </div>
      </div>
    </div>
    <div class="modal-backdrop fade show"></div>
  </div>
</template>

<style lang="scss" scoped>
.edit-modal-container {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  // 降至 1040 確保導覽列 (1050) 可點擊
  z-index: 1040;

  .modal-backdrop {
    z-index: -1; // 相對於 container 的背景
  }
}

.modal-custom-shadow {
  box-shadow: 0 1rem 3rem rgba($dark, 0.175);
  border-radius: $luiu-card-radius;
}

.avatar-edit-group {
  width: 120px;
  height: 120px;

  .upload-btn {
    width: 36px;
    height: 36px;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: $white;
    color: $primary;
    border-color: $input-border;

    &:hover {
      background-color: $luiu-bg-light;
    }
  }
}

.nav-link {
  color: $text-muted;
  font-weight: 500;
  padding: 0.8rem 1.2rem;
  border-bottom: 2px solid transparent;
  transition: $luiu-transition;

  &.active {
    color: $primary !important;
    border-bottom-color: $primary;
  }
}

.form-control {
  border-radius: 10px;
  border: 1px solid $input-border;
  padding: 0.6rem 1rem;

  &:focus {
    border-color: $primary;
    box-shadow: 0 0 0 0.2rem rgba($primary, 0.1);
  }
}

.input-group-text {
  border-radius: 10px 0 0 10px;
  color: $text-muted;
  border-color: $input-border;
}
</style>
