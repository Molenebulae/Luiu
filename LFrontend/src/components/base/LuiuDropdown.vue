<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { RouterLink, useRoute } from 'vue-router';

const props = defineProps({
  title: { type: String, default: 'Pages' },
  menuItems: { type: Array, required: true }
});

const route = useRoute();
const isOpen = ref(false);
const dropdownRef = ref(null); // 用於綁定最外層 DOM
const toggleDropdown = () => (isOpen.value = !isOpen.value);
const closeDropdown = () => (isOpen.value = false);

// 邏輯 1：點擊外部自動關閉
const handleClickOutside = (event) => {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target)) {
    closeDropdown();
  }
};

// 邏輯 2：監聽路由變化，跳轉即關閉
watch(() => route.path, () => {
  closeDropdown();
});

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});
</script>

<template>
  <div class="nav-item dropdown" @mouseleave="closeDropdown">
    <!-- 下拉選單標題 -->
    <a
      href="javascript:void(0)"
      class="nav-link dropdown-toggle"
      :class="{ 'show': isOpen }"
      aria-haspopup="true"
      :aria-expanded="isOpen"
      @click.prevent="toggleDropdown"
    >
      {{ title }}
    </a>

    <!-- 下拉選單內容 -->
    <transition name="fade">
      <div v-if="isOpen" class="dropdown-menu show">
        <RouterLink
          v-for="(item, index) in menuItems"
          :key="index"
          class="dropdown-item"
          :to="{ name: item.routeName }"
          role="menuitem"
          @click="closeDropdown"
        >
          {{ item.label }}
        </RouterLink>
      </div>
    </transition>
  </div>
</template>



<style lang="scss" scoped>
@import '@/assets/scss/components/dropdown';
</style>
