<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'

// 引入你的兩個核心組件 (請確認檔案路徑與名稱正確)
import LuiuFileUploader from '@/components/base/LuiuFileUploader.vue'
import LuiuDatePickerRange from '@/components/base/LuiuDatePickerRange.vue'
import { useUserStore } from '@/stores/user'
import { getPlanList, getPlan, asArrayPayload, unwrapPlanPayload } from '@/api/planning/plan'

const userStore = useUserStore()
const userId = computed(() => userStore.userInfo?.userId || '')

const router = useRouter()

// 表單資料綁定 (預設為空)
const form = ref({
  title: '',
  dateRange: '',
  coverImage: '',
  destinations: [],
  sourceTripId: null,
  dailyEvents: {},
})

/* 測試用假資料 (保留供參考)
const mockForm = {
    title: '台灣環島五日遊',
    dateRange: '2026-04-15 ~ 2026-04-19',
    coverImage: '',
    destinations: ['台北', '台中', '台南', '高雄', '花蓮']
};
*/

// 在元件載入時，檢查是否有草稿
onMounted(() => {
  const draft = localStorage.getItem('memory_draft')
  if (draft) {
    try {
      const parsedDraft = JSON.parse(draft)
      // 將草稿內容覆蓋回表單
      form.value = { ...form.value, ...parsedDraft }
    } catch (e) {
      console.error('讀取草稿失敗:', e)
    }
  }
})

// 新增目的地的暫存變數
const newDestination = ref('')

// 自動計算總天數
const totalDays = computed(() => {
  if (!form.value.dateRange) return 0

  let startDate, endDate

  if (typeof form.value.dateRange === 'string') {
    const parts = form.value.dateRange.split(' ~ ')
    if (parts.length < 2) return 0 // 還沒選完結束日期時，回傳 0
    startDate = new Date(parts[0])
    endDate = new Date(parts[1])
  } else if (Array.isArray(form.value.dateRange) && form.value.dateRange.length === 2) {
    startDate = new Date(form.value.dateRange[0])
    endDate = new Date(form.value.dateRange[1])
  } else {
    return 0
  }

  const diffTime = Math.abs(endDate - startDate)
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1 // 包含頭尾天數
  return diffDays > 0 ? diffDays : 0
})

// 新增目的地標籤
const addDestination = () => {
  if (newDestination.value.trim() !== '') {
    form.value.destinations.push(newDestination.value.trim())
    newDestination.value = '' // 清空輸入框
  }
}

// 刪除目的地標籤
const removeDestination = (index) => {
  form.value.destinations.splice(index, 1)
}

// 處理照片上傳成功的邏輯
const handleCoverSuccess = (response) => {
  // 後端回傳的是 ResultDTO: { success: true, data: "/uploads/memories/..." }
  const imageUrl = response.data || response.url || response.filePath || response
  form.value.coverImage = imageUrl
}

// ==================== 匯入行程邏輯 ====================
const planList = ref([])
const isFetchingPlans = ref(false)

const openImportModal = async () => {
  if (!userId.value) {
    alert('請先登入以匯入行程')
    return
  }
  isFetchingPlans.value = true
  try {
    const payload = await getPlanList(userId.value)
    planList.value = asArrayPayload(payload)
  } catch (e) {
    console.error('獲取行程列表失敗:', e)
    alert('獲取行程列表失敗')
  } finally {
    isFetchingPlans.value = false
  }
}

const importPlan = async (tripId) => {
  try {
    const payload = await getPlan(userId.value, tripId)
    const planData = unwrapPlanPayload(payload)

    // 轉換日期範圍
    const dateRangeStr = `${planData.Trip.StartDate} ~ ${planData.Trip.EndDate}`

    // 將 TTripDetail 按照 DayNumber 分組並轉換為 dailyEvents 格式
    const dailyEventsMap = {}
    if (planData.TripDetails && Array.isArray(planData.TripDetails)) {
      planData.TripDetails.forEach((detail) => {
        const day = detail.DayNumber || 1
        if (!dailyEventsMap[day]) {
          dailyEventsMap[day] = []
        }
        dailyEventsMap[day].push({
          time: detail.StartTime ? detail.StartTime.substring(0, 5) : '', // 'HH:mm'
          title: detail.SpotName || '',
          duration: detail.StayTime ? `${detail.StayTime} 小時` : '',
          description: detail.SpotDesc || '',
          expense: 0,
          rating: 0,
          imageUrls: [],
          videoUrl: '',
        })
      })
    }

    const importedDailyEvents = {}
    Object.keys(dailyEventsMap).forEach((day) => {
      // 根據時間排序
      importedDailyEvents[day] = dailyEventsMap[day].sort((a, b) => a.time.localeCompare(b.time))
    })

    // 填入表單
    form.value.title = planData.Trip.TripName || ''
    form.value.dateRange = dateRangeStr
    form.value.sourceTripId = planData.Trip.TripID
    form.value.dailyEvents = importedDailyEvents
    form.value.destinations = [] // 清空之前的目的地，因為已經匯入了完整的景點

    alert('行程匯入成功！請點選「下一步」繼續上傳照片與填寫心得。')

    // 關閉 Modal (這裡透過原生 Bootstrap 方式關閉)
    const closeBtn = document.getElementById('closeImportModalBtn')
    if (closeBtn) closeBtn.click()
  } catch (e) {
    console.error('匯入行程失敗:', e)
    alert('匯入行程失敗')
  }
}

