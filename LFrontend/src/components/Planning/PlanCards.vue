<script setup>
import { deletePlan, unwrapPlanPayload, updatePlan } from '@/api/planning/plan'
import LuiuAvator from '@/components/base/LuiuAvator.vue'
import PlanSettingsDialog from '@/components/Planning/PlanSettingsDialog.vue'
import { PLAN_PLACEHOLDER_COVER_URL } from '@/constants/planDefaults'
import { normalizePrivacyStatus, resolvePrivacySuggest } from '@/utils/planPrivacy'
import { luiuNotify, toast } from '@/utils/sweetAlert'
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'

const props = defineProps({
  userId: {
    type: String,
    required: true,
  },
  items: {
    type: Array,
    default: () => [],
  },
})

const emit = defineEmits(['update-plan', 'delete-plan'])
const router = useRouter()

const activePlanId = ref(null)
const copyMessage = ref('')
const isDeleting = ref(false)
const isSaving = ref(false)

const placeholderCover = PLAN_PLACEHOLDER_COVER_URL

const activeItem = computed(() => props.items.find((item) => item.TripID === activePlanId.value))
const isDialogOpen = computed(() => Boolean(activePlanId.value))

const getPlanValue = (plan, ...keys) => {
  for (const key of keys) {
    const value = plan?.[key]
    if (value !== undefined && value !== null) return value
  }

  return ''
}

const getPlanCreateAt = (plan) =>
  getPlanValue(plan, 'CreateAt', 'createAt', 'CreatedAt', 'createdAt')

const getPlanUpdateAt = (plan) =>
  getPlanValue(plan, 'UpdateAt', 'updateAt', 'UpdatedAt', 'updatedAt')

const goToPlan = (tripId) => {
  router.push({
    name: 'Plan',
    params: {
      userId: props.userId,
      tripId,
    },
  })
}

const goToInfo = (planId) => {
  router.push({
    name: 'PlanInfo',
    params: {
      userId: props.userId,
      planId,
    },
  })
}

const goToOwner = () => {
  router.push({ name: 'MemberProfile' })
}

const getPlanInfoHref = (planId) => {
  const href = router.resolve({
    name: 'PlanInfo',
    params: {
      userId: props.userId,
      planId,
    },
  }).href

  return `${window.location.origin}${href}`
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

const copyShareLink = async (item) => {
  try {
    await copyText(getPlanInfoHref(item.TripID))
    copyMessage.value = '已複製行程資訊連結'
  } catch {
    copyMessage.value = '複製失敗，請稍後再試'
  }
}

const handleImageError = (event) => {
  // 當圖片載入失敗時，將 src 指向預設圖
  if (event.target.src !== PLAN_PLACEHOLDER_COVER_URL) {
    event.target.src = PLAN_PLACEHOLDER_COVER_URL
  }
}

const openSettings = (item) => {
  activePlanId.value = item.TripID
  copyMessage.value = ''
}

const closeSettings = () => {
  activePlanId.value = null
  copyMessage.value = ''
}

const appendOptionalFormValue = (formData, key, value) => {
  if (value !== null && value !== undefined && value !== '') {
    formData.append(key, value)
  }
}

const saveSettings = async ({ draftPlan: settingsDraft, photoFile }) => {
  const currentPlan = activeItem.value
  const tripName = settingsDraft.TripName.trim()

  if (!currentPlan) {
    return
  }

  if (!tripName) {
    toast('請輸入旅行名稱', 'error')
    return
  }

  if (!settingsDraft.StartDate || !settingsDraft.EndDate) {
    toast('缺少行程日期，請重新整理後再試', 'error')
    return
  }

  const privacyStatus = normalizePrivacyStatus(settingsDraft.PrivacyStatus)
  const isSuggest = resolvePrivacySuggest(privacyStatus)
  const formData = new FormData()

  formData.append('TripName', tripName)
  formData.append('StartDate', settingsDraft.StartDate)
  formData.append('EndDate', settingsDraft.EndDate)
  formData.append('TripDesc', settingsDraft.TripDesc ?? '')
  formData.append('TripTag', settingsDraft.TripTag ?? '')
  formData.append('PrivacyStatus', String(privacyStatus))
  formData.append('IsSuggest', String(isSuggest))
  appendOptionalFormValue(formData, 'ShortCode', settingsDraft.ShortCode)
  appendOptionalFormValue(formData, 'ListId', settingsDraft.ListId)

  if (photoFile) {
    formData.append('Photo', photoFile)
  }

  isSaving.value = true

  try {
    const payload = await updatePlan(props.userId, settingsDraft.TripID, formData)
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
      PrivacyStatus: privacyStatus,
      IsSuggest: isSuggest,
      OfficeOper: currentPlan.OfficeOper,
      CreateAt: getPlanCreateAt(currentPlan),
      UpdateAt: getPlanUpdateAt(currentPlan),
      ShortCode: settingsDraft.ShortCode,
      ListID: settingsDraft.ListId,
      ListId: settingsDraft.ListId,
      Collaborators: [...settingsDraft.Collaborators],
    }

    emit('update-plan', {
      ...fallbackPlan,
      ...(savedPlan && typeof savedPlan === 'object' && savedPlan),
      CreateAt: getPlanCreateAt(savedPlan) || getPlanCreateAt(fallbackPlan),
      UpdateAt: getPlanUpdateAt(savedPlan) || getPlanUpdateAt(fallbackPlan),
      OwnerIconURL: savedPlan?.OwnerIconURL || currentPlan.OwnerIconURL,
      OwnerName: savedPlan?.OwnerName || currentPlan.OwnerName,
      Collaborators: savedPlan?.Collaborators || fallbackPlan.Collaborators,
    })
    closeSettings()
    toast('行程設定已儲存')
  } catch (error) {
    const errorMessage = error?.message || error?.Message || '儲存失敗，請稍後再試'
    toast(errorMessage, 'error')
  } finally {
    isSaving.value = false
  }
}

