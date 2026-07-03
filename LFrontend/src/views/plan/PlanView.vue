<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import LuiuCheck from '@/components/base/LuiuCheck.vue'
import LuiuDraggableCards from '@/components/base/LuiuDraggableCards.vue'
import LuiuDraggableContainer from '@/components/base/LuiuDraggableContainer.vue'
import PackingListDialog from '@/components/Planning/PackingListDialog.vue'
import PlanCreateMemoryDialog from '@/components/Planning/PlanCreateMemoryDialog.vue'
import PlanSettingsDialog from '@/components/Planning/PlanSettingsDialog.vue'
import { PLAN_PLACEHOLDER_COVER_URL } from '@/constants/planDefaults'
import {
  autocompletePlaces,
  getGoogleMapPlace,
  getPlaceByGoogleMapId,
  searchPlaces,
} from '@/api/googleMaps'
import { getFavoritesApi } from '@/api/favorite'
import {
  getPlan,
  unwrapPlanPayload,
  asArrayPayload,
  syncPlanDetails,
  updatePlan,
  deletePlan,
} from '@/api/planning/plan'
import {
  createTripComment,
  deleteTripComment,
  getTripComments,
  updateTripComment,
} from '@/api/planning/tripComment'
import { useDropdownToggle } from '@/composables/useDropdownToggle'
import { toFavoriteTargetId, useFavoriteTargets } from '@/composables/useFavorites'
import { usePlanMapMarkers } from '@/composables/usePlanMapMarkers'
import { usePlanRoutePlanner } from '@/composables/usePlanRoutePlanner'
import { usePlanDayOrderStore } from '@/stores/planDayOrder'
import { googleMaps } from '@/utils/googleMaps'
import {
  formatDistanceLabel,
  getRouteStepDescription,
  getRouteStepTimeLabel,
  getRouteStepTitle,
  getTransitStepIconName,
  getTransportIconName,
  getVisibleRouteSteps,
  normalizeTransportMode,
  toLatLngLiteral,
  transportOptions,
} from '@/utils/planRouteUtils'
import { normalizePrivacyStatus, resolvePrivacySuggest } from '@/utils/planPrivacy'
import { LuiuAlert, luiuNotify, toast } from '@/utils/sweetAlert'

const route = useRoute()
const router = useRouter()
const planDayOrderStore = usePlanDayOrderStore()

const plan = ref(null)
const savedPlanDateRange = ref({ start: '', end: '' })
const isLoading = ref(false)
const errorMessage = ref('')

const isToolBarCollapsed = ref(false)
const isDiscussionOpen = ref(false)
const isPlaceCardOpen = ref(false)
const scheduleRenderKey = ref(0)
const dayDragListRef = ref(null)
const itineraryDragListRef = ref(null)
const mapElement = ref(null)
const mapSearchInput = ref(null)
const routeMapSwitchRef = ref(null)
const mapSearchQuery = ref('')
const mapSearchResults = ref([])
const mapSearchSessionToken = ref('')
const isMapSearchLoading = ref(false)
const mapStatusMessage = ref('地圖載入中...')
const mapErrorMessage = ref('')
const selectedMapPlace = ref(null)
const isAddingMapPlace = ref(false)
const isResolvingFavoriteSpot = ref(false)
const showFavoriteSpots = ref(false)
const favoriteSpots = ref([])
const isLoadingFavoriteSpots = ref(false)
const {
  isSavingFavorite: isSavingSpotFavorite,
  isFavoriteTarget: isFavoriteSpotTarget,
  loadFavoriteTargets: loadFavoriteSpotIds,
  toggleFavoriteTarget: toggleSpotFavorite,
} = useFavoriteTargets('Spot')

const isSavingTrip = ref(false)
const isPlanSettingsOpen = ref(false)
const isSavingSettings = ref(false)
const isDeletingPlan = ref(false)
const settingsCopyMessage = ref('')

let mapStatusTimer = null
let mapSearchDebounceTimer = null

//找個地方塞route
const goBackPlanList = () => {
  router.push({
    name: 'PlanList',
    params: {
      userId: userId.value,
    },
  })
}

// 新增留言
const newMessageText = ref('')
const replyMessageText = ref('')
const isSubmittingMessage = ref(false)
const replyingCommentId = ref(null)
const editingCommentId = ref(null)
const editingCommentText = ref('')
const savingCommentIds = ref([])
const deletingCommentIds = ref([])
const discussionStatusMessage = ref('')
let discussionStatusTimer = null

// 編輯天數 dialog
const editingDay = ref(null)
const editingItineraryId = ref(null)

// 選中的 marker id（點擊 / 未點擊不同樣式）
const selectedMarkerId = ref(null)

const KAOHSIUNG_CENTER = { lat: 22.6273, lng: 120.3014 }
const DEFAULT_MAP_ZOOM = 15

let googleMap = null
let markerConstructor = null
let mapClickListener = null
let mapDragListener = null
let mapZoomListener = null
let routePolylines = []
let routeSpotMarkers = []

const userId = computed(() => route.params.userId)
const tripId = computed(() => route.params.tripId)

const itineraryItems = ref([])
const originalItineraryItems = ref([])
const DRAFT_STATUS = {
  clean: 'clean',
  created: 'created',
  updated: 'updated',
  deleted: 'deleted',
}
const placeholderCover = PLAN_PLACEHOLDER_COVER_URL