// ==================== 草稿處理邏輯 ====================
// 1. 儲存草稿並離開
const saveDraftAndLeave = () => {
  // 存在瀏覽器的 localStorage 當作草稿暫存
  localStorage.setItem('memory_draft', JSON.stringify(form.value))
  alert('草稿已儲存！')
  router.back()
}

// 2. 放棄編輯並離開
const leaveWithoutSaving = () => {
  // 既然使用者選擇放棄，我們就把舊的草稿清掉，確保下次進來是全新的表單
  localStorage.removeItem('memory_draft')
  router.back()
}
// 點擊下一步，進入規劃行程頁面
const goToNextStep = () => {
  // 1. 先把目前填寫的基本資訊存起來 (這樣第二頁才抓得到總共有幾天)
  localStorage.setItem('memory_draft', JSON.stringify(form.value))

  // 2. 透過 Router 跳轉到第二個頁面 (我們假設新頁面叫做 PlanMemory)
  router.push({ name: 'PlanMemory' })
}
</script>

<template>
  <div class="create-page bg-light min-vh-100 py-4">
    <div class="container" style="max-width: 700px">
      <div class="d-flex align-items-center mb-4">
        <button
          class="btn btn-link text-dark text-decoration-none p-0 me-3"
          data-bs-toggle="modal"
          data-bs-target="#draftModal"
        >
          <i class="ri-arrow-left-line fs-4"></i>
        </button>
        <h4 class="fw-bold mb-0" style="color: #1a2b4c">建立新貼文</h4>
        <button
          class="btn btn-outline-primary ms-auto rounded-pill px-4 shadow-sm"
          data-bs-toggle="modal"
          data-bs-target="#importPlanModal"
          @click="openImportModal"
        >
          <i class="ri-file-download-line me-1"></i>從我的行程匯入
        </button>
      </div>

      <div class="card border-0 shadow-sm rounded-4 p-4 p-md-5 bg-white">
        <div class="mb-4">
          <label class="form-label small fw-bold text-muted"
            >行程標題 <span class="text-danger">*</span></label
          >
          <input
            v-model="form.title"
            type="text"
            class="form-control rounded-3 p-3 bg-light border-0"
            placeholder="例如：台灣環島五日遊"
          />
        </div>

        <div class="mb-4">
          <label class="form-label small fw-bold text-muted"
            >行程日期 (起 - 迄) <span class="text-danger">*</span></label
          >
          <LuiuDatePickerRange
            v-model="form.dateRange"
            placeholder="請點擊選擇出發與結束日期"
            :options="{ dateFormat: 'yyyy-MM-dd' }"
            class="custom-datepicker"
          />
        </div>

        <div class="mb-4 text-muted small fw-bold">總共 {{ totalDays }} 天</div>

        <div class="mb-4">
          <label class="form-label small fw-bold text-muted mb-3"
            >行程封面圖片 <span class="text-danger">*</span></label
          >

          <LuiuFileUploader
            title="上傳封面圖片"
            :maxFiles="1"
            uploadType="memory"
            @upload-success="handleCoverSuccess"
          />

          <div
            v-if="form.coverImage"
            class="mt-3 rounded-4 overflow-hidden shadow-sm position-relative"
            style="height: 250px"
          >
            <img
              :src="$img(form.coverImage || '')"
              class="w-100 h-100 object-fit-cover"
              alt="Cover Preview"
            />

            <div
              class="position-absolute top-0 start-0 m-3 badge bg-success text-white px-3 py-2 rounded-pill shadow"
            >
              <i class="ri-check-line me-1"></i> 已套用封面
            </div>
          </div>
        </div>

        <div class="mb-5">
          <label class="form-label small fw-bold text-muted">目的地</label>
          <div class="d-flex gap-2 mb-3">
            <input
              v-model="newDestination"
              type="text"
              class="form-control rounded-3 p-3 bg-light border-0"
              placeholder="例如：台北 (輸入後按 Enter 建立)"
              @keyup.enter="addDestination"
            />
            <button
              class="btn btn-primary rounded-3 px-4 d-flex align-items-center justify-content-center"
              @click="addDestination"
            >
              <i class="ri-add-line fs-5"></i>
            </button>
          </div>

          <div class="d-flex flex-wrap gap-2">
            <span
              v-for="(dest, index) in form.destinations"
              :key="index"
              class="badge rounded-pill d-flex align-items-center gap-1 px-3 py-2 fw-normal dest-badge"
            >
              <i class="ri-map-pin-line"></i> {{ dest }}
              <i
                class="ri-close-line ms-1 pointer"
                style="cursor: pointer"
                @click="removeDestination(index)"
              ></i>
            </span>
          </div>
        </div>

        <button
          class="btn rounded-pill w-100 py-3 text-white fw-bold shadow-sm custom-navy-btn"
          @click="goToNextStep"
        >
          下一步
        </button>
      </div>
    </div>

    <div id="draftModal" class="modal fade" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered modal-sm">
        <div class="modal-content border-0 rounded-4 shadow-lg">
          <div class="modal-body p-4 text-center">
            <div class="text-warning mb-3">
              <i class="ri-error-warning-fill" style="font-size: 3.5rem"></i>
            </div>

            <h5 class="fw-bold mb-2" style="color: #1a2b4c">尚未儲存行程</h5>
            <p class="text-muted small mb-4">是否要將目前的進度儲存為草稿？</p>

            <div class="d-flex flex-column gap-2">
              <button
                type="button"
                class="btn text-white fw-bold rounded-pill shadow-sm"
                style="background-color: #1a2b4c"
                data-bs-dismiss="modal"
                @click="saveDraftAndLeave"
              >
                儲存草稿並離開
              </button>

              <button
                type="button"
                class="btn btn-light text-danger fw-bold rounded-pill border"
                data-bs-dismiss="modal"
                @click="leaveWithoutSaving"
              >
                放棄編輯並離開
              </button>

              <button
                type="button"
                class="btn btn-link text-muted text-decoration-none small mt-1"
                data-bs-dismiss="modal"
              >
                取消，繼續編輯
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 行程匯入 Modal -->
    <div id="importPlanModal" class="modal fade" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
        <div class="modal-content border-0 rounded-4 shadow-lg">
          <div class="modal-header border-0 pb-0">
            <h5 class="fw-bold mb-0" style="color: #1a2b4c">選擇要匯入的行程</h5>
            <button
              type="button"
              id="closeImportModalBtn"
              class="btn-close"
              data-bs-dismiss="modal"
              aria-label="Close"
            ></button>
          </div>
          <div class="modal-body p-4">
            <div v-if="isFetchingPlans" class="text-center py-5">
              <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">載入中...</span>
              </div>
              <p class="text-muted mt-3 mb-0 small">正在讀取您的行程清單...</p>
            </div>
            <div v-else-if="planList.length === 0" class="text-center py-5">
              <i class="ri-calendar-event-line text-muted" style="font-size: 3rem"></i>
              <p class="text-muted mt-3 mb-0">您目前還沒有建立任何行程喔！</p>
            </div>
            <div v-else class="list-group border-0 gap-2">
              <button
                v-for="plan in planList"
                :key="plan.TripID"
                type="button"
                class="list-group-item list-group-item-action border-0 rounded-3 p-3 text-start shadow-sm d-flex flex-column"
                style="background-color: #f8f9fa"
                @click="importPlan(plan.TripID)"
              >
                <div class="d-flex w-100 justify-content-between align-items-center mb-2">
                  <h6 class="mb-0 fw-bold" style="color: #1a2b4c">{{ plan.TripName }}</h6>
                  <span class="badge bg-primary rounded-pill">{{ plan.Days }} 天</span>
                </div>
                <small class="text-muted"
                  ><i class="ri-calendar-line me-1"></i>{{ plan.StartDate }} ~
                  {{ plan.EndDate }}</small
                >
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 目的地標籤樣式 */
.dest-badge {
  background-color: #00c4cc;
  color: white;
  font-size: 0.85rem;
}

/* 藏青色主按鈕 */
.custom-navy-btn {
  background-color: #1a2b4c;
  letter-spacing: 1px;
  transition: all 0.3s ease;
}

.custom-navy-btn:hover {
  background-color: #111d33;
  transform: translateY(-2px);
}

/* 一般輸入框聚焦時的效果 */
.form-control:focus {
  box-shadow: none;
  border: 1px solid #1a2b4c !important;
  background-color: #fff !important;
}

/* 讓 LuiuDatePickerRange 裡面的輸入框風格與其他欄位一致 */
:deep(.custom-datepicker .form-control) {
  background-color: #f8f9fa !important;
  /* bg-light */
  border: 0 !important;
  border-radius: 0.5rem !important;
  /* rounded-3 */
  padding: 1rem !important;
  /* p-3 */
  box-shadow: none;
  cursor: pointer;
  /* 讓鼠標變成點擊樣式，提示用戶這是用點的 */
}

:deep(.custom-datepicker .form-control:focus) {
  border: 1px solid #1a2b4c !important;
  background-color: #fff !important;
}
</style>
