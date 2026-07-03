<script setup>
import LuiuSidebar from '@/components/layout/LuiuSidebar.vue';
import LuiuFooter from '@/components/layout/LuiuFooter.vue';
</script>

<template>
    <div class="d-flex flex-row flex-nowrap vh-100 w-100 bg-light overflow-hidden">
        
        <LuiuSidebar />

        <div class="main-content-wrapper flex-grow-1 overflow-y-auto d-flex flex-column" style="min-width: 0;">
            <main class="flex-grow-1">
                <router-view />
            </main>

            <LuiuFooter />
        </div>

    </div>
</template>

<style scoped>
/* 隱藏外層多餘卷軸的防呆設定 */
.overflow-hidden {
    overflow: hidden;
}

/* 手機版：因為側邊欄變成了底部導航列，必須在主要內容區塊最下方預留空間 */
@media (max-width: 767.98px) {
    .main-content-wrapper {
        padding-bottom: 75px !important;
    }
}
</style>