// ── 日期工具 ───────────────────────────────────────────────────────────────
const addDays = (dateStr, n) => {
  const d = new Date(dateStr)
  d.setDate(d.getDate() + n)
  return d.toISOString().slice(0, 10)
}
const toInputDate = (value) => {
  if (!value) return ''
  const normalized = String(value).replaceAll('/', '-')
  const dateOnlyMatch = normalized.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/)
  if (dateOnlyMatch) {
    const [, year, month, day] = dateOnlyMatch
    return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`
  }
  const d = new Date(value)
  return Number.isNaN(d.getTime()) ? normalized.slice(0, 10) : d.toISOString().slice(0, 10)
}
const getPlanValue = (item, ...keys) => {
  for (const key of keys) {
    const value = item?.[key]

    if (value !== undefined && value !== null) {
      return value
    }
  }

  return ''
}
const getPlanCreateAt = (item) =>
  getPlanValue(item, 'CreateAt', 'createAt', 'CreatedAt', 'createdAt')
const getPlanUpdateAt = (item) =>
  getPlanValue(item, 'UpdateAt', 'updateAt', 'UpdatedAt', 'updatedAt')
const toBoolean = (value) =>
  value === true || value === 1 || value === '1' || String(value).toLowerCase() === 'true'
const formatDateLabel = (value) => toInputDate(value).replaceAll('-', '/')
const formatTimeLabel = (value) => {
  if (!value) return ''
  const d = new Date(value)
  if (!Number.isNaN(d.getTime())) {
    return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
  }
  return String(value).slice(0, 5)
}
const formatDateTimeLabel = (value) => {
  const d = value ? new Date(value) : new Date()
  if (Number.isNaN(d.getTime())) return String(value || '')
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const hour = String(d.getHours()).padStart(2, '0')
  const minute = String(d.getMinutes()).padStart(2, '0')
  return `${year}/${month}/${day} ${hour}:${minute}`
}
const formatSettingsDateTime = (value) => (value ? formatDateTimeLabel(value) : '尚無建立時間')
const unwrapApiData = (payload) => payload?.data ?? payload?.Data ?? payload

// ── Days（天數名稱改成「當天簡介」） ────────────────────────────────────────
const days = ref([])

// 「第 N 天」label
const dayLabel = (index) => `第 ${index + 1} 天`

const activeDialog = ref(null) // 'day' | 'editDay' | 'itinerary' | 'transfer'
const isPackingDialogOpen = ref(false)
const isCreateMemoryDialogOpen = ref(false)
const dayForm = reactive({ intro: '', date: '' })
const editDayForm = reactive({ intro: '', date: '' })
const itineraryForm = reactive({ alias: '', notes: '', durationMinutes: 60, cost: 0 })
const transferForm = reactive({ mode: 'DRIVE' })
const discussionMessages = ref([])

const getFirstStringValue = (...values) => {
  for (const value of values.flat()) {
    if (typeof value === 'string' && value.trim()) return value
    if (value && typeof value === 'object') {
      const nestedValue = getFirstStringValue(
        value.photoUrl,
        value.PhotoURL,
        value.PhotoUrl,
        value.photo_url,
        value.imageUrl,
        value.ImageURL,
        value.url,
        value.Url,
        value.uri,
        value.Uri,
      )
      if (nestedValue) return nestedValue
    }
  }
  return ''
}

const getPlaceImageUrl = (place = {}) =>
  getFirstStringValue(
    place.photoUrl,
    place.PhotoURL,
    place.PhotoUrl,
    place.imageUrl,
    place.ImageURL,
    place.photo_url,
    place.Photo,
    place.photo,
    place.Photos,
    place.photos,
  )

const parseOpeningHoursValue = (value) => {
  if (Array.isArray(value)) {
    return value
      .map((item) => {
        if (typeof item === 'string') return item
        if (item && typeof item === 'object') {
          return (
            item.text ||
            item.Text ||
            item.label ||
            item.Label ||
            item.weekdayDescription ||
            item.WeekdayDescription ||
            item.displayName ||
            item.DisplayName ||
            ''
          )
        }
        return ''
      })
      .filter(Boolean)
  }
  if (!value) return []
  if (typeof value === 'object') {
    return parseOpeningHoursValue(
      value.weekday_text ||
        value.weekdayText ||
        value.WeekdayText ||
        value.weekdayDescriptions ||
        value.WeekdayDescriptions ||
        value.weekday_descriptions ||
        value.currentOpeningHours ||
        value.CurrentOpeningHours ||
        value.regularOpeningHours ||
        value.RegularOpeningHours ||
        value.openingHours ||
        value.OpeningHours ||
        value.periods ||
        value.Periods ||
        value.text ||
        value.Text,
    )
  }

  const text = String(value).trim()
  if (!text) return []
  if (text.startsWith('[') || text.startsWith('{')) {
    try {
      return parseOpeningHoursValue(JSON.parse(text))
    } catch {
      return [text]
    }
  }
  return [text]
}

const formatOpeningHoursList = (value) => {
  const hours = parseOpeningHoursValue(value)
  return hours.length ? hours : ['營業時間未提供']
}

const getFirstNonBlankValue = (...values) =>
  values.find((value) => value !== null && value !== undefined && String(value).trim() !== '')

const formatPriceLevelLabel = (value) => {
  if (value === null || value === undefined || value === '') return '價格未提供'
  const text = String(value).trim()
  if (!text) return '價格未提供'

  const priceLevelLabels = {
    PRICE_LEVEL_FREE: '免費',
    PRICE_LEVEL_INEXPENSIVE: '平價',
    PRICE_LEVEL_MODERATE: '中等價位',
    PRICE_LEVEL_EXPENSIVE: '高價位',
    PRICE_LEVEL_VERY_EXPENSIVE: '高級價位',
    FREE: '免費',
    INEXPENSIVE: '平價',
    MODERATE: '中等價位',
    EXPENSIVE: '高價位',
    VERY_EXPENSIVE: '高級價位',
    0: '免費',
    1: '平價',
    2: '中等價位',
    3: '高價位',
    4: '高級價位',
  }
  const normalizedText = text.toUpperCase()
  return priceLevelLabels[normalizedText] || priceLevelLabels[text] || text
}

const selectedPlace = computed(() => {
  if (!selectedMapPlace.value) return {}
  const hoursList = formatOpeningHoursList(selectedMapPlace.value.openingHours)
  return {
    name: selectedMapPlace.value.title,
    rating: selectedMapPlace.value.rating ?? '尚無評分',
    reviews: selectedMapPlace.value.userRatingCount?.toLocaleString?.('zh-TW') || '0',
    category: selectedMapPlace.value.category || '景點',
    address: selectedMapPlace.value.address || '尚未提供地址',
    phone: selectedMapPlace.value.phone || '尚未提供電話',
    hours: hoursList[0],
    hoursList,
    price: formatPriceLevelLabel(selectedMapPlace.value.priceLevel),
    imageUrl:
      selectedMapPlace.value.photoUrl ||
      plan.value?.PhotoURL ||
      'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80',
  }
})

const tripTitle = computed(() => plan.value?.TripName || '行程規劃')
const tripTag = computed(() => String(plan.value?.TripTag || '').trim())
const tripDescription = computed(() => String(plan.value?.TripDesc || '').trim())
const ownerName = computed(() => plan.value?.OwnerName || '')
const getPlanStartDate = () =>
  toInputDate(getPlanValue(plan.value, 'StartDate', 'startDate', 'StartDateTime', 'startDateTime'))
const getPlanEndDate = () =>
  toInputDate(getPlanValue(plan.value, 'EndDate', 'endDate', 'EndDateTime', 'endDateTime'))
const getPlanDateCount = () => getDateRangeDates(getPlanStartDate(), getPlanEndDate()).length
const dateRange = computed(() => {
  const start = formatDateLabel(getPlanStartDate())
  const end = formatDateLabel(getPlanEndDate())
  if (start && end) return `${start} - ${end}`
  return start
})
const duration = computed(() => (days.value.length ? `${days.value.length} 天` : '尚未設定天數'))
const selectedDayId = ref(days.value[0]?.id ?? null)
const activeDayIndex = computed(() => {
  const index = days.value.findIndex((day) => String(day.id) === String(selectedDayId.value))
  return index === -1 ? 0 : index
})
const activeDay = computed(() => days.value[activeDayIndex.value])
const activeDayNumber = computed(() => activeDay.value?.dayNumber ?? activeDayIndex.value + 1)
const activeItineraryItems = computed(() =>
  activeDay.value
    ? itineraryItems.value
        .filter(
          (item) =>
            item.draftStatus !== DRAFT_STATUS.deleted &&
            Number(item.dayNumber || 1) === Number(activeDayNumber.value),
        )
        .sort((a, b) => Number(a.sortOrder || 0) - Number(b.sortOrder || 0))
    : [],
)
const timelineRows = computed(() =>
  activeItineraryItems.value.map((item, index) => ({
    item,
    index,
    transfer: getTransferAfterItem(item, index),
  })),
)
const totalCost = computed(() =>
  activeItineraryItems.value.reduce((sum, item) => sum + item.cost, 0),
)
const getDayItineraryItems = (day) =>
  day
    ? itineraryItems.value
        .filter(
          (item) =>
            item.draftStatus !== DRAFT_STATUS.deleted &&
            Number(item.dayNumber || 1) === Number(day.dayNumber || 1),
        )
        .sort((a, b) => Number(a.sortOrder || 0) - Number(b.sortOrder || 0))
    : []
const getDaySummaryStops = (day) =>
  getDayItineraryItems(day)
    .slice(0, 2)
    .map((item) => ({ category: item.category || '景點', name: item.title }))
const getDaySummaryExtraCount = (day) => Math.max(getDayItineraryItems(day).length - 2, 0)
const activeDateLabel = computed(() => activeDay.value?.date || '未設定日期')
const activeDayLabel = computed(() =>
  activeDay.value ? dayLabel(activeDayIndex.value) : '尚未設定天數',
)
const discussionThreadCount = computed(() => discussionMessages.value.length)
const discussionThreads = computed(() => {
  const roots = []
  const byId = new Map()

  discussionMessages.value.forEach((message) => {
    byId.set(String(message.id), { ...message, replies: [] })
  })

  byId.forEach((message) => {
    const parentKey = String(message.parentId || '')
    if (parentKey && byId.has(parentKey)) {
      byId.get(parentKey).replies.push(message)
    } else {
      roots.push(message)
    }
  })

  return roots
})

const formatCost = (value) => Number(value || 0).toLocaleString('zh-TW')
const parseDurationMinutes = (value) => {
  if (typeof value === 'number') return Number.isFinite(value) ? value : 60
  const text = String(value || '').trim()
  const hourMatch = text.match(/(\d+(?:\.\d+)?)\s*小時/)
  const minuteMatch = text.match(/(\d+)\s*分/)
  const hours = hourMatch ? Number(hourMatch[1]) * 60 : 0
  const minutes = minuteMatch ? Number(minuteMatch[1]) : 0
  const directNumber = Number(text)
  return Math.max(
    0,
    Math.round(hours + minutes || (Number.isFinite(directNumber) ? directNumber : 60)),
  )
}
const formatDurationMinutes = (minutes) => `${Math.max(0, Number(minutes) || 0)} 分鐘`
const addMinutesToTime = (time, minutes) => {
  const [hour = 9, minute = 0] = String(time || '09:00')
    .split(':')
    .map(Number)
  const date = new Date(2000, 0, 1, hour, minute)
  date.setMinutes(date.getMinutes() + Number(minutes || 0))
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
}
const toOpeningHoursJson = (value) => {
  const hours = parseOpeningHoursValue(value)
  return hours.length ? JSON.stringify(hours) : null
}
const toApiTime = (value) => {
  const time = formatTimeLabel(value)
  return time ? `${time}:00` : '09:00:00'
}
const copyText = async (text) => {
  if (navigator?.clipboard?.writeText) {
    await navigator.clipboard.writeText(text)
    return
  }

  const textarea = document.createElement('textarea')
  textarea.value = text
  textarea.setAttribute('readonly', '')
  textarea.style.position = 'fixed'
  textarea.style.opacity = '0'
  document.body.appendChild(textarea)
  textarea.select()
  document.execCommand('copy')
  document.body.removeChild(textarea)
}

const getPlanHref = (targetTripId = tripId.value) => {
  const href = router.resolve({
    name: 'Plan',
    params: {
      userId: userId.value,
      tripId: targetTripId,
    },
  }).href

  return `${window.location.origin}${href}`
}

const openPackingDialog = () => {
  isPackingDialogOpen.value = true
}
const closePackingDialog = () => {
  isPackingDialogOpen.value = false
}
const openCreateMemoryDialog = () => {
  isCreateMemoryDialogOpen.value = true
}
const closeCreateMemoryDialog = () => {
  isCreateMemoryDialogOpen.value = false
}

const appendOptionalFormValue = (formData, key, value) => {
  if (value !== null && value !== undefined && value !== '') formData.append(key, value)
}

const applyPlanDateRangeByDayCount = (dayCount) => {
  if (!plan.value || !dayCount) return

  const startDate = getPlanStartDate() || toInputDate(new Date())
  const endDate = addDays(startDate, Math.max(0, Number(dayCount) - 1))

  plan.value = {
    ...plan.value,
    StartDate: startDate,
    EndDate: endDate,
  }
}

const buildPlanSettingsFormData = (settingsDraft) => {
  const privacyStatus = normalizePrivacyStatus(settingsDraft.PrivacyStatus)
  const isSuggest = resolvePrivacySuggest(privacyStatus)
  const formData = new FormData()

  formData.append('TripName', settingsDraft.TripName)
  formData.append('StartDate', settingsDraft.StartDate)
  formData.append('EndDate', settingsDraft.EndDate)
  formData.append('TripDesc', settingsDraft.TripDesc ?? '')
  formData.append('TripTag', settingsDraft.TripTag ?? '')
  formData.append('PrivacyStatus', String(privacyStatus))
  formData.append('IsSuggest', String(isSuggest))
  appendOptionalFormValue(formData, 'ShortCode', settingsDraft.ShortCode)
  appendOptionalFormValue(formData, 'ListId', settingsDraft.ListId)

  return { formData, privacyStatus, isSuggest }
}

const rememberSavedPlanDateRange = () => {
  savedPlanDateRange.value = {
    start: getPlanStartDate(),
    end: getPlanEndDate(),
  }
}

const isPlanDateRangeChanged = () =>
  getPlanStartDate() !== savedPlanDateRange.value.start ||
  getPlanEndDate() !== savedPlanDateRange.value.end

const ensurePlanDateRangeForDayNumber = async (dayNumber) => {
  const currentPlan = plan.value
  const currentDateCount = getPlanDateCount()

  if (!currentPlan || (Number(dayNumber || 0) <= currentDateCount && !isPlanDateRangeChanged())) {
    return false
  }

  const safeDayNumber = Math.max(1, Number(dayNumber) || 1)
  const startDate = getPlanStartDate() || toInputDate(new Date())
  const endDate = addDays(startDate, safeDayNumber - 1)
  const settingsDraft = {
    ...currentPlan,
    TripID: currentPlan.TripID ?? currentPlan.tripId ?? tripId.value,
    TripName: getPlanValue(currentPlan, 'TripName', 'tripName') || tripTitle.value,
    TripTag: getPlanValue(currentPlan, 'TripTag', 'tripTag') || '',
    TripDesc: getPlanValue(currentPlan, 'TripDesc', 'tripDesc') || '',
    StartDate: startDate,
    EndDate: endDate,
    CreateAt: getPlanCreateAt(currentPlan) || '',
    UpdateAt: getPlanUpdateAt(currentPlan) || '',
    PrivacyStatus: normalizePrivacyStatus(
      getPlanValue(currentPlan, 'PrivacyStatus', 'privacyStatus'),
    ),
    ShortCode: getPlanValue(currentPlan, 'ShortCode', 'shortCode') || '',
    ListId: currentPlan.ListId ?? currentPlan.ListID ?? null,
  }
  const { formData, privacyStatus, isSuggest } = buildPlanSettingsFormData(settingsDraft)
  const payload = await updatePlan(userId.value, settingsDraft.TripID, formData)
  const responseData = unwrapPlanPayload(payload)
  const savedPlan = Array.isArray(responseData) ? responseData[0] : responseData

  plan.value = {
    ...currentPlan,
    ...settingsDraft,
    ...(savedPlan && typeof savedPlan === 'object' && savedPlan),
    CreateAt:
      getPlanCreateAt(savedPlan) || getPlanCreateAt(settingsDraft) || getPlanCreateAt(currentPlan),
    UpdateAt:
      getPlanUpdateAt(savedPlan) || getPlanUpdateAt(settingsDraft) || getPlanUpdateAt(currentPlan),
    PrivacyStatus: privacyStatus,
    IsSuggest: isSuggest,
    StartDate: startDate,
    EndDate: endDate,
  }
  rememberSavedPlanDateRange()
  syncDaysWithDateRange(days.value)
  return true
}

const copyShareLink = async (settingsDraft = {}) => {
  try {
    await copyText(getPlanHref(settingsDraft.TripID || tripId.value))
    settingsCopyMessage.value = '已複製行程連結'
  } catch {
    settingsCopyMessage.value = '複製失敗，請稍後再試'
  }
}

const openPlanSettings = () => {
  if (!plan.value) return
  settingsCopyMessage.value = ''
  isPlanSettingsOpen.value = true
}

const closePlanSettings = () => {
  isPlanSettingsOpen.value = false
  settingsCopyMessage.value = ''
}

const goToOwner = () => {
  router.push({ name: 'MemberProfile' })
}

const savePlanSettings = async ({ draftPlan: settingsDraft, photoFile }) => {
  const currentPlan = plan.value
  const tripName = settingsDraft.TripName.trim()
  if (!currentPlan || isSavingSettings.value) return

  if (!tripName) {
    toast('請輸入旅行名稱', 'error')
    return
  }
  if (!settingsDraft.StartDate || !settingsDraft.EndDate) {
    toast('缺少行程日期，請重新整理後再試', 'error')
    return
  }
  if (settingsDraft.EndDate < settingsDraft.StartDate) {
    toast('結束日期不可早於開始日期', 'error')
    return
  }

  const { formData, privacyStatus, isSuggest } = buildPlanSettingsFormData({
    ...settingsDraft,
    TripName: tripName,
  })
  if (photoFile) formData.append('Photo', photoFile)

  isSavingSettings.value = true
  try {
    const payload = await updatePlan(userId.value, settingsDraft.TripID, formData)
    const responseData = unwrapPlanPayload(payload)
    const savedPlan = Array.isArray(responseData) ? responseData[0] : responseData
    const fallbackPlan = {
      ...currentPlan,
      TripID: settingsDraft.TripID,
      TripName: tripName,
      TripTag: settingsDraft.TripTag,
      TripDesc: settingsDraft.TripDesc,
      StartDate: settingsDraft.StartDate,
      EndDate: settingsDraft.EndDate,
      CreateAt: getPlanCreateAt(currentPlan),
      UpdateAt: getPlanUpdateAt(currentPlan),
      PrivacyStatus: privacyStatus,
      IsSuggest: isSuggest,
      OfficeOper: currentPlan.OfficeOper,
      ShortCode: settingsDraft.ShortCode,
      ListID: settingsDraft.ListId,
      ListId: settingsDraft.ListId,
      PhotoURL: currentPlan.PhotoURL,
      Collaborators: [...settingsDraft.Collaborators],
    }
    plan.value = {
      ...fallbackPlan,
      ...(savedPlan && typeof savedPlan === 'object' && savedPlan),
      CreateAt: getPlanCreateAt(savedPlan) || getPlanCreateAt(fallbackPlan),
      UpdateAt: getPlanUpdateAt(savedPlan) || getPlanUpdateAt(fallbackPlan),
      OwnerIconURL: savedPlan?.OwnerIconURL || currentPlan.OwnerIconURL,
      OwnerName: savedPlan?.OwnerName || currentPlan.OwnerName,
      Collaborators: savedPlan?.Collaborators || fallbackPlan.Collaborators,
    }
    rememberSavedPlanDateRange()
    syncDaysWithDateRange(days.value)
    closePlanSettings()
    toast('行程設定已儲存')
  } catch (error) {
    toast(error?.message || error?.Message || '儲存失敗，請稍後再試', 'error')
  } finally {
    isSavingSettings.value = false
  }
}

const deleteCurrentPlan = async (settingsDraft = {}) => {
  if (isDeletingPlan.value || isSavingSettings.value) return
  const result = await luiuNotify.confirmDelete(
    '確定要刪除此行程嗎?',
    '刪除後此行程將不會出現在清單中。',
  )
  if (!result.isConfirmed) return

  isDeletingPlan.value = true
  try {
    await deletePlan(userId.value, settingsDraft.TripID || tripId.value)
    closePlanSettings()
    toast('行程已刪除')
    router.push({ name: 'PlanList', params: { userId: userId.value } })
  } catch (error) {
    toast(error?.message || error?.Message || '刪除失敗，請稍後再試', 'error')
  } finally {
    isDeletingPlan.value = false
  }
}

const selectDay = async (day) => {
  if (String(day?.id) === String(selectedDayId.value)) return
  const previousDayNumber = activeDayNumber.value
  const shouldKeepPlanning = isRoutePlanningActive.value
  clearRouteState(previousDayNumber)
  isRoutePlanningActive.value = shouldKeepPlanning
  selectedDayId.value = day?.id ?? null
  await nextTick()
  await rerouteIfPlanningActive()
}
const toNumberOrNull = (value) => {
  const n = Number(value)
  return Number.isFinite(n) ? n : null
}
const getPlacePosition = (place) => {
  const lat = toNumberOrNull(place?.lat)
  const lng = toNumberOrNull(place?.lng)
  return lat === null || lng === null ? null : { lat, lng }
}
const normalizeMapPlace = (place = {}) => ({
  id: place.id ?? place.DetailID ?? place.spotID ?? place.SpotID ?? place.spotId ?? null,
  spotId: place.spotID ?? place.SpotID ?? place.spotId ?? null,
  regionId: place.regionId ?? place.RegionID ?? place.regionID ?? null,
  title:
    place.name ||
    place.title ||
    place.mainText ||
    place.description ||
    place.SpotAlias ||
    place.SpotName ||
    place.Spot?.SpotName ||
    place.Spot?.spotName ||
    place.spotName ||
    '未命名地點',
  category:
    place.category || place.Category || place.Spot?.Category || place.Spot?.category || '景點',
  address:
    place.address ||
    place.Address ||
    place.formatted_address ||
    place.secondaryText ||
    place.description ||
    '',
  phone:
    place.phone ||
    place.Tel ||
    place.tel ||
    place.Spot?.Tel ||
    place.Spot?.tel ||
    place.formatted_phone_number ||
    '',
  rating: place.rating ?? place.Rating ?? place.Spot?.Rating ?? place.Spot?.rating ?? null,
  userRatingCount:
    place.userRatingCount ??
    place.UserRatingCount ??
    place.Spot?.UserRatingCount ??
    place.Spot?.userRatingCount ??
    place.user_ratings_total ??
    null,
  openingHours:
    place.openingHours ||
    place.OpeningHours ||
    place.hours ||
    place.Hours ||
    place.OpeningHoursJson ||
    place.openingHoursJson ||
    place.Spot?.OpeningHoursJson ||
    place.Spot?.openingHoursJson ||
    place.opening_hours?.weekday_text ||
    place.opening_hours?.weekdayDescriptions ||
    place.opening_hours?.weekday_descriptions ||
    place.currentOpeningHours?.weekdayDescriptions ||
    place.CurrentOpeningHours?.WeekdayDescriptions ||
    place.regularOpeningHours?.weekdayDescriptions ||
    place.RegularOpeningHours?.WeekdayDescriptions ||
    place.openingHours?.weekdayDescriptions ||
    place.OpeningHours?.WeekdayDescriptions ||
    place.weekdayDescriptions ||
    place.WeekdayDescriptions ||
    place.weekday_text ||
    place.WeekdayText ||
    '營業時間未提供',
  placeId: place.placeId || place.GoogleMapID || place.googleMapID || place.place_id || '',
  googleMapUrl: place.googleMapUrl || place.GoogleMapURL || place.googleMapURL || place.url || '',
  officialUrl: place.officialUrl || place.OfficialURL || place.website || '',
  priceLevel:
    place.priceLevel ??
    place.PriceLevel ??
    place.price_level ??
    place.PRICE_LEVEL ??
    place.priceLevelText ??
    place.PriceLevelText ??
    null,
  photoUrl: getPlaceImageUrl(place),
  photoReference:
    place.photoReference ||
    place.PhotoReference ||
    place.photos?.[0]?.name ||
    place.Photos?.[0]?.Name ||
    '',
  lat: toNumberOrNull(
    place.lat ?? place.Lat ?? place.Latitude ?? place.latitude ?? place.Spot?.Latitude,
  ),
  lng: toNumberOrNull(
    place.lng ?? place.Lng ?? place.Longitude ?? place.longitude ?? place.Spot?.Longitude,
  ),
})

const selectAndFocusPlace = (place) => {
  const normalized = normalizeMapPlace(place)
  selectedMapPlace.value = normalized
  selectedMarkerId.value = normalized.id
  isPlaceCardOpen.value = true
  updateMarkerStyles()
  const position = getPlacePosition(normalized)
  if (googleMap && position) {
    googleMap.panTo(position)
  }
}

const closePlaceCard = () => {
  isPlaceCardOpen.value = false
  selectedMapPlace.value = null
  selectedMarkerId.value = null
  updateMarkerStyles()
}

const showMapStatus = (message, durationMs = 2600) => {
  mapStatusMessage.value = message
  clearTimeout(mapStatusTimer)
  mapStatusTimer = setTimeout(() => {
    mapStatusMessage.value = ''
  }, durationMs)
}

const {
  cleanupMapMarkers,
  getPrimaryMapColor,
  renderFavoriteSpotMarkers,
  renderMapMarkers,
  updateMarkerStyles,
} = usePlanMapMarkers({
  activeItineraryItems,
  clearMapStatus: () => {
    mapStatusMessage.value = ''
  },
  favoriteSpots,
  getMap: () => googleMap,
  getMarkerConstructor: () => markerConstructor,
  getPlacePosition,
  selectedMarkerId,
  selectAndFocusPlace,
  showFavoriteSpots,
  showMapStatus,
})

const getApiErrorMessage = (error, fallback) =>
  error?.message || error?.Message || error?.title || error?.Title || fallback

const showDiscussionStatus = (message, durationMs = 2600) => {
  discussionStatusMessage.value = message
  clearTimeout(discussionStatusTimer)
  discussionStatusTimer = setTimeout(() => {
    discussionStatusMessage.value = ''
  }, durationMs)
}

const handleMapClick = async (event) => {
  if (event.placeId) {
    event.stop()
    mapStatusMessage.value = '正在取得地點資訊...'
    try {
      const place = await getGoogleMapPlace(event.placeId)
      const position = event.latLng ? { lat: event.latLng.lat(), lng: event.latLng.lng() } : null
      const normalized = normalizeMapPlace({
        ...place,
        lat: place?.lat ?? position?.lat,
        lng: place?.lng ?? position?.lng,
      })
      selectedMarkerId.value = null
      selectedMapPlace.value = normalized
      isPlaceCardOpen.value = true
      updateMarkerStyles()
      if (googleMap && position) {
        googleMap.panTo(position)
      }
    } catch {
      showMapStatus('無法取得地點資訊')
    } finally {
      if (mapStatusMessage.value === '正在取得地點資訊...') mapStatusMessage.value = ''
    }
  } else {
    closePlaceCard()
  }
}

const normalizeBackendSearchResults = (payload) => {
  const data = unwrapApiData(payload)
  if (Array.isArray(data)) return data.map(normalizeMapPlace)
  const results = data?.results || data?.Results || data?.places || data?.Places
  if (Array.isArray(results)) return results.map(normalizeMapPlace)
  return data ? [normalizeMapPlace(data)] : []
}

const selectedPlaceSpotId = computed(() => toFavoriteTargetId(selectedMapPlace.value?.spotId))
const isSelectedPlaceFavorite = computed(
  () => selectedPlaceSpotId.value !== null && isFavoriteSpotTarget(selectedPlaceSpotId.value),
)
const isSavingPlaceFavorite = computed(
  () => isResolvingFavoriteSpot.value || isSavingSpotFavorite.value,
)

const resolveSelectedPlaceSpotId = async () => {
  const existingSpotId = selectedPlaceSpotId.value
  if (existingSpotId) return existingSpotId

  const googleMapId = selectedMapPlace.value?.placeId
  if (!googleMapId) return null

  const place = await getGoogleMapPlace(googleMapId)
  const normalized = normalizeMapPlace(place)
  const spotId = toFavoriteTargetId(normalized.spotId)

  if (spotId) {
    selectedMapPlace.value = normalizeMapPlace({
      ...selectedMapPlace.value,
      ...normalized,
      spotID: spotId,
    })
  }

  return spotId
}

const addPlaceToFavorites = async () => {
  if (!selectedMapPlace.value || isSavingPlaceFavorite.value) return

  isResolvingFavoriteSpot.value = true

  try {
    const spotId = await resolveSelectedPlaceSpotId()
    if (!spotId) {
      showMapStatus('目前無法收藏此地點，請稍後再試')
      return
    }

    const nextFavoriteState = await toggleSpotFavorite(spotId)
    await loadFavoriteSpots()
    showMapStatus(nextFavoriteState ? '已加入收藏' : '已取消收藏')
  } catch (error) {
    showMapStatus(error?.message || error?.Message || '收藏狀態更新失敗，請稍後再試')
  } finally {
    isResolvingFavoriteSpot.value = false
  }
}

const normalizeFavoriteSpot = (favorite = {}) => {
  const targetId = favorite.targetId ?? favorite.TargetID ?? favorite.TargetId
  const spot = favorite.spot ?? favorite.Spot ?? {}
  return normalizeMapPlace({
    ...spot,
    ...favorite,
    id: `favorite-${targetId ?? favorite.collectId ?? favorite.CollectID ?? favorite.title}`,
    spotId: targetId,
    SpotID: targetId,
    name: favorite.title ?? favorite.Title ?? favorite.SpotName ?? spot.SpotName ?? spot.spotName,
    address: favorite.subTitle ?? favorite.SubTitle ?? favorite.Address ?? spot.Address,
    imageUrl: favorite.imageUrl ?? favorite.ImageUrl ?? favorite.ImageURL ?? spot.PhotoUrl,
  })
}

const getFavoriteSpotItems = (payload) => {
  const data = unwrapApiData(payload)
  if (Array.isArray(data)) return data
  if (Array.isArray(data?.items)) return data.items
  if (Array.isArray(data?.Items)) return data.Items
  return []
}

const loadFavoriteSpots = async () => {
  isLoadingFavoriteSpots.value = true
  try {
    const payload = await getFavoritesApi()
    favoriteSpots.value = getFavoriteSpotItems(payload)
      .filter((item) => item?.type === 'Spot' || item?.Type === 'Spot')
      .map(normalizeFavoriteSpot)
  } catch {
    favoriteSpots.value = []
  } finally {
    isLoadingFavoriteSpots.value = false
    renderFavoriteSpotMarkers()
  }
}

const toggleFavoriteSpotMarkers = async () => {
  showFavoriteSpots.value = !showFavoriteSpots.value
  if (showFavoriteSpots.value && !favoriteSpots.value.length) await loadFavoriteSpots()
  renderFavoriteSpotMarkers()
}

const createMapSearchSessionToken = () =>
  window.crypto?.randomUUID?.() || `${Date.now()}-${Math.random().toString(16).slice(2)}`

const ensureMapSearchSessionToken = () => {
  if (!mapSearchSessionToken.value) mapSearchSessionToken.value = createMapSearchSessionToken()
  return mapSearchSessionToken.value
}

const selectBackendSearchResult = async (place) => {
  const normalizedPlace = normalizeMapPlace(place)
  const googleMapId = normalizedPlace.placeId
  let googlePlace = null
  let savedPlace = null
  if (googleMapId) {
    try {
      googlePlace = await getGoogleMapPlace(googleMapId)
    } catch {
      googlePlace = null
    }
    try {
      savedPlace = await getPlaceByGoogleMapId(googleMapId)
    } catch {
      savedPlace = null
    }
  }

  const normalizedGooglePlace = googlePlace ? normalizeMapPlace(googlePlace) : null
  const normalizedSavedPlace = savedPlace ? normalizeMapPlace(savedPlace) : null
  mapSearchQuery.value = normalizedPlace.title
  mapSearchResults.value = []
  mapSearchSessionToken.value = ''
  selectAndFocusPlace({
    ...normalizedPlace,
    ...normalizedGooglePlace,
    ...normalizedSavedPlace,
    title: normalizedSavedPlace?.title || normalizedGooglePlace?.title || normalizedPlace.title,
    placeId: googleMapId || savedPlace?.GoogleMapID || savedPlace?.googleMapID || '',
    lat:
      toNumberOrNull(savedPlace?.lat ?? savedPlace?.Lat ?? savedPlace?.Latitude) ??
      normalizedGooglePlace?.lat ??
      normalizedPlace.lat,
    lng:
      toNumberOrNull(savedPlace?.lng ?? savedPlace?.Lng ?? savedPlace?.Longitude) ??
      normalizedGooglePlace?.lng ??
      normalizedPlace.lng,
    priceLevel: getFirstNonBlankValue(
      normalizedSavedPlace?.priceLevel,
      normalizedGooglePlace?.priceLevel,
      normalizedPlace.priceLevel,
    ),
  })
}

const handleMapSearchInput = () => {
  clearTimeout(mapSearchDebounceTimer)
  const input = mapSearchQuery.value.trim()
  if (input.length < 2) {
    mapSearchResults.value = []
    return
  }

  mapSearchDebounceTimer = setTimeout(async () => {
    isMapSearchLoading.value = true
    try {
      mapSearchResults.value = normalizeBackendSearchResults(
        await autocompletePlaces(input, ensureMapSearchSessionToken()),
      )
    } catch {
      mapSearchResults.value = []
    } finally {
      isMapSearchLoading.value = false
    }
  }, 300)
}

const handleMapSearch = async () => {
  const query = mapSearchQuery.value.trim()
  if (!query) {
    showMapStatus('請先輸入要搜尋的地點')
    return
  }

  isMapSearchLoading.value = true
  clearTimeout(mapSearchDebounceTimer)
  mapStatusMessage.value = '正在搜尋地點...'
  try {
    const results = normalizeBackendSearchResults(await searchPlaces(query))
    mapSearchResults.value = results
    if (!results.length) {
      showMapStatus('找不到符合的地點')
      return
    }

    if (results.length === 1) await selectBackendSearchResult(results[0])
    mapStatusMessage.value = ''
  } catch {
    mapSearchResults.value = []
    showMapStatus('後端地點搜尋失敗，請稍後再試')
  } finally {
    isMapSearchLoading.value = false
  }
}

const initializeMap = async () => {
  if (!mapElement.value) return
  mapErrorMessage.value = ''
  mapStatusMessage.value = '地圖載入中...'
  try {
    const { Map } = await googleMaps.loadMaps()
    const mapId = import.meta.env.VITE_GOOGLE_MAPS_MAP_ID
    const { AdvancedMarkerElement } = await googleMaps.loadMarker()
    markerConstructor =
      mapId && AdvancedMarkerElement ? AdvancedMarkerElement : window.google.maps.Marker

    googleMap = new Map(mapElement.value, {
      center: KAOHSIUNG_CENTER,
      zoom: DEFAULT_MAP_ZOOM,
      mapId: mapId || undefined,
      clickableIcons: true,
      streetViewControl: false,
      mapTypeControl: false,
      fullscreenControl: true,
      zoomControl: false, // 移除右下角移動/縮放控制
      panControl: false,
    })

    mapClickListener = googleMap.addListener('click', handleMapClick)
    mapDragListener = googleMap.addListener('dragstart', closePlaceCard)
    mapZoomListener = googleMap.addListener('zoom_changed', closePlaceCard)
    renderMapMarkers()
  } catch {
    mapErrorMessage.value = 'Google Map 載入失敗。'
  } finally {
    mapStatusMessage.value = ''
  }
}

const cleanupMap = () => {
  clearRoutePolyline()
  cleanupMapMarkers()
  if (mapClickListener) window.google?.maps?.event?.removeListener(mapClickListener)
  if (mapDragListener) window.google?.maps?.event?.removeListener(mapDragListener)
  if (mapZoomListener) window.google?.maps?.event?.removeListener(mapZoomListener)
  mapClickListener = null
  mapDragListener = null
  mapZoomListener = null
  googleMap = null
  markerConstructor = null
  selectedMarkerId.value = null
}

// ── 路線 ───────────────────────────────────────────────────────────────────
const clearRoutePolyline = () => {
  routePolylines.forEach((polyline) => polyline.setMap(null))
  routeSpotMarkers.forEach((marker) => {
    marker.map = null
    if (marker.setMap) marker.setMap(null)
  })
  routePolylines = []
  routeSpotMarkers = []
}

const createRouteSpotMarker = (position, title) => {
  if (!googleMap || !markerConstructor || !position) return null
  const primaryColor = getPrimaryMapColor()
  const bodyBg = getComputedStyle(document.documentElement).getPropertyValue('--bs-body-bg').trim()
  if (
    markerConstructor?.name === 'AdvancedMarkerElement' ||
    markerConstructor?.toString?.().includes('AdvancedMarker')
  ) {
    const el = document.createElement('div')
    el.className = 'map-route-spot'
    el.style.borderColor = primaryColor || ''
    return new markerConstructor({ map: googleMap, position, title, content: el })
  }
  return new markerConstructor({
    map: googleMap,
    position,
    title,
    icon: {
      path: window.google.maps.SymbolPath.CIRCLE,
      scale: 5,
      fillColor: primaryColor || undefined,
      fillOpacity: 1,
      strokeColor: bodyBg || undefined,
      strokeWeight: 2,
    },
  })
}

const drawRoutePolylines = async (segments) => {
  const drawingSegments = segments
    .map((segment) => (typeof segment === 'string' ? { polyline: segment } : segment))
    .filter((segment) => segment?.polyline || segment?.type === 'WALK')
  if (!googleMap || !drawingSegments.length) {
    clearRoutePolyline()
    return
  }
  const { encoding } = await googleMaps.loadGeometry()
  clearRoutePolyline()
  const primaryColor = getPrimaryMapColor()

  const nextPolylines = []
  const nextSpotMarkers = []
  drawingSegments.forEach((segment) => {
    if (segment.type === 'WALK') {
      if (segment.polyline) {
        const path = encoding.decodePath(segment.polyline)
        if (path.length) {
          nextPolylines.push(
            new window.google.maps.Polyline({
              path,
              geodesic: true,
              ...(primaryColor && { strokeColor: primaryColor }),
              strokeOpacity: 0.72,
              strokeWeight: 4,
              map: googleMap,
            }),
          )
          return
        }
      }
      const spots = [
        toLatLngLiteral(segment.startLocation),
        toLatLngLiteral(segment.endLocation),
      ].filter(Boolean)
      if (spots.length === 2) {
        nextPolylines.push(
          new window.google.maps.Polyline({
            path: spots,
            geodesic: true,
            ...(primaryColor && { strokeColor: primaryColor }),
            strokeOpacity: 0.64,
            strokeWeight: 4,
            map: googleMap,
          }),
        )
      }
      spots.forEach((spot, index) => {
        const marker = createRouteSpotMarker(spot, index === 0 ? '步行起點' : '步行終點')
        if (marker) nextSpotMarkers.push(marker)
      })
      return
    }

    const path = encoding.decodePath(segment.polyline)
    if (!path.length) return
    nextPolylines.push(
      new window.google.maps.Polyline({
        path,
        geodesic: true,
        ...(primaryColor && { strokeColor: primaryColor }),
        strokeOpacity: 0.88,
        strokeWeight: 5,
        map: googleMap,
      }),
    )
  })
  routePolylines = nextPolylines
  routeSpotMarkers = nextSpotMarkers
}

// ── 留言 ───────────────────────────────────────────────────────────────────
const normalizeComment = (comment = {}) => ({
  id: comment.CommentID ?? comment.id ?? Date.now(),
  parentId: comment.ParentID ?? comment.parentId ?? comment.ParentId ?? null,
  author: comment.UserName || comment.author || '我',
  time: formatDateTimeLabel(
    comment.CreateAt || comment.CreateTime || comment.UpdateAt || comment.time || new Date(),
  ),
  message: comment.Content || comment.message || '',
  userIcon: comment.UserIcon || '',
  canEdit: Boolean(comment.CanEdit ?? comment.canEdit),
  canDelete: Boolean(comment.CanDelete ?? comment.canDelete),
})

const loadComments = async () => {
  try {
    const payload = await getTripComments(userId.value, tripId.value)
    discussionMessages.value = asArrayPayload(payload).map(normalizeComment)
  } catch {
    showDiscussionStatus('留言載入失敗')
  }
}

const submitMessage = async () => {
  const text = newMessageText.value.trim()
  if (!text || isSubmittingMessage.value) return
  isSubmittingMessage.value = true
  try {
    const payload = await createTripComment(userId.value, tripId.value, {
      Content: text,
      ParentID: null,
    })
    discussionMessages.value.push(normalizeComment(unwrapApiData(payload)))
    newMessageText.value = ''
  } catch {
    showDiscussionStatus('留言新增失敗，請稍後再試')
  } finally {
    isSubmittingMessage.value = false
  }
}

const startReplyComment = (message) => {
  replyingCommentId.value = message.id
  replyMessageText.value = ''
  isDiscussionOpen.value = true
}

const cancelReplyComment = () => {
  replyingCommentId.value = null
  replyMessageText.value = ''
}

const submitReply = async (message) => {
  const text = replyMessageText.value.trim()
  if (!text || isSubmittingMessage.value) return
  isSubmittingMessage.value = true
  try {
    const payload = await createTripComment(userId.value, tripId.value, {
      Content: text,
      ParentID: message.id,
    })
    discussionMessages.value.push(
      normalizeComment({
        ...unwrapApiData(payload),
        Content: text,
        ParentID: message.id,
      }),
    )
    cancelReplyComment()
  } catch {
    showDiscussionStatus('回覆新增失敗，請稍後再試')
  } finally {
    isSubmittingMessage.value = false
  }
}

const isCommentSaving = (commentId) => savingCommentIds.value.includes(commentId)
const isCommentDeleting = (commentId) => deletingCommentIds.value.includes(commentId)
const setBusyComment = (targetRef, commentId, isBusy) => {
  targetRef.value = isBusy
    ? [...targetRef.value, commentId]
    : targetRef.value.filter((id) => id !== commentId)
}
const startEditComment = (message) => {
  editingCommentId.value = message.id
  editingCommentText.value = message.message
}
const cancelEditComment = () => {
  editingCommentId.value = null
  editingCommentText.value = ''
}
const saveEditComment = async (message) => {
  const text = editingCommentText.value.trim()
  if (!text || isCommentSaving(message.id)) return
  setBusyComment(savingCommentIds, message.id, true)
  try {
    const payload = await updateTripComment(userId.value, tripId.value, message.id, {
      Content: text,
      ParentID: message.parentId ?? null,
    })
    const updatedComment = normalizeComment({
      ...message,
      ...unwrapApiData(payload),
      Content: text,
    })
    discussionMessages.value = discussionMessages.value.map((item) =>
      item.id === message.id ? { ...item, ...updatedComment } : item,
    )
    cancelEditComment()
  } catch {
    showDiscussionStatus('留言編輯失敗，請稍後再試')
  } finally {
    setBusyComment(savingCommentIds, message.id, false)
  }
}
const removeComment = async (message) => {
  if (isCommentDeleting(message.id)) return
  setBusyComment(deletingCommentIds, message.id, true)
  try {
    await deleteTripComment(userId.value, tripId.value, message.id)
    discussionMessages.value = discussionMessages.value.filter(
      (item) => item.id !== message.id && item.parentId !== message.id,
    )
    if (editingCommentId.value === message.id) cancelEditComment()
    if (replyingCommentId.value === message.id) cancelReplyComment()
  } catch {
    showDiscussionStatus('留言刪除失敗，請稍後再試')
  } finally {
    setBusyComment(deletingCommentIds, message.id, false)
  }
}

// ── 天數 CRUD ──────────────────────────────────────────────────────────────
const openDayDialog = () => {
  const lastDay = days.value[days.value.length - 1]
  dayForm.intro = ''
  dayForm.date = lastDay ? addDays(lastDay.date, 1) : getPlanStartDate()
  addDay()
}
const openEditDayDialog = (day) => {
  editingDay.value = day
  editDayForm.intro = day.intro
  editDayForm.date = day.date
  activeDialog.value = 'editDay'
}
const closeDialog = () => {
  activeDialog.value = null
  editingDay.value = null
  editingItineraryId.value = null
  editingTransferId.value = null
  editingTransferRoute.value = null
}
const addDay = () => {
  const newId = Math.max(0, ...days.value.map((d) => d.id)) + 1
  const nextDayNumber = Math.max(0, ...days.value.map((d) => Number(d.dayNumber || 0))) + 1
  const lastDay = days.value[days.value.length - 1]
  const nextDate = dayForm.date || (lastDay ? addDays(lastDay.date, 1) : getPlanStartDate())
  days.value.push({
    id: newId,
    dayNumber: nextDayNumber,
    intro: dayForm.intro,
    date: nextDate,
    summaryStops: [],
  })
  applyPlanDateRangeByDayCount(days.value.length)
  syncDaysWithDateRange(days.value)
  selectedDayId.value = newId
  closeDialog()
}
const saveEditDay = () => {
  if (!editingDay.value) return
  const target = days.value.find((d) => d.id === editingDay.value.id)
  if (target) {
    target.intro = editDayForm.intro
    target.date = editDayForm.date
  }
  closeDialog()
}
const openItineraryDialog = (item) => {
  editingItineraryId.value = item.id
  itineraryForm.alias = item.title || ''
  itineraryForm.notes = item.description ?? ''
  itineraryForm.durationMinutes = parseDurationMinutes(item.duration)
  itineraryForm.cost = Number(item.cost || 0)
  activeDialog.value = 'itinerary'
}
const saveItinerary = async () => {
  const targetId = editingItineraryId.value
  if (!targetId) return
  itineraryItems.value = itineraryItems.value.map((item) =>
    item.id === targetId
      ? markDraftUpdated({
          ...item,
          title: itineraryForm.alias.trim() || item.title,
          description: itineraryForm.notes.trim(),
          duration: formatDurationMinutes(itineraryForm.durationMinutes),
          cost: Number(itineraryForm.cost || 0),
        })
      : item,
  )
  closeDialog()
  recalculateActiveItineraryTimes()
  await rerouteIfPlanningActive()
}
const removeDay = (id) => {
  const targetDay = days.value.find((day) => String(day.id) === String(id))
  if (!targetDay) return

  const removedDayNumber = Number(targetDay.dayNumber || 1)
  const removedDayIndex = days.value.findIndex((day) => String(day.id) === String(id))
  const maxAffectedDayNumber = Math.max(
    removedDayNumber,
    ...itineraryItems.value.map((item) => Number(item.dayNumber || 1)),
  )

  for (let dayNumber = removedDayNumber; dayNumber <= maxAffectedDayNumber; dayNumber += 1) {
    clearRouteState(dayNumber)
  }

  itineraryItems.value = itineraryItems.value
    .map((item) => {
      const itemDayNumber = Number(item.dayNumber || 1)

      if (itemDayNumber < removedDayNumber) return item
      if (itemDayNumber === removedDayNumber) {
        return item.draftStatus === DRAFT_STATUS.created
          ? null
          : { ...item, draftStatus: DRAFT_STATUS.deleted }
      }

      return markDraftUpdated({
        ...item,
        dayNumber: itemDayNumber - 1,
      })
    })
    .filter(Boolean)

  days.value = days.value.filter((day) => String(day.id) !== String(id))
  applyPlanDateRangeByDayCount(days.value.length)
  syncDaysWithDateRange(days.value)
  selectedDayId.value = days.value[Math.min(removedDayIndex, days.value.length - 1)]?.id ?? null
  recalculateActiveItineraryTimes()
}

const addTripDetailToItinerary = async (detail) => {
  const previousRows = [...activeTransferRows.value]
  const normalizedDetail = {
    ...normalizeTripDetail(detail),
    id: detail.id ?? detail.DetailID ?? detail.detailId,
    detailId: detail.detailId ?? detail.DetailID ?? null,
    draftStatus: detail.draftStatus ?? DRAFT_STATUS.clean,
  }
  itineraryItems.value.push(normalizedDetail)
  await nextTick()
  recalculateActiveItineraryTimes()
  return rerouteIfPlanningActive(previousRows)
}

const cloneItineraryItems = (items) => items.map((item) => ({ ...item }))

const getOriginalItem = (item) =>
  originalItineraryItems.value.find((original) => String(original.id) === String(item.id))

const markDraftUpdated = (item) => {
  if (item.draftStatus === DRAFT_STATUS.created || item.draftStatus === DRAFT_STATUS.deleted) {
    return item
  }

  return { ...item, draftStatus: DRAFT_STATUS.updated }
}

const buildTripDetailPayloadFromItem = (item) => {
  const transportPayload = getTripDetailTransportPayload(item)
  return {
    DetailID: item.detailId ?? undefined,
    SpotId: item.spotId ?? 0,
    Spot: {
      GoogleMapID: item.placeId || '',
      RegionID: item.regionId ?? null,
      MemberID: null,
      SpotName: item.title,
      Longitude: item.lng,
      Latitude: item.lat,
      Tel: item.phone || null,
      Address: item.address || null,
      OfficialURL: item.officialUrl || null,
      OpeningHoursJson: toOpeningHoursJson(item.openingHours),
      Rating: item.rating,
      UserRatingCount: item.userRatingCount,
      GoogleMapURL: item.googleMapUrl || null,
      PriceLevel: item.priceLevel,
      PhotoUrl: item.photoUrl || null,
      PhotoReference: item.photoReference || null,
    },
    SpotAlias: item.title || null,
    Notes: item.description || '',
    DayNumber: item.dayNumber,
    SortOrder: item.sortOrder,
    ArrivalTime: toApiTime(item.time),
    StayDuration: parseDurationMinutes(item.duration),
    Budget: Number(item.cost || 0),
    TransportMode: transportPayload.TransportMode,
    TransportTime: transportPayload.TransportTime,
    VersionId: item.versionId ?? 1,
    IsMaster: false,
    SuggestBy: null,
  }
}

const isItemChanged = (item) => {
  const original = getOriginalItem(item)
  if (!original) return false
  const transportPayload = getTripDetailTransportPayload(item)

  return (
    Number(item.dayNumber || 1) !== Number(original.dayNumber || 1) ||
    Number(item.sortOrder || 0) !== Number(original.sortOrder || 0) ||
    Number(transportPayload.TransportMode || 0) !== Number(original.transportMode || 0) ||
    Number(transportPayload.TransportTime || 0) !== Number(original.transportTime || 0)
  )
}

const buildPlanDetailsSyncPayload = () => {
  const created = itineraryItems.value
    .filter((item) => item.draftStatus === DRAFT_STATUS.created)
    .map(buildTripDetailPayloadFromItem)
  const updated = itineraryItems.value
    .filter(
      (item) =>
        item.draftStatus !== DRAFT_STATUS.created &&
        item.draftStatus !== DRAFT_STATUS.deleted &&
        (item.draftStatus === DRAFT_STATUS.updated || isItemChanged(item)),
    )
    .map(buildTripDetailPayloadFromItem)
  const deletedDetailIds = itineraryItems.value
    .filter((item) => item.draftStatus === DRAFT_STATUS.deleted && item.detailId)
    .map((item) => item.detailId)

  return { created, updated, deletedDetailIds }
}

const getMaxVisibleDayNumber = () =>
  Math.max(
    1,
    days.value.length,
    ...itineraryItems.value
      .filter((item) => item.draftStatus !== DRAFT_STATUS.deleted)
      .map((item) => Number(item.dayNumber || 1)),
  )

const hasPlanDetailDraftChanges = computed(() => {
  const payload = buildPlanDetailsSyncPayload()
  return Boolean(
    payload.created.length || payload.updated.length || payload.deletedDetailIds.length,
  )
})

const findSavedPlace = async (googleMapId) => {
  if (!googleMapId) return null
  try {
    return await getPlaceByGoogleMapId(googleMapId)
  } catch {
    return null
  }
}

const buildPlanDetailPayload = (place, savedPlace) => {
  const normalized = normalizeMapPlace({ ...place, ...savedPlace })
  return {
    SpotId: normalized.spotId ?? 0,
    Spot: {
      GoogleMapID: normalized.placeId,
      RegionID: normalized.regionId,
      MemberID: null,
      SpotName: normalized.title,
      Longitude: normalized.lng,
      Latitude: normalized.lat,
      Tel: normalized.phone || null,
      Address: normalized.address || null,
      OfficialURL: normalized.officialUrl || null,
      OpeningHoursJson: toOpeningHoursJson(normalized.openingHours),
      Rating: normalized.rating,
      UserRatingCount: normalized.userRatingCount,
      GoogleMapURL: normalized.googleMapUrl || null,
      PriceLevel: normalized.priceLevel,
      PhotoUrl: normalized.photoUrl || null,
      PhotoReference: normalized.photoReference || null,
    },
    SpotAlias: null,
    Notes: '',
    DayNumber: activeDayNumber.value,
    SortOrder: activeItineraryItems.value.length + 1,
    ArrivalTime: toApiTime(place.time),
    StayDuration: 60,
    Budget: 0,
    TransportMode: null,
    TransportTime: null,
    VersionId: 1,
    IsMaster: false,
    SuggestBy: null,
  }
}

const buildDraftTripDetail = (payload) => ({
  DetailID: payload.DetailID,
  SpotID: payload.SpotId,
  SpotName: payload.Spot?.SpotName,
  Longitude: payload.Spot?.Longitude,
  Latitude: payload.Spot?.Latitude,
  Tel: payload.Spot?.Tel,
  Address: payload.Spot?.Address,
  OfficialURL: payload.Spot?.OfficialURL,
  OpeningHoursJson: payload.Spot?.OpeningHoursJson,
  Rating: payload.Spot?.Rating,
  UserRatingCount: payload.Spot?.UserRatingCount,
  GoogleMapID: payload.Spot?.GoogleMapID,
  GoogleMapURL: payload.Spot?.GoogleMapURL,
  PriceLevel: payload.Spot?.PriceLevel,
  PhotoUrl: payload.Spot?.PhotoUrl,
  PhotoReference: payload.Spot?.PhotoReference,
  SpotAlias: payload.SpotAlias,
  Notes: payload.Notes,
  DayNumber: payload.DayNumber,
  SortOrder: payload.SortOrder,
  ArrivalTime: payload.ArrivalTime,
  StayDuration: payload.StayDuration,
  Budget: payload.Budget ?? payload.Cost,
  TransportMode: payload.TransportMode,
  TransportTime: payload.TransportTime,
  VersionID: payload.VersionId,
  IsMaster: payload.IsMaster,
  SuggestBy: payload.SuggestBy,
})

const addSelectedMapPlace = async () => {
  if (!selectedMapPlace.value || isAddingMapPlace.value) return
  const googleMapId = selectedMapPlace.value.placeId
  if (!googleMapId) {
    showMapStatus('缺少 Google 地點 ID，無法加入行程')
    return
  }

  isAddingMapPlace.value = true
  mapStatusMessage.value = '正在加入行程...'
  try {
    const savedPlace = await findSavedPlace(googleMapId)
    const payload = buildPlanDetailPayload(selectedMapPlace.value, savedPlace)
    const draftDetail = normalizeTripDetail(buildDraftTripDetail(payload))
    const didReroute = await addTripDetailToItinerary({
      ...draftDetail,
      id: `draft-${Date.now()}`,
      detailId: null,
      draftStatus: DRAFT_STATUS.created,
    })
    if (didReroute !== false) showMapStatus('已加入行程')
  } catch {
    showMapStatus('加入行程失敗，請稍後再試')
  } finally {
    isAddingMapPlace.value = false
  }
}
const removeItem = async (id) => {
  const item = itineraryItems.value.find((x) => x.id === id)
  if (!item) return
  const previousRows = [...activeTransferRows.value]

  if (item.draftStatus === DRAFT_STATUS.created) {
    itineraryItems.value = itineraryItems.value.filter((x) => x.id !== id)
  } else {
    itineraryItems.value = itineraryItems.value.map((x) =>
      x.id === id ? { ...x, draftStatus: DRAFT_STATUS.deleted } : x,
    )
  }

  await nextTick()
  recalculateActiveItineraryTimes()
  let didReroute = null
  if (Number(item.dayNumber || 1) === Number(activeDayNumber.value)) {
    didReroute = await rerouteIfPlanningActive(previousRows)
  } else {
    clearRouteState(item.dayNumber ?? activeDayNumber.value)
  }
  if (didReroute !== false) showMapStatus('已從畫面移除，儲存行程後才會同步刪除')
}
const hasTransferAfter = (index) =>
  activeItineraryItems.value.length >= 2 && index < activeItineraryItems.value.length - 1
const toggleToolBar = () => {
  isToolBarCollapsed.value = !isToolBarCollapsed.value
}
const toggleDiscussion = () => {
  isDiscussionOpen.value = !isDiscussionOpen.value
}
const getOrderedDatasetValues = (event, selector, datasetKey) =>
  Array.from(event?.to?.querySelectorAll?.(selector) || [])
    .map((el) => el.dataset[datasetKey])
    .filter(Boolean)

const getOrderedDayIdsFromDom = (event) => {
  const eventIds = getOrderedDatasetValues(event, '[data-day-id]', 'dayId')
  if (eventIds.length) return eventIds

  return Array.from(dayDragListRef.value?.$el?.querySelectorAll?.('[data-day-id]') || [])
    .map((el) => el.dataset.dayId)
    .filter(Boolean)
}

const getOrderedItineraryIdsFromDom = (event) => {
  const eventIds = getOrderedDatasetValues(event, '[data-itinerary-id]', 'itineraryId')
  if (eventIds.length) return eventIds

  return Array.from(
    itineraryDragListRef.value?.$el?.querySelectorAll?.('[data-itinerary-id]') || [],
  )
    .map((el) => el.dataset.itineraryId)
    .filter(Boolean)
}

const hasSameOrder = (currentIds, orderedIds) =>
  currentIds.length === orderedIds.length &&
  currentIds.every((id, index) => String(id) === String(orderedIds[index]))

const syncDayOrderFromDom = async (event = {}) => {
  const orderedIds = Array.from(new Set(getOrderedDayIdsFromDom(event).map((id) => String(id))))
  if (!orderedIds.length) return

  const currentDayIds = days.value.map((day) => String(day.id))
  if (hasSameOrder(currentDayIds, orderedIds)) return

  const selectedId = selectedDayId.value
  const orderedDays = planDayOrderStore.normalizeDaysByPosition(days.value, orderedIds)
  const startDate = getPlanStartDate()
  const dayNumberMap = new Map()
  orderedDays.forEach((day, index) => {
    dayNumberMap.set(Number(day.dayNumber || index + 1), index + 1)
  })

  days.value = orderedDays.map((day, index) => ({
    ...day,
    dayNumber: index + 1,
    date: startDate ? addDays(startDate, index) : day.date,
  }))

  itineraryItems.value = itineraryItems.value.map((item) => {
    const itemDayNumber = Number(item.dayNumber || 1)
    const nextDayNumber = dayNumberMap.get(itemDayNumber)
    if (!nextDayNumber || Number(nextDayNumber) === itemDayNumber) return item

    return markDraftUpdated({
      ...item,
      dayNumber: nextDayNumber,
    })
  })

  orderedDays.forEach((day) => clearRouteState(day.dayNumber, false))
  selectedDayId.value = selectedId
  await nextTick()
  recalculateActiveItineraryTimes()
}
const syncItineraryOrderFromDom = async (options = {}) => {
  const shouldReroute = options?.reroute !== false
  const event = options?.to ? options : options?.event
  const previousRows = [...activeTransferRows.value]
  const orderedIds = Array.from(
    new Set(getOrderedItineraryIdsFromDom(event).map((id) => String(id))),
  )
  if (!orderedIds.length) return

  const activeIdSet = new Set(activeItineraryItems.value.map((item) => String(item.id)))
  const currentActiveIds = activeItineraryItems.value.map((item) => String(item.id))
  if (hasSameOrder(currentActiveIds, orderedIds)) return

  const activeById = new Map(activeItineraryItems.value.map((item) => [String(item.id), item]))
  const orderedActiveItems = orderedIds
    .map((id) => activeById.get(String(id)))
    .filter(Boolean)
    .map((item, index) => markDraftUpdated({ ...item, sortOrder: index + 1 }))
  const untouchedItems = itineraryItems.value.filter((item) => !activeIdSet.has(String(item.id)))

  itineraryItems.value = [...untouchedItems, ...orderedActiveItems].sort(
    (a, b) =>
      Number(a.dayNumber || 1) - Number(b.dayNumber || 1) ||
      Number(a.sortOrder || 0) - Number(b.sortOrder || 0),
  )
  await nextTick()
  recalculateActiveItineraryTimes()
  if (shouldReroute) await rerouteIfPlanningActive(previousRows)
}

const {
  activeTransferRows,
  buildStoredTransferRows,
  buildTravelModesForCurrentStops,
  clearRouteState,
  defaultTransportOption,
  defaultTravelMode,
  editingTransferId,
  editingTransferRoute,
  getTransferAfterItem,
  getTransferLabel,
  getTripDetailTransportPayload,
  handleRouteButton,
  handleRouteMapToggle,
  isRouteLoading,
  isRoutePlanningActive,
  isRouteShown,
  openTransferDialog,
  recalculateActiveItineraryTimes,
  rerouteIfPlanningActive,
  requestRoute,
  routeResult,
  setDefaultTravelMode,
  shouldShowRouteSteps,
  showRouteMap,
  transferRows,
  updateTransfer,
} = usePlanRoutePlanner({
  activeDayNumber,
  activeDialog,
  activeItineraryItems,
  addMinutesToTime,
  clearRoutePolyline,
  closeDialog,
  DRAFT_STATUS,
  drawRoutePolylines,
  getApiErrorMessage,
  getDayItineraryItems,
  itineraryItems,
  markDraftUpdated,
  parseDurationMinutes,
  routeMapSwitchRef,
  scheduleRenderKey,
  showMapStatus,
  syncItineraryOrderFromDom,
  transferForm,
  tripId,
  unwrapApiData,
  userId,
})

const {
  dropdownRef: routeModeDropdownRef,
  isDropdownOpen: isRouteModeDropdownOpen,
  runDropdownAction: runRouteModeDropdownAction,
  toggleDropdown: toggleRouteModeDropdown,
} = useDropdownToggle({ disabled: isRouteLoading })

const applySyncedPlanDetails = async (result) => {
  if (Array.isArray(result?.TripDetails)) {
    setPlanDetails(result.TripDetails)
    return
  }
  if (Array.isArray(result?.tripDetails)) {
    setPlanDetails(result.tripDetails)
    return
  }
  if (Array.isArray(result)) {
    setPlanDetails(result)
    return
  }
  await loadPlan()
}

const handleSaveTrip = async () => {
  if (isSavingTrip.value) return

  await syncDayOrderFromDom({ reroute: false })
  await syncItineraryOrderFromDom({ reroute: false })

  const payload = buildPlanDetailsSyncPayload()
  if (!payload.created.length && !payload.updated.length && !payload.deletedDetailIds.length) {
    if (isPlanDateRangeChanged()) {
      isSavingTrip.value = true
      mapStatusMessage.value = '正在儲存行程日期...'
      try {
        await ensurePlanDateRangeForDayNumber(getMaxVisibleDayNumber())
        mapStatusMessage.value = ''
        toast('行程日期已更新')
      } catch (error) {
        showMapStatus(getApiErrorMessage(error, '行程日期儲存失敗，請稍後再試'))
      } finally {
        isSavingTrip.value = false
      }
      return
    }
    showMapStatus('目前沒有需要儲存的景點變更')
    return
  }

  isSavingTrip.value = true
  mapStatusMessage.value = '正在儲存行程...'
  const shouldRestoreRoute =
    isRoutePlanningActive.value &&
    activeItineraryItems.value.length >= 2 &&
    activeTransferRows.value.length >= Math.max(activeItineraryItems.value.length - 1, 0)
  const routeDayNumber = activeDayNumber.value
  const routeTravelModes = shouldRestoreRoute ? buildTravelModesForCurrentStops() : []
  try {
    await ensurePlanDateRangeForDayNumber(getMaxVisibleDayNumber())
    const response = await syncPlanDetails(userId.value, tripId.value, payload)
    const result = unwrapApiData(response)
    await applySyncedPlanDetails(result)
    if (shouldRestoreRoute && Number(activeDayNumber.value) === Number(routeDayNumber)) {
      const didRestoreRoute = await requestRoute(routeTravelModes)
      if (!didRestoreRoute) {
        throw new Error('行程已儲存，但路線交通資訊寫入失敗，請重新規劃路線後再儲存')
      }
    } else {
      clearRouteState(activeDayNumber.value)
    }
    mapStatusMessage.value = ''
    const alertResult = await LuiuAlert.fire({
      icon: 'success',
      title: '行程已儲存',
      text: '你可以繼續編輯，或回到行程列表。',
      showCancelButton: true,
      confirmButtonText: '繼續編輯',
      cancelButtonText: '回到行程列表',
    })
    if (alertResult.dismiss === 'cancel') {
      router.push({ name: 'PlanList', params: { userId: userId.value } })
    }
  } catch (error) {
    showMapStatus(getApiErrorMessage(error, '行程儲存失敗，請稍後再試'))
    console.error('行程儲存失敗:', error)
  } finally {
    isSavingTrip.value = false
  }
}

const normalizeDay = (day, index) => ({
  id: day.id ?? day.DayID ?? day.DayNumber ?? index + 1,
  dayNumber: day.dayNumber ?? day.DayNumber ?? index + 1,
  intro: day.intro || day.Intro || day.label || day.Label || '',
  date:
    toInputDate(day.date || day.Date) ||
    (getPlanStartDate() ? addDays(getPlanStartDate(), index) : ''),
  summaryStops: day.summaryStops || day.SummaryStops || [],
})

const getDateRangeDates = (startDate, endDate) => {
  const start = toInputDate(startDate)
  const end = toInputDate(endDate) || start
  const startTime = new Date(`${start}T00:00:00`).getTime()
  const endTime = new Date(`${end}T00:00:00`).getTime()

  if (!start || Number.isNaN(startTime)) return []
  if (Number.isNaN(endTime) || endTime < startTime) return [start]

  const dates = []
  for (
    let current = start, guard = 0;
    new Date(`${current}T00:00:00`).getTime() <= endTime && guard < 366;
    guard += 1
  ) {
    dates.push(current)
    current = addDays(current, 1)
  }
  return dates
}

const buildDaysFromDateRange = (sourceDays = []) => {
  const normalizedSourceDays = sourceDays.map(normalizeDay)
  const rangeDates = getDateRangeDates(getPlanStartDate(), getPlanEndDate())

  if (!rangeDates.length) return normalizedSourceDays

  const dayByNumber = new Map(
    normalizedSourceDays.map((day) => [Number(day.dayNumber || day.id || 0), day]),
  )
  const dayByDate = new Map(normalizedSourceDays.map((day) => [day.date, day]))

  return rangeDates.map((date, index) => {
    const dayNumber = index + 1
    const existingDay = dayByNumber.get(dayNumber) || dayByDate.get(date) || {}

    return {
      id: existingDay.id ?? dayNumber,
      dayNumber,
      intro: existingDay.intro || '',
      date,
      summaryStops: existingDay.summaryStops || [],
    }
  })
}

const syncDaysWithDateRange = (sourceDays = days.value) => {
  days.value = buildDaysFromDateRange(sourceDays)
  selectedDayId.value = days.value[0]?.id ?? null
}

const normalizeTripDetail = (detail) => {
  const normalized = normalizeMapPlace(detail)
  const detailId = detail.DetailID ?? detail.detailId ?? null
  return {
    id: detail.id ?? detailId ?? normalized.spotId ?? Date.now(),
    detailId,
    spotId: normalized.spotId,
    dayNumber: detail.DayNumber ?? detail.dayNumber ?? 1,
    sortOrder: detail.SortOrder ?? detail.sortOrder ?? 0,
    time: formatTimeLabel(detail.ArrivalTime ?? detail.time) || '--:--',
    title: detail.SpotAlias || detail.SpotName || detail.title || normalized.title,
    category: detail.Category || normalized.category,
    description: detail.Notes ?? detail.description ?? '',
    duration: detail.StayDuration ? `${detail.StayDuration} 分鐘` : detail.duration || '1 小時',
    cost: Number(detail.Budget ?? detail.budget ?? detail.Cost ?? detail.cost ?? 0),
    lat: normalized.lat,
    lng: normalized.lng,
    address: normalized.address,
    phone: normalized.phone,
    rating: normalized.rating,
    userRatingCount: normalized.userRatingCount,
    openingHours: normalized.openingHours,
    placeId: normalized.placeId,
    googleMapUrl: normalized.googleMapUrl,
    officialUrl: normalized.officialUrl,
    priceLevel: normalized.priceLevel,
    photoUrl: normalized.photoUrl,
    photoReference: normalized.photoReference,
    regionId: normalized.regionId,
    transportMode: detail.TransportMode ?? detail.transportMode ?? null,
    transportTime: detail.TransportTime ?? detail.transportTime ?? null,
    polylineEncoded: detail.PolylineEncoded ?? detail.polylineEncoded ?? '',
    versionId: detail.VersionID ?? detail.VersionId ?? detail.versionId ?? 1,
    draftStatus: detail.draftStatus ?? DRAFT_STATUS.clean,
  }
}

const normalizePlanDetails = (tripDetails = []) =>
  tripDetails
    .filter((detail) => !(detail.IsDeleted ?? detail.isDeleted))
    .sort(
      (a, b) => (a.DayNumber || 0) - (b.DayNumber || 0) || (a.SortOrder || 0) - (b.SortOrder || 0),
    )
    .map(normalizeTripDetail)

const setPlanDetails = (tripDetails = []) => {
  const normalizedDetails = normalizePlanDetails(tripDetails)
  itineraryItems.value = normalizedDetails
  originalItineraryItems.value = cloneItineraryItems(normalizedDetails)
  transferRows.value = buildStoredTransferRows(normalizedDetails)
}

const loadPlan = async () => {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const payload = await getPlan(userId.value, tripId.value)
    const currentPlan = unwrapPlanPayload(payload)
    plan.value = {
      ...currentPlan,
      CreateAt: getPlanCreateAt(currentPlan),
      UpdateAt: getPlanUpdateAt(currentPlan),
    }
    rememberSavedPlanDateRange()

    syncDaysWithDateRange(Array.isArray(currentPlan?.Days) ? currentPlan.Days : [])

    if (Array.isArray(currentPlan?.TripDetails)) {
      setPlanDetails(currentPlan.TripDetails)
    }

    await loadComments()
  } catch (error) {
    errorMessage.value = error?.message || '行程載入失敗。'
  } finally {
    isLoading.value = false
  }
}

watch(itineraryItems, renderMapMarkers, { deep: true })
watch(activeDayNumber, renderMapMarkers)
watch(
  days,
  (nextDays) => {
    if (!nextDays.length) {
      selectedDayId.value = null
      return
    }
    if (!nextDays.some((day) => String(day.id) === String(selectedDayId.value))) {
      selectedDayId.value = nextDays[0].id
    }
  },
  { deep: true },
)
onMounted(async () => {
  await loadPlan()
  await loadFavoriteSpotIds()
  await nextTick()
  await initializeMap()
})
onBeforeUnmount(() => {
  cleanupMap()
  clearTimeout(mapSearchDebounceTimer)
  clearTimeout(mapStatusTimer)
  clearTimeout(discussionStatusTimer)
})
</script>

<template>
  <section class="plan-page">
    <header class="plan-hero d-flex align-items-center justify-content-between gap-3">
      <div>
        <h1>{{ tripTitle }}</h1>
        <div
          v-if="tripDescription || tripTag"
          class="trip-summary d-flex align-items-center flex-wrap gap-2 mb-2"
        >
          <p v-if="tripDescription" class="trip-description mb-0">{{ tripDescription }}</p>
          <span v-if="tripTag" class="badge text-bg-light plan-hero-tag">{{ tripTag }}</span>
        </div>
        <div class="trip-meta d-flex align-items-center flex-wrap gap-3">
          <span v-if="dateRange"><i class="fa-solid fa-calendar-days"></i>{{ dateRange }}</span>
          <span v-if="duration">{{ duration }}</span>
          <span v-if="ownerName"><i class="fa-solid fa-user"></i>{{ ownerName }}</span>
        </div>
      </div>
      <div class="hero-actions d-flex align-items-center flex-wrap gap-2">
        <button class="btn btn-light shadow-sm" type="button" @click="goBackPlanList">
          回行程列表
        </button>
        <button class="btn btn-light shadow-sm" type="button" @click="openPlanSettings">
          行程設定
        </button>
        <!-- 儲存行程（主要操作保留在標題區）-->
        <button
          class="btn btn-success btn-save-trip"
          type="button"
          :disabled="isSavingTrip || !hasPlanDetailDraftChanges"
          @click="handleSaveTrip"
        >
          <i class="fa-solid fa-floppy-disk"></i>
          {{ isSavingTrip ? '儲存中...' : '儲存行程' }}
        </button>
        <!-- 個人 icon 已移除 -->
      </div>
    </header>

    <div v-if="isLoading" class="plan-state">行程載入中...</div>
    <div v-else-if="errorMessage" class="plan-state plan-state-error">{{ errorMessage }}</div>
    <div v-else class="planner-shell" :class="{ 'tools-collapsed': isToolBarCollapsed }">
      <aside class="plan-tools" aria-label="行程工具">
        <button class="tool-button" type="button" @click="openPackingDialog">
          <i class="fa-solid fa-suitcase-rolling"></i><span>行李清單</span>
        </button>
        <button class="tool-button" type="button" @click="openCreateMemoryDialog">
          <i class="fa-solid fa-camera"></i><span>建立回憶</span>
        </button>
        <!-- <button class="tool-button" type="button">
          <i class="fa-solid fa-wallet"></i><span>預算管理</span>
        </button> -->
        <button
          class="collapse-button"
          type="button"
          :aria-expanded="!isToolBarCollapsed"
          aria-label="收合工具列"
          @click="toggleToolBar"
        >
          <i
            class="fa-solid"
            :class="isToolBarCollapsed ? 'fa-angles-right' : 'fa-angles-left'"
          ></i>
        </button>
      </aside>

      <!-- 天數面板 -->
      <aside class="day-panel">
        <div class="panel-heading"><h2>行程天數</h2></div>
        <LuiuDraggableContainer
          ref="dayDragListRef"
          group="days"
          class="day-drag-list"
          @dragend="syncDayOrderFromDom"
          @drop="syncDayOrderFromDom"
          @mouseup="syncDayOrderFromDom"
          @touchend="syncDayOrderFromDom"
        >
          <p v-if="!days.length" class="empty-day">尚未設定行程日期</p>
          <div
            v-for="(day, index) in days"
            :key="day.id"
            class="day-card-wrap"
            :class="{ 'is-selected': String(day.id) === String(selectedDayId) }"
            :data-day-id="day.id"
            @click="selectDay(day)"
          >
            <span class="drag-grip">::</span>
            <LuiuDraggableCards @close="removeDay(day.id)">
              <template #header>
                <div class="day-card-header">
                  <!-- 第 N 天 -->
                  <strong>{{ dayLabel(index) }}</strong>
                  <span class="day-date-chip">{{ day.date }}</span>
                  <!-- 修改按鈕（點擊才彈出浮動視窗） -->
                  <button
                    class="day-edit-btn"
                    type="button"
                    aria-label="修改天數"
                    @click.stop="openEditDayDialog(day)"
                  >
                    <i class="fa-regular fa-pen-to-square"></i>
                  </button>
                </div>
              </template>
              <div class="day-summary">
                <!-- 顯示當天簡介 -->
                <p v-if="day.intro" class="day-intro">{{ day.intro }}</p>
                <template v-if="getDaySummaryStops(day).length">
                  <div
                    v-for="stop in getDaySummaryStops(day)"
                    :key="`${day.id}-${stop.name}`"
                    class="summary-stop"
                  >
                    <span>{{ stop.category }}</span>
                    <p>{{ stop.name }}</p>
                  </div>
                  <small v-if="getDaySummaryExtraCount(day)">
                    +{{ getDaySummaryExtraCount(day) }} 個待安排項目
                  </small>
                </template>
                <p v-else class="empty-day">尚未安排景點</p>
              </div>
            </LuiuDraggableCards>
          </div>
        </LuiuDraggableContainer>

        <!-- 新增天數：日期自動往後，有 dialog 但日期不需手動填 -->
        <button class="add-day-button" type="button" @click="openDayDialog">+ 新增天數</button>
      </aside>

      <main class="schedule-panel">
        <section
          class="day-overview d-flex align-items-center justify-content-between gap-3 rounded-2 shadow-sm"
        >
          <div>
            <!-- 第 N 天 + 當天簡介 -->
            <h2>{{ activeDayLabel }}</h2>
            <p v-if="activeDay?.intro" class="day-intro-text">{{ activeDay.intro }}</p>
            <p v-else-if="tripDescription">{{ tripDescription }}</p>
          </div>
          <div class="budget-box">
            <span>預估花費</span>
            <strong>NT$ {{ formatCost(totalCost) }}</strong>
          </div>
        </section>

        <LuiuDraggableContainer
          ref="itineraryDragListRef"
          :key="`schedule-${activeDayNumber}-${scheduleRenderKey}`"
          group="itinerary"
          class="itinerary-list"
          @dragend="syncItineraryOrderFromDom"
          @drop="syncItineraryOrderFromDom"
          @mouseup="syncItineraryOrderFromDom"
          @touchend="syncItineraryOrderFromDom"
        >
          <p v-if="!timelineRows.length" class="empty-day">尚未安排景點</p>
          <template v-for="row in timelineRows" :key="row.item.id">
            <div class="timeline-row">
              <div class="timeline-item" :data-itinerary-id="row.item.id">
                <div class="pin-column">
                  <span class="pin"><i class="fa-solid fa-location-dot"></i></span>
                  <small class="pin-time">{{ row.item.time }}</small>
                  <span
                    v-if="hasTransferAfter(row.index)"
                    class="time-node"
                    aria-hidden="true"
                  ></span>
                </div>
                <LuiuDraggableCards
                  class="itinerary-card"
                  @close="removeItem(row.item.id)"
                  @click="selectAndFocusPlace(row.item)"
                >
                  <template #header>
                    <div class="itinerary-card-header">
                      <div>
                        <h3>{{ row.item.title }}</h3>
                        <span>{{ row.item.category }}</span>
                      </div>
                      <div class="card-tools">
                        <button
                          type="button"
                          aria-label="編輯行程"
                          @click.stop="openItineraryDialog(row.item)"
                        >
                          <i class="fa-regular fa-pen-to-square"></i>
                        </button>
                      </div>
                    </div>
                  </template>
                  <p class="item-description">{{ row.item.description }}</p>
                  <div class="item-meta d-flex align-items-center gap-3">
                    <span><i class="fa-regular fa-clock"></i>{{ row.item.duration }}</span>
                    <span>NT$ {{ formatCost(row.item.cost) }}</span>
                  </div>
                </LuiuDraggableCards>
              </div>

              <div
                v-if="hasTransferAfter(row.index) && row.transfer?.visible"
                class="transfer-row"
                role="button"
                tabindex="0"
                @click="openTransferDialog(row.transfer)"
                @keydown.enter="openTransferDialog(row.transfer)"
              >
                <div class="transfer-row-summary">
                  <span>
                    <font-awesome-icon
                      :key="`transfer-${row.transfer.id}-${normalizeTransportMode(row.transfer.mode)}`"
                      :icon="['fas', getTransportIconName(row.transfer.mode)]"
                    />
                    {{ getTransferLabel(row.transfer) }}
                  </span>
                  <small v-if="row.transfer.distanceMeters">
                    {{ formatDistanceLabel(row.transfer.distanceMeters) }}
                  </small>
                </div>
                <div v-if="shouldShowRouteSteps(row.transfer)" class="transfer-step-list">
                  <article
                    v-for="(step, stepIndex) in getVisibleRouteSteps(row.transfer)"
                    :key="`${row.transfer.id}-${stepIndex}`"
                    class="transfer-step-card"
                    :class="{ 'transfer-step-card--summary': step.type === 'SUMMARY' }"
                  >
                    <span v-if="step.type === 'SUMMARY'" class="transfer-step-summary-mark"
                      >...</span
                    >
                    <font-awesome-icon v-else :icon="['fas', getTransitStepIconName(step)]" />
                    <div>
                      <strong>{{ getRouteStepTitle(step) }}</strong>
                      <span v-if="!step.hideDescription">{{ getRouteStepDescription(step) }}</span>
                      <small v-if="getRouteStepTimeLabel(step)">{{
                        getRouteStepTimeLabel(step)
                      }}</small>
                    </div>
                  </article>
                </div>
              </div>
            </div>
          </template>
        </LuiuDraggableContainer>

        <!-- 私人地點入口已停用 -->

        <!-- 留言區 -->
        <section class="discussion" :class="{ 'discussion-open': isDiscussionOpen }">
          <div class="discussion-header d-flex align-items-center justify-content-between">
            <div>
              <i class="fa-regular fa-comment"></i>
              <strong>留言區</strong>
              <span>({{ discussionThreadCount }})</span>
            </div>
            <button
              type="button"
              :aria-expanded="isDiscussionOpen"
              aria-label="切換留言區"
              @click="toggleDiscussion"
            >
              <i class="fa-solid" :class="isDiscussionOpen ? 'fa-angles-up' : 'fa-angles-down'"></i>
            </button>
          </div>
          <div class="discussion-list" aria-live="polite">
            <p v-if="!discussionThreads.length" class="empty-day">目前沒有留言</p>
            <article
              v-for="message in discussionThreads"
              :key="message.id"
              class="discussion-message"
            >
              <div>
                <strong>{{ message.author }}</strong>
                <span>{{ message.time }}</span>
              </div>
              <div v-if="editingCommentId === message.id" class="discussion-edit">
                <input
                  v-model="editingCommentText"
                  class="form-control form-control-sm"
                  type="text"
                  :disabled="isCommentSaving(message.id)"
                  @keydown.enter="saveEditComment(message)"
                  @keydown.esc="cancelEditComment"
                />
                <div class="discussion-actions">
                  <a
                    href="javascript:void(0)"
                    :aria-disabled="isCommentSaving(message.id)"
                    @click="saveEditComment(message)"
                  >
                    儲存
                  </a>
                  <a href="javascript:void(0)" @click="cancelEditComment">取消</a>
                </div>
              </div>
              <template v-else>
                <p>{{ message.message }}</p>
                <div class="discussion-actions">
                  <a href="javascript:void(0)" @click="startReplyComment(message)"> 回覆 </a>
                  <a
                    v-if="message.canEdit"
                    href="javascript:void(0)"
                    @click="startEditComment(message)"
                  >
                    編輯
                  </a>
                  <a
                    v-if="message.canDelete"
                    href="javascript:void(0)"
                    :aria-disabled="isCommentDeleting(message.id)"
                    @click="removeComment(message)"
                  >
                    刪除
                  </a>
                </div>
                <div v-if="replyingCommentId === message.id" class="discussion-reply-input">
                  <input
                    v-model="replyMessageText"
                    class="form-control form-control-sm"
                    type="text"
                    :placeholder="`回覆 ${message.author}...`"
                    :disabled="isSubmittingMessage"
                    @keydown.enter="submitReply(message)"
                    @keydown.esc="cancelReplyComment"
                  />
                  <div class="discussion-actions">
                    <a
                      href="javascript:void(0)"
                      :aria-disabled="isSubmittingMessage"
                      @click="submitReply(message)"
                    >
                      送出
                    </a>
                    <a href="javascript:void(0)" @click="cancelReplyComment">取消</a>
                  </div>
                </div>
                <div v-if="message.replies.length" class="discussion-replies">
                  <article
                    v-for="reply in message.replies"
                    :key="reply.id"
                    class="discussion-message discussion-message--reply"
                  >
                    <div>
                      <strong>{{ reply.author }}</strong>
                      <span>{{ reply.time }}</span>
                    </div>
                    <div v-if="editingCommentId === reply.id" class="discussion-edit">
                      <input
                        v-model="editingCommentText"
                        class="form-control form-control-sm"
                        type="text"
                        :disabled="isCommentSaving(reply.id)"
                        @keydown.enter="saveEditComment(reply)"
                        @keydown.esc="cancelEditComment"
                      />
                      <div class="discussion-actions">
                        <a
                          href="javascript:void(0)"
                          :aria-disabled="isCommentSaving(reply.id)"
                          @click="saveEditComment(reply)"
                        >
                          儲存
                        </a>
                        <a href="javascript:void(0)" @click="cancelEditComment">取消</a>
                      </div>
                    </div>
                    <template v-else>
                      <p>{{ reply.message }}</p>
                      <div v-if="reply.canEdit || reply.canDelete" class="discussion-actions">
                        <a
                          v-if="reply.canEdit"
                          href="javascript:void(0)"
                          @click="startEditComment(reply)"
                        >
                          編輯
                        </a>
                        <a
                          v-if="reply.canDelete"
                          href="javascript:void(0)"
                          :aria-disabled="isCommentDeleting(reply.id)"
                          @click="removeComment(reply)"
                        >
                          刪除
                        </a>
                      </div>
                    </template>
                  </article>
                </div>
              </template>
            </article>
          </div>
          <!-- 留言輸入區（展開時顯示） -->
          <div v-if="isDiscussionOpen" class="discussion-input">
            <input
              v-model="newMessageText"
              class="form-control"
              type="text"
              placeholder="輸入留言..."
              :disabled="isSubmittingMessage"
              @keydown.enter="submitMessage"
            />
            <button
              class="btn btn-primary btn-sm"
              type="button"
              :disabled="isSubmittingMessage"
              @click="submitMessage"
            >
              <i class="fa-solid fa-paper-plane"></i>
            </button>
          </div>
          <transition name="toast-fade">
            <p v-if="discussionStatusMessage" class="discussion-status">
              {{ discussionStatusMessage }}
            </p>
          </transition>
        </section>
      </main>

      <!-- 地圖面板 -->
      <section class="map-panel">
        <div class="map-heading d-flex align-items-center justify-content-between gap-3">
          <div class="map-heading-title d-flex align-items-center flex-wrap gap-2">
            <h2><i class="fa-regular fa-paper-plane"></i>路線地圖</h2>
            <div v-if="isRouteShown" class="route-menu">
              <span>目前路線</span>
              <span>{{ activeDayLabel }}（{{ activeDateLabel }}）</span>
              <span v-if="routeResult">
                {{ routeResult.totalDurationMinutes }} 分鐘 ·
                {{ (routeResult.totalDistanceMeters / 1000).toFixed(1) }} 公里
              </span>
              <span v-else>{{ duration || '尚未設定天數' }}</span>
            </div>
          </div>
          <div class="map-heading-actions d-flex align-items-center flex-wrap gap-2">
            <LuiuCheck
              ref="routeMapSwitchRef"
              class="map-route-switch mb-0"
              @change="handleRouteMapToggle"
            >
              顯示路線
            </LuiuCheck>
            <button
              class="btn btn-outline-warning btn-sm"
              type="button"
              :disabled="isLoadingFavoriteSpots"
              @click="toggleFavoriteSpotMarkers"
            >
              {{
                isLoadingFavoriteSpots
                  ? '收藏載入中...'
                  : showFavoriteSpots
                    ? '隱藏收藏'
                    : '顯示收藏'
              }}
            </button>
            <div ref="routeModeDropdownRef" class="dropdown route-mode-dropdown">
              <button
                class="btn btn-outline-primary btn-sm dropdown-toggle route-mode-toggle"
                :class="{ show: isRouteModeDropdownOpen }"
                type="button"
                :disabled="isRouteLoading"
                :aria-expanded="isRouteModeDropdownOpen"
                @click.stop="toggleRouteModeDropdown"
              >
                <font-awesome-icon
                  :key="`default-${normalizeTransportMode(defaultTravelMode)}`"
                  :icon="['fas', defaultTransportOption.iconName]"
                />
                {{ isRouteLoading ? '路線計算中...' : `規劃路線：${defaultTransportOption.label}` }}
              </button>
              <ul
                class="dropdown-menu dropdown-menu-end"
                :class="{ show: isRouteModeDropdownOpen }"
              >
                <li v-for="option in transportOptions" :key="option.value">
                  <button
                    class="dropdown-item"
                    :class="{ active: option.value === normalizeTransportMode(defaultTravelMode) }"
                    type="button"
                    :disabled="isRouteLoading"
                    @click.stop="runRouteModeDropdownAction(setDefaultTravelMode, option.value)"
                  >
                    <font-awesome-icon :icon="['fas', option.iconName]" />
                    {{ option.label }}
                  </button>
                </li>
              </ul>
            </div>
            <!-- 路線：依目前 timeline row 綁定的交通方式重新計算 -->
            <button
              class="btn btn-outline-primary btn-sm"
              type="button"
              :disabled="isRouteLoading"
              @click="handleRouteButton"
            >
              {{ isRouteLoading ? '路線計算中...' : '規劃路線' }}
            </button>
          </div>
        </div>

        <div class="map-canvas">
          <div ref="mapElement" class="map-instance" aria-label="Google 路線地圖"></div>

          <!-- 搜尋框（透過後端 Google Places API 搜尋與自動完成） -->
          <div class="map-search">
            <input
              ref="mapSearchInput"
              v-model="mapSearchQuery"
              type="text"
              placeholder="搜尋地點並加入行程..."
              class="map-search-input-field"
              autocomplete="off"
              @input="handleMapSearchInput"
              @keydown.enter.prevent="handleMapSearch"
            />
            <button
              type="button"
              aria-label="搜尋地點"
              :disabled="isMapSearchLoading"
              @click="handleMapSearch"
            >
              <i class="fa-solid fa-magnifying-glass"></i>
            </button>
            <ul v-if="mapSearchResults.length" class="map-search-results">
              <li v-for="place in mapSearchResults" :key="place.placeId || place.id || place.title">
                <button type="button" @click="selectBackendSearchResult(place)">
                  <strong>{{ place.title }}</strong>
                  <span>{{ place.address || '地址未提供' }}</span>
                </button>
              </li>
            </ul>
          </div>

          <!-- 錯誤訊息 -->
          <div v-if="mapErrorMessage" class="map-note map-note--error">{{ mapErrorMessage }}</div>
          <!-- 載入訊息（只在真的有內容時顯示） -->
          <transition name="map-note-fade">
            <div v-if="!mapErrorMessage && mapStatusMessage" class="map-note">
              {{ mapStatusMessage }}
            </div>
          </transition>

          <!-- 地點卡片 -->
          <article v-if="isPlaceCardOpen" class="place-card">
            <img :src="$img(selectedPlace.imageUrl)" :alt="selectedPlace.name" />
            <div class="place-content">
              <h3>{{ selectedPlace.name }}</h3>
              <p>
                {{ selectedPlace.rating }}
                <span class="stars">★★★★★</span>
                ({{ selectedPlace.reviews }}) · {{ selectedPlace.price }}
              </p>
              <small>{{ selectedPlace.category }}</small>
              <div class="place-actions">
                <button
                  type="button"
                  class="place-favorite-btn"
                  :class="{ 'is-active': isSelectedPlaceFavorite }"
                  :disabled="!selectedMapPlace || isSavingPlaceFavorite"
                  @click="addPlaceToFavorites"
                >
                  <!-- <i
                    class="fa-heart"
                    :class="isSelectedPlaceFavorite ? 'fa-solid' : 'fa-regular'"
                  ></i> -->
                  {{
                    isSavingPlaceFavorite
                      ? '更新中...'
                      : isSelectedPlaceFavorite
                        ? '取消收藏'
                        : '加入收藏'
                  }}
                </button>
                <button
                  type="button"
                  :disabled="!selectedMapPlace || isAddingMapPlace"
                  @click="addSelectedMapPlace"
                >
                  {{ isAddingMapPlace ? '加入中...' : '加入行程' }}
                </button>
              </div>
              <ul>
                <li><i class="fa-solid fa-clock"></i>建議停留 1 小時 30 分鐘</li>
                <li><i class="fa-solid fa-location-dot"></i>{{ selectedPlace.address }}</li>
                <li><i class="fa-solid fa-phone"></i>{{ selectedPlace.phone }}</li>
                <li v-for="(hour, index) in selectedPlace.hoursList" :key="`${index}-${hour}`">
                  <i class="fa-solid fa-store"></i>{{ hour }}
                </li>
              </ul>
            </div>
          </article>
        </div>
      </section>
    </div>

    <PlanSettingsDialog
      :is-open="isPlanSettingsOpen"
      :plan="plan"
      :is-saving="isSavingSettings"
      :is-deleting="isDeletingPlan"
      :copy-message="settingsCopyMessage"
      :placeholder-cover="placeholderCover"
      :image-resolver="$img"
      :format-date-time="formatSettingsDateTime"
      @close="closePlanSettings"
      @save="savePlanSettings"
      @delete="deleteCurrentPlan"
      @copy-share-link="copyShareLink"
      @owner-click="goToOwner"
    />

    <PackingListDialog
      :is-open="isPackingDialogOpen"
      :user-id="userId"
      :trip-id="tripId"
      :trip-title="tripTitle"
      @close="closePackingDialog"
    />

    <PlanCreateMemoryDialog
      :is-open="isCreateMemoryDialogOpen"
      :plan="plan"
      :itinerary-items="itineraryItems"
      :trip-id="tripId"
      :user-id="userId"
      @close="closeCreateMemoryDialog"
      @success="closeCreateMemoryDialog"
    />

    <!-- Dialogs -->
    <div v-if="activeDialog" class="plan-dialog-backdrop" @click.self="closeDialog">
      <!-- 新增天數 -->
      <form
        v-if="activeDialog === 'day'"
        class="plan-dialog"
        aria-label="新增天數"
        @submit.prevent="addDay"
      >
        <header>
          <h2>新增天數</h2>
          <button type="button" class="dialog-close" @click="closeDialog">
            <i class="fa-solid fa-xmark"></i>
          </button>
        </header>
        <label>
          當天簡介
          <input
            v-model="dayForm.intro"
            class="form-control"
            type="text"
            placeholder="例如：探索老街與夜市"
          />
        </label>
        <label>
          日期
          <input v-model="dayForm.date" class="form-control" type="date" />
        </label>
        <footer>
          <button type="button" class="btn btn-light" @click="closeDialog">取消</button>
          <button type="submit" class="btn btn-primary">新增</button>
        </footer>
      </form>

      <!-- 修改天數（點擊鉛筆按鈕才出現） -->
      <form
        v-else-if="activeDialog === 'editDay'"
        class="plan-dialog"
        aria-label="修改天數"
        @submit.prevent="saveEditDay"
      >
        <header>
          <h2>修改天數資訊</h2>
          <button type="button" class="dialog-close" @click="closeDialog">
            <i class="fa-solid fa-xmark"></i>
          </button>
        </header>
        <label>
          當天簡介
          <input
            v-model="editDayForm.intro"
            class="form-control"
            type="text"
            placeholder="例如：探索老街與夜市"
          />
        </label>
        <label>
          日期
          <input v-model="editDayForm.date" class="form-control" type="date" />
        </label>
        <footer>
          <button type="button" class="btn btn-light" @click="closeDialog">取消</button>
          <button type="submit" class="btn btn-primary">儲存</button>
        </footer>
      </form>

      <!-- 修改景點 -->
      <form
        v-else-if="activeDialog === 'itinerary'"
        class="plan-dialog"
        aria-label="修改景點資訊"
        @submit.prevent="saveItinerary"
      >
        <header>
          <h2>修改景點資訊</h2>
          <button type="button" class="dialog-close" @click="closeDialog">
            <i class="fa-solid fa-xmark"></i>
          </button>
        </header>
        <label>
          顯示名稱
          <input
            v-model="itineraryForm.alias"
            class="form-control"
            type="text"
            placeholder="例如：早午餐或自訂景點名稱"
          />
        </label>
        <label>
          備註
          <textarea
            v-model="itineraryForm.notes"
            class="form-control"
            rows="3"
            placeholder="輸入景點備註"
          ></textarea>
        </label>
        <label>
          停留時間（分鐘）
          <input
            v-model.number="itineraryForm.durationMinutes"
            class="form-control"
            type="number"
            min="0"
            step="5"
          />
        </label>
        <label>
          預算
          <input
            v-model.number="itineraryForm.cost"
            class="form-control"
            type="number"
            min="0"
            step="1"
          />
        </label>
        <footer>
          <button type="button" class="btn btn-light" @click="closeDialog">取消</button>
          <button type="submit" class="btn btn-primary">儲存</button>
        </footer>
      </form>

      <!-- 交通方式 -->
      <form
        v-else-if="activeDialog === 'transfer'"
        class="plan-dialog"
        aria-label="編輯交通方式"
        @submit.prevent="updateTransfer"
      >
        <header>
          <h2>編輯交通方式</h2>
          <button type="button" class="dialog-close" @click="closeDialog">
            <i class="fa-solid fa-xmark"></i>
          </button>
        </header>
        <label>
          交通方式
          <select v-model="transferForm.mode" class="form-select">
            <option v-for="option in transportOptions" :key="option.value" :value="option.value">
              {{ option.label }}
            </option>
          </select>
        </label>
        <footer>
          <button type="button" class="btn btn-light" @click="closeDialog">取消</button>
          <button type="submit" class="btn btn-primary" :disabled="isRouteLoading">
            {{ isRouteLoading ? '重新計算中...' : '重新計算' }}
          </button>
        </footer>
      </form>
    </div>
  </section>
</template>

<style scoped lang="scss">
@import '@/assets/scss/pages/plan';
</style>
