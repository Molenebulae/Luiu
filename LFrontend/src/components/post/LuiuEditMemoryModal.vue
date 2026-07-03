<script setup>
import { ref, computed, watch } from 'vue';
import Swal from 'sweetalert2';
import { useUserStore } from '@/stores/user';
import { updateMemory, getMemoryDetail } from '@/api/memory';

import MemoryStep1BasicInfo from './MemoryStep1BasicInfo.vue';
import MemoryStep2Timeline from './MemoryStep2Timeline.vue';

// 外部控制顯示狀態
const props = defineProps({
    show: { type: Boolean, default: false },
    memoryId: { type: [Number, String], required: true }
});
const emit = defineEmits(['close', 'success']);

const userStore = useUserStore();
const userId = computed(() => userStore.userInfo?.userId || '');

const step = ref(1);
const isLoadingData = ref(false);

const form = ref({
    title: '',
    dateRange: '',
    coverImage: '',
    destinations: [],
    sourceTripId: null,
    dailyEvents: {}
});

const totalDaysCount = computed(() => {
    if (!form.value.dateRange) return 0;
    let startDate, endDate;
    if (typeof form.value.dateRange === 'string') {
        const parts = form.value.dateRange.split(' ~ ');
        if (parts.length < 2) return 0;
        startDate = new Date(parts[0]);
        endDate = new Date(parts[1]);
    } else if (Array.isArray(form.value.dateRange) && form.value.dateRange.length === 2) {
        startDate = new Date(form.value.dateRange[0]);
        endDate = new Date(form.value.dateRange[1]);
    } else {
        return 0;
    }
    const diffTime = Math.abs(endDate - startDate);
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
    return diffDays > 0 ? diffDays : 0;
});

const fetchMemoryData = async () => {
    isLoadingData.value = true;
    try {
        console.log('Fetching memory detail for ID:', props.memoryId);
        const response = await getMemoryDetail(props.memoryId);
        console.log('API Response:', response);

        if (response && response.success) {
            const data = response.data;
            console.log('Extracted Data:', data);
            
            // 轉換日期
            const startDate = data.startDate ? data.startDate.split('T')[0].replace(/-/g, '/') : '';
            const endDate = data.endDate ? data.endDate.split('T')[0].replace(/-/g, '/') : '';
            const dateRange = (startDate && endDate) ? `${startDate} ~ ${endDate}` : '';

            // 轉換行程
            const dailyEventsObj = {};
            if (data.days && Array.isArray(data.days)) {
                data.days.forEach(day => {
                    const dayNum = day.dayNumber;
                    dailyEventsObj[dayNum] = [];
                    if (day.stops && Array.isArray(day.stops)) {
                        day.stops.forEach((stop) => {
                            // 修正：將後端回傳的時間 (HH:mm:ss 或 DateTime) 轉為 HH:mm 格式
                            let timeFormatted = '09:00';
                            if (stop.arrivalTime) {
                                // 如果是完整 DateTime 字串，如 "2024-04-15T14:30:00"
                                if (stop.arrivalTime.includes('T')) {
                                    const timePart = stop.arrivalTime.split('T')[1];
                                    const timeParts = timePart.split(':');
                                    timeFormatted = `${timeParts[0]}:${timeParts[1]}`;
                                } else if (stop.arrivalTime.includes(':')) {
                                    const timeParts = stop.arrivalTime.split(':');
                                    timeFormatted = `${timeParts[0]}:${timeParts[1]}`;
                                }
                            }

                            dailyEventsObj[dayNum].push({
                                id: stop.stopId || crypto.randomUUID(),
                                time: timeFormatted,
                                title: stop.placeName || '',
                                location: stop.placeName || '', // Location and title are basically placeName here
                                duration: stop.duration || '1小時',
                                description: stop.memoryText || '',
                                expense: Number(stop.expense) || 0,
                                rating: stop.rating || 0,
                                imageUrls: stop.imageUrls || [],
                                videoUrl: stop.videoEmbedUrl || ''
                            });
                        });
                    }
                });
            }

            form.value = {
                title: data.title || '',
                dateRange: dateRange,
                coverImage: data.coverImage || '',
                destinations: [],
                sourceTripId: data.sourceTripId || null,
                dailyEvents: dailyEventsObj
            };
            
            console.log('Final Form Data mapped:', form.value);
        } else {
            throw new Error(response?.message || '讀取資料失敗');
        }
    } catch (error) {
        console.error('取得回憶資料失敗:', error);
        Swal.fire({
            icon: 'error',
            title: '載入失敗',
            text: '無法載入貼文資料',
            customClass: { popup: 'rounded-4' }
        });
        emit('close');
    } finally {
        isLoadingData.value = false;
    }
};

