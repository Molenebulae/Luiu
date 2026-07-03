<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getMemoryDetail } from '@/api/memory'
import { getImageUrl } from '@/utils/pathHelper'

// 引入你需要的元件
import LuiuAvator from '@/components/base/LuiuAvator.vue'
import LuiuStatCard from '@/components/base/LuiuStatCard.vue'
import LuiuVideoPlayer from '@/components/base/LuiuVideoPlayer.vue'
import logoUrl from '@/assets/Images/P1.jpg'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const goBack = () => {
  if (window.history.length > 2) {
    router.back() // 如果有上一頁，就乖乖回上一頁 (例如回 Profile 或 首頁)
  } else {
    router.push({ name: 'Memory' }) // 如果使用者是直接貼網址進來的(沒有上一頁)，才預設讓他回首頁
  }
}

const handleImageError = (e) => {
  // 當圖片載入失敗時，將 src 指向預設圖
  e.target.src = logoUrl
}
// 判定網址是否為影片 (YouTube 或是直連影片)
const isVideoUrl = (url) => {
  if (!url) return false
  return (
    url.includes('youtube.com') ||
    url.includes('youtu.be') ||
    /^[a-zA-Z0-9_-]{11}$/.test(url) ||
    url.endsWith('.mp4') ||
    url.endsWith('.webm') ||
    url.endsWith('.ogg')
  )
}

// 模擬資料 (保留供後續測試或 UI 調整參考)
/*
const mockMemory = {
    id: 1,
    title: '台灣環島五日遊',
    date: '2026年4月15日',
    coverImage: 'https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=1200&q=80',
    author: {
        name: '旅行規劃師 Amy',
        avatar: 'https://i.pravatar.cc/150?img=1',
        postDate: '發佈於 2026年4月15日'
    },
    stats: {
        days: 5,
        locations: 9,
        budget: '11,200'
    },
    days: [
        {
            day: 1,
            label: 'Day 1',
            date: '4月15日',
            events: [
                {
                    id: 101,
                    time: '07:00',
                    location: '桃園國際機場',
                    title: '桃園國際機場出發',
                    description: '享受五天四夜準備開始環島之旅',
                    image: 'https://images.unsplash.com/photo-1436491865332-7a61a109cc05?w=800&q=80',
                    duration: '1小時',
                    cost: 'NT$ 0'
                }
            ]
        },
        { day: 2, label: 'Day 2', date: '4月16日', events: [] },
        { day: 3, label: 'Day 3', date: '4月17日', events: [] }
    ]
};
*/

// 初始化空的結構，避免畫面渲染時報錯
const memory = ref({
  id: null,
  title: '載入中...',
  date: '',
  coverImage: logoUrl,
  author: { name: '', avatar: '', postDate: '' },
  stats: { days: 0, locations: 0, budget: '0' },
  destinations: [],
  days: [],
})

