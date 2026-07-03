<script setup>
import { computed } from 'vue';

const props = defineProps({
  users: {
    type: Array,
    required: true
  },
  max: {
    type: Number,
    default: 4
  }
});

// 1. 定義預設圖片路徑 (可以是本地圖片或一個線上的 Placeholder)
const defaultAvatar = 'https://placehold.co/100x100?text=User';

// 2. 處理圖片加載失敗的情況 (例如網址是壞的)
const handleImgError = (e) => {
  e.target.src = defaultAvatar;
};

const visibleUsers = computed(() => props.users.slice(0, props.max));
const remainingCount = computed(() => Math.max(0, props.users.length - props.max));

</script>
<template>
  <div class="avatar-group">
    <div
      v-for="user in visibleUsers"
      :key="user.id"
      class="avatar-wrapper"
    >
        <img
        :src="user.avatarUrl || defaultAvatar"
        :alt="user.name"
        :title="user.name"
        class="avatar"
        @error="handleImgError"
      />
    </div>

    <div v-if="remainingCount > 0" class="avatar extra-counter">
      +{{ remainingCount }}
    </div>
  </div>
</template>

<style lang="scss" scoped>
@import '@/assets/scss/components/avatarGroup';
</style>
