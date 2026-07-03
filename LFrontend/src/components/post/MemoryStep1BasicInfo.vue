<script setup>
import { ref, computed, onMounted } from 'vue';
import LuiuFileUploader from '@/components/base/LuiuFileUploader.vue';
import LuiuDatePickerRange from '@/components/base/LuiuDatePickerRange.vue';
import { useUserStore } from '@/stores/user';
import { getPlanList, getPlan, asArrayPayload, unwrapPlanPayload } from '@/api/planning/plan';

const props = defineProps({
    modelValue: {
        type: Object,
        required: true
    },
    totalDaysCount: {
        type: Number,
        default: 0
    }
});

const emit = defineEmits(['update:modelValue']);

const userStore = useUserStore();
const userId = computed(() => userStore.userInfo?.userId || userStore.userInfo?.memberId || '');

// Local helper to update the parent's form object safely
const updateForm = (key, value) => {
    emit('update:modelValue', { ...props.modelValue, [key]: value });
};

const newDestination = ref('');

const addDestination = () => {
    if (newDestination.value.trim() !== '') {
        const updatedDestinations = [...props.modelValue.destinations, newDestination.value.trim()];
        updateForm('destinations', updatedDestinations);
        newDestination.value = '';
    }
};

const removeDestination = (index) => {
    const updatedDestinations = [...props.modelValue.destinations];
    updatedDestinations.splice(index, 1);
    updateForm('destinations', updatedDestinations);
};

const handleCoverSuccess = (response) => {
    const imageUrl = response.data || response.url || response.filePath || response;
    updateForm('coverImage', imageUrl);
};

// ==================== 匯入行程邏輯 ====================
const showImportModal = ref(false);
const planList = ref([]);
const isFetchingPlans = ref(false);

const openImportModal = async () => {
    if (!userId.value) {
        alert('請先登入以匯入行程');
        return;
    }
    showImportModal.value = true;
    isFetchingPlans.value = true;
    try {
        const payload = await getPlanList(userId.value);
        planList.value = asArrayPayload(payload);
    } catch (e) {
        console.error('獲取行程列表失敗:', e);
        alert('獲取行程列表失敗');
        showImportModal.value = false;
    } finally {
        isFetchingPlans.value = false;
    }
};

const importFullPlan = async (tripId) => {
    try {
        const payload = await getPlan(userId.value, tripId);
        const planData = unwrapPlanPayload(payload);

        // 轉換日期範圍
        const dateRangeStr = `${planData.StartDate} ~ ${planData.EndDate}`;

        // 將 TTripDetail 按照 DayNumber 分組並轉換為 dailyEvents 格式
        const dailyEventsMap = {};
        if (planData.TripDetails && Array.isArray(planData.TripDetails)) {
            planData.TripDetails.forEach(detail => {
                const day = detail.DayNumber || 1;
                if (!dailyEventsMap[day]) {
                    dailyEventsMap[day] = [];
                }
                dailyEventsMap[day].push({
                    time: detail.ArrivalTime ? detail.ArrivalTime.substring(0, 5) : '', // 'HH:mm'
                    title: detail.SpotName || '',
                    location: detail.SpotName || '',
                    duration: detail.StayDuration ? `${detail.StayDuration} 分鐘` : '',
                    description: detail.Notes || '',
                    expense: 0,
                    rating: 0,
                    imageUrls: [],
                    videoUrl: ''
                });
            });
        }

        const importedDailyEvents = {};
        Object.keys(dailyEventsMap).forEach(day => {
            // 根據時間排序
            importedDailyEvents[day] = dailyEventsMap[day].sort((a, b) => a.time.localeCompare(b.time));
        });

        // 整批更新父層的表單
        emit('update:modelValue', {
            ...props.modelValue,
            title: planData.TripName || '',
            dateRange: dateRangeStr,
            sourceTripId: planData.TripID,
            dailyEvents: importedDailyEvents,
            destinations: [] // 清空之前的目的地
        });

        alert('行程匯入成功！您可以繼續上傳照片與確認基本資訊。');
        showImportModal.value = false;

    } catch (e) {
        console.error('匯入行程失敗:', e);
        alert('匯入行程失敗');
    }
};

// When date range changes
const onDateRangeChange = (newDateRange) => {
    updateForm('dateRange', newDateRange);
};

// 清除匯入資料
const clearImport = () => {
    if (confirm('確定要清除已匯入的行程資料嗎？這將重置您的日期與行程設定。')) {
        emit('update:modelValue', {
            ...props.modelValue,
            sourceTripId: null,
            dateRange: '',
            dailyEvents: []
        });
    }
};

// 不修改子組件的前提下，透過 DOM 操作加上 readonly
onMounted(() => {
    setTimeout(() => {
        const dateInputs = document.querySelectorAll('.custom-datepicker input');
        dateInputs.forEach(input => {
            input.setAttribute('readonly', 'true');
        });
    }, 100); // 確保子組件已經完全渲染
});
</script>