onMounted(async () => {
  try {
    const id = route.params.id
    if (!id) return

    const response = await getMemoryDetail(id)
    if (response && response.success) {
      const data = response.data

      const startDateStr = data.startDate ? data.startDate.replace(/-/g, '/') : '未知日期'

      let totalLocations = 0
      let totalBudget = 0
      const uniqueDestinations = new Set()
      const majorCities = [
        '台北',
        '新北',
        '基隆',
        '桃園',
        '新竹',
        '苗栗',
        '台中',
        '彰化',
        '南投',
        '雲林',
        '嘉義',
        '台南',
        '高雄',
        '屏東',
        '宜蘭',
        '花蓮',
        '台東',
        '澎湖',
        '金門',
        '馬祖',
        '東京',
        '大阪',
        '京都',
        '北海道',
        '沖繩',
        '首爾',
        '釜山',
        '曼谷',
        '香港',
        '澳門',
      ]

      const formattedDays =
        data.days?.map((d) => {
          totalLocations += d.stops?.length || 0

          const events =
            d.stops?.map((s) => {
              totalBudget += Number(s.expense) || 0

              if (s.placeName) {
                const foundCity = majorCities.find((c) => s.placeName.includes(c))
                if (foundCity) {
                  uniqueDestinations.add(foundCity)
                } else if (uniqueDestinations.size < 3) {
                  const shortName =
                    s.placeName.length > 4 ? s.placeName.substring(0, 4) : s.placeName
                  uniqueDestinations.add(shortName)
                }
              }

              let timeStr = '未定'
              if (s.arrivalTime) {
                const dateObj = new Date(s.arrivalTime)
                timeStr = dateObj.toLocaleTimeString([], {
                  hour: '2-digit',
                  minute: '2-digit',
                  hour12: false,
                })
              }

                    return {
                        id: s.stopId,
                        time: timeStr,
                        location: s.placeName || '未知地點',
                        title: s.placeName || '未命名景點',
                        description: s.memoryText || '',
                        videoUrl: s.videoEmbedUrl || '',
                        images: s.imageUrls || [],
                        duration: s.duration || '',
                        cost: s.expense ? `NT$ ${s.expense}` : '免費'
                    };
                }) || [];

          return {
            day: d.dayNumber,
            label: `Day ${d.dayNumber}`,
            date: d.dayDate ? d.dayDate.replace(/-/g, '/') : '',
            events: events,
          }
        }) || []

            memory.value = {
                id: data.memoryId,
                title: data.title || '無標題',
                date: startDateStr,
                coverImage: data.coverImage || '',
                author: {
                    name: data.authorName || '無名旅人',
                    avatar: data.authorAvatarUrl || '',
                    postDate: `發佈於 ${startDateStr}`
                },
                stats: {
                    days: formattedDays.length,
                    locations: totalLocations,
                    budget: totalBudget.toLocaleString()
                },
                destinations: Array.from(uniqueDestinations).slice(0, 4),
                days: formattedDays
            };

      if (formattedDays.length > 0) {
        activeDay.value = formattedDays[0].day
      }

      // 確保畫面渲染完畢後，如果有 hash，則捲動到該區塊
      await nextTick()
      if (route.hash) {
        const el = document.querySelector(route.hash)
        if (el) {
          // 給一點延遲確保其他元件也準備好
          setTimeout(() => {
            el.scrollIntoView({ behavior: 'smooth' })
          }, 300)
        }
      }
    }
  } catch (error) {
    console.error('無法取得回憶明細', error)
  }
})

const activeDay = ref(1)
const currentDayEvents = computed(() => {
  const dayData = memory.value.days.find((d) => d.day === activeDay.value)
  return dayData ? dayData.events : []
})

// --- 前端假留言板區塊 (Demo用) ---
const newCommentText = ref('')
const isSubmittingComment = ref(false)

const initialComments = JSON.parse(
  localStorage.getItem(`luiu_mock_comments_${route.params.id}`),
) || [
  {
    id: 1,
    author: '山林探險家',
    avatar: 'https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=100&q=80',
    text: '請問這個行程花費大概多少？飯店好訂嗎？',
    time: '2 天前',
  },
  {
    id: 2,
    author: '吃貨女孩',
    avatar: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=100&q=80',
    text: '那間餐廳真的超好吃！我上次去也大推～',
    time: '5 小時前',
  },
]

const mockComments = ref(initialComments)

const submitComment = () => {
  if (!newCommentText.value.trim()) return

  isSubmittingComment.value = true

  // 模擬網路延遲，讓 Demo 看起來更真實
  setTimeout(() => {
    mockComments.value.push({
      id: Date.now(),
      author: userStore.userInfo?.name || '我 (目前登入者)', // 取用真實名稱
      avatar:
        userStore.userInfo?.avatarUrl ||
        'https://images.unsplash.com/photo-1633332755192-727a05c4013d?w=100&q=80', // 取用真實大頭貼
      text: newCommentText.value.trim(),
      time: '剛剛',
    })

    // 存入 localStorage 達到與首頁卡片的資料同步
    localStorage.setItem(
      `luiu_mock_comments_${route.params.id}`,
      JSON.stringify(mockComments.value),
    )
    localStorage.setItem(
      `luiu_mock_comments_count_${route.params.id}`,
      mockComments.value.length.toString(),
    )

    newCommentText.value = '' // 清空輸入框
    isSubmittingComment.value = false

        // 捲動到底部
        window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' });
    }, 400); // 假裝 loading 0.4 秒
};

const handleCoverError = (e) => {
    e.target.src = logoUrl;
};

