<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import LuiuAvatar from '@/components/base/LuiuAvator.vue'
import { getHomeRecommendPlanApi } from '@/api/planning/plan'

const router = useRouter()
const sliderRef = ref(null)
const defaultUrl = 'https://images.unsplash.com/photo-1488646953014-85cb44e25828?q=80&w=400'
const handleCardClick = (userId, planId) => {
  router.push({
    name: 'PlanInfo',
    params: {
      userId: userId,
      planId: planId,
    },
  })
}

const scrollLeft = () => {
  if (sliderRef.value) {
    sliderRef.value.scrollBy({ left: -374, behavior: 'smooth' })
  }
}

const scrollRight = () => {
  if (sliderRef.value) {
    sliderRef.value.scrollBy({ left: 374, behavior: 'smooth' })
  }
}

const plans = ref([])

const getHotMemories = async () => {
  try {
    const result = await getHomeRecommendPlanApi()
    console.log(result.data)
    plans.value = result.data
  } catch (error) {
    console.error(error?.message || '行程規劃載入失敗')
  }
}

onMounted(() => {
  getHotMemories()
})
</script>

<template>
  <div class="carousel-section w-full my-5">
    <div class="title-container mb-4">
      <span class="text-brand-blue fw-bold d-block mb-1"># 官方精選</span>
      <h3 class="fw-bold mb-0 text-dark">推薦行程規劃</h3>
    </div>

    <div v-if="plans && plans.length > 0" class="carousel-container position-relative w-100 px-5">
      <button class="nav-btn left-btn shadow border" @click="scrollLeft">
        <i class="bi bi-chevron-left"></i>
      </button>

      <div
        ref="sliderRef"
        class="card-track no-scrollbar d-flex gap-4 overflow-auto scroll-smooth py-2"
      >
        <div
          v-for="plan in plans"
          :key="plan.id"
          class="flex-shrink-0 card custom-plan-card border-0 shadow-sm"
          @click="handleCardClick(plan.userId, plan.tripId)"
        >
          <div class="card-img-wrapper position-relative text-white d-flex align-items-end p-3">
            <img
              :src="$img(plan.coverImage) || defaultUrl"
              class="position-absolute w-100 h-100"
              style="object-fit: cover; top: 0; left: 0"
              @error="(e) => (e.target.src = defaultUrl)"
            />
            <div
              class="position-absolute w-100 h-100"
              style="
                background: linear-gradient(
                  to bottom,
                  rgba(0, 0, 0, 0) 40%,
                  rgba(0, 0, 0, 0.85) 100%
                );
                top: 0;
                left: 0;
              "
            ></div>

            <div class="w-100 text-left text-white z-1">
              <span class="badge bg-blur mb-2">{{ plan.tag }}</span>
              <h6 class="fw-bold mb-0 text-white text-truncate-2" :title="plan.title">
                {{ plan.title }}
              </h6>
            </div>
          </div>

          <div class="card-body py-3 px-3">
            <div class="d-flex align-items-center justify-content-between metadata-row">
              <div class="left-block d-flex align-items-center gap-1 text-truncate">
                <LuiuAvatar
                  :avatar="$img(plan.avatarUrl)"
                  size="sm"
                  class="flex-shrink-0 card-avatar"
                />
                <span class="user-name text-truncate" :title="plan.userName">
                  {{ plan.author }}
                </span>
              </div>

              <div class="right-block d-flex align-items-center flex-shrink-0">
                <span class="duration-text" :title="`${plan.durationDays} 天`">
                  {{ plan.durationDays }}天
                </span>
                <span class="divider-space">/</span>
                <span class="spot-count" :title="`${plan.spotCount} 景點`">
                  {{ plan.spotCount }}景點
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <button class="nav-btn right-btn shadow border" @click="scrollRight">
        <i class="bi bi-chevron-right"></i>
      </button>
    </div>

    <div v-else class="text-center py-5 text-muted">
      <i class="bi bi-info-circle" style="font-size: 2rem"></i>
      <p class="mt-2">目前暫無推薦行程規劃</p>
    </div>
  </div>
</template>

<style scoped>
/* 標題與內文對齊線 */
.title-container {
  padding-left: calc(3rem + 15px);
  text-align: left;
}

.text-brand-blue {
  color: #007bff !important;
}

.carousel-container {
  box-sizing: border-box;
}

/* 左右按鈕定位 */
.nav-btn {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  z-index: 100;
  width: 44px;
  height: 44px;
  background: #ffffff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s ease;
}

.nav-btn:hover {
  background: #f8f9fa;
  transform: translateY(-50%) scale(1.1);
}

.left-btn {
  left: 10px;
}

.right-btn {
  right: 10px;
}

/* 滾動軌道 */
.card-track {
  scroll-snap-type: x mandatory;
  padding-bottom: 10px;
}

.no-scrollbar::-webkit-scrollbar {
  display: none;
}

.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}

/* 卡片主體 */
.custom-plan-card {
  width: 350px;
  border-radius: 16px;
  overflow: hidden;
  cursor: pointer;
  scroll-snap-align: start;
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
}

.custom-plan-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.12) !important;
}

/* 圖片區塊 */
.card-img-wrapper {
  height: 220px;
  width: 100%;
  background-size: cover;
  background-position: center;
}

.bg-blur {
  background: rgba(255, 255, 255, 0.25);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
  color: white;
}

.text-truncate-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

/* 資訊列佈局 */
.metadata-row {
  font-size: 14px;
  width: 100%;
}

/* 左側：大頭貼與名字 */
.left-block {
  width: 65%;
  min-width: 0;
  /* 確保 text-truncate 正常運作 */
}

.user-name {
  font-weight: 700;
  color: #334155;
}

/* 右側：數據資訊 */
.right-block {
  text-align: right;
  white-space: nowrap;
}

.duration-text {
  color: #64748b;
}

.divider-space {
  color: #cbd5e1;
  margin: 0 6px;
  font-size: 12px;
}

.spot-count {
  font-weight: 600;
  color: #2563eb;
}

/* 基礎元件頭像尺寸強制覆寫 */
.card-avatar :deep(img),
.card-avatar {
  width: 24px !important;
  height: 24px !important;
  max-width: 24px !important;
  max-height: 24px !important;
}
</style>
