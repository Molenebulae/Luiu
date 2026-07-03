<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { createPlan, unwrapPlanPayload } from '@/api/planning/plan'
import { getPackingListSummaries } from '@/api/planning/packingList'
import LuiuDatePickerRange from '@/components/base/LuiuDatePickerRange.vue'
import PlanCoverPhotoField from '@/components/Planning/PlanCoverPhotoField.vue'
import PlanDialogShell from '@/components/Planning/PlanDialogShell.vue'
import PlanPrivacySuggestControls from '@/components/Planning/PlanPrivacySuggestControls.vue'
import { PLAN_PLACEHOLDER_COVER_URL } from '@/constants/planDefaults'
import {
  PLAN_PRIVACY,
  isPublicPrivacyStatus,
  normalizePrivacyStatus,
  resolvePrivacySuggest,
} from '@/utils/planPrivacy'
import { toast } from '@/utils/sweetAlert'

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
})

const emit = defineEmits(['close', 'created'])

const today = new Date().toISOString().slice(0, 10)
const placeholderCover = PLAN_PLACEHOLDER_COVER_URL

const isCreatingPlan = ref(false)
const selectedPhotoFile = ref(null)
const packingListOptions = ref([])
const packingListDropdownRef = ref(null)
const isLoadingPackingLists = ref(false)
const isPackingDropdownOpen = ref(false)
const packingListError = ref('')
const dateRange = ref([today, today])

const createForm = reactive({
  TripName: '',
  TripDesc: '',
  TripTag: '',
  StartDate: today,
  EndDate: today,
  PrivacyStatus: 0,
  IsSuggest: false,
  ListId: '',
})

const selectedPackingListName = computed(() => {
  const selectedList = packingListOptions.value.find(
    (item) => String(getPackingListId(item)) === String(createForm.ListId),
  )

  return getPackingListName(selectedList) || '不套用行李清單'
})
const datePickerOptions = {
  dateFormat: 'yyyy-MM-dd',
}

watch(
  () => createForm.PrivacyStatus,
  (privacyStatus) => {
    createForm.IsSuggest = resolvePrivacySuggest(privacyStatus)
  },
)

watch(dateRange, (value) => {
  const [startDate, endDate] = normalizeDateRange(value)

  createForm.StartDate = startDate
  createForm.EndDate = endDate || startDate
})

const appendOptionalFormValue = (formData, key, value) => {
  if (value !== null && value !== undefined && value !== '') formData.append(key, value)
}

const unwrapPackingSummaryPayload = (payload) => payload?.data ?? payload?.Data ?? payload

const asPackingSummaryArray = (payload) => {
  const data = unwrapPackingSummaryPayload(payload)

  if (Array.isArray(data)) return data
  if (Array.isArray(data?.items)) return data.items
  if (Array.isArray(data?.Items)) return data.Items

  return []
}

const getPackingListId = (item) => item?.ListID ?? item?.ListId ?? item?.listId ?? ''

const getPackingListName = (item) => item?.ListName ?? item?.listName ?? ''

const normalizeDateRange = (value) => {
  if (Array.isArray(value)) return value.map((item) => String(item || '').trim()).filter(Boolean)

  return String(value || '')
    .split(' ~ ')
    .map((item) => item.trim())
    .filter(Boolean)
}

const loadPackingListOptions = async () => {
  isLoadingPackingLists.value = true
  packingListError.value = ''

  try {
    const payload = await getPackingListSummaries(props.userId)
    packingListOptions.value = asPackingSummaryArray(payload)
  } catch (error) {
    packingListOptions.value = []
    packingListError.value = error?.message || error?.Message || '行李清單載入失敗'
  } finally {
    isLoadingPackingLists.value = false
  }
}

const closePackingDropdown = () => {
  isPackingDropdownOpen.value = false
}

const togglePackingDropdown = () => {
  if (isCreatingPlan.value || isLoadingPackingLists.value) return
  isPackingDropdownOpen.value = !isPackingDropdownOpen.value
}

const selectPackingList = (listId) => {
  createForm.ListId = listId
  closePackingDropdown()
}

const handleDocumentClick = (event) => {
  if (packingListDropdownRef.value && !packingListDropdownRef.value.contains(event.target)) {
    closePackingDropdown()
  }
}

const updatePrivacyStatus = (value) => {
  createForm.PrivacyStatus = normalizePrivacyStatus(value)
  createForm.IsSuggest = resolvePrivacySuggest(createForm.PrivacyStatus)
}

const updateIsSuggest = (value) => {
  if (!isPublicPrivacyStatus(createForm.PrivacyStatus)) {
    createForm.IsSuggest = false
    return
  }

  createForm.PrivacyStatus = value ? PLAN_PRIVACY.SUGGEST : PLAN_PRIVACY.PUBLIC
  createForm.IsSuggest = resolvePrivacySuggest(createForm.PrivacyStatus)
}

const resetPhotoState = () => {
  selectedPhotoFile.value = null
}

const closeDialog = () => {
  if (isCreatingPlan.value) return
  resetPhotoState()
  emit('close')
}

