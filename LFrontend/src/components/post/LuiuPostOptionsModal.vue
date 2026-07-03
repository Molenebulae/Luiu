<script setup>
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import Swal from 'sweetalert2'
import { updateRecommendationApi } from '@/api/admin'
import { followUser, unfollowUser, checkFollowStatus, deleteMemory } from '@/api/memory'
import { useUserStore } from '@/stores/user'

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  post: {
    type: Object,
    default: null,
  },
})

const emit = defineEmits(['update:modelValue', 'edit', 'delete', 'follow-change'])
const router = useRouter()

const userStore = useUserStore()
const currentUserId = computed(() => userStore.userInfo?.userId)
const isAdmin = computed(() => {
  const role = userStore.userInfo?.role
  return role === 'OfficialManager' || (Array.isArray(role) && role.includes('OfficialManager'))
})

// 判斷是否為當前使用者發的貼文
const isOwnPost = computed(() => {
  const postAuthorId = props.post?.userId || props.post?.author?.id
  return currentUserId.value && String(currentUserId.value) === String(postAuthorId)
})

// 追蹤狀態
const isFollowing = ref(false)
const isLoadingFollow = ref(false)

const fetchFollowStatus = async () => {
  const targetUserId = props.post?.userId || props.post?.author?.id
  if (!targetUserId || isOwnPost.value) return

  try {
    isLoadingFollow.value = true
    const response = await checkFollowStatus(targetUserId)
    if (response && response.success) {
      isFollowing.value = response.data
    }
  } catch (error) {
    console.error('查詢追蹤狀態失敗:', error)
  } finally {
    isLoadingFollow.value = false
  }
}

// 當開啟彈窗時，如果是他人的貼文則去撈取追蹤狀態
watch(
  () => props.modelValue,
  (newVal) => {
    if (newVal) {
      fetchFollowStatus()
    }
  },
)

// 關閉彈窗
const closeModal = () => {
  emit('update:modelValue', false)
}

// 1. 檢舉 (Fake API)
const handleReport = () => {
  closeModal()
  Swal.fire({
    icon: 'success',
    title: '感謝您的回報',
    text: '我們會盡快審查此貼文！',
    confirmButtonColor: '#1a2b4c',
    confirmButtonText: '確定',
    background: '#fff',
    customClass: { popup: 'rounded-4' },
  })
}

// 2. 切換追蹤狀態 (真實 API)
const handleFollowToggle = async () => {
  closeModal()
  const targetUserId = props.post?.userId || props.post?.author?.id
  if (!targetUserId) {
    Swal.fire({
      toast: true,
      position: 'bottom',
      icon: 'success',
      title: '已變更追蹤狀態 (模擬成功)',
      showConfirmButton: false,
      timer: 2000,
      background: '#333',
      color: '#fff',
      customClass: { popup: 'rounded-pill' },
    })
    return
  }

  const originalState = isFollowing.value
  try {
    let response
    if (originalState) {
      response = await unfollowUser(targetUserId)
    } else {
      response = await followUser(targetUserId)
    }

    if (response && response.success) {
      Swal.fire({
        toast: true,
        position: 'bottom',
        icon: 'success',
        title: originalState ? '已成功取消追蹤該用戶' : '已成功追蹤該用戶',
        showConfirmButton: false,
        timer: 2000,
        background: '#333',
        color: '#fff',
        customClass: { popup: 'rounded-pill' },
      })
      emit('follow-change', { userId: targetUserId, isFollowing: !originalState })
    } else {
      throw new Error(response?.message || '操作失敗')
    }
  } catch (error) {
    console.error('追蹤狀態切換失敗:', error)
    Swal.fire({
      icon: 'error',
      title: '操作失敗',
      text: '無法變更追蹤狀態，請稍後再試。',
      confirmButtonColor: '#1a2b4c',
    })
  }
}

// 3. 強力推薦 (Boost)
const handleBoost = async () => {
  closeModal()

  const memoryId = props.post?.id

  if (!memoryId) {
    Swal.fire({
      title: '推薦失敗',
      text: '無法取得貼文 ID。',
      icon: 'error',
      confirmButtonColor: '#1a2b4c',
      customClass: { popup: 'rounded-4' },
    })
    return
  }

  try {
    const response = await updateRecommendationApi('memory', memoryId, true)
    if (response && response.success) {
      Swal.fire({
        title: '🚀 官方強力推薦！',
        text: '已成功將此回憶設為官方推薦！',
        icon: 'success',
        confirmButtonColor: '#ff7b00',
        confirmButtonText: '讚啦',
        showClass: { popup: 'animate__animated animate__bounceIn' },
        customClass: { popup: 'rounded-4' },
      })
    } else {
      throw new Error(response?.message || '推薦失敗')
    }
  } catch (error) {
    console.error('推薦失敗', error)
    if (error.response?.status === 403) {
      Swal.fire({
        title: '權限不足',
        text: '只有官方管理員 (OfficialManager) 可以執行此操作。',
        icon: 'error',
        confirmButtonColor: '#1a2b4c',
        customClass: { popup: 'rounded-4' },
      })
    } else {
      Swal.fire({
        title: '推薦失敗',
        text: '無法將回憶設為推薦，請稍後再試。',
        icon: 'error',
        confirmButtonColor: '#1a2b4c',
        customClass: { popup: 'rounded-4' },
      })
    }
  }
}

