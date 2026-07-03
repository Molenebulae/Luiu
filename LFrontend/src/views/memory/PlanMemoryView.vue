<script setup>
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { createMemory, updateMemory } from '@/api/memory'; // 引入新增與更新 API

// 引入你的上傳與影片元件
import LuiuFileUploader from '@/components/base/LuiuFileUploader.vue';
import LuiuVideoPlayer from '@/components/base/LuiuVideoPlayer.vue';

const router = useRouter();

// 1. 從 localStorage 抓取上一頁(CreateMemoryView)存的草稿
const draft = localStorage.getItem('memory_draft');
const editId = ref(localStorage.getItem('memory_edit_id') || null);

const tripData = ref(draft ? JSON.parse(draft) : {
    title: '未命名行程',
    dateRange: '',
    coverImage: ''
});

//  2. 關鍵修復：把 activeDay 移到所有 computed 的最前面！
const activeDay = ref(1);

// 3. 接著才能寫依賴 activeDay 的推算邏輯
const totalDaysCount = computed(() => {
    const range = tripData.value.dateRange;
    if (!range) return tripData.value.totalDays || 5;

    let startDate, endDate;
    if (typeof range === 'string') {
        const parts = range.split(' ~ ');
        if (parts.length < 2) return 1;
        startDate = new Date(parts[0]);
        endDate = new Date(parts[1]);
    } else if (Array.isArray(range) && range.length >= 2) {
        startDate = new Date(range[0]);
        endDate = new Date(range[1]);
    } else {
        return 1;
    }
    return Math.ceil(Math.abs(endDate - startDate) / (1000 * 60 * 60 * 24)) + 1;
});

const currentDateDisplay = computed(() => {
    const range = tripData.value.dateRange;
    if (!range) return '日期未定';

    let startDate;
    if (typeof range === 'string') {
        startDate = new Date(range.split(' ~ ')[0]);
    } else if (Array.isArray(range) && range.length > 0) {
        startDate = new Date(range[0]);
    } else {
        return '日期未定';
    }

    if (isNaN(startDate.getTime())) return '日期未定';

    const targetDate = new Date(startDate);
    targetDate.setDate(startDate.getDate() + (activeDay.value - 1));

    const year = targetDate.getFullYear();
    const month = targetDate.getMonth() + 1;
    const date = targetDate.getDate();
    const weekdays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];

    return `${year}年${month}月${date}日 ${weekdays[targetDate.getDay()]}`;
});

// 4. 行程清單邏輯
const dailyEvents = ref(
    tripData.value.dailyEvents && Object.keys(tripData.value.dailyEvents).length > 0
        ? tripData.value.dailyEvents
        : { 1: [{ id: Date.now(), time: '09:00', title: '', location: '', duration: '1小時', description: '', expense: 0, rating: 5, imageUrls: [], videoUrl: '' }] }
);

const currentEvents = computed(() => {
    return dailyEvents.value[activeDay.value] || [];
});

const addEvent = () => {
    if (!dailyEvents.value[activeDay.value]) {
        dailyEvents.value[activeDay.value] = [];
    }
    dailyEvents.value[activeDay.value].push({
        id: Date.now(), time: '10:00', title: '', location: '', duration: '', description: '', expense: 0, rating: 0, imageUrls: [], videoUrl: ''
    });
};

const removeEvent = (id) => {
    if (dailyEvents.value[activeDay.value]) {
        dailyEvents.value[activeDay.value] = dailyEvents.value[activeDay.value].filter(e => e.id !== id);
    }
};

const handleImageUpload = (response, eventId) => {
    const imageUrl = response.data || response.url || response.filePath || response;
    const targetEvent = dailyEvents.value[activeDay.value].find(e => e.id === eventId);

    if (targetEvent) {
        if (!targetEvent.imageUrls) targetEvent.imageUrls = [];

        if (targetEvent.imageUrls.length < 3) {
            targetEvent.imageUrls.push(imageUrl);
        } else {
            alert('這個行程最多只能上傳 3 張照片喔！');
        }
    }
};

const removeImage = (eventId, indexToRemove) => {
    const targetEvent = dailyEvents.value[activeDay.value].find(e => e.id === eventId);
    if (targetEvent && targetEvent.imageUrls) {
        targetEvent.imageUrls.splice(indexToRemove, 1);
    }
};

const handleImport = () => {
    console.log(`準備把外部行程導入至 Day ${activeDay.value}`);
    alert('觸發導入行程功能！');
};

const handleBack = () => {
    router.back();
};

//  儲存後打 API 並跳轉回首頁
const handleSubmit = async () => {
    try {
        // 1. 處理日期格式
        let startDate = null;
        let endDate = null;
        if (typeof tripData.value.dateRange === 'string' && tripData.value.dateRange.includes(' ~ ')) {
            const parts = tripData.value.dateRange.split(' ~ ');
            startDate = parts[0];
            endDate = parts[1];
        } else if (Array.isArray(tripData.value.dateRange)) {
            startDate = tripData.value.dateRange[0];
            endDate = tripData.value.dateRange[1];
        }

        // 2. 組裝每天的景點 (Stops)
        const daysArray = [];
        for (let dayNum = 1; dayNum <= totalDaysCount.value; dayNum++) {
            const stopsForDay = dailyEvents.value[dayNum] || [];

            const formattedStops = stopsForDay.map(stop => ({
                title: stop.title || '未命名景點',
                time: stop.time ? `${stop.time}:00` : null, // 後端 TimeSpan 需要 hh:mm:ss 格式
                location: stop.location,
                duration: stop.duration,
                description: stop.description,
                expense: Number(stop.expense) || 0,
                rating: stop.rating,
                videoUrl: stop.videoUrl,
                imageUrls: stop.imageUrls || []
            }));

            daysArray.push({
                dayNumber: dayNum,
                stops: formattedStops
            });
        }

        // 3. 準備送給後端的 Payload
        const payload = {
            // userId 不再由前端傳送，後端從 JWT Token 自動解析
            title: tripData.value.title,
            coverImage: tripData.value.coverImage || 'P1.jpg', // 如果沒有封面圖片，預設給定 P1.jpg
            startDate: startDate,
            endDate: endDate,
            sourceTripId: tripData.value.sourceTripId || null,
            days: daysArray
        };

        console.log('準備送給後端的 Payload：', payload);

        // 4. 呼叫 API
        let response;
        if (editId.value) {
            response = await updateMemory(editId.value, payload);
        } else {
            response = await createMemory(payload);
        }

        // 注意：Axios 攔截器已經直接回傳了 res (也就是 response.data)
        // 所以 response 本身就是 { success: true, message: "...", data: {...} }
        if (response && response.success) {
            alert(editId.value ? '行程貼文更新成功！' : '行程貼文建立成功！');
            localStorage.removeItem('memory_draft'); // 成功後清除草稿
            localStorage.removeItem('memory_edit_id'); // 成功後清除編輯 ID
            router.push('/memory'); // 跳轉回 FrontPage (記憶首頁)
        } else {
            alert('建立失敗：' + (response?.message || '發生未知錯誤'));
        }

    } catch (error) {
        console.error('API 呼叫失敗：', error);
        alert('無法連線到伺服器或發生未預期的錯誤！');
    }
};
</script>

