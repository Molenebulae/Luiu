<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { getFavoritesApi } from '@/api/favorite'
import ProfileContentCard from './ProfileContentCard.vue'
import SpotFavoriteCard from './SpotFavoriteCard.vue'

const router = useRouter()
const activeTab = ref('plan') // 預設顯示行程
const allFavorites = ref([])

onMounted(async () => {
  try {
    const res = await getFavoritesApi()
    allFavorites.value = res.data
  } catch (error) {
    console.error('載入收藏資料失敗', error)
  }
})

const tabs = [
  { id: 'plan', label: '行程規劃', icon: 'bi-map', type: 'Plan' },
  { id: 'memory', label: '旅遊回憶', icon: 'bi-camera', type: 'Memory' },
  { id: 'spot', label: '我的景點', icon: 'bi-geo-alt', type: 'Spot' },
]

// 核心：根據 activeTab 自動過濾資料
const filteredItems = computed(() => {
  const selectedType = tabs.find((t) => t.id === activeTab.value).type
  return allFavorites.value.filter((item) => item.type === selectedType)
})

const handleCardClick = (item) => {
  if (item.type === 'Plan') {
    router.push({ name: 'PlanInfo', params: { userId: item.userId, planId: item.targetId } })
  } else if (item.type === 'Memory') {
    router.push({ name: 'MemoryDetail', params: { id: item.targetId } })
  }
}
</script>

<template>
  <div class="container py-4">
    <ul class="nav nav-pills mb-4">
      <li v-for="tab in tabs" :key="tab.id" class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === tab.id }"
          @click="activeTab = tab.id"
        >
          <i :class="['bi', tab.icon, 'me-1']"></i> {{ tab.label }}
        </button>
      </li>
    </ul>

    <div class="row g-4">
      <template v-if="filteredItems.length > 0">
        <div
          v-for="item in filteredItems"
          :key="item.collectId"
          :class="activeTab === 'spot' ? 'col-3' : 'col-4'"
        >
          <SpotFavoriteCard
            v-if="activeTab === 'spot'"
            :name="item.title"
            :image="item.imageUrl"
            :category="item.category"
            :address="item.subTitle"
            :rating="item.rating"
            :user-rating-count="item.userRatingCount"
          />

          <ProfileContentCard
            v-else
            :title="item.title"
            :sub-title="item.subTitle"
            :cover-url="item.imageUrl"
            :icon="activeTab === 'plan' ? 'bi-map-fill' : 'bi-camera-fill'"
            @click="handleCardClick(item)"
          />
        </div>
      </template>

      <div v-else class="col-12 text-center py-5">
        <p class="text-muted">目前沒有 {{ tabs.find((t) => t.id === activeTab).label }} 收藏</p>
      </div>
    </div>
  </div>
</template>