// 當 Modal 開啟且有 memoryId 時，取得回憶資料並填入
watch(() => props.show, async (newVal) => {
    if (newVal && props.memoryId) {
        step.value = 1;
        await fetchMemoryData();
    }
}, { immediate: true });

// --- Smart Close Logic (SweetAlert2) ---
const handleClose = async () => {
    if (step.value === 1) {
        const result = await Swal.fire({
            title: '確定要放棄編輯嗎？',
            text: "關閉後將會遺失目前修改的進度。",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#be2626',
            cancelButtonColor: '#6c757d',
            confirmButtonText: '放棄編輯',
            cancelButtonText: '繼續編輯',
            reverseButtons: true,
            customClass: {
                popup: 'rounded-4'
            }
        });

        if (result.isConfirmed) {
            resetForm();
            emit('close');
        }
    } else {
        // 如果在第二步，返回第一步
        step.value = 1;
    }
};

const resetForm = () => {
    form.value = { title: '', dateRange: '', coverImage: '', destinations: [], sourceTripId: null, dailyEvents: {} };
    step.value = 1;
};

const goToStep2 = () => {
    if (!form.value.title || !form.value.dateRange || !form.value.coverImage) {
        Swal.fire({
            icon: 'warning',
            title: '資料不完整',
            text: '請填寫必填欄位 (標題、日期、封面圖片)',
            confirmButtonColor: '#1a2b4c',
            customClass: { popup: 'rounded-4' }
        });
        return;
    }
    // 如果沒有任何行程，預設給第一天一筆空的行程
    if (!form.value.dailyEvents || Object.keys(form.value.dailyEvents).length === 0) {
        form.value.dailyEvents = { 1: [{ id: Date.now(), time: '09:00', title: '', location: '', duration: '1小時', description: '', expense: 0, rating: 5, imageUrls: [], videoUrl: '' }] };
    }
    step.value = 2;
};

