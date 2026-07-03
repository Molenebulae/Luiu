<script setup>
import { computed } from 'vue';

const props = defineProps({
   Message: { type: [Number, String], default: 0 },
    badgeClass: { type: String, default: 'bg-danger' },
    // 支援：'top-right' (右上角), 'bottom-right' (右下角), 'inline' (不懸浮，緊貼右側)
    position: { type: String, default: 'top-right' }
});

const positionClass = computed(() => {
    switch (props.position) {
        case 'top-right': return 'luiu-badge-top-right';
        case 'bottom-right': return 'luiu-badge-bottom-right';
        case 'inline':
        default: return 'luiu-badge-inline';
    }
});
</script>

<template>
    <div class="luiu-badge-wrapper">
        <slot></slot>

        <span 
            v-if="Message" 
            class="badge rounded-pill"
            :class="[badgeClass, positionClass]"
        >
            {{ Message }}
        </span>
    </div>
</template>

<style scoped>
.luiu-badge-wrapper {
    position: relative;
    display: inline-flex;
    align-items: center;
}

/*  懸浮 Badge 的共用設定 */
.luiu-badge-top-right,
.luiu-badge-bottom-right {
    position: absolute;
    z-index: 10;
    /*  UI 魔法：加上 2px 的白邊，重疊時才會有「呼吸感」不會糊在一起 */
    border: 2px solid #ffffff; 
}

/*  右上角：微調 translate，讓它「緊貼」角落，而不是飛出去 */
.luiu-badge-top-right {
    top: 0;
    right: 0;
    transform: translate(35%, -35%); 
}

/* 右下角 (適合頭像狀態)：緊貼右下角 */
.luiu-badge-bottom-right {
    bottom: 0;
    right: 0;
    transform: translate(20%, 20%);
}

/*  跟在右邊的文字 (加大一點 margin 避免黏太緊) */
.luiu-badge-inline {
    margin-left: 0.75rem;
}
</style>