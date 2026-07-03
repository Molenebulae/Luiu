<script setup>
const props = defineProps({
  type: {
    type: String,
    default: 'back',
  },
})
</script>

<template>
  <div class="separator-container">
    <div :class="props.type === 'front' ? 'luiu-front-separator' : 'luiu-back-separator'">
      <slot>我是分隔線(預設)</slot>
    </div>
  </div>
</template>

<style scoped>
/* 前端：利用 flex 讓左右偽元素自動填滿空間 */
.luiu-front-separator {
  display: flex;
  align-items: center;
  text-align: center;
  font-weight: bold;
  color: var(--bs-primary);
  justify-content: center;
}

.luiu-front-separator::before,
.luiu-front-separator::after {
  content: '';
  flex: 0 0 50px;
  /* 固定線條長度 50px */
  border-top: 2px solid var(--bs-primary);
  margin: 0 15px;
}

/* 後端：虛線貫穿且標題在中間 */
.luiu-back-separator {
  display: flex;
  align-items: center;
  text-align: center;
  color: var(--bs-primary);
  font-weight: bold;
  min-width: 256px;
}

.luiu-back-separator::before,
.luiu-back-separator::after {
  content: '';
  flex: 1;
  /* 自動填滿剩餘空間 */
  border-top: 2px solid var(--bs-primary);
}

.luiu-back-separator::before {
  margin-right: 15px;
}

.luiu-back-separator::after {
  margin-left: 15px;
}
</style>
