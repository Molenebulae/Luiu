<script setup>
/*
載入範例 js
import Swiper from 'swiper';

css
import 'swiper/css';
import 'swiper/css/navigation';
import 'swiper/css/pagination';
*/

import { onMounted } from 'vue';
import { LuiuAlert } from '@/utils/sweetAlert';  // 載入套件
 
const handleDelete = () => {
    LuiuAlert.fire({
        title: "你確定嗎？",
        text: "刪除後資料將無法恢復！",
        icon: "error",
        showCancelButton: true,
        confirmButtonText: "是的，刪除它！",
        cancelButtonText: "取消"
    }).then((result) => {
        if (result.isConfirmed) {
            LuiuAlert.fire("已刪除！", "該筆資料已從 Luiu 系統移除。", "success");
        }
    });
};

onMounted(() => { });
</script>

<template>
    <!-- 這只是範例 要刪掉-->
    <div>
        元件內容1
        <slot></slot>
        元件內容2
    </div>

    <div class="col-md-6 col-xl-4 col-xxl-3">
        <div class="card">
            <div class="card-header">
                <h5 class="card-title mb-0">
                    Error Message
                </h5>
            </div>
            <div class="card-body">
                <p class="text-muted">
                    Show an error message with a title and footer
                </p>
                <button class="btn btn-primary" @click="handleDelete">
                    Click Me
                </button>
            </div>
        </div>
    </div>
</template>

<style scoped>
/*
如果這個 component 只有樣式的修改的話 直接建一個專屬的 class 之後使用就直接使用這個 class 就好
.luiu-button {
    color: 'red'
}
*/
</style>
