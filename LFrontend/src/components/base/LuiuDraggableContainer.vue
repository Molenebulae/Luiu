<script setup>
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import Sortable from 'sortablejs'

const props = defineProps({
  group: { type: String, default: 'shared' },
  animation: { type: Number, default: 150 },
  handle: { type: String, default: '.card-move' },
})
const emit = defineEmits(['dragend'])

const containerRef = ref(null)
let sortableInstance = null

onMounted(async () => {
  // 核心修正：等待 Vue 完成 DOM 更新
  await nextTick()

  if (containerRef.value) {
    sortableInstance = new Sortable(containerRef.value, {
      group: props.group,
      animation: props.animation,
      // handle: props.handle,
      // 修正：確保拖拽時的樣式不會跑掉
      handle: null,
      //讓整張卡片可以拖動
      fallbackOnBody: true,
      swapThreshold: 0.65,
      ghostClass: 'sortable-ghost', // 正在拖動項目的樣式
      chosenClass: 'sortable-chosen', // 被選中項目的樣式
      dragClass: 'sortable-drag', // 拖動中滑鼠下的樣式

      onEnd: (evt) => {
        console.log('Moved:', evt.oldIndex, '->', evt.newIndex)
        emit('dragend', evt)
      },
    })
  }
})

onUnmounted(() => {
  if (sortableInstance) sortableInstance.destroy()
})
</script>

<template>
  <!-- 注意：這裡必須保留 row 類別，Sortable 會對此容器下的直接子元素進行排序 -->
  <div ref="containerRef" class="row g-3 draggable-zone">
    <slot></slot>
  </div>
</template>

<style scoped>
/* 增加一些視覺回饋，避免拖拽時「卡片變文字」的視覺錯覺 */
.draggable-zone {
  min-height: 50px; /* 確保空容器也能被丟入 */
}

:deep(.sortable-ghost) {
  opacity: 0.4;
  background-color: var(--bs-light) !important;
  border: 1px dashed var(--bs-primary) !important;
}

:deep(.sortable-chosen) {
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1);
}
</style>
