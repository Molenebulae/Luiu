<script setup>
import { ref } from 'vue'
import HeroCarousel from '@/components/home/HeroCarousel.vue'
import PlanCarousel from '@/components/home/PlanCarousel.vue'
import MemoryCarousel from '@/components/home/MemoryCarousel.vue'

// 僅保留傳遞給元件的 Banner 原始資料
const originalBanners = ref([
  {
    id: 1,
    title: '探索台北之美',
    imageUrl:
      'https://plus.unsplash.com/premium_photo-1661963920742-f5b23a6a1b44?auto=format&fit=crop&w=1200&q=80',
  },
  {
    id: 2,
    title: '漫步神隱九份山城',
    imageUrl:
      'https://images.unsplash.com/photo-1571474004502-c1def214ac6d?auto=format&fit=crop&w=1200&q=80',
  },
  {
    id: 3,
    title: '尋訪台東三仙台景',
    imageUrl:
      'https://images.pexels.com/photos/17655550/pexels-photo-17655550.jpeg?auto=compress&cs=tinysrgb&w=1200',
  },
  {
    id: 4,
    title: '體驗熱鬧夜市文化',
    imageUrl:
      'https://images.unsplash.com/photo-1552993873-0dd1110e025f?auto=format&fit=crop&w=1200&q=80',
  },
  {
    id: 5,
    title: '祈福左營龍虎雙塔',
    imageUrl:
      'https://plus.unsplash.com/premium_photo-1661955467302-bd779d9eae00?auto=format&fit=crop&w=1200&q=80',
  },
])

// 其餘靜態資料保持不變
const features = ref([
  {
    title: '智慧排序 聰明避開繞路',
    description:
      '只要加入想去的景點，即可輕鬆排好最順路線。不管是自由行、一日遊還是多日旅程，讓行程規劃變得更有效率！',
    icon: 'bi-signpost-split-fill', // 換成更符合路線排序的圖示
    bgClass: 'bg-warning-subtle',
    textClass: 'text-warning',
  },
  {
    title: '多人共編 旅遊小工具',
    description: '和朋友一起共編行程、分工查資料，行程時間直接同步。內建景點筆記，討論不發散。',
    icon: 'bi-people-fill',
    bgClass: 'bg-primary-subtle',
    textClass: 'text-primary',
  },
  {
    title: '彈性調整 掌握旅行節奏',
    description:
      '隨時拖曳即可調整景點順序與停留時間，完美串聯每個行程片段，讓你從容應對旅途中的各種突發狀況。',
    icon: 'bi-calendar-week-fill', // 換成符合時間與行程管理的圖示
    bgClass: 'bg-success-subtle',
    textClass: 'text-success',
  },
])