const deleteCurrentPlan = async (settingsDraft = {}) => {
  const targetTripId = settingsDraft.TripID
  const result = await luiuNotify.confirmDelete(
    '確定要刪除此行程嗎?',
    '刪除後此行程將不會出現在清單中。',
  )

  if (!result.isConfirmed) {
    return
  }

  isDeleting.value = true

  try {
    await deletePlan(props.userId, targetTripId)
    emit('delete-plan', targetTripId)
    closeSettings()
    toast('行程已刪除')
  } catch (error) {
    const errorMessage = error?.message || error?.Message || '刪除失敗，請稍後再試'
    toast(errorMessage, 'error')
  } finally {
    isDeleting.value = false
  }
}

const formatDateTime = (value) => {
  if (!value) {
    return '尚無建立時間'
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleString('zh-TW', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}
</script>

<template>
  <div class="container-fluid blog plan-cards py-5">
    <div class="container py-5">
      <div class="row g-4 justify-content-center plan-card-row">
        <!-- 遍歷每一張卡片 -->
        <div v-for="item in items" :key="item.TripID" class="col-lg-4 col-md-6">
          <article
            class="blog-item d-flex flex-column h-100"
            role="button"
            tabindex="0"
            @click="goToPlan(item.TripID)"
            @keydown.enter.prevent="goToPlan(item.TripID)"
            @keydown.space.prevent="goToPlan(item.TripID)"
          >
            <!-- 圖片區域 -->
            <div class="blog-img">
              <div
                class="blog-img-inner"
                role="button"
                tabindex="0"
                :aria-label="`複製 ${item.TripName || '行程'} 分享連結`"
                @click.stop="copyShareLink(item)"
                @keydown.enter.stop.prevent="copyShareLink(item)"
                @keydown.space.stop.prevent="copyShareLink(item)"
              >
                <img
                  class="img-fluid w-100 rounded-top"
                  :src="item.PhotoURL ? $img(item.PhotoURL) : placeholderCover"
                  :alt="item.TripName"
                  @error="handleImageError"
                />
                <span v-if="item.TripTag" class="badge text-bg-primary plan-trip-tag">
                  {{ item.TripTag }}
                </span>
                <!-- 日期資訊 (放在圖片底部或圖片下方) -->
                <div class="blog-info d-flex align-items-center border border-start-0 border-end-0">
                  <small class="flex-fill text-center border-end py-2">
                    <i class="fa fa-calendar-alt me-2"></i>{{ item.StartDate }} -
                    {{ item.EndDate }}
                  </small>
                </div>
                <!-- 遮罩層：Hover 擴展效果 -->
                <div class="blog-icon" aria-hidden="true">
                  <i class="fas fa-link fa-2x text-white"></i>
                </div>
                <!-- 點擊圖片動作 -->
                <div class="plan-card-actions">
                  <button
                    class="plan-owner"
                    type="button"
                    :aria-label="`${item.OwnerName || 'User'} profile`"
                    @click.stop="goToOwner"
                  >
                    <LuiuAvator
                      :avatar="item.OwnerIconURL ? $img(item.OwnerIconURL) : undefined"
                      size="sm"
                    />
                  </button>
                  <button
                    class="plan-more"
                    type="button"
                    aria-label="更多"
                    @click.stop="openSettings(item)"
                  >
                    <span aria-hidden="true">...</span>
                  </button>
                </div>
              </div>
              <!-- /.blog-img-inner -->
            </div>
            <!-- /.blog-img -->

            <!-- 內容區域 -->
            <div
              class="blog-content border border-top-0 border-bottom-0 p-4 flex-grow-1"
              @click.stop
            >
              <p class="plan-card-owner mb-1">{{ item.OwnerName }}</p>
              <h2 class="plan-card-title">{{ item.TripName }}</h2>
              <p class="plan-card-desc my-3">
                {{ item.TripDesc }}
              </p>
            </div>
            <!-- 底部按鈕 -->
            <div
              class="blog-footer border border-top-0 rounded-bottom p-4 pt-0 d-flex justify-content-between"
            >
              <button
                type="button"
                class="luiu-btn-primary flex-fill me-2"
                @click.stop="goToInfo(item.TripID)"
              >
                檢視行程
              </button>
              <button
                type="button"
                class="luiu-btn-primary flex-fill ms-2"
                @click.stop="goToPlan(item.TripID)"
              >
                編輯行程
              </button>
            </div>
          </article>
          <!-- /.blog-item -->
        </div>
        <!-- /v-for col -->
      </div>
      <!-- /.row -->
    </div>
    <!-- /.container -->

    <PlanSettingsDialog
      :is-open="isDialogOpen"
      :plan="activeItem"
      :is-saving="isSaving"
      :is-deleting="isDeleting"
      :copy-message="copyMessage"
      :placeholder-cover="placeholderCover"
      :image-resolver="$img"
      :format-date-time="formatDateTime"
      @close="closeSettings"
      @save="saveSettings"
      @delete="deleteCurrentPlan"
      @copy-share-link="copyShareLink"
      @owner-click="goToOwner"
    />
  </div>
</template>

<style lang="scss" scoped>
@import '@/assets/scss/pages/plan-list';
</style>