const handleEventImageError = (e) => {
    e.target.src = logoUrl;
};
</script>

<template>
  <div class="memory-detail-page bg-light min-vh-100 pb-5">
    <div class="hero-section position-relative">
      <!-- 返回首頁按鈕 -->
      <button
        class="btn btn-dark bg-opacity-50 text-white rounded-circle position-absolute top-0 start-0 m-4 d-flex align-items-center justify-content-center border-0 shadow-sm transition-all"
        style="width: 40px; height: 40px; z-index: 10"
        @click="goBack"
      >
        <i class="ri-arrow-left-line fs-5"></i>
      </button>

            <img :src="$img(memory.coverImage) || logoUrl" class="w-100 object-fit-cover" style="height: 300px; filter: brightness(0.7);"
                alt="Cover" @error="handleCoverError">
            <div class="position-absolute bottom-0 start-0 p-4 text-white">
                <div v-if="memory.destinations && memory.destinations.length > 0" class="d-flex gap-2 mb-2">
                    <span v-for="(dest, idx) in memory.destinations" :key="idx"
                        class="badge bg-light bg-opacity-25 text-white rounded-pill px-3">{{ dest }}</span>
                </div>
                <h1 class="fw-bold mb-2">{{ memory.title }}</h1>
                <p class="mb-0 opacity-75 small">
                    <i class="ri-calendar-line me-1"></i> {{ memory.date }} · {{ memory.stats.days }}天行程 · {{
                        memory.stats.locations }}個景點
                </p>
            </div>
        </div>

        <div class="container mt-4" style="max-width: 900px;">

            <div class="bg-white rounded-4 p-3 shadow-sm mb-4">
                <LuiuAvator :avatar="$img(memory.author.avatar)" :username="memory.author.name"
                    :subtitle="memory.author.postDate" actionText="收藏" size="md" />
            </div>

      <div class="row g-3 mb-4">
        <div class="col-4">
          <LuiuStatCard
            title="旅遊天數"
            :value="memory.stats.days"
            suffix=" 天"
            iconClass="ri-calendar-event-line"
            iconBgClass="bg-danger bg-opacity-10 text-danger"
          />
        </div>
        <div class="col-4">
          <LuiuStatCard
            title="景點數量"
            :value="memory.stats.locations"
            suffix=" 個"
            iconClass="ri-map-pin-line"
            iconBgClass="bg-primary bg-opacity-10 text-primary"
          />
        </div>
        <div class="col-4">
          <LuiuStatCard
            title="預估花費"
            :value="memory.stats.budget"
            prefix="NT$ "
            iconClass="ri-wallet-3-line"
            iconBgClass="bg-success bg-opacity-10 text-success"
          />
        </div>
      </div>

      <div class="d-flex gap-2 overflow-auto pb-2 mb-4 hide-scrollbar">
        <button
          v-for="d in memory.days"
          :key="d.day"
          class="btn rounded-4 px-4 py-2 text-center flex-shrink-0"
          :class="activeDay === d.day ? 'btn-dark' : 'bg-white border-0 text-muted shadow-sm'"
          @click="activeDay = d.day"
        >
          <span class="d-block fw-bold">{{ d.label }}</span>
          <small class="d-block" style="font-size: 0.75rem">{{ d.date }}</small>
        </button>
      </div>

      <div class="timeline-container ps-3 pt-3">
        <div v-if="currentDayEvents.length === 0" class="text-center text-muted py-5">
          <p>這天還沒有安排行程喔！</p>
        </div>

        <div
          v-for="event in currentDayEvents"
          v-else
          :key="event.id"
          class="timeline-item position-relative mb-5"
        >
          <div
            class="timeline-time-badge position-absolute bg-danger text-white rounded-pill small fw-bold px-2 py-1 shadow-sm"
          >
            {{ event.time }}
          </div>

          <div class="card border-0 shadow-sm rounded-4 ms-4 overflow-hidden">
            <div class="card-body p-4">
              <h5 class="fw-bold mb-1">{{ event.title }}</h5>
              <p class="text-muted small mb-3">
                <i class="ri-map-pin-line me-1"></i>{{ event.location }}
              </p>
              <p class="mb-3">{{ event.description }}</p>

              <!-- 如果有影片連結，優先使用播放器 -->
              <LuiuVideoPlayer
                v-if="event.videoUrl && isVideoUrl(event.videoUrl)"
                :src="event.videoUrl"
                class="w-100 rounded-3 overflow-hidden mb-3 border shadow-sm"
              />
              <a
                v-else-if="event.videoUrl"
                :href="event.videoUrl"
                target="_blank"
                class="d-block mb-3 text-primary text-truncate"
              >
                <i class="ri-link me-1"></i> {{ event.videoUrl }}
              </a>

                            <!-- 顯示所有圖片連結 -->
                            <div v-if="event.images && event.images.length > 0" class="d-flex flex-column gap-2 mb-3">
                                <img v-for="(imgUrl, idx) in event.images" :key="idx" :src="$img(imgUrl) || logoUrl"
                                    class="w-100 rounded-3 object-fit-cover" style="max-height: 350px;"
                                    alt="Event Image" @error="handleEventImageError">
                            </div>

              <div class="d-flex gap-2">
                <span class="badge bg-info bg-opacity-10 text-info rounded-pill px-3 py-2"
                  ><i class="ri-time-line me-1"></i>{{ event.duration }}</span
                >
                <span class="badge bg-info bg-opacity-10 text-info rounded-pill px-3 py-2"
                  ><i class="ri-money-dollar-circle-line me-1"></i>{{ event.cost }}</span
                >
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 💬 假留言板區塊開始 -->
      <div class="mt-5 pt-4 border-top">
        <h5 class="fw-bold mb-4">留言 ({{ mockComments.length }})</h5>

        <!-- 留言列表 -->
        <div class="d-flex flex-column gap-4 mb-4">
          <div v-for="comment in mockComments" :key="comment.id" class="d-flex gap-3">
            <img
              :src="comment.avatar"
              class="rounded-circle object-fit-cover"
              style="width: 40px; height: 40px"
              alt="User"
            />
            <div class="bg-white p-3 rounded-4 shadow-sm w-100">
              <div class="d-flex justify-content-between align-items-center mb-1">
                <span class="fw-bold text-dark" style="font-size: 0.9rem">{{
                  comment.author
                }}</span>
                <span class="text-muted" style="font-size: 0.75rem">{{ comment.time }}</span>
              </div>
              <p class="mb-0 text-dark" style="font-size: 0.95rem">{{ comment.text }}</p>
            </div>
          </div>
        </div>

        <!-- 輸入框 -->
        <div
          id="comment-input-section"
          class="d-flex gap-3 align-items-center bg-white p-3 rounded-pill shadow-sm mt-3"
        >
          <!-- 使用真實帳號的大頭貼 -->
          <img
            :src="
              userStore.userInfo?.avatarUrl ||
              'https://images.unsplash.com/photo-1633332755192-727a05c4013d?w=100&q=80'
            "
            class="rounded-circle object-fit-cover"
            style="width: 40px; height: 40px"
            alt="Me"
          />
          <input
            v-model="newCommentText"
            type="text"
            class="form-control border-0 shadow-none bg-transparent"
            placeholder="新增留言..."
            @keyup.enter="submitComment"
          />
          <button
            class="btn btn-dark rounded-pill px-4"
            :disabled="!newCommentText.trim() || isSubmittingComment"
            @click="submitComment"
          >
            <span
              v-if="isSubmittingComment"
              class="spinner-border spinner-border-sm me-1"
              role="status"
              aria-hidden="true"
            ></span>
            發佈
          </button>
        </div>
      </div>
      <!-- 💬 假留言板區塊結束 -->
    </div>
  </div>
</template>

<style scoped>
/* 隱藏頁籤的捲軸 */
.hide-scrollbar::-webkit-scrollbar {
  display: none;
}

.hide-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}

/* 垂直時間軸的線 */
.timeline-container {
  border-left: 2px solid #dee2e6;
  /* 淡紫/灰色的垂直線 */
  margin-left: 1.5rem;
}

/* 時間標籤定位 (蓋在線上) */
.timeline-time-badge {
  left: -40px;
  top: 0;
  z-index: 2;
  border: 3px solid #f8f9fa;
  /* 加上白邊讓他有挖空線條的感覺 */
}
</style>