<template>
    <div class="modal-body-scroll p-4 bg-light w-100 position-relative">
        <div class="d-flex justify-content-end mb-3">
            <button class="btn bg-white rounded-pill px-3 shadow-sm fw-bold border transition-all"
                style="color: #1a2b4c; border-color: #1a2b4c !important;" @click="openImportModal">
                <i class="ri-download-cloud-line me-1"></i> 全部匯入
            </button>
        </div>
        <div class="mb-4">
            <label class="form-label small fw-bold text-muted">行程標題 <span class="text-danger">*</span></label>
            <input :value="modelValue.title" type="text" class="form-control rounded-3 p-3 bg-white border-0 shadow-sm"
                placeholder="例如：台灣環島五日遊" @input="updateForm('title', $event.target.value)">
        </div>

        <div class="mb-4">
            <label class="form-label small fw-bold text-muted">行程日期 (起 - 迄) <span class="text-danger">*</span></label>

            <!-- 情境 A：如果有匯入行程，顯示鎖定/唯讀狀態 -->
            <div v-if="modelValue.sourceTripId"
                class="p-3 bg-white border shadow-sm rounded-3 text-muted d-flex align-items-center justify-content-between">
                <div>
                    <i class="ri-calendar-check-fill text-success fs-5 align-middle me-2"></i>
                    <span class="fw-bold align-middle" style="font-size: 1.05rem;">{{ modelValue.dateRange }}</span>
                    <span class="badge bg-secondary ms-2 align-middle">已由行程匯入鎖定</span>
                </div>
                <button class="btn btn-sm btn-outline-danger rounded-pill px-3" @click="clearImport">
                    <i class="ri-close-circle-line me-1"></i>清除匯入
                </button>
            </div>

            <!-- 情境 B：沒有匯入行程，保持原來的日期選擇器 -->
            <div v-else>
                <!-- LuiuDatePickerRange 使用 v-model 時，我們必須攔截它的更新並通知父層 -->
                <LuiuDatePickerRange :modelValue="modelValue.dateRange" placeholder="請點擊選擇出發與結束日期"
                    :options="{ dateFormat: 'yyyy-MM-dd' }" class="custom-datepicker shadow-sm"
                    @update:modelValue="onDateRangeChange" />
            </div>

            <div class="mt-2 text-muted small fw-bold text-end">總共 {{ totalDaysCount }} 天</div>
        </div>

        <div class="mb-4">
            <label class="form-label small fw-bold text-muted mb-2">行程封面圖片 </label>
            <LuiuFileUploader class="bg-white rounded-3 shadow-sm border-0" title="上傳封面圖片" :maxFiles="1"
                uploadType="memory" @upload-success="handleCoverSuccess" />
            <div v-if="modelValue.coverImage" class="mt-3 rounded-4 overflow-hidden shadow-sm position-relative"
                style="height: 200px;">
                <img :src="$img(modelValue.coverImage || '')" class="w-100 h-100 object-fit-cover" alt="Cover Preview">
                <div
                    class="position-absolute top-0 start-0 m-2 badge bg-success text-white px-2 py-1 rounded-pill shadow">
                    <i class="ri-check-line me-1"></i>已套用
                </div>
            </div>
        </div>

        <div class="mb-4">
            <label class="form-label small fw-bold text-muted">目的地標籤 (選填)</label>
            <div class="d-flex gap-2 mb-2">
                <input v-model="newDestination" type="text"
                    class="form-control rounded-3 p-2 bg-white border-0 shadow-sm" placeholder="輸入地點並按 Enter 或右側按鈕新增"
                    @keyup.enter="addDestination">
                <button class="btn btn-primary rounded-3 px-3 shadow-sm" @click="addDestination"><i
                        class="ri-add-line"></i></button>
            </div>
            <div class="d-flex flex-wrap gap-2">
                <span v-for="(dest, index) in modelValue.destinations" :key="index"
                    class="badge rounded-pill bg-white border px-3 py-2 fw-normal shadow-sm text-primary border-primary">
                    <i class="ri-map-pin-line me-1"></i>{{ dest }}
                    <i class="ri-close-line ms-1 cursor-pointer fw-bold" @click="removeDestination(index)"></i>
                </span>
            </div>
        </div>

        <!-- 全部匯入視窗的疊加遮罩 -->
        <div v-if="showImportModal" class="import-modal-overlay d-flex align-items-center justify-content-center p-3">
            <div class="bg-white rounded-4 shadow p-4 w-100" style="max-width: 400px;">
                <h6 class="fw-bold mb-3">選擇行程進行全部匯入</h6>
                <div v-if="isFetchingPlans" class="text-center py-3">
                    <div class="spinner-border text-primary" role="status"></div>
                </div>
                <div v-else-if="planList.length === 0" class="text-center text-muted py-3">沒有可用的行程</div>
                <div v-else class="list-group overflow-auto" style="max-height: 250px;">
                    <button v-for="plan in planList" :key="plan.TripID || plan.id"
                        class="list-group-item list-group-item-action text-start"
                        @click="importFullPlan(plan.TripID || plan.id)">
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
.cursor-pointer {
    cursor: pointer;
}

.transition-all {
    transition: all 0.2s ease-in-out;
}

/* 視窗的疊加遮罩 */
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

/* 確保輸入框看起來是可點擊的且背景為白色 */
:deep(.custom-datepicker input) {
    background-color: #fff !important;
    cursor: pointer !important;
}
</style>

<style>
/* 使用非 scoped 的樣式來覆蓋掛載到 body 的 air-datepicker */
.air-datepicker {
    z-index: 9999 !important;
}
</style>