// 🏆 提供給 PlanCarousel 元件的 10 筆官方精選行程假資料
const recommendedPlans = ref([
  {
    id: 201,
    title: '台灣南部秘境鄉村鐵道二日遊',
    tag: '鐵道漫遊',
    userName: '旅行者小安gfhfhgfhdfghgfhdfghgfhhghdfghfh',
    durationDays: 2,
    spotCount: 6,
    coverUrl:
      'https://images.unsplash.com/photo-1542640244-7e672d6cef21?w=600&auto=format&fit=crop&q=80',
  },
  {
    id: 202,
    title: '花蓮太魯閣大自然療癒充電行程',
    tag: '山林戶外',
    userName: '戶外咖阿健',
    durationDays: 3,
    spotCount: 8,
    coverUrl:
      'https://images.unsplash.com/photo-1549693578-d683be217e58?w=600&auto=format&fit=crop&q=80',
  },
  {
    id: 203,
    title: '台南美食暴食不踩雷終極大計畫',
    tag: '在地小吃',
    userName: '美食大師老饕饗宴之夜', // 測試超長人名截斷
    durationDays: 2,
    spotCount: 15,
    coverUrl:
      'https://images.unsplash.com/photo-1552993873-0dd1110e025f?w=600&auto=format&fit=crop&q=80',
  },
  {
    id: 204,
    title: '台北捷運文青一日輕旅行：咖啡廳與古蹟巡禮', // 測試超長標題截斷
    tag: '城市散策',
    userName: '台北地陪',
    durationDays: 1,
    spotCount: 5,
    coverUrl:
      'https://plus.unsplash.com/premium_photo-1661963920742-f5b23a6a1b44?w=600&auto=format&fit=crop&q=80',
  },
  {
    id: 205,
    title: '漫步神隱九份山城與金瓜石淘金探險',
    tag: '懷舊山城',
    userName: '阿明大叔',
    durationDays: 2,
    spotCount: 7,
    coverUrl:
      'https://images.unsplash.com/photo-1571474004502-c1def214ac6d?w=600&auto=format&fit=crop&q=80',
  },
  {
    id: 206,
    title: '高雄左營龍虎塔與蓮池潭深度文化體驗',
    tag: '傳統廟宇',
    userName: '打狗囡仔',
    durationDays: 1,
    spotCount: 4,
    coverUrl:
      'https://plus.unsplash.com/premium_photo-1661955467302-bd779d9eae00?w=600&auto=format&fit=crop&q=80',
  },
  {
    id: 207,
    title: '墾丁夏日熱血衝浪與放鬆海景四天三夜',
    tag: '度假勝地',
    userName: 'Sunny',
    durationDays: 4,
    spotCount: 11,
    coverUrl:
      'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=600&auto=format&fit=crop&q=80',
  },
  {
    id: 208,
    title: '阿里山日出雲海與森林鐵道慢活療癒之旅',
    tag: '高山避暑',
    userName: '芬多精狂熱者',
    durationDays: 3,
    spotCount: 9,
    coverUrl:
      'https://images.unsplash.com/photo-1588614959160-e8b3b44b25df?w=600&auto=format&fit=crop&q=80',
  },
  {
    id: 209,
    title: '台東三仙台與伯朗大道騎行慢遊計畫',
    tag: '東岸風情',
    userName: '漂流木',
    durationDays: 3,
    spotCount: 10,
    coverUrl:
      'https://images.unsplash.com/photos/17655550/pexels-photo-17655550.jpeg?w=600&auto=compress&cs=tinysrgb',
  },
  {
    id: 210,
    title: '宜蘭礁溪溫泉與蔥油餅 DIY 親子放鬆兩日遊',
    tag: '親子同樂',
    userName: '小咪媽咪',
    durationDays: 2,
    spotCount: 8,
    coverUrl:
      'https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=600&auto=format&fit=crop&q=80',
  },
])
</script>

<template>
  <div class="chictrip-landing">
    <HeroCarousel :banners="originalBanners" />

    <section id="features" class="py-5 bg-light">
      <div class="container py-4">
        <div class="text-center max-w-600 mx-auto mb-5">
          <h2 class="fw-bold mb-3">為什麼大家都選擇 Luiu？</h2>
          <p class="text-muted">告別雜亂的景點碎片，用最直覺的方式串起你們的專屬精彩旅程</p>
        </div>

        <div class="row g-4">
          <div v-for="(feature, index) in features" :key="index" class="col-md-4">
            <div class="card h-100 border-0 shadow-sm p-4 rounded-4 hover-translate">
              <div
                class="icon-square rounded-3 mb-4 d-flex align-items-center justify-content-center"
                :class="feature.bgClass"
              >
                <i class="bi fs-3" :class="[feature.icon, feature.textClass]"></i>
              </div>
              <h5 class="fw-bold mb-3">{{ feature.title }}</h5>
              <p class="text-muted small mb-0 lh-base">{{ feature.description }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- <section id="community" class="py-5 bg-white">
      <div class="container py-4">
        <div class="row align-items-center g-5">
          <div class="col-lg-5 order-lg-2 text-center text-lg-start">
            <span class="text-brand-blue fw-bold d-block mb-2"># 百萬旅人互動</span>
            <h2 class="fw-bold mb-3">最直覺的社群動態牆</h2>
            <p class="text-muted mb-4">
              看看大家最近都去哪玩、怎麼玩！真實旅人分享精美行程攻略、景點影音與美食探店紀錄。看到心動的，直接複製行程，靈感立刻變計畫！
            </p>
            <a href="#download" class="btn btn-brand-yellow text-dark rounded-pill px-4 py-2 fw-bold shadow-sm">
              探索熱門行程 <i class="bi bi-arrow-right ms-1"></i>
            </a>
          </div>
          <div class="col-lg-7 order-lg-1">
            <div class="row g-3">
              <div v-for="(post, idx) in communityPosts" :key="idx" class="col-sm-6"
                :class="{ 'mt-sm-5': idx === 1 || idx === 3 }">
                <div class="card border-0 shadow-sm rounded-4 overflow-hidden">
                  <div class="bg-secondary position-relative text-white d-flex align-items-end p-3" :style="{
                    height: '200px',
                    backgroundImage: `linear-gradient(to bottom, rgba(0,0,0,0) 50%, rgba(0,0,0,0.7) 100%), url(${post.image})`,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                  }">
                    <div>
                      <span class="badge bg-blur mb-2">{{ post.tag }}</span>
                      <h6 class="fw-bold mb-0 text-white text-truncate-2">{{ post.title }}</h6>
                    </div>
                  </div>
                  <div class="card-body p-3 d-flex align-items-center justify-content-between">
                    <div class="d-flex align-items-center">
                      <div class="bg-warning rounded-circle me-2" style="width: 28px; height: 28px"></div>
                      <span class="small fw-bold text-dark">{{ post.author }}</span>
                    </div>
                    <span class="text-muted small"><i class="bi bi-heart-fill text-danger me-1"></i>{{ post.likes
                      }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section> -->

    <div class="chictrip-landing">
      <!-- <section class="w-full text-left">
        <div class="d-flex justify-content-between align-items-end mb-4">
          <div>
            <span class="text-brand-blue fw-bold d-block mb-1"># 官方精選</span>
            <h3 class="fw-bold mb-0">推薦行程規劃</h3>
          </div>
          <div class="slider-arrows">
            <button class="btn btn-outline-secondary btn-sm rounded-circle me-2"
              @click="scrollSlider('plan-slider', 'left')">
              <i class="bi bi-chevron-left"></i>
            </button>
            <button class="btn btn-outline-secondary btn-sm rounded-circle"
              @click="scrollSlider('plan-slider', 'right')">
              <i class="bi bi-chevron-right"></i>
            </button>
          </div>
        </div> -->

      <PlanCarousel :plans="recommendedPlans" />
      <!-- </section> -->
      <MemoryCarousel />
      <!-- <section class="w-full text-left">
        <div class="border-l-4 border-emerald-600 pl-3 mb-4">
          <h2 class="text-2xl font-bold text-gray-800">旅遊回憶分享</h2>
          <p class="text-sm text-gray-500 mt-1">紀錄旅途中的點點滴滴與美好瞬間</p>
        </div>


      </section> -->
    </div>
  </div>
</template>

<style scoped>
/* 其他品牌樣式 */
.bg-brand-yellow {
  background-color: #ffd600 !important;
}

.text-brand-blue {
  color: #007bff !important;
}

.bg-brand-blue {
  background-color: #007bff !important;
}

.btn-brand-yellow {
  background-color: #ffd600;
  border-color: #ffd600;
  transition: all 0.2s ease-in-out;
}

.btn-brand-yellow:hover {
  background-color: #e5bf00;
  transform: translateY(-2px);
}

.hover-translate {
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
}

.hover-translate:hover {
  transform: translateY(-5px);
  box-shadow: 0 1rem 3rem rgba(0, 0, 0, 0.1) !important;
}

.icon-square {
  width: 56px;
  height: 56px;
}

.bg-blur {
  background: rgba(255, 255, 255, 0.25);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
  color: white;
}

.max-w-600 {
  max-width: 600px;
}

.text-truncate-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
