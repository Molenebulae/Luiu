<script setup>
import { computed } from 'vue'
import defaultAvatar from '@/assets/Images/person.svg'
const props = defineProps({
  avatar: {
    type: String,
    // required: true,
    default: defaultAvatar, // 預設頭像圖片
  },
  // 沒傳名字就不顯示文字區塊
  username: {
    type: String,
    default: '',
  },
  // 支援 'sm', 'md', 'lg', 'xl' 四種大小
  size: {
    type: String,
    default: 'md',
    validator: (value) => ['sm', 'md', 'lg', 'xl'].includes(value),
  },
})

const handleImageError = (e) => {
  // 當圖片載入失敗時，將 src 指向預設圖
  e.target.src = defaultAvatar
}

// 移除 subClass，因為已經不需要副標題樣式了
const sizeConfig = computed(() => {
  switch (props.size) {
    case 'sm':
      return { avatar: 36, titleClass: 'fs-6', gap: 'gap-2' }
    case 'lg':
      return { avatar: 64, titleClass: 'fs-4', gap: 'gap-3' }
    case 'xl':
      return { avatar: 120, titleClass: 'fs-3', gap: 'gap-4' }
    case 'md':
    default:
      return { avatar: 48, titleClass: 'fs-5', gap: 'gap-2' }
  }
})
</script>

<template>
  <div class="d-flex align-items-center py-2 luiu-user-row" :class="sizeConfig.gap">
    <div class="flex-shrink-0">
      <img :src="avatar" class="rounded-circle object-fit-cover shadow-sm"
        :style="{ width: `${sizeConfig.avatar}px`, height: `${sizeConfig.avatar}px` }" alt="User Avatar"
        @error="handleImageError" />
    </div>

    <div v-if="username" class="overflow-hidden d-flex flex-column justify-content-center">
      <h6 class="fw-bold text-truncate luiu-username m-0" :class="sizeConfig.titleClass">
        {{ username }}
      </h6>
    </div>
  </div>
</template>

<style scoped>
.luiu-user-row {
  border-radius: 0.5rem;
}

.luiu-username {
  color: #2c3e50;
  line-height: 1.2;
}

/* 移除了不需要的 .fs-7 和 .luiu-subtitle */
</style>
