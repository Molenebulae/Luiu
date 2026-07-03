<script setup>
import { onMounted, onUnmounted, ref } from 'vue'

const props = defineProps({
  banners: {
    type: Array,
    required: true,
    default: () => []
  }
})

const carouselRef = ref(null)
let carouselInstance = null

onMounted(async () => {
  const { Carousel } = await import('bootstrap')

  if (carouselRef.value && props.banners.length > 0) {
    carouselInstance = new Carousel(carouselRef.value, {
      interval: 4000, // 自動輪播間隔 4000 毫秒
      wrap: true      // 循環輪播
    })

    // 核心修正：手動啟動自動輪播計時器
    carouselInstance.cycle()
  }
})

onUnmounted(() => {
  if (carouselInstance) {
    carouselInstance.dispose()
  }
})

const prevSlide = () => {
  if (carouselInstance) carouselInstance.prev()
}

const nextSlide = () => {
  if (carouselInstance) carouselInstance.next()
}
</script>

<template>
  <section class="hero-carousel-section py-4 overflow-hidden position-relative has-side-masks">
    <div class="container-fluid px-0">

      <div id="topBannerCarousel" ref="carouselRef" class="carousel slide custom-carousel">
        <div class="carousel-inner">
          <div v-for="(banner, idx) in props.banners" :key="banner.id || idx" class="carousel-item"
            :class="{ active: idx === 0 }">
            <div class="banner-card-wrapper mx-auto">
              <div class="banner-card rounded-4 shadow-sm overflow-hidden position-relative border-0">
                <img :src="banner.imageUrl" class="w-100 h-100 object-fit-cover" :alt="banner.title" />

                <div class="banner-caption">
                  <h4 class="fw-bold text-white mb-0">{{ banner.title }}</h4>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 操控按鈕 -->
        <button class="carousel-control-prev" type="button" @click="prevSlide">
          <span class="carousel-control-prev-icon bg-dark rounded-circle p-3" aria-hidden="true"></span>
          <span class="visually-hidden">Previous</span>
        </button>
        <button class="carousel-control-next" type="button" @click="nextSlide">
          <span class="carousel-control-next-icon bg-dark rounded-circle p-3" aria-hidden="true"></span>
          <span class="visually-hidden">Next</span>
        </button>
      </div>

    </div>
  </section>
</template>

<style lang="scss" scoped>
.hero-carousel-section {
  background-color: $chictrip-bg-color !important;
}

/* 左右固定不動的半透明淡化遮罩 */
.has-side-masks {

  &::before,
  &::after {
    content: "";
    position: absolute;
    top: 0;
    bottom: 0;
    width: $mask-width;
    z-index: 10;
    pointer-events: none;
  }

  &::before {
    left: 0;
    background: linear-gradient(to right, rgba($chictrip-bg-color, 1) 0%, rgba($chictrip-bg-color, 0) 100%);
  }

  &::after {
    right: 0;
    background: linear-gradient(to left, rgba($chictrip-bg-color, 1) 0%, rgba($chictrip-bg-color, 0) 100%);
  }
}

/* 核心修正：還原 Bootstrap 結構，並透過 3D 加速實現無縫 */
.custom-carousel {
  overflow: visible !important;
  z-index: 5;

  .carousel-inner {
    overflow: visible !important;
    // 絕對不能加 display: flex，維持 Bootstrap 預設的 block 與 position 規則
  }

  .carousel-item {
    // 確保動畫移動時，左右兩側被排擠出去的卡片不會因為渲染硬體加速問題而被切掉
    backface-visibility: hidden;
    perspective: 1000px;
    persisted-transform: translate3d(0, 0, 0);
  }
}

// 調整卡片寬度，搭配 Bootstrap 的 mx-auto 置中
.banner-card-wrapper {
  width: 85%;
  max-width: 1100px;
  padding: 0 12px;
}

.banner-card {
  height: 360px;
  background-color: #fff;
}

// 響應式斷點
@media (max-width: 576px) {
  .banner-card {
    height: 160px;
  }

  .banner-card-wrapper {
    width: 90%;
  }
}

.banner-caption {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 20px;
  background: linear-gradient(transparent, rgba(0, 0, 0, 0.5));
  display: flex;
  align-items: flex-end;
}

// 確保箭頭在遮罩上方
.carousel-control-prev {
  left: 3%;
  width: 5%;
  z-index: 15;
}

.carousel-control-next {
  right: 3%;
  width: 5%;
  z-index: 15;
}
</style>