const submitCreatePlan = async () => {
  const tripName = createForm.TripName.trim()

  if (isCreatingPlan.value) return
  if (!tripName) {
    toast('請輸入行程名稱', 'error')
    return
  }
  if (!createForm.StartDate || !createForm.EndDate) {
    toast('請選擇行程日期', 'error')
    return
  }
  if (createForm.EndDate < createForm.StartDate) {
    toast('結束日期不可早於開始日期', 'error')
    return
  }

  const privacyStatus = normalizePrivacyStatus(createForm.PrivacyStatus)
  const isSuggest = resolvePrivacySuggest(privacyStatus)
  const formData = new FormData()
  formData.append('TripName', tripName)
  formData.append('StartDate', createForm.StartDate)
  formData.append('EndDate', createForm.EndDate)
  formData.append('TripDesc', createForm.TripDesc ?? '')
  formData.append('TripTag', createForm.TripTag ?? '')
  formData.append('PrivacyStatus', String(privacyStatus))
  formData.append('IsSuggest', String(isSuggest))
  appendOptionalFormValue(formData, 'ListId', createForm.ListId)
  if (selectedPhotoFile.value) formData.append('Photo', selectedPhotoFile.value)

  isCreatingPlan.value = true
  try {
    const payload = await createPlan(props.userId, formData)
    const createdPlan = unwrapPlanPayload(payload)
    const createdTripId = createdPlan?.TripID ?? createdPlan?.tripId ?? createdPlan?.id

    if (!createdTripId) {
      toast('建立成功，但缺少行程編號', 'error')
      return
    }

    toast('行程已建立')
    resetPhotoState()
    emit('created', createdTripId)
  } catch (error) {
    toast(error?.message || error?.Message || '新增行程失敗，請稍後再試', 'error')
  } finally {
    isCreatingPlan.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick)
  loadPackingListOptions()
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleDocumentClick)
  resetPhotoState()
})
</script>

<template>
  <PlanDialogShell
    title-id="plan-create-title"
    title="新增行程"
    kicker="Create Trip"
    panel-class="plan-settings-panel plan-create-panel"
    @close="closeDialog"
  >
    <div class="form-floating">
      <input
        id="create-trip-name-input"
        v-model.trim="createForm.TripName"
        class="form-control custom-border"
        type="text"
        placeholder="行程名稱"
        :disabled="isCreatingPlan"
      />
      <label for="create-trip-name-input">行程名稱</label>
    </div>

    <div class="plan-create-date-range">
      <label class="form-label">行程日期</label>
      <fieldset :disabled="isCreatingPlan">
        <LuiuDatePickerRange
          class="plan-create-date-picker"
          v-model="dateRange"
          placeholder="請選擇開始與結束日期"
          :options="datePickerOptions"
        />
      </fieldset>
    </div>

    <div class="form-floating">
      <input
        id="create-trip-tag-input"
        v-model.trim="createForm.TripTag"
        class="form-control custom-border"
        type="text"
        maxlength="10"
        placeholder="行程標籤"
        :disabled="isCreatingPlan"
      />
      <label for="create-trip-tag-input">行程標籤</label>
    </div>

    <div class="form-floating">
      <textarea
        id="create-trip-desc-input"
        v-model.trim="createForm.TripDesc"
        class="form-control custom-border plan-settings-textarea"
        placeholder="旅行簡介"
        :disabled="isCreatingPlan"
      ></textarea>
      <label for="create-trip-desc-input">旅行簡介</label>
    </div>

    <div class="plan-create-grid">
      <div
        ref="packingListDropdownRef"
        class="dropdown luiu-form-dropdown plan-create-list-dropdown"
      >
        <button
          id="create-trip-list-id-input"
          class="dropdown-toggle custom-border"
          :class="{ show: isPackingDropdownOpen }"
          type="button"
          :disabled="isCreatingPlan || isLoadingPackingLists"
          :aria-expanded="isPackingDropdownOpen"
          @click="togglePackingDropdown"
        >
          <span>
            {{ isLoadingPackingLists ? '清單載入中...' : selectedPackingListName }}
          </span>
        </button>
        <label for="create-trip-list-id-input">清單名稱</label>
        <transition name="fade">
          <div v-if="isPackingDropdownOpen" class="dropdown-menu show">
            <button class="dropdown-item" type="button" @click="selectPackingList('')">
              不套用行李清單
            </button>
            <button
              v-for="packingList in packingListOptions"
              :key="getPackingListId(packingList)"
              class="dropdown-item"
              type="button"
              @click="selectPackingList(getPackingListId(packingList))"
            >
              {{ getPackingListName(packingList) }}
            </button>
          </div>
        </transition>
        <p v-if="packingListError" class="plan-settings-empty mt-1 mb-0">
          {{ packingListError }}
        </p>
      </div>
    </div>

    <PlanPrivacySuggestControls
      variant="check"
      :privacy-status="createForm.PrivacyStatus"
      :is-suggest="createForm.IsSuggest"
      :disabled="isCreatingPlan"
      @update:privacy-status="updatePrivacyStatus"
      @update:is-suggest="updateIsSuggest"
    />

    <PlanCoverPhotoField
      :placeholder-url="placeholderCover"
      :disabled="isCreatingPlan"
      input-id="create-trip-photo-input"
      scale-id="create-trip-photo-scale"
      input-label="選擇一張封面圖片"
      helper-text="未選擇圖片時會使用預設封面。"
      @update:photo-file="selectedPhotoFile = $event"
    />

    <template #footer>
      <div class="plan-settings-actions">
        <button
          type="button"
          class="luiu-btn-outline-primary"
          :disabled="isCreatingPlan"
          @click="closeDialog"
        >
          取消
        </button>
        <button
          type="button"
          class="luiu-btn-primary"
          :disabled="isCreatingPlan"
          @click="submitCreatePlan"
        >
          {{ isCreatingPlan ? '建立中...' : '確定' }}
        </button>
      </div>
    </template>
  </PlanDialogShell>
</template>

<style scoped lang="scss">
@import '@/assets/scss/pages/plan-list';
@import '@/assets/scss/components/dropdown';
</style>
