 <script setup>
import { onMounted, onUnmounted, ref } from 'vue';
import Plyr from 'plyr';
import 'plyr/dist/plyr.css';

const props = defineProps({
    src: { type: String, default: 'https://cdn.plyr.io/static/demo/Kishi_Bashi_-_It_All_Began_With_a_Burst.mp3' },
    title: { type: String, default: 'Audio Player' }
});

const audioElement = ref(null);
let player = null;

onMounted(() => { player = new Plyr(audioElement.value); });
onUnmounted(() => { if (player) player.destroy(); });
</script>

<template>
    <div class="card border-0 shadow-sm rounded-4 overflow-hidden">
        <div class="card-header bg-transparent border-0 p-4 pb-0" v-if="title">
            <h5 class="card-title mb-0 fw-bold">{{ title }}</h5>
        </div>
        <div class="card-body p-4">
            <audio ref="audioElement" controls>
                <source :src="src" type="audio/mpeg" />
            </audio>
        </div>
    </div>
</template>