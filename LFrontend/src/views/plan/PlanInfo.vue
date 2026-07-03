<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { asArrayPayload, getPlan, unwrapPlanPayload } from '@/api/planning/plan'
import { updateRecommendationApi } from '@/api/admin'
import {
  createTripComment,
  deleteTripComment,
  getTripComments,
  updateTripComment,
} from '@/api/planning/tripComment'
import defaultAvatar from '@/assets/Images/person.svg'
import LuiuStatCard from '@/components/base/LuiuStatCard.vue'
import LuiuAvatarGroup from '@/components/base/LuiuAvatarGroup.vue'
import { useFavoriteTargets } from '@/composables/useFavorites'
import { PLAN_PLACEHOLDER_COVER_URL } from '@/constants/planDefaults'
import { ROLES } from '@/constants/roles'
import { useUserStore } from '@/stores/user'
import { googleMaps } from '@/utils/googleMaps'
import {
  isPublicPrivacyStatus,
  isOfficeRecommendedStatus,
  normalizeOfficeOperStatus,
  normalizePrivacyStatus,
  resolvePrivacySuggest,
} from '@/utils/planPrivacy'
import { toast } from '@/utils/sweetAlert'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const plan = ref(null)
const comments = ref([])
const isLoading = ref(false)
const errorMessage = ref('')
const commentStatusMessage = ref('')
const newComment = ref('')
const isSubmittingComment = ref(false)
const replyingCommentId = ref(null)
const replyText = ref('')
const submittingReplyIds = ref([])
const editingCommentId = ref(null)
const editingCommentText = ref('')
const savingCommentIds = ref([])
const deletingCommentIds = ref([])
const activeDay = ref(1)
const isSavingSuggest = ref(false)
const {
  isSavingFavorite: isSavingPlanFavorite,
  isFavoriteTarget: isFavoritePlanTarget,
  loadFavoriteTargets: loadPlanFavorites,
  toggleFavoriteTarget: togglePlanFavorite,
} = useFavoriteTargets('Plan')

// ── 地圖狀態 ────────────────────────────────────────────────────────────────
const mapElement = ref(null)
const mapErrorMessage = ref('')
const selectedMarkerId = ref(null)

let googleMap = null
let markerConstructor = null
let mapClickListener = null
let mapMarkers = []

const routeUserId = computed(() => route.params.userId)
const tripId = computed(() => String(route.params.planId || '').trim())
const isValidTripId = computed(() => /^\d+$/.test(tripId.value))
const currentUserId = computed(
  () =>
    userStore.userInfo?.userId ??
    userStore.userInfo?.memberId ??
    userStore.userInfo?.MemberID ??
    userStore.userInfo?.MemberId ??
    '',
)
const commentUserId = computed(() => userStore.userInfo?.userId || routeUserId.value)
const canShowSuggestButton = computed(() => userStore.hasRole(ROLES.OFFICIAL_MANAGER))
const isPlanFavorite = computed(() => isFavoritePlanTarget(tripId.value))

const placeholderCover = PLAN_PLACEHOLDER_COVER_URL
const KAOHSIUNG_CITY_CENTER = { lat: 22.6273, lng: 120.3014 }
const DEFAULT_MAP_ZOOM = 15

// ── 日期 / 時間工具 ──────────────────────────────────────────────────────────