<template>
    <div class="luiu-plan-memory-container bg-light py-4">
        <div class="container" style="max-width: 800px;">

            <div class="card border-0 shadow-sm rounded-4 p-4 p-md-5 bg-white position-relative">

                <div class="d-flex justify-content-between align-items-center mb-4">
                    <h5 class="fw-bold mb-0" style="color: #1a2b4c;">{{ editId ? '編輯行程' : '建立新行程' }} - 規劃行程 ({{ tripData.title }})</h5>
                    <button class="btn btn-link text-muted p-0 text-decoration-none" @click="handleBack">
                        <i class="ri-close-line fs-4"></i>
                    </button>
                </div>

                <div class="d-flex gap-2 mb-4 overflow-auto pb-2 luiu-custom-scrollbar">
                    <button v-for="day in totalDaysCount" :key="day"
                        class="btn rounded-pill px-4 py-2 fw-bold transition-all text-nowrap"
                        :class="activeDay === day ? 'btn-primary luiu-navy-btn' : 'btn-light text-muted'"
                        @click="activeDay = day">
                        Day {{ day }}
                    </button>
                </div>

                <div class="d-flex flex-column flex-sm-row justify-content-between align-items-sm-end gap-3 mb-4">
                    <div>
                        <h4 class="fw-bold mb-1" style="color: #1a2b4c;">Day {{ activeDay }}</h4>
                        <small class="text-muted">{{ currentDateDisplay }}</small>
                    </div>

                    <div class="d-flex gap-2">
                        <button
                            class="btn bg-white text-primary rounded-pill px-3 shadow-sm fw-bold border border-primary transition-all luiu-outline-btn"
                            @click="handleImport">
                            <i class="ri-download-cloud-line me-1"></i> 導入行程
                        </button>
                        <button class="btn rounded-pill px-3 shadow-sm luiu-blue-btn text-white fw-bold border-0"
                            @click="addEvent">
                            <i class="ri-add-line me-1"></i> 新增行程
                        </button>
                    </div>
                </div>

                <div class="d-flex flex-column gap-4 mb-5">

                    <div v-if="currentEvents.length === 0"
                        class="bg-light rounded-4 d-flex flex-column align-items-center justify-content-center p-5 border"
                        style="border-style: dashed !important;">
                        <i class="ri-calendar-event-line text-muted mb-2" style="font-size: 3rem;"></i>
                        <p class="text-muted fw-bold">Day {{ activeDay }} 還沒有任何行程，點擊右上方按鈕新增或導入</p>
                    </div>

                    <div v-for="event in currentEvents" :key="event.id"
                        class="border rounded-4 p-4 position-relative bg-white shadow-sm">

                        <div class="d-flex justify-content-between align-items-center mb-4">
                            <div class="d-flex align-items-center gap-2">
                                <i class="ri-time-line text-muted"></i>
                                <input v-model="event.time" type="time"
                                    class="form-control form-control-sm border-0 bg-light fw-bold text-muted rounded-3 px-2"
                                    style="width: 100px;">
                            </div>
                            <button class="btn btn-link text-danger p-0 text-decoration-none"
                                @click="removeEvent(event.id)">
                                <i class="ri-delete-bin-line fs-5"></i>
                            </button>
                        </div>

                        <div class="mb-3">
                            <input v-model="event.title" type="text"
                                class="form-control bg-light border-0 p-3 rounded-3" placeholder="行程標題（例如：台北101觀景台）">
                        </div>

                        <div class="row g-3 mb-3">
                            <div class="col-md-6">
                                <input v-model="event.location" type="text"
                                    class="form-control bg-light border-0 p-3 rounded-3" placeholder="地點名稱">
                            </div>
                            <div class="col-md-6">
                                <input v-model="event.duration" type="text"
                                    class="form-control bg-light border-0 p-3 rounded-3" placeholder="1小時">
                            </div>
                        </div>

                        <div class="mb-3">
                            <textarea v-model="event.description" rows="3"
                                class="form-control bg-light border-0 p-3 rounded-3" placeholder="描述這個行程..."></textarea>
                        </div>

                        <div class="row g-3 mb-4">
                            <div class="col-md-6">
                                <label class="form-label small fw-bold text-muted"><i
                                        class="ri-money-dollar-circle-line me-1"></i>費用 (NT$)</label>
                                <input v-model.number="event.expense" type="number"
                                    class="form-control bg-light border-0 p-3 rounded-3" placeholder="0">
                            </div>
                            <div class="col-md-6">
                                <label class="form-label small fw-bold text-muted"><i
                                        class="ri-star-line me-1"></i>評分</label>
                                <input v-model="event.rating" type="number" max="5" min="0"
                                    class="form-control bg-light border-0 p-3 rounded-3" placeholder="5">
                            </div>
                        </div>

                        <div class="mb-4">
                            <div class="d-flex justify-content-between align-items-end mb-2">
                                <label class="form-label small fw-bold text-muted mb-0"><i
                                        class="ri-image-line me-1"></i>景點照片</label>
                                <small class="text-muted" style="font-size: 0.75rem;">
                                    已上傳 {{ event.imageUrls ? event.imageUrls.length : 0 }} / 3 張
                                </small>
                            </div>

                            <div v-if="event.imageUrls && event.imageUrls.length > 0"
                                class="d-flex flex-wrap gap-3 mb-2">
                                <div v-for="(imgUrl, imgIdx) in event.imageUrls" :key="imgIdx"
                                    class="position-relative rounded-4 overflow-hidden shadow-sm border"
                                    style="width: 120px; height: 120px;">
                                    <img :src="imgUrl" class="w-100 h-100 object-fit-cover" alt="Event Image">
                                    <button
                                        class="btn btn-danger btn-sm position-absolute top-0 end-0 m-1 rounded-circle shadow d-flex align-items-center justify-content-center"
                                        style="width: 26px; height: 26px; padding: 0;"
                                        @click="removeImage(event.id, imgIdx)">
                                        <i class="ri-close-line" style="font-size: 0.9rem;"></i>
                                    </button>
                                </div>
                            </div>

                            <LuiuFileUploader v-if="!event.imageUrls || event.imageUrls.length < 3" title="上傳景點照片"
                                :maxFiles="3 - (event.imageUrls ? event.imageUrls.length : 0)" uploadType="memory"
                                @upload-success="(res) => handleImageUpload(res, event.id)" />
                        </div>

                        <div class="pt-3 border-top">
                            <label class="form-label small fw-bold text-muted mb-2"><i
                                    class="ri-video-line me-1"></i>景點影片</label>
                            <input v-model="event.videoUrl" type="text"
                                class="form-control bg-light border-0 p-3 rounded-3 mb-3"
                                placeholder="貼上影片網址 (例如 YouTube 連結或 MP4)">

                            <LuiuVideoPlayer v-if="event.videoUrl" :src="event.videoUrl"
                                class="w-100 rounded-4 overflow-hidden shadow-sm border" />
                        </div>

                    </div>
                </div>

                <div class="d-flex gap-3 mt-4">
                    <button class="btn btn-light rounded-pill w-50 py-3 fw-bold border text-muted" @click="handleBack">
                        上一步 / 取消
                    </button>
                    <button class="btn rounded-pill w-50 py-3 text-white fw-bold shadow-sm luiu-blue-btn border-0"
                        @click="handleSubmit">
                        {{ editId ? '儲存更新' : '儲存行程貼文' }}
                    </button>
                </div>

            </div>
        </div>
    </div>
</template>

<style scoped>
.luiu-navy-btn {
    background-color: #1a2b4c !important;
    border-color: #1a2b4c !important;
    color: white !important;
}

.luiu-blue-btn {
    background-color: #2b5fe8;
    transition: all 0.3s ease;
}

.luiu-blue-btn:hover {
    background-color: #1e45b2;
    transform: translateY(-2px);
}

.luiu-outline-btn:hover {
    background-color: #e8f0fe !important;
}

.form-control:focus {
    box-shadow: none;
    border: 1px solid #2b5fe8 !important;
    background-color: #fff !important;
}

.luiu-custom-scrollbar::-webkit-scrollbar {
    display: none;
}

.luiu-custom-scrollbar {
    -ms-overflow-style: none;
    scrollbar-width: none;
}
</style>