// --- 送出發佈邏輯 (更新) ---
const handleSubmit = async () => {
    try {
        let startDate = null;
        let endDate = null;
        if (typeof form.value.dateRange === 'string' && form.value.dateRange.includes(' ~ ')) {
            const parts = form.value.dateRange.split(' ~ ');
            startDate = parts[0];
            endDate = parts[1];
        } else if (Array.isArray(form.value.dateRange)) {
            startDate = form.value.dateRange[0];
            endDate = form.value.dateRange[1];
        }

        const daysArray = [];
        for (let dayNum = 1; dayNum <= totalDaysCount.value; dayNum++) {
            const stopsForDay = form.value.dailyEvents[dayNum] || [];
            const formattedStops = stopsForDay.map(stop => ({
                title: stop.title || stop.location || '未命名景點', // Fallback name
                placeName: stop.location || stop.title || '未命名景點',
                time: stop.time ? (stop.time.length <= 5 ? `${stop.time}:00` : stop.time) : null,
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

        const payload = {
            title: form.value.title,
            coverImage: form.value.coverImage,
            startDate: startDate ? startDate.replace(/\//g, '-') : null,
            endDate: endDate ? endDate.replace(/\//g, '-') : null,
            sourceTripId: form.value.sourceTripId || null,
            days: daysArray
        };

        // 發佈中狀態提示
        Swal.fire({
            title: '儲存中...',
            text: '正在更新您的回憶貼文',
            allowOutsideClick: false,
            didOpen: () => { Swal.showLoading(); },
            customClass: { popup: 'rounded-4' }
        });

        const response = await updateMemory(props.memoryId, payload);

        if (response && response.success) {
            Swal.fire({
                icon: 'success',
                title: '更新成功！',
                text: '您的旅遊回憶已經成功儲存',
                showConfirmButton: false,
                timer: 1500,
                customClass: { popup: 'rounded-4' }
            });
            resetForm();
            emit('success');
            emit('close');
        } else {
            Swal.fire({
                icon: 'error',
                title: '更新失敗',
                text: response?.message || '發生未知錯誤',
                confirmButtonColor: '#1a2b4c',
                customClass: { popup: 'rounded-4' }
            });
        }

    } catch (error) {
        console.error('API 呼叫失敗：', error);
        Swal.fire({
            icon: 'error',
            title: '系統錯誤',
            text: '無法連線到伺服器或發生未預期的錯誤！',
            confirmButtonColor: '#1a2b4c',
            customClass: { popup: 'rounded-4' }
        });
    }
};
</script>

<template>
    <div v-if="show" class="modal-backdrop-custom d-flex align-items-center justify-content-center">
        <!-- 點擊背景不自動關閉，避免誤觸 -->
        <div class="custom-modal rounded-4 shadow overflow-hidden bg-white d-flex flex-column" @click.stop>
            
            <!-- Modal Header (IG Style) -->
            <div class="d-flex justify-content-between align-items-center border-bottom px-3 py-2 bg-white sticky-top z-3">
                <button class="btn btn-link text-dark text-decoration-none p-0" @click="handleClose">
                    <i v-if="step === 1" class="ri-close-line fs-3"></i>
                    <i v-if="step === 2" class="ri-arrow-left-line fs-3"></i>
                </button>
                <h6 class="fw-bold mb-0 m-auto">編輯貼文</h6>
                <button v-if="step === 1" class="btn btn-link fw-bold text-decoration-none p-0" style="color: #2b5fe8;" :disabled="isLoadingData" @click="goToStep2">
                    下一步
                </button>
                <button v-if="step === 2" class="btn btn-link fw-bold text-decoration-none p-0" style="color: #2b5fe8;" @click="handleSubmit">
                    儲存
                </button>
            </div>

            <!-- Loading 遮罩 -->
            <div v-if="isLoadingData" class="d-flex flex-column align-items-center justify-content-center h-100 flex-grow-1">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-3 text-muted">載入貼文中...</p>
            </div>

            <!-- Modal Body (Dynamic Component Rendering) -->
            <div v-else class="modal-body-scroll d-flex flex-column">
                <MemoryStep1BasicInfo 
                    v-if="step === 1" 
                    v-model="form" 
                    :totalDaysCount="totalDaysCount" 
                />

                <MemoryStep2Timeline 
                    v-if="step === 2" 
                    v-model="form" 
                    :totalDaysCount="totalDaysCount" 
                    :userId="userId"
                />
            </div>

        </div>
    </div>
</template>

<style scoped>
/* 背景遮罩 (IG 風格) */
.modal-backdrop-custom {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background-color: rgba(0, 0, 0, 0.65);
    z-index: 1050;
}

/* 彈窗容器 */
.custom-modal {
    width: 100%;
    max-width: 700px;
    height: 85vh; /* 高度固定為螢幕的 85% */
    max-height: 900px;
    position: relative;
}

/* 讓 Body 可以捲動 */
.modal-body-scroll {
    flex: 1;
    overflow-y: auto;
    background-color: #f8f9fa;
}
</style>