const toInputDate = (value) => {
  if (!value) return ''
  const normalized = String(value).replaceAll('/', '-')
  const dateOnlyMatch = normalized.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/)
  if (dateOnlyMatch) {
    const [, year, month, day] = dateOnlyMatch
    return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`
  }
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? normalized.slice(0, 10) : date.toISOString().slice(0, 10)
}

const formatDateLabel = (value) => toInputDate(value).replaceAll('-', '/')

const formatShortDate = (value) => {
  const date = toInputDate(value)
  if (!date) return ''
  const [, month, day] = date.split('-')
  return `${Number(month)}月${Number(day)}日`
}

const formatDateTimeLabel = (value) => {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return date.toLocaleString('zh-TW', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const formatPostDate = (value) => {
  const label = formatDateTimeLabel(value)
  return label ? `發佈於 ${label}` : '尚無發佈時間'
}

const formatTimeLabel = (value) => {
  if (!value) return ''
  const date = new Date(value)
  if (!Number.isNaN(date.getTime())) {
    return date.toLocaleString('zh-TW', {
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    })
  }
  return String(value).slice(0, 16)
}

// 將 'HH:MM:SS' 或 'HH:MM' 轉為 'HH:MM'
const formatArrivalTime = (value) => {
  if (!value) return ''
  return String(value).slice(0, 5)
}

const addDays = (dateStr, n) => {
  const date = new Date(dateStr)
  if (Number.isNaN(date.getTime())) return ''
  date.setDate(date.getDate() + n)
  return date.toISOString().slice(0, 10)
}

const toNumberOrNull = (value) => {
  const number = Number(value)
  return Number.isFinite(number) ? number : null
}

const getFirstStringValue = (...values) => {
  for (const value of values.flat()) {
    if (typeof value === 'string' && value.trim()) return value
    if (value && typeof value === 'object') {
      const nestedValue = getFirstStringValue(
        value.photoUrl,
        value.PhotoURL,
        value.PhotoUrl,
        value.photo_url,
        value.photoURL,
        value.Photo,
        value.photo,
        value.imageUrl,
        value.ImageURL,
        value.ImageUrl,
        value.url,
        value.Url,
      )
      if (nestedValue) return nestedValue
    }
  }
  return ''
}

const formatTransportLabel = (value) => {
  const aliases = {
    1: '開車',
    2: '機車',
    3: '步行',
    4: '自行車',
    5: '大眾運輸',
    DRIVE: '開車',
    DRIVING: '開車',
    TWO_WHEELER: '機車',
    WALK: '步行',
    WALKING: '步行',
    BICYCLE: '自行車',
    BICYCLING: '自行車',
    TRANSIT: '大眾運輸',
    BUS: '公車',
    TRAIN: '火車',
    RAIL: '火車',
    MRT: '捷運',
    METRO: '捷運',
  }
  if (value === null || value === undefined || value === '') return ''
  return aliases[String(value).toUpperCase()] || String(value)
}

// 依交通方式中文名稱回傳對應 Remix Icon class
const transportIcon = (label) => {
  const icons = {
    開車: 'ri-car-line',
    機車: 'ri-motorbike-line',
    步行: 'ri-walk-line',
    自行車: 'ri-bike-line',
    大眾運輸: 'ri-bus-line',
    公車: 'ri-bus-line',
    火車: 'ri-train-line',
    捷運: 'ri-subway-line',
  }
  if (icons[label]) return icons[label]
  if (String(label).includes('捷運')) return 'ri-subway-line'
  if (String(label).includes('公車')) return 'ri-bus-line'
  if (String(label).includes('火車') || String(label).includes('鐵路')) return 'ri-train-line'
  return 'ri-route-line'
}

const formatDurationLabel = (value) => {
  if (value === null || value === undefined || value === '') return ''
  const minutes = Number(value)
  if (!Number.isFinite(minutes)) return String(value)
  if (minutes >= 60 && minutes % 60 === 0) return `${minutes / 60} 小時`
  return `${minutes} 分鐘`
}

const getTransitValue = (detail = {}, ...keys) => {
  for (const key of keys) {
    const value = detail[key] ?? detail.TransportInfo?.[key] ?? detail.transportInfo?.[key]
    if (value !== null && value !== undefined && String(value).trim()) return String(value).trim()
  }
  return ''
}

const normalizeTransitInfo = (detail = {}) => ({
  duration: formatDurationLabel(
    detail.TransportDuration ??
      detail.transportDuration ??
      detail.TravelDuration ??
      detail.travelDuration ??
      detail.TransportTime ??
      detail.transportTime ??
      detail.TrafficTime ??
      detail.trafficTime,
  ),
  line: getTransitValue(
    detail,
    'TransitLine',
    'transitLine',
    'RouteName',
    'routeName',
    'LineName',
    'lineName',
    'BusRoute',
    'busRoute',
    'MrtLine',
    'mrtLine',
    'MRTLine',
  ),
  from: getTransitValue(
    detail,
    'FromStation',
    'fromStation',
    'DepartureStation',
    'departureStation',
    'StartStation',
    'startStation',
    'BoardingStop',
    'boardingStop',
  ),
  to: getTransitValue(
    detail,
    'ToStation',
    'toStation',
    'ArrivalStation',
    'arrivalStation',
    'EndStation',
    'endStation',
    'AlightingStop',
    'alightingStop',
  ),
})

const isTransitTransport = (label) =>
  ['大眾運輸', '公車', '火車', '捷運'].includes(label) ||
  ['捷運', '公車', '火車', '鐵路'].some((keyword) => String(label).includes(keyword))

const formatCostLabel = (value) => {
  const cost = toNumberOrNull(value)
  if (cost === null) return ''
  if (cost === 0) return '免費'
  return `NT$ ${cost.toLocaleString('zh-TW')}`
}

const normalizeNote = (detail = {}) => {
  const note = getFirstStringValue(detail.Notes, detail.Note, detail.note, detail.description)
  const address = detail.Address || detail.address || ''
  return note && note !== address ? note : ''
}

// ── 資料正規化 ───────────────────────────────────────────────────────────────

const normalizeSpot = (detail = {}) => ({
  id: detail.DetailID ?? detail.id ?? `${detail.DayNumber || 1}-${detail.SortOrder || 0}`,
  dayNumber: Number(detail.DayNumber ?? detail.dayNumber ?? 1),
  sortOrder: Number(detail.SortOrder ?? detail.sortOrder ?? 0),
  name: detail.SpotAlias || detail.SpotName || detail.title || '未命名地點',
  description: normalizeNote(detail),
  address: detail.Address || detail.address || '',
  // 照片：優先取景點本身，再往 Spot 子物件找
  image: getFirstStringValue(
    detail.PhotoURL,
    detail.PhotoUrl,
    detail.Photo,
    detail.photoUrl,
    detail.photoURL,
    detail.ImageURL,
    detail.ImageUrl,
    detail.Spot?.PhotoURL,
    detail.Spot?.PhotoUrl,
    detail.Spot?.Photo,
    detail.Spot?.photoUrl,
    detail.Spot?.photoURL,
    detail.Spot?.photo_url,
    detail.Spot?.ImageURL,
    detail.Spot?.ImageUrl,
    detail.Spot?.Photos,
    detail.Spot?.photos,
  ),
  // 預計到達時間（'HH:MM'）
  time: formatArrivalTime(detail.ArrivalTime ?? detail.arrivalTime),
  transport: formatTransportLabel(
    detail.TransportMode ??
      detail.transportMode ??
      detail.Transport ??
      detail.transport ??
      detail.TransferMode ??
      detail.transferMode ??
      detail.mode,
  ),
  transit: normalizeTransitInfo(detail),
  duration: formatDurationLabel(detail.StayDuration ?? detail.duration),
  cost: formatCostLabel(detail.Budget ?? detail.budget ?? detail.Cost ?? detail.cost),
  // 地圖用座標
  lat: toNumberOrNull(
    detail.Latitude ??
      detail.latitude ??
      detail.lat ??
      detail.Spot?.Latitude ??
      detail.Spot?.latitude,
  ),
  lng: toNumberOrNull(
    detail.Longitude ??
      detail.longitude ??
      detail.lng ??
      detail.Spot?.Longitude ??
      detail.Spot?.longitude,
  ),
})

const normalizeCollaborator = (collaborator = {}) => ({
  id:
    collaborator.CollaboratorID ?? collaborator.id ?? collaborator.UserID ?? collaborator.MemberID,
  name: collaborator.Name || collaborator.name || '共同編輯者',
  avatarUrl: collaborator.IconURL || collaborator.Avatar || collaborator.avatarUrl || '',
})

const buildDays = (currentPlan, spots) => {
  const apiDays = Array.isArray(currentPlan?.Days) ? currentPlan.Days : []

  if (apiDays.length) {
    return apiDays.map((day, index) => {
      const dayNumber = Number(day.DayNumber ?? day.dayNumber ?? day.day ?? index + 1)
      return {
        day: dayNumber,
        label: day.label || day.Label || `Day ${dayNumber}`,
        date:
          formatShortDate(day.Date || day.date) ||
          formatShortDate(addDays(currentPlan.StartDate, index)),
        spots: spots.filter((spot) => spot.dayNumber === dayNumber),
      }
    })
  }

  const startDate = toInputDate(currentPlan?.StartDate)
  const endDate = toInputDate(currentPlan?.EndDate)
  const dateDiff =
    startDate && endDate ? Math.floor((new Date(endDate) - new Date(startDate)) / 86400000) + 1 : 0
  const maxSpotDay = Math.max(0, ...spots.map((spot) => spot.dayNumber || 0))
  const daysCount = Math.max(1, dateDiff, maxSpotDay)

  return Array.from({ length: daysCount }, (_, index) => {
    const dayNumber = index + 1
    return {
      day: dayNumber,
      label: `Day ${dayNumber}`,
      date: formatShortDate(addDays(startDate, index)),
      spots: spots.filter((spot) => spot.dayNumber === dayNumber),
    }
  })
}

const normalizePlan = (currentPlan = {}) => {
  const spots = (Array.isArray(currentPlan.TripDetails) ? currentPlan.TripDetails : [])
    .filter((detail) => !(detail.IsDeleted ?? detail.isDeleted))
    .map(normalizeSpot)
    .sort((a, b) => a.dayNumber - b.dayNumber || a.sortOrder - b.sortOrder)
  const days = buildDays(currentPlan, spots)
  const totalCost = spots.reduce((sum, spot) => {
    const cost = Number(String(spot.cost).replace(/[^\d.-]/g, ''))
    return Number.isFinite(cost) ? sum + cost : sum
  }, 0)

  return {
    id: currentPlan.TripID ?? currentPlan.id ?? tripId.value,
    title: currentPlan.TripName || currentPlan.title || '未命名行程',
    tripTag: String(currentPlan.TripTag || '').trim(),
    startDate: formatDateLabel(currentPlan.StartDate || currentPlan.startDate),
    endDate: formatDateLabel(currentPlan.EndDate || currentPlan.endDate),
    coverImage: getFirstStringValue(
      currentPlan.PhotoURL,
      currentPlan.PhotoUrl,
      currentPlan.coverImage,
    ),
    isSuggest: resolvePrivacySuggest(currentPlan.PrivacyStatus ?? currentPlan.privacyStatus),
    privacyStatus: normalizePrivacyStatus(currentPlan.PrivacyStatus ?? currentPlan.privacyStatus),
    officeOper: normalizeOfficeOperStatus(currentPlan.OfficeOper ?? currentPlan.officeOper),
    isOfficeRecommended: isOfficeRecommendedStatus(
      currentPlan.OfficeOper ?? currentPlan.officeOper,
    ),
    tags: asArrayPayload(currentPlan.Tags || currentPlan.tags),
    desc: currentPlan.TripDesc || currentPlan.desc || '',
    owner: {
      id:
        currentPlan.OwnerID ??
        currentPlan.OwnerId ??
        currentPlan.ownerId ??
        currentPlan.UserID ??
        currentPlan.MemberID ??
        currentPlan.owner?.id ??
        '',
      name: currentPlan.OwnerName || currentPlan.owner?.name || '',
      avatar:
        currentPlan.OwnerIconURL || currentPlan.OwnerAvatar || currentPlan.owner?.avatar || '',
      postDate: formatPostDate(currentPlan.CreateAt || currentPlan.createAt),
    },
    collaborators: asArrayPayload(currentPlan.Collaborators || currentPlan.collaborators).map(
      normalizeCollaborator,
    ),
    stats: {
      days: days.length,
      spots: spots.length,
      budget: totalCost.toLocaleString('zh-TW'),
    },
    days,
  }
}

const normalizeComment = (comment = {}) => ({
  id: comment.CommentID ?? comment.id ?? Date.now(),
  parentId: comment.ParentID ?? comment.parentId ?? comment.ParentId ?? null,
  author: comment.UserName || comment.author || '我',
  avatar: comment.UserIcon || comment.avatar || '',
  time: formatTimeLabel(comment.CreateAt || comment.time || new Date()),
  text: comment.Content || comment.message || comment.text || '',
  canEdit: Boolean(comment.CanEdit ?? comment.canEdit),
  canDelete: Boolean(comment.CanDelete ?? comment.canDelete),
})

// ── Computed ─────────────────────────────────────────────────────────────────

const currentDaySpots = computed(() => {
  const day = plan.value?.days.find((item) => item.day === activeDay.value)
  return day ? day.spots : []
})

const commentThreads = computed(() => {
  const commentMap = new Map()
  const roots = []

  comments.value.forEach((comment) => {
    commentMap.set(comment.id, { ...comment, children: [] })
  })

  comments.value.forEach((comment) => {
    const threadedComment = commentMap.get(comment.id)
    const parent = comment.parentId === null ? null : commentMap.get(comment.parentId)

    if (parent) {
      parent.children.push(threadedComment)
    } else {
      roots.push(threadedComment)
    }
  })

  return roots
})

const flattenedCommentThreads = computed(() => {
  const flattened = []
  const appendComment = (comment, depth = 0) => {
    flattened.push({
      ...comment,
      depth,
      desktopDepth: Math.min(depth, 3),
      mobileDepth: Math.min(depth, 2),
    })
    comment.children.forEach((child) => appendComment(child, depth + 1))
  }

  commentThreads.value.forEach((comment) => appendComment(comment))
  return flattened
})

const currentUserAvatar = computed(
  () =>
    userStore.userInfo?.avatarUrl ||
    userStore.userInfo?.AvatarUrl ||
    userStore.userInfo?.UserIcon ||
    plan.value?.owner.avatar ||
    '',
)
const isOwnPlanInfo = computed(() => {
  const routeOwnerUserId = String(routeUserId.value || '').trim()
  const planOwnerId = String(plan.value?.owner?.id || '').trim()
  const loginIds = [
    currentUserId.value,
    userStore.userInfo?.memberId,
    userStore.userInfo?.MemberID,
    userStore.userInfo?.MemberId,
  ]
    .map((id) => String(id || '').trim())
    .filter(Boolean)

  if (routeOwnerUserId && loginIds.includes(routeOwnerUserId)) return true
  if (planOwnerId && loginIds.includes(planOwnerId)) return true

  return false
})
const canSuggestPlan = computed(() => isPublicPrivacyStatus(plan.value?.privacyStatus))
const suggestButtonLabel = computed(() =>
  plan.value?.isOfficeRecommended ? '取消推薦' : '推薦行程',
)
const suggestButtonIcon = computed(() =>
  plan.value?.isOfficeRecommended ? 'ri-star-fill' : 'ri-star-line',
)

const setDefaultAvatar = (event) => {
  event.target.src = defaultAvatar
}

const goToPlanView = () => {
  router.push({
    name: 'Plan',
    params: {
      userId: routeUserId.value,
      tripId: tripId.value,
    },
  })
}

const toggleSuggestStatus = async () => {
  if (!plan.value || isSavingSuggest.value) return

  if (!canSuggestPlan.value) {
    toast('私密行程不能加入推薦', 'error')
    return
  }

  const nextState = !plan.value.isOfficeRecommended
  isSavingSuggest.value = true

  try {
    await updateRecommendationApi('trip', Number(tripId.value), nextState)
    plan.value = {
      ...plan.value,
      isOfficeRecommended: nextState,
    }
    toast(nextState ? '已加入推薦' : '已取消推薦')
  } catch (error) {
    toast(error?.message || error?.Message || '推薦狀態更新失敗，請稍後再試', 'error')
  } finally {
    isSavingSuggest.value = false
  }
}

// ── 收藏操作 ─────────────────────────────────────────────────────────────────

const syncPlanFavoriteStatus = async () => {
  try {
    if (isValidTripId.value) await loadPlanFavorites()
  } catch {
    // 收藏列表載入失敗不影響行程資訊顯示。
  }
}

const addPlanToFavorites = async () => {
  if (!isValidTripId.value || isSavingPlanFavorite.value) return

  const targetId = Number(tripId.value)
  try {
    const nextFavoriteState = await togglePlanFavorite(targetId)
    toast(nextFavoriteState ? '已加入收藏' : '已取消收藏')
  } catch (error) {
    const message = error?.message || error?.Message || '收藏狀態更新失敗，請稍後再試'
    toast(message, 'error')
  }
}

// ── 留言操作 ─────────────────────────────────────────────────────────────────

const setBusyComment = (targetRef, commentId, isBusy) => {
  targetRef.value = isBusy
    ? [...targetRef.value, commentId]
    : targetRef.value.filter((id) => id !== commentId)
}

const isCommentSaving = (commentId) => savingCommentIds.value.includes(commentId)
const isCommentDeleting = (commentId) => deletingCommentIds.value.includes(commentId)
const isReplySubmitting = (commentId) => submittingReplyIds.value.includes(commentId)
const isValidCommentText = (text) => {
  if (text.length <= 50) return true
  commentStatusMessage.value = '留言最多 50 字'
  return false
}

const loadComments = async () => {
  try {
    const payload = await getTripComments(commentUserId.value, tripId.value)
    comments.value = asArrayPayload(payload).map(normalizeComment)
  } catch {
    commentStatusMessage.value = '留言載入失敗'
    comments.value = []
  }
}

const submitComment = async () => {
  const text = newComment.value.trim()
  if (!text || isSubmittingComment.value) return
  if (!isValidCommentText(text)) return
  isSubmittingComment.value = true
  commentStatusMessage.value = ''
  try {
    const payload = await createTripComment(commentUserId.value, tripId.value, {
      Content: text,
      ParentID: null,
    })
    comments.value.unshift(normalizeComment({ Content: text, ...unwrapPlanPayload(payload) }))
    newComment.value = ''
  } catch {
    commentStatusMessage.value = '留言新增失敗，請稍後再試'
  } finally {
    isSubmittingComment.value = false
  }
}

const getTransportBetween = (currentSpot, nextSpot) =>
  currentSpot?.transport || nextSpot?.transport || ''

const getTransportDetailBetween = (currentSpot, nextSpot) => {
  const transport = getTransportBetween(currentSpot, nextSpot)
  const transit = currentSpot?.transit || nextSpot?.transit || {}
  return {
    label: transport,
    duration: transit.duration || '',
    line: transit.line || '',
    from: transit.from || '',
    to: transit.to || '',
    isTransit: isTransitTransport(transport),
  }
}

const startEditComment = (comment) => {
  editingCommentId.value = comment.id
  editingCommentText.value = comment.text
  replyingCommentId.value = null
  replyText.value = ''
}

const cancelEditComment = () => {
  editingCommentId.value = null
  editingCommentText.value = ''
}

const startReplyComment = (comment) => {
  replyingCommentId.value = comment.id
  replyText.value = ''
  cancelEditComment()
}

const cancelReplyComment = () => {
  replyingCommentId.value = null
  replyText.value = ''
}

const submitReply = async (comment) => {
  const text = replyText.value.trim()
  if (!text || isReplySubmitting(comment.id)) return
  if (!isValidCommentText(text)) return
  setBusyComment(submittingReplyIds, comment.id, true)
  commentStatusMessage.value = ''
  try {
    const payload = await createTripComment(commentUserId.value, tripId.value, {
      Content: text,
      ParentID: comment.id,
    })
    comments.value.push(
      normalizeComment({
        Content: text,
        ParentID: comment.id,
        ...unwrapPlanPayload(payload),
      }),
    )
    cancelReplyComment()
  } catch {
    commentStatusMessage.value = '回覆新增失敗，請稍後再試'
  } finally {
    setBusyComment(submittingReplyIds, comment.id, false)
  }
}

const saveEditComment = async (comment) => {
  const text = editingCommentText.value.trim()
  if (!text || isCommentSaving(comment.id)) return
  if (!isValidCommentText(text)) return
  setBusyComment(savingCommentIds, comment.id, true)
  commentStatusMessage.value = ''
  try {
    const payload = await updateTripComment(commentUserId.value, tripId.value, comment.id, {
      Content: text,
    })
    const updatedComment = normalizeComment({
      ...comment,
      ...unwrapPlanPayload(payload),
      Content: text,
    })
    comments.value = comments.value.map((item) =>
      item.id === comment.id ? { ...item, ...updatedComment } : item,
    )
    cancelEditComment()
  } catch {
    commentStatusMessage.value = '留言編輯失敗，請稍後再試'
  } finally {
    setBusyComment(savingCommentIds, comment.id, false)
  }
}

const removeComment = async (comment) => {
  if (isCommentDeleting(comment.id)) return
  setBusyComment(deletingCommentIds, comment.id, true)
  commentStatusMessage.value = ''
  try {
    await deleteTripComment(commentUserId.value, tripId.value, comment.id)
    comments.value = comments.value.filter((item) => item.id !== comment.id)
    if (editingCommentId.value === comment.id) cancelEditComment()
  } catch {
    commentStatusMessage.value = '留言刪除失敗，請稍後再試'
  } finally {
    setBusyComment(deletingCommentIds, comment.id, false)
  }
}

// ── Google Maps ───────────────────────────────────────────────────────────────

const getPlacePosition = (spot) => {
  const lat = toNumberOrNull(spot?.lat)
  const lng = toNumberOrNull(spot?.lng)
  return lat === null || lng === null ? null : { lat, lng }
}

const getCssColor = (variableName, fallback = '') =>
  getComputedStyle(document.documentElement).getPropertyValue(variableName).trim() || fallback

const getPrimaryMapColor = () =>
  getCssColor(
    '--bs-primary',
    getCssColor('--bs-primary-rgb') ? `rgb(${getCssColor('--bs-primary-rgb')})` : '',
  )

const createMarkerElement = (isSelected, label) => {
  const el = document.createElement('div')
  el.className = isSelected ? 'pi-map-marker pi-map-marker--selected' : 'pi-map-marker'
  el.innerHTML = `<span class="pi-map-marker-label">${label}</span>`
  return el
}

const createLegacyMarkerIcon = (isSelected) => {
  const primaryColor = getPrimaryMapColor()
  return {
    path: window.google.maps.SymbolPath.CIRCLE,
    scale: isSelected ? 18 : 15,
    fillColor: primaryColor || undefined,
    fillOpacity: 1,
    strokeColor: getCssColor('--bs-white') || undefined,
    strokeWeight: 3,
  }
}

const updateMarkerStyles = () => {
  if (!googleMap) return
  mapMarkers.forEach(({ marker, spot }) => {
    if (marker.content) {
      marker.content.className =
        spot.id === selectedMarkerId.value
          ? 'pi-map-marker pi-map-marker--selected'
          : 'pi-map-marker'
    } else if (marker.setIcon) {
      marker.setIcon(createLegacyMarkerIcon(spot.id === selectedMarkerId.value))
    }
  })
}

const createMapMarker = (spot, index) => {
  const position = getPlacePosition(spot)
  if (!googleMap || !position) return null

  let marker
  const isSelected = spot.id === selectedMarkerId.value
  const isAdvanced =
    markerConstructor?.name === 'AdvancedMarkerElement' ||
    markerConstructor?.toString?.().includes('AdvancedMarker')

  if (isAdvanced) {
    const el = createMarkerElement(isSelected, index + 1)
    marker = new markerConstructor({ map: googleMap, position, title: spot.name, content: el })
    marker.addEventListener('gmp-click', () => {
      selectedMarkerId.value = spot.id
      updateMarkerStyles()
      googleMap.panTo(position)
    })
  } else {
    marker = new markerConstructor({
      map: googleMap,
      position,
      title: spot.name,
      icon: createLegacyMarkerIcon(isSelected),
      label: {
        text: String(index + 1),
        color: getCssColor('--bs-white') || undefined,
        fontSize: isSelected ? '18px' : '16px',
        fontWeight: 'bold',
      },
    })
    marker.addListener('click', () => {
      selectedMarkerId.value = spot.id
      updateMarkerStyles()
      googleMap.panTo(position)
    })
  }

  return { marker, position, spot }
}

const renderMapMarkers = () => {
  if (!googleMap || !markerConstructor) return
  mapMarkers.forEach(({ marker }) => {
    marker.map = null
    if (marker.setMap) marker.setMap(null)
  })
  mapMarkers = []

  const validSpots = currentDaySpots.value.filter((spot) => getPlacePosition(spot) !== null)
  if (!validSpots.length) {
    googleMap.setCenter(KAOHSIUNG_CITY_CENTER)
    googleMap.setZoom(DEFAULT_MAP_ZOOM)
    return
  }

  mapMarkers = validSpots.map((spot, index) => createMapMarker(spot, index)).filter(Boolean)
  const bounds = new window.google.maps.LatLngBounds()
  mapMarkers.forEach(({ position }) => bounds.extend(position))
  googleMap.fitBounds(bounds)
  if (mapMarkers.length === 1) googleMap.setZoom(14)
}

const refreshMapView = async () => {
  await nextTick()
  window.requestAnimationFrame(() => {
    renderMapMarkers()
  })
}

const selectSpotOnMap = (spot) => {
  selectedMarkerId.value = spot.id
  updateMarkerStyles()
  const position = getPlacePosition(spot)
  if (googleMap && position) googleMap.panTo(position)
}

const initializeMap = async () => {
  if (!mapElement.value) return
  mapErrorMessage.value = ''
  try {
    const { Map } = await googleMaps.loadMaps()
    const mapId = import.meta.env.VITE_GOOGLE_MAPS_MAP_ID
    const { AdvancedMarkerElement } = await googleMaps.loadMarker()
    markerConstructor =
      mapId && AdvancedMarkerElement ? AdvancedMarkerElement : window.google.maps.Marker

    googleMap = new Map(mapElement.value, {
      center: KAOHSIUNG_CITY_CENTER,
      zoom: DEFAULT_MAP_ZOOM,
      mapId: mapId || undefined,
      clickableIcons: false,
      streetViewControl: false,
      mapTypeControl: false,
      fullscreenControl: true,
      zoomControl: true,
    })

    mapClickListener = googleMap.addListener('click', () => {
      selectedMarkerId.value = null
      updateMarkerStyles()
    })

    await refreshMapView()
  } catch {
    mapErrorMessage.value = 'Google Map 載入失敗。'
  }
}

const cleanupMap = () => {
  mapMarkers.forEach(({ marker }) => {
    marker.map = null
    if (marker.setMap) marker.setMap(null)
  })
  if (mapClickListener) window.google?.maps?.event?.removeListener(mapClickListener)
  googleMap = null
}

// ── 資料載入 ─────────────────────────────────────────────────────────────────

const loadPlanInfo = async () => {
  isLoading.value = true
  errorMessage.value = ''
  commentStatusMessage.value = ''

  if (!isValidTripId.value) {
    errorMessage.value = '行程編號格式錯誤，請重新從行程列表進入。'
    plan.value = null
    comments.value = []
    isLoading.value = false
    return
  }

  try {
    const payload = await getPlan(routeUserId.value, tripId.value)
    const currentPlan = unwrapPlanPayload(payload)
    plan.value = normalizePlan(currentPlan)
    activeDay.value = plan.value.days[0]?.day ?? 1
    await syncPlanFavoriteStatus()
    await loadComments()
  } catch (error) {
    errorMessage.value = error?.message || error?.Message || '行程載入失敗。'
    plan.value = null
    comments.value = []
  } finally {
    isLoading.value = false
  }
}

watch(activeDay, () => {
  selectedMarkerId.value = null
  refreshMapView()
})

onMounted(async () => {
  await loadPlanInfo()
  await initializeMap()
})

onBeforeUnmount(cleanupMap)
</script>

<template>
  <div class="plan-info-page">
    <!-- ── Loading 骨架 ── -->
    <template v-if="isLoading">
      <div class="pi-skeleton-hero"></div>
      <div class="pi-body">
        <div class="pi-left">
          <div class="pi-section">
            <div class="skeleton-block pi-skeleton-owner"></div>
          </div>
          <div class="pi-section">
            <div class="row g-2">
              <div class="col-4"><div class="skeleton-block pi-skeleton-stat"></div></div>
              <div class="col-4"><div class="skeleton-block pi-skeleton-stat"></div></div>
              <div class="col-4"><div class="skeleton-block pi-skeleton-stat"></div></div>
            </div>
          </div>
        </div>
        <div class="pi-right pi-right--skeleton"></div>
      </div>
    </template>

    <!-- ── 錯誤訊息 ── -->
    <div v-else-if="errorMessage" class="pi-error-wrap">
      <div class="alert alert-danger mb-0" role="alert">{{ errorMessage }}</div>
    </div>

    <!-- ── 正常內容 ── -->
    <template v-else-if="plan">
      <!-- Hero 封面區（全寬） -->
      <div class="hero-section">
        <img
          :src="plan.coverImage ? $img(plan.coverImage) : placeholderCover"
          class="hero-img"
          alt="行程封面"
        />
        <div class="hero-gradient"></div>

        <!-- 返回按鈕（左上角） -->
        <button class="hero-back-btn" aria-label="返回上一頁" @click="router.back()">
          <i class="ri-arrow-left-line fs-5"></i>
        </button>

        <!-- 精選推薦 badge（右上角） -->
        <div v-if="plan.isOfficeRecommended" class="hero-suggest-badge">
          <i class="ri-star-fill"></i> 精選推薦
        </div>

        <!-- 行程標題文字 -->
        <div class="hero-text">
          <div class="hero-title-wrap">
            <div class="hero-title-content">
              <div class="hero-heading-row">
                <h1>{{ plan.title }}</h1>
                <span v-if="plan.tripTag" class="badge bg-light text-dark hero-trip-tag">
                  {{ plan.tripTag }}
                </span>
              </div>
              <div class="hero-meta">
                <span
                  ><i class="ri-calendar-line me-1"></i>{{ plan.startDate }} ~
                  {{ plan.endDate }}</span
                >
                <span>·</span>
                <span>{{ plan.stats.days }} 天行程</span>
                <span>·</span>
                <span>{{ plan.stats.spots }} 個景點</span>
                <span v-if="canSuggestPlan" class="badge bg-light text-dark fw-normal py-1 px-2">
                  <i class="ri-earth-line me-1"></i>公開
                </span>
                <span v-else class="badge bg-secondary fw-normal py-1 px-2">
                  <i class="ri-lock-line me-1"></i>私密
                </span>
              </div>
            </div>
            <div class="hero-action-stack">
              <button
                v-if="canShowSuggestButton"
                type="button"
                class="hero-suggest-btn"
                :class="{ 'is-active': plan.isOfficeRecommended }"
                :disabled="isSavingSuggest || !canSuggestPlan"
                @click="toggleSuggestStatus"
              >
                <i :class="suggestButtonIcon"></i>
                {{ isSavingSuggest ? '更新中...' : suggestButtonLabel }}
              </button>
              <button
                v-if="!isOwnPlanInfo"
                type="button"
                class="hero-edit-btn"
                :class="{ 'is-active': isPlanFavorite }"
                :disabled="isSavingPlanFavorite"
                @click="addPlanToFavorites"
              >
                <i :class="isPlanFavorite ? 'ri-heart-3-fill' : 'ri-heart-3-line'"></i>
                {{ isSavingPlanFavorite ? '更新中...' : isPlanFavorite ? '取消收藏' : '加入收藏' }}
              </button>
              <button
                v-if="isOwnPlanInfo"
                type="button"
                class="hero-edit-btn"
                @click="goToPlanView"
              >
                <i class="ri-edit-line"></i>
                前往編輯行程
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- ── 主體：左右分割 ── -->
      <div class="pi-body">
        <!-- 左欄：可捲動內容 -->
        <div class="pi-left">
          <!-- 版主資訊卡 -->
          <div class="pi-section">
            <div class="owner-card">
              <div class="owner-row">
                <!-- 左：頭像 + 姓名 + 日期 -->
                <div class="owner-info">
                  <img
                    :src="plan.owner.avatar ? $img(plan.owner.avatar) : defaultAvatar"
                    alt="版主頭像"
                    class="owner-avatar"
                    @error="setDefaultAvatar"
                  />
                  <div class="owner-meta">
                    <span class="owner-name">{{ plan.owner.name || '未知使用者' }}</span>
                    <span class="owner-date">{{ plan.owner.postDate }}</span>
                  </div>
                </div>
                <!-- 右：共同編輯者 + 推薦 -->
                <div class="d-flex align-items-center gap-2">
                  <LuiuAvatarGroup
                    v-if="plan.collaborators.length"
                    :users="
                      plan.collaborators.map((user) => ({
                        ...user,
                        avatarUrl: user.avatarUrl ? $img(user.avatarUrl) : '',
                      }))
                    "
                    :max="3"
                  />
                </div>
              </div>
              <!-- 行程簡介 -->
              <p v-if="plan.desc" class="plan-desc mt-3 mb-0">{{ plan.desc }}</p>
            </div>
          </div>

          <!-- 統計卡片 -->
          <div class="pi-section">
            <div class="row g-2">
              <div class="col-4">
                <LuiuStatCard
                  title="旅遊天數"
                  :value="plan.stats.days"
                  suffix=" 天"
                  iconClass="ri-calendar-event-line"
                  iconBgClass="bg-danger bg-opacity-10 text-danger"
                />
              </div>
              <div class="col-4">
                <LuiuStatCard
                  title="景點數量"
                  :value="plan.stats.spots"
                  suffix=" 個"
                  iconClass="ri-map-pin-line"
                  iconBgClass="bg-primary bg-opacity-10 text-primary"
                />
              </div>
              <div class="col-4">
                <LuiuStatCard
                  title="預估花費"
                  :value="plan.stats.budget"
                  prefix="NT$ "
                  iconClass="ri-wallet-3-line"
                  iconBgClass="bg-success bg-opacity-10 text-success"
                />
              </div>
            </div>
          </div>

          <!-- 標籤列 -->
          <div v-if="plan.tags && plan.tags.length" class="plan-tags pi-section">
            <span v-for="tag in plan.tags" :key="tag" class="plan-tag">
              <i class="ri-price-tag-3-line"></i>{{ tag }}
            </span>
          </div>

          <!-- Day 選擇器 -->
          <div class="pi-section">
            <div class="day-selector hide-scrollbar">
              <button
                v-for="d in plan.days"
                :key="d.day"
                class="day-btn"
                :class="{ active: activeDay === d.day }"
                @click="activeDay = d.day"
              >
                {{ d.label }}
                <span class="day-date">{{ d.date }}</span>
              </button>
            </div>
          </div>

          <!-- 時間軸景點列表 -->
          <div class="pi-section">
            <div class="timeline-wrap">
              <!-- 無景點提示 -->
              <div v-if="currentDaySpots.length === 0" class="empty-day">
                <i class="ri-calendar-check-line fs-2 d-block mb-2 opacity-50"></i>
                這天尚未安排景點。
              </div>

              <!-- 景點列表 -->
              <div v-for="(spot, idx) in currentDaySpots" :key="spot.id" class="timeline-item">
                <!-- 時間標示 + 圓點 -->
                <div class="timeline-time-row">
                  <span class="timeline-dot"></span>
                  <span v-if="spot.time" class="timeline-time">{{ spot.time }}</span>
                </div>

                <!-- 景點卡片 -->
                <div
                  class="spot-card card"
                  :class="{ active: selectedMarkerId === spot.id }"
                  role="button"
                  tabindex="0"
                  @click="selectSpotOnMap(spot)"
                  @keydown.enter.prevent="selectSpotOnMap(spot)"
                  @keydown.space.prevent="selectSpotOnMap(spot)"
                >
                  <img
                    v-if="spot.image"
                    :src="$img(spot.image)"
                    class="spot-img"
                    :alt="spot.name"
                  />
                  <div class="spot-body">
                    <h5 class="spot-name">{{ spot.name }}</h5>
                    <p v-if="spot.address" class="spot-address">
                      <i class="ri-map-pin-2-line"></i>{{ spot.address }}
                    </p>
                    <p v-if="spot.description" class="spot-desc">{{ spot.description }}</p>
                    <div class="spot-meta">
                      <span v-if="spot.duration" class="spot-meta-chip">
                        <i class="ri-time-line"></i>{{ spot.duration }}
                      </span>
                      <span v-if="spot.cost" class="spot-meta-chip">
                        <i class="ri-money-dollar-circle-line"></i>{{ spot.cost }}
                      </span>
                    </div>
                  </div>
                </div>

                <!-- 前往下一景點的交通方式（最後一個不顯示） -->
                <div
                  v-if="
                    idx < currentDaySpots.length - 1 &&
                    getTransportBetween(spot, currentDaySpots[idx + 1])
                  "
                  class="transport-row"
                >
                  <div
                    class="transport-badge"
                    :class="{
                      'transport-badge--stacked': getTransportDetailBetween(
                        spot,
                        currentDaySpots[idx + 1],
                      ).isTransit,
                    }"
                  >
                    <span class="transport-main">
                      <i
                        :class="transportIcon(getTransportBetween(spot, currentDaySpots[idx + 1]))"
                      ></i>
                      {{ getTransportBetween(spot, currentDaySpots[idx + 1]) }}
                      <span
                        v-if="getTransportDetailBetween(spot, currentDaySpots[idx + 1]).duration"
                        class="transport-duration"
                      >
                        {{ getTransportDetailBetween(spot, currentDaySpots[idx + 1]).duration }}
                      </span>
                    </span>
                    <span
                      v-if="
                        getTransportDetailBetween(spot, currentDaySpots[idx + 1]).isTransit &&
                        (getTransportDetailBetween(spot, currentDaySpots[idx + 1]).line ||
                          getTransportDetailBetween(spot, currentDaySpots[idx + 1]).from ||
                          getTransportDetailBetween(spot, currentDaySpots[idx + 1]).to)
                      "
                      class="transport-transit"
                    >
                      <span v-if="getTransportDetailBetween(spot, currentDaySpots[idx + 1]).line">
                        {{ getTransportDetailBetween(spot, currentDaySpots[idx + 1]).line }}
                      </span>
                      <span
                        v-if="
                          getTransportDetailBetween(spot, currentDaySpots[idx + 1]).from ||
                          getTransportDetailBetween(spot, currentDaySpots[idx + 1]).to
                        "
                      >
                        {{
                          getTransportDetailBetween(spot, currentDaySpots[idx + 1]).from || '未定站'
                        }}
                        <i class="ri-arrow-right-line"></i>
                        {{
                          getTransportDetailBetween(spot, currentDaySpots[idx + 1]).to || '未定站'
                        }}
                      </span>
                    </span>
                  </div>
                </div>
              </div>
              <!-- /.timeline-item -->
            </div>
            <!-- /.timeline-wrap -->
          </div>

          <!-- 留言區 -->
          <div class="comment-section pi-section">
            <div class="comment-section-title">
              <h2>
                留言<span class="comment-count ms-1">({{ comments.length }})</span>
              </h2>
            </div>

            <!-- 留言輸入列 -->
            <div class="comment-input-row">
              <img
                :src="currentUserAvatar ? $img(currentUserAvatar) : defaultAvatar"
                alt="我的頭像"
                class="comment-avatar"
                @error="setDefaultAvatar"
              />
              <textarea
                v-model="newComment"
                class="comment-input"
                placeholder="留言…"
                rows="1"
                maxlength="50"
                :disabled="isSubmittingComment"
                @keydown.enter.exact.prevent="submitComment"
              ></textarea>
              <button
                class="comment-submit-btn"
                :disabled="!newComment.trim() || isSubmittingComment"
                @click="submitComment"
              >
                {{ isSubmittingComment ? '送出中' : '送出' }}
              </button>
            </div>

            <p v-if="commentStatusMessage" class="comment-status" role="status">
              {{ commentStatusMessage }}
            </p>

            <!-- 留言列表 -->
            <div v-if="comments.length" class="comment-list">
              <div
                v-for="comment in flattenedCommentThreads"
                :key="comment.id"
                class="comment-item"
                :class="{ 'comment-item--reply': comment.depth > 0 }"
                :style="{
                  '--comment-depth': comment.desktopDepth,
                  '--comment-mobile-depth': comment.mobileDepth,
                }"
              >
                <img
                  :src="comment.avatar ? $img(comment.avatar) : defaultAvatar"
                  :alt="comment.author"
                  class="comment-item-avatar"
                  @error="setDefaultAvatar"
                />
                <div class="comment-item-body">
                  <div class="comment-item-header">
                    <span class="comment-author">{{ comment.author }}</span>
                    <span class="comment-time">{{ comment.time }}</span>
                  </div>
                  <div v-if="editingCommentId === comment.id" class="comment-edit">
                    <textarea
                      v-model="editingCommentText"
                      class="form-control custom-border"
                      rows="2"
                      maxlength="50"
                      :disabled="isCommentSaving(comment.id)"
                      @keydown.enter.exact.prevent="saveEditComment(comment)"
                      @keydown.esc="cancelEditComment"
                    ></textarea>
                    <div class="comment-actions">
                      <button
                        type="button"
                        :disabled="isCommentSaving(comment.id) || !editingCommentText.trim()"
                        @click="saveEditComment(comment)"
                      >
                        {{ isCommentSaving(comment.id) ? '儲存中' : '儲存' }}
                      </button>
                      <button type="button" @click="cancelEditComment">取消</button>
                    </div>
                  </div>
                  <template v-else>
                    <p class="comment-text">{{ comment.text }}</p>
                    <div class="comment-actions">
                      <button
                        v-if="comment.depth === 0"
                        type="button"
                        @click="startReplyComment(comment)"
                      >
                        回覆
                      </button>
                      <button
                        v-if="comment.canEdit"
                        type="button"
                        @click="startEditComment(comment)"
                      >
                        編輯
                      </button>
                      <button
                        v-if="comment.canDelete"
                        type="button"
                        :disabled="isCommentDeleting(comment.id)"
                        @click="removeComment(comment)"
                      >
                        {{ isCommentDeleting(comment.id) ? '刪除中' : '刪除' }}
                      </button>
                    </div>
                    <div v-if="replyingCommentId === comment.id" class="comment-reply">
                      <textarea
                        v-model="replyText"
                        class="form-control custom-border"
                        rows="2"
                        placeholder="寫下回覆…"
                        maxlength="50"
                        :disabled="isReplySubmitting(comment.id)"
                        @keydown.enter.exact.prevent="submitReply(comment)"
                        @keydown.esc="cancelReplyComment"
                      ></textarea>
                      <div class="comment-actions">
                        <button
                          type="button"
                          :disabled="isReplySubmitting(comment.id) || !replyText.trim()"
                          @click="submitReply(comment)"
                        >
                          {{ isReplySubmitting(comment.id) ? '送出中' : '送出回覆' }}
                        </button>
                        <button type="button" @click="cancelReplyComment">取消</button>
                      </div>
                    </div>
                  </template>
                </div>
              </div>
            </div>

            <!-- 無留言 -->
            <div v-else class="comment-empty">
              <i class="ri-chat-3-line"></i>
              還沒有留言，成為第一個留言的人吧！
            </div>
          </div>
          <!-- /.comment-section -->
        </div>
        <!-- /.pi-left -->

        <!-- 右欄：地圖面板（sticky） -->
        <div class="pi-right">
          <div class="pi-map-header">
            <span class="pi-map-title"> <i class="ri-map-2-line"></i> 行程地圖 </span>
            <span class="map-panel-badge">
              <i class="ri-map-pin-line"></i>
              Day {{ activeDay }}・{{ currentDaySpots.length }} 景點
            </span>
          </div>

          <div class="pi-map-area">
            <!-- Google Map 容器 -->
            <div ref="mapElement" class="pi-map">
              <div v-if="mapErrorMessage" class="pi-map-error">
                <i class="ri-error-warning-line"></i>
                {{ mapErrorMessage }}
              </div>
            </div>

            <!-- 景點快速列表 -->
            <div v-if="currentDaySpots.length" class="map-spot-list">
              <div
                v-for="(spot, idx) in currentDaySpots"
                :key="spot.id"
                class="map-spot-item"
                :class="{ active: selectedMarkerId === spot.id }"
                @click="selectSpotOnMap(spot)"
              >
                <div class="map-spot-pin">{{ idx + 1 }}</div>
                <div class="map-spot-info">
                  <div class="map-spot-name">{{ spot.name }}</div>
                  <div v-if="spot.address" class="map-spot-addr">{{ spot.address }}</div>
                </div>
                <span v-if="spot.time" class="map-spot-time">{{ spot.time }}</span>
              </div>
            </div>
            <div v-else class="map-spot-empty">今日尚無景點</div>
          </div>
        </div>
        <!-- /.pi-right -->
      </div>
      <!-- /.pi-body -->
    </template>
  </div>
  <!-- /.plan-info-page -->
</template>

<style lang="scss" scoped>
@import '@/assets/scss/pages/planinfo';
</style>
