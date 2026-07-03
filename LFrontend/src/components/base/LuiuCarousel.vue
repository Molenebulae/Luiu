<script setup>
// 💡 1. 在最上面，直接把 src/assets/Images 裡的本地圖片 import 進來
// （請把 .jpg 換成你實際的檔名與副檔名）
import defaultImg1 from '@/assets/Images/P1.jpg';
import defaultImg2 from '@/assets/Images/P2.jpg';
import defaultImg3 from '@/assets/Images/P3.jpg';

const props = defineProps({
    images: {
        type: Array,
        // 💡 2. 直接把剛剛 import 的變數，當作沒有傳入資料時的預設值
        default: () => [
            { src: defaultImg1, alt: '翠綠山巒導覽' },
            { src: defaultImg2, alt: '湛藍海灘假期' },
            { src: defaultImg3, alt: '星空露營體驗' }
        ]
    },
    interval: { 
        type: Number, 
        default: 3000 
    }
});

const carouselId = 'luiu-carousel-' + Math.random().toString(36).substring(2, 9);
</script>

<template>
    <div :id="carouselId" class="carousel slide" data-bs-ride="carousel" :data-bs-interval="props.interval">
        <div class="carousel-inner shadow-sm rounded-4">
            <div v-for="(img, idx) in props.images" :key="idx" class="carousel-item" :class="{ 'active': idx === 0 }">
                <img :src="img.src" class="d-block w-100 luiu-carousel-img" :alt="img.alt">
            </div>
        </div>
        
        <template v-if="props.images.length > 1">
            <button class="carousel-control-prev" type="button" :data-bs-target="`#${carouselId}`" data-bs-slide="prev">
                <span class="carousel-control-prev-icon"></span>
            </button>
            <button class="carousel-control-next" type="button" :data-bs-target="`#${carouselId}`" data-bs-slide="next">
                <span class="carousel-control-next-icon"></span>
            </button>
        </template>
    </div>
</template>

<style scoped>
.luiu-carousel-img { 
    height: 400px; 
    object-fit: cover; 
}
</style>