// 4. 關於這個帳號 (前往個人檔案)
const goToProfile = () => {
  const userId = props.post?.userId || props.post?.author?.id || 'travel_user01'
  closeModal()
  router.push({ name: 'MemberProfile', params: { userId: userId } })
}

// 5. 編輯貼文 (Emit 到外部父組件)
const handleEdit = () => {
  closeModal()
  emit('edit', props.post)
}

// 6. 刪除貼文 (真實 API 刪除)
const handleDelete = async () => {
  closeModal()

  const result = await Swal.fire({
    title: '確定要刪除此貼文嗎？',
    text: '刪除後將無法還原此旅遊回憶唷！',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#d33',
    cancelButtonColor: '#3085d6',
    confirmButtonText: '確定刪除',
    cancelButtonText: '取消',
    customClass: { popup: 'rounded-4' },
  })

  if (result.isConfirmed) {
    try {
      const response = await deleteMemory(props.post.id)
      if (response && response.success) {
        Swal.fire({
          title: '已刪除！',
          text: '您的旅遊回憶已成功刪除。',
          icon: 'success',
          confirmButtonColor: '#1a2b4c',
          customClass: { popup: 'rounded-4' },
        })
        emit('delete', props.post.id)
      } else {
        throw new Error(response?.message || '刪除失敗')
      }
    } catch (error) {
      console.error('刪除貼文失敗:', error)
      Swal.fire({
        icon: 'error',
        title: '刪除失敗',
        text: '無法刪除貼文，請稍後再試。',
        confirmButtonColor: '#1a2b4c',
        customClass: { popup: 'rounded-4' },
      })
    }
  }
}
</script>

<template>
  <!-- 全螢幕遮罩 -->
  <div
    v-if="modelValue"
    class="modal-backdrop-custom d-flex align-items-center justify-content-center"
    @click="closeModal"
  >
    <!-- 彈窗內容 (點擊內部不關閉) -->
    <div class="options-menu bg-white rounded-4 shadow overflow-hidden" @click.stop>
      <!-- 強力推薦 (只有管理員看得到) -->
      <button
        v-if="isAdmin"
        class="menu-item fw-bold text-primary border-bottom"
        @click="handleBoost"
      >
        🚀 官方強力推薦
      </button>

      <!-- 自己的貼文選項 -->
      <template v-if="isOwnPost">
        <button class="menu-item fw-bold border-bottom" @click="handleEdit">✏️ 編輯貼文</button>
        <button class="menu-item text-danger fw-bold border-bottom" @click="handleDelete">
          🗑️ 刪除貼文
        </button>
      </template>

      <!-- 他人的貼文選項 -->
      <template v-else>
        <button class="menu-item text-danger fw-bold border-bottom" @click="handleReport">
          🚩 檢舉
        </button>

        <button class="menu-item border-bottom" @click="goToProfile">👤 關於這個帳號</button>

        <button
          class="menu-item fw-bold border-bottom"
          :class="isFollowing ? 'text-danger' : 'text-primary'"
          :disabled="isLoadingFollow"
          @click="handleFollowToggle"
        >
          {{ isFollowing ? '⛔ 取消追蹤' : '➕ 追蹤此帳號' }}
        </button>
      </template>

      <button class="menu-item" @click="closeModal">取消</button>
    </div>
  </div>
</template>

<style scoped>
/* 半透明黑底遮罩 */
.modal-backdrop-custom {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background-color: rgba(0, 0, 0, 0.65);
  z-index: 9999;
  /* 確保在最上層 */
  animation: fadeIn 0.2s ease-out;
}

/* 彈窗卡片本體 */
.options-menu {
  width: 90%;
  max-width: 400px;
  animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  display: flex;
  flex-direction: column;
}

/* 每一顆按鈕選項 */
.menu-item {
  width: 100%;
  background: transparent;
  border: none;
  padding: 16px 0;
  font-size: 15px;
  color: #262626;
  border-color: #efefef !important;
  /* IG 風格的極淡灰線 */
  transition: background-color 0.1s;
}

.menu-item:hover,
.menu-item:active {
  background-color: #f8f9fa;
}

.menu-item:focus {
  outline: none;
}

/* 淡入與滑動動畫 */
@keyframes fadeIn {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@keyframes slideUp {
  from {
    transform: translateY(30px) scale(0.95);
    opacity: 0;
  }

  to {
    transform: translateY(0) scale(1);
    opacity: 1;
  }
}
</style>
