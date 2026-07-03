<script setup>
import { ref, onMounted } from 'vue'
import ProfileContentCard from './ProfileContentCard.vue'
import { useRouter, useRoute } from 'vue-router'
import { getMemoriesByUserId } from '@/api/memory'

const router = useRouter()
const route = useRoute()
const memories = ref([])

const fetchMemories = async () => {
  const userId = route.params.userId

  try {
    const result = await getMemoriesByUserId(userId)
    memories.value = result.data
  } catch (error) {
    console.error(error?.message || '旅遊回憶載入失敗')
  }
}
const handleMemoryClick = (id) => {
  if (!id) return

  router.push({
    name: 'MemoryDetail',
    params: { id: id },
  })
}

onMounted(() => {
  fetchMemories()
})
</script>

<template>
  <div v-if="memories.length > 0">
    <div class="row g-4 mt-1">
      <div v-for="memory in memories" :key="memory.MemoryId" class="col-4">
        <ProfileContentCard
          :id="memory.memoryId"
          :title="memory.title"
          :sub-title="`${memory.startDate} ~ ${memory.endDate}`"
          :cover-url="memory.coverImage"
          icon="bi bi-camera-fill"
          @click="handleMemoryClick"
        />
      </div>
    </div>
  </div>

  <div v-else class="row g-2">
    <div class="col-12 text-center py-5">
      <i class="bi bi-camera fs-1 text-muted"></i>
      <p class="mt-2 text-muted">尚未上傳旅遊回憶</p>
    </div>
  </div>
</template>

<style scoped></style>
