<script setup>
import { ref, computed } from 'vue';
import LuiuFileUploader from '@/components/base/LuiuFileUploader.vue';
import LuiuVideoPlayer from '@/components/base/LuiuVideoPlayer.vue';
import { getPlanList, getPlan, asArrayPayload, unwrapPlanPayload } from '@/api/planning/plan';

const props = defineProps({
    modelValue: {
        type: Object,
        required: true
    },
    totalDaysCount: {
        type: Number,
        default: 1
    },
    userId: {
        type: String,
        required: true
    }
});

const emit = defineEmits(['update:modelValue']);

// Local Helper to update the form safely
const updateForm = (key, value) => {
    emit('update:modelValue', { ...props.modelValue, [key]: value });
};

// State
const activeDay = ref(1);
const showImportModal = ref(false);
const planList = ref([]);
const isFetchingPlans = ref(false);

const currentDateDisplay = computed(() => {
    const range = props.modelValue.dateRange;
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

const currentEvents = computed(() => {
    return props.modelValue.dailyEvents[activeDay.value] || [];
});

const addEvent = () => {
    const newEventsMap = { ...props.modelValue.dailyEvents };
    if (!newEventsMap[activeDay.value]) {
        newEventsMap[activeDay.value] = [];
    }
    newEventsMap[activeDay.value].push({
        id: crypto.randomUUID(), time: '10:00', title: '', location: '', duration: '', description: '', expense: 0, rating: 0, imageUrls: [], videoUrl: ''
    });
    updateForm('dailyEvents', newEventsMap);
};

const removeEvent = (id) => {
    const newEventsMap = { ...props.modelValue.dailyEvents };
    if (newEventsMap[activeDay.value]) {
        newEventsMap[activeDay.value] = newEventsMap[activeDay.value].filter(e => e.id !== id);
        updateForm('dailyEvents', newEventsMap);
    }
};

const handleImageUpload = (response, eventId) => {
    const imageUrl = response.data || response.url || response.filePath || response;
    const newEventsMap = { ...props.modelValue.dailyEvents };
    const targetEvent = newEventsMap[activeDay.value].find(e => e.id === eventId);
    if (targetEvent) {
        if (!targetEvent.imageUrls) targetEvent.imageUrls = [];
        if (targetEvent.imageUrls.length < 3) {
            targetEvent.imageUrls.push(imageUrl);
            updateForm('dailyEvents', newEventsMap);
        } else {
            alert('這個行程最多只能上傳 3 張照片喔！');
        }
    }
};

const removeImage = (eventId, indexToRemove) => {
    const newEventsMap = { ...props.modelValue.dailyEvents };
    const targetEvent = newEventsMap[activeDay.value].find(e => e.id === eventId);
    if (targetEvent && targetEvent.imageUrls) {
        targetEvent.imageUrls.splice(indexToRemove, 1);
        updateForm('dailyEvents', newEventsMap);
    }
};

const openLocalImportModal = async () => {
    if (!props.userId) { alert('請先登入'); return; }
    showImportModal.value = true;
    isFetchingPlans.value = true;
    try {
        const payload = await getPlanList(props.userId);
        planList.value = asArrayPayload(payload);
    } catch (e) {
        console.error('獲取行程列表失敗:', e);
    } finally {
        isFetchingPlans.value = false;
    }
};

const importPlanToDay = async (tripId) => {
    try {
        const payload = await getPlan(props.userId, tripId);
        const planData = unwrapPlanPayload(payload);

        const newEvents = [];
        if (planData.TripDetails && Array.isArray(planData.TripDetails)) {
            planData.TripDetails.forEach(detail => {
                newEvents.push({
                    id: detail.DetailID || Date.now() + Math.random(),
                    time: detail.ArrivalTime ? detail.ArrivalTime.substring(0, 5) : '09:00',
                    title: detail.SpotAlias || detail.SpotName || '未命名景點',
                    duration: detail.StayDuration ? `${detail.StayDuration} 分鐘` : '',
                    description: detail.Notes || '',
                    expense: 0,
                    rating: 0,
                    imageUrls: [],
                    videoUrl: ''
                });
            });
        }

        if (newEvents.length > 0) {
            const newEventsMap = { ...props.modelValue.dailyEvents };
            const existingEvents = newEventsMap[activeDay.value] || [];
            newEventsMap[activeDay.value] = [...existingEvents, ...newEvents].sort((a, b) => a.time.localeCompare(b.time));
            updateForm('dailyEvents', newEventsMap);
            alert(`已成功將行程匯入至 Day ${activeDay.value}！`);
        } else {
            alert('該行程沒有任何景點資料。');
        }
        showImportModal.value = false;
    } catch (e) {
        console.error('匯入行程失敗:', e);
        alert('匯入行程失敗');
    }
};
</script>

<template>
    <div class="modal-body-scroll p-4 bg-light w-100 position-relative">

        <!-- Day 導航 -->
        <div class="d-flex gap-2 mb-4 overflow-auto pb-2 luiu-custom-scrollbar">
            <button v-for="day in totalDaysCount" :key="day"
                class="btn rounded-pill px-4 py-2 fw-bold transition-all text-nowrap border-0"
                :class="activeDay === day ? 'text-white' : 'btn-white text-muted shadow-sm'"
                :style="activeDay === day ? 'background-color: #1a2b4c;' : ''" @click="activeDay = day">
                Day {{ day }}
            </button>
        </div>

        <div class="d-flex justify-content-between align-items-center mb-3">
            <div>
                <h5 class="fw-bold mb-1" style="color: #1a2b4c;">Day {{ activeDay }}</h5>
                <small class="text-muted">{{ currentDateDisplay }}</small>
            </div>
            <div class="d-flex gap-2">
                <!-- <button class="btn bg-white rounded-pill px-3 shadow-sm fw-bold border transition-all" style="color: #1a2b4c; border-color: #1a2b4c !important;" @click="openLocalImportModal">
                    <i class="ri-download-cloud-line me-1"></i> 局部匯入
                </button> -->
                <button class="btn rounded-pill px-3 shadow-sm text-white fw-bold border-0"
                    style="background-color: #2b5fe8;" @click="addEvent">
                    <i class="ri-add-line me-1"></i> 新增景點
                </button>
            </div>
        </div>

        <div class="d-flex flex-column gap-3 mb-4">
            <div v-if="currentEvents.length === 0"
                class="bg-white rounded-4 d-flex flex-column align-items-center justify-content-center p-4 border shadow-sm"
                style="border-style: dashed !important;">
                <i class="ri-calendar-event-line text-muted mb-2" style="font-size: 2rem;"></i>
                <p class="text-muted small fw-bold mb-0">尚未新增任何行程</p>
            </div>

            <div v-for="event in currentEvents" :key="event.id"
                class="border rounded-4 p-3 position-relative bg-white shadow-sm">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <div class="d-flex align-items-center gap-2">
                        <i class="ri-time-line text-muted"></i>
                        <input v-model="event.time" type="time"
                            class="form-control form-control-sm border-0 bg-light fw-bold text-muted rounded-3 px-2"
                            style="width: 100px;">
                    </div>
                    <button class="btn btn-link text-danger p-0 text-decoration-none" @click="removeEvent(event.id)">
                        <i class="ri-delete-bin-line fs-5"></i>
                    </button>
                </div>

                <div class="mb-2">
                    <input v-model="event.title" type="text" class="form-control bg-light border-0 p-2 rounded-3"
                        placeholder="行程標題（例如：台北101）">
                </div>

                <div class="row g-2 mb-2">
                    <div class="col-6"><input v-model="event.location" type="text"
                            class="form-control bg-light border-0 p-2 rounded-3" placeholder="地點名稱"></div>
                    <div class="col-6"><input v-model="event.duration" type="text"
                            class="form-control bg-light border-0 p-2 rounded-3" placeholder="停留時間 (例:1小時)"></div>
                </div>

                <div class="mb-2">
                    <textarea v-model="event.description" rows="2" class="form-control bg-light border-0 p-2 rounded-3"
                        placeholder="寫些心得..."></textarea>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <input v-model.number="event.expense" type="number"
                            class="form-control bg-light border-0 p-2 rounded-3" placeholder="費用 (NT$)">
                    </div>
                    <div class="col-6">
                        <input v-model="event.rating" type="number" max="5" min="0"
                            class="form-control bg-light border-0 p-2 rounded-3" placeholder="評分 (0-5)">
                    </div>
                </div>

                <div class="mb-3">
                    <div class="d-flex justify-content-between align-items-end mb-2">
                        <label class="form-label small fw-bold text-muted mb-0"><i class="ri-image-line me-1"></i>照片 ({{
                            event.imageUrls ? event.imageUrls.length : 0 }}/3)</label>
                    </div>
                    <div v-if="event.imageUrls && event.imageUrls.length > 0" class="d-flex flex-wrap gap-2 mb-2">
                        <div v-for="(imgUrl, imgIdx) in event.imageUrls" :key="imgIdx"
                            class="position-relative rounded-3 overflow-hidden shadow-sm border"
                            style="width: 80px; height: 80px;">
                            <img :src="$img(imgUrl || '')" class="w-100 h-100 object-fit-cover" alt="Event Image">
                            <button
                                class="btn btn-danger btn-sm position-absolute top-0 end-0 m-1 rounded-circle shadow d-flex align-items-center justify-content-center"
                                style="width: 20px; height: 20px; padding: 0;" @click="removeImage(event.id, imgIdx)">
                                <i class="ri-close-line" style="font-size: 0.8rem;"></i>
                            </button>
                        </div>
                    </div>
                    <LuiuFileUploader v-if="!event.imageUrls || event.imageUrls.length < 3" class="bg-light rounded-3"
                        title="上傳照片" :maxFiles="3 - (event.imageUrls ? event.imageUrls.length : 0)" uploadType="memory"
                        @upload-success="(res) => handleImageUpload(res, event.id)" />
                </div>

                <div class="pt-2 border-top">
                    <input v-model="event.videoUrl" type="text"
                        class="form-control bg-light border-0 p-2 rounded-3 mb-2" placeholder="貼上 YouTube 或 MP4 連結">
                    <LuiuVideoPlayer v-if="event.videoUrl" :src="event.videoUrl"
                        class="w-100 rounded-3 overflow-hidden shadow-sm border" />
                </div>
            </div>
        </div>

        <!-- 局部匯入視窗的疊加遮罩 -->
        <div v-if="showImportModal" class="import-modal-overlay d-flex align-items-center justify-content-center p-3">
            <div class="bg-white rounded-4 shadow p-4 w-100" style="max-width: 400px;">
                <h6 class="fw-bold mb-3">選擇行程匯入至 Day {{ activeDay }}</h6>
                <div v-if="isFetchingPlans" class="text-center py-3">
                    <div class="spinner-border text-primary" role="status"></div>
                </div>
                <div v-else-if="planList.length === 0" class="text-center text-muted py-3">沒有可用的行程</div>
                <div v-else class="list-group overflow-auto" style="max-height: 250px;">
                    <button v-for="plan in planList" :key="plan.TripID || plan.id"
                        class="list-group-item list-group-item-action text-start"
                        @click="importPlanToDay(plan.TripID || plan.id)">
                        <div class="fw-bold">{{ plan.TripName || plan.title }}</div>
                        <small class="text-muted">{{ plan.StartDate }}</small>
                    </button>
                </div>
                <button class="btn btn-light w-100 mt-3 rounded-pill fw-bold"
                    @click="showImportModal = false">取消</button>
            </div>
        </div>
    </div>
</template>

<style scoped>
.luiu-custom-scrollbar::-webkit-scrollbar {
    display: none;
}

.luiu-custom-scrollbar {
    -ms-overflow-style: none;
    scrollbar-width: none;
}

.transition-all {
    transition: all 0.2s ease-in-out;
}

/* 局部匯入的二層遮罩 */
.import-modal-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.4);
    z-index: 1060;
    border-radius: 1rem;
}
</style>
