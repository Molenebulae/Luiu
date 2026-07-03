<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import LuiuAvatar from '@/components/base/LuiuAvator.vue'
import { getHomeHotMemoriesApi } from '@/api/memory'

// const props = defineProps({
//   memories: { type: Array, required: true, default: () => [] },
// })

const router = useRouter()
const sliderRef = ref(null)
const defaultUrl = 'https://images.unsplash.com/photo-1488646953014-85cb44e25828?q=80&w=400'
const handleCardClick = (id) => {
  router.push({
    name: 'MemoryDetail',
    params: { id: id },
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

// const memories = ref([
//   {
//     id: 101,
//     title: '東京五天四夜櫻花季最強攻略！深度私房景點全公開',
//     tag: '日本自由行',
//     author: '艾倫愛旅遊',
//     likes: '1.2k',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 102,
//     title: '京都絕美抹茶店與宇治一日隱密散策路線分享',
//     tag: '美食探店',
//     author: '甜點控CC',
//     likes: '2.4k',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1503899036084-c55cdd92da26?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 103,
//     title: '漫步神隱九份山城，尋找千與千尋的茶樓與絕美夕陽',
//     tag: '台灣在地遊',
//     author: '旅行者小安',
//     likes: '958',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1571474004502-c1def214ac6d?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 104,
//     title: '花蓮太魯閣大自然療癒充電之旅，燕子口與錐麓古道健行',
//     tag: '戶外秘境',
//     author: '戶外咖阿健極限運動狂熱',
//     likes: '841',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1549693578-d683be217e58?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 105,
//     title: '體驗熱鬧夜市文化！饒河夜市必吃胡椒餅與藥燉排骨美食地圖',
//     tag: '夜市文化',
//     author: '吃貨一號',
//     likes: '1.7k',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1552993873-0dd1110e025f?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 106,
//     title: '曼谷五天四夜奢華高空酒吧與泰式按摩極致放鬆度假行程',
//     tag: '泰國踩雷',
//     author: 'Sabrina',
//     likes: '3.1k',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 107,
//     title: '台東太麻里金針花海季，滿山遍野金黃地毯全攻略',
//     tag: '季節限定',
//     author: '攝影師阿翔',
//     likes: '620',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1470240731273-7821a6eeb6bd?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 108,
//     title: '冰島冬季雷克雅維克極光獵人追光筆記，冒險日記大公開',
//     tag: '極光冒險',
//     author: 'Alex_Around',
//     likes: '4.2k',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1529963183134-61a90db47eaf?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 109,
//     title: '澎湖花火節無人機燈光秀最佳觀賞點，三天兩夜跳島玩透透',
//     tag: '離島旅遊',
//     author: '澎湖地陪小張',
//     likes: '1.1k',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1492562080023-ab3db95bfbce?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1537135032545-802da2952c15?w=600&auto=format&fit=crop&q=80',
//   },
//   {
//     id: 110,
//     title: '首爾聖水洞文青咖啡廳網美打卡店不藏私分享清單',
//     tag: '韓國探店',
//     author: '歐爸帶你玩',
//     likes: '920',
//     avatarUrl:
//       'https://images.unsplash.com/photo-1517841905240-472988babdf9?w=150&auto=format&fit=crop&q=80',
//     coverUrl:
//       'https://images.unsplash.com/photo-1538481199705-c710c4e965fc?w=600&auto=format&fit=crop&q=80',
//   },
// ])
const memories = ref([])

const getHotMemories = async () => {
  try {
    const result = await getHomeHotMemoriesApi()
    console.log(result.data)
    memories.value = result.data
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
      <span class="text-brand-red fw-bold d-block mb-1"># 熱門動態</span>
      <h3 class="fw-bold mb-0 text-dark">精選旅人回憶</h3>
    </div>

    <div
      v-if="memories && memories.length > 0"
      class="carousel-container position-relative w-100 px-5"
    >
      <button class="nav-btn left-btn shadow border" @click="scrollLeft">
        <i class="bi bi-chevron-left"></i>
      </button>

      <div
        ref="sliderRef"
        class="card-track no-scrollbar d-flex gap-4 overflow-auto scroll-smooth py-2"
      >
        <div
          v-for="memory in memories"
          :key="memory.id"
          class="flex-shrink-0 card custom-memory-card border-0 shadow-sm"
          @click="handleCardClick(memory.memoryId)"
        >
          <div class="card-img-wrapper position-relative text-white d-flex align-items-end p-3">
            <img
              :src="$img(memory.coverImage) || defaultUrl"
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
              <h6 class="fw-bold mb-0 text-white text-truncate-2" :title="memory.title">
                {{ memory.title }}
              </h6>
            </div>
          </div>

          <div class="card-body p-3 d-flex align-items-center justify-content-between bg-white">
            <div class="d-flex align-items-center text-truncate me-2">
              <LuiuAvatar
                :avatar="$img(memory.avatarUrl)"
                size="sm"
                class="me-2 flex-shrink-0 card-avatar"
              />

              <span
                class="small fw-bold text-dark text-truncate"
                :title="memory.author || memory.userName"
              >
                {{ memory.author || memory.userName }}
              </span>
            </div>
            <span class="text-muted small flex-shrink-0">
              <i class="bi bi-heart-fill text-danger me-1"></i>{{ memory.likeCount }}
            </span>
          </div>
        </div>
      </div>

      <button class="nav-btn right-btn shadow border" @click="scrollRight">
        <i class="bi bi-chevron-right"></i>
      </button>
    </div>

    <div v-else class="text-center py-5 text-muted">
      <i class="bi bi-info-circle" style="font-size: 2rem"></i>
      <p class="mt-2">目前暫無推薦旅遊回憶</p>
    </div>
  </div>
</template>

<style scoped>
/* 標題與內文對齊線 */
.title-container {
  padding-left: calc(3rem + 15px);
  /* 對齊 px-5 的卡片邊緣 */
  text-align: left;
}

.text-brand-red {
  color: #dc3545 !important;
  /* 品牌紅色 */
}

.carousel-container {
  box-sizing: border-box;
}

/* 左右按鈕定位完全移出卡片範圍 */
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

/* 卡片寬度與規劃同步鎖定 350px */
.custom-memory-card {
  width: 350px;
  border-radius: 16px;
  overflow: hidden;
  cursor: pointer;
  scroll-snap-align: start;
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
}

.custom-memory-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.12) !important;
}

/* 圖片區塊與背景處理 */
.card-img-wrapper {
  height: 220px;
  /* 與規劃卡片高度一致 */
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

.card-avatar :deep(img),
.card-avatar {
  width: 24px !important;
  height: 24px !important;
  max-width: 24px !important;
  max-height: 24px !important;
}
</style>
