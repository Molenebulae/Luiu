<script setup>
import { onMounted, onUnmounted, ref, computed, watch, nextTick } from 'vue';
import Plyr from 'plyr';
import 'plyr/dist/plyr.css';

const props = defineProps({
    videoId: { type: String, default: null },
    src: { type: String, default: null },
    title: { type: String, default: null },
    showInput: { type: Boolean, default: false }
});

const videoContainer = ref(null);
let player = null;

// 解析 YouTube ID 的函式，支援各種常見的 YouTube 網址與純 ID
const parseYoutubeId = (urlOrId) => {
    if (!urlOrId) return '';
    // 若本身就是 11 碼的 YouTube ID
    if (/^[a-zA-Z0-9_-]{11}$/.test(urlOrId)) {
        return urlOrId;
    }
    // 比對各種 YouTube 網址格式 (包含 watch?v=, embed/, youtu.be/)
    const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=)([^#\&\?]*).*/;
    const match = urlOrId.match(regExp);
    return (match && match[2].length === 11) ? match[2] : '';
};

// 判定影片來源與格式
const videoData = computed(() => {
    const source = props.src || props.videoId || '';
    if (!source) {
        // 預設 fallback ID
        return { type: 'youtube', id: '2RZQN_ko0iU', url: 'https://www.youtube.com/embed/2RZQN_ko0iU?origin=' + window.location.origin + '&iv_load_policy=3&modestbranding=1&playsinline=1&enablejsapi=1' };
    }
    
    const ytId = parseYoutubeId(source);
    if (ytId) {
        return {
            type: 'youtube',
            id: ytId,
            url: `https://www.youtube.com/embed/${ytId}?origin=${window.location.origin}&iv_load_policy=3&modestbranding=1&playsinline=1&enablejsapi=1`
        };
    }
    
    // 如果是直連影片網址 (如 mp4, webm, ogg)
    return {
        type: 'direct',
        id: null,
        url: source
    };
});

// 初始化播放器的函式
const initPlayer = () => {
    if (player) {
        player.destroy();
        player = null;
    }
    if (videoContainer.value) {
        player = new Plyr(videoContainer.value, {
            youtube: { noCookie: false, rel: 0, showinfo: 0, iv_load_policy: 3, modestbranding: 1 }
        });
    }
};

onMounted(() => { 
    initPlayer();
});

onUnmounted(() => { 
    if (player) {
        player.destroy();
    }
});

// 當網址變更時，重新初始化播放器以載入新影片
watch(() => videoData.value.url, async () => {
    await nextTick();
    initPlayer();
});
</script>

<template>
    <div class="video-player-wrapper p-3">
        <!-- YouTube 嵌入式播放器 -->
        <div v-if="videoData.type === 'youtube'" ref="videoContainer" class="plyr__video-embed rounded-3 overflow-hidden">
            <iframe 
                :src="videoData.url" 
                allowfullscreen 
                allowtransparency 
                allow="autoplay"
            ></iframe>
        </div>
        <!-- 原生 HTML5 播放器 (支援 MP4/WebM 等直連影片) -->
        <div v-else class="rounded-3 overflow-hidden">
            <video ref="videoContainer" playsinline controls class="w-100">
                <source :src="videoData.url" type="video/mp4" />
            </video>
        </div>
    </div>
</template>

<style scoped>
.video-player-wrapper {
    background: #000;
}
.btn-primary {
    background-color: #5b73e8;
    border-color: #5b73e8;
}
.input-group .form-control:focus {
    box-shadow: none;
    background-color: #f1f3fe !important;
}
</style>