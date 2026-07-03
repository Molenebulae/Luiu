<script setup>
defineProps({
  id: [String, Number],
  title: String,
  content: String,
  variant: { type: String, default: '' }, // 用於處理 bg-primary-subtle 等樣式
})
const emit = defineEmits(['close'])

const handleClose = () => {
  emit('close')
}
</script>

<template>
  <!-- <div class="card border mb-0" :class="variant">
    <div class="card-header d-flex align-items-center gap-3 justify-content-between flex-wrap">
      <slot class="cardHeader">
        <h5 class="card-title mb-0">{{ title }}</h5>
      </slot>
      <ul class="card-actions list-inline mb-0">
        <li class="list-inline-item">
          <a class="card-close" href="javascript:void(0)" @click="$emit('close')">
            <i class="fa-solid fa-xmark"></i>
          </a>
        </li>
      </ul>
    </div>
    <div class="card-body">
      <slot class="cardBody">
        <p>{{ content }}</p>
      </slot>
    </div>
  </div> -->

  <div class="card border mb-0" :class="variant">
    <div class="card-header d-flex align-items-center gap-3 justify-content-between flex-wrap">
      <!-- 修改這裡：加上 name="header" -->
      <slot name="header">
        <h5 class="card-title mb-0">{{ title }}</h5>
      </slot>

      <ul class="card-actions list-inline mb-0">
        <li class="list-inline-item">
          <a class="card-close" href="javascript:void(0)" @click="$emit('close')">
            <i class="fa-solid fa-xmark"></i>
          </a>
        </li>
      </ul>
    </div>
    <div class="card-body">
      <!-- 這裡保持預設插槽即可 -->
      <slot>
        <p>{{ content }}</p>
      </slot>
    </div>
  </div>
</template>

<style scoped>
.card-move {
  cursor: move;
}
</style>
