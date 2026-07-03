<script setup>
import { computed, reactive, ref, watch } from 'vue'
import LuiuAvator from '@/components/base/LuiuAvator.vue'
import LuiuDatePickerRange from '@/components/base/LuiuDatePickerRange.vue'
import { toast } from '@/utils/sweetAlert'
import PlanCoverPhotoField from './PlanCoverPhotoField.vue'
import PlanDialogShell from './PlanDialogShell.vue'
import PlanPrivacySuggestControls from './PlanPrivacySuggestControls.vue'
import {
  PLAN_PRIVACY,
  isPublicPrivacyStatus,
  normalizePrivacyStatus,
  resolvePrivacySuggest,
} from '@/utils/planPrivacy'

const props = defineProps({
  isOpen: {
    type: Boolean,
    default: false,
  },
  plan: {
    type: Object,
    default: null,
  },
  isSaving: {
    type: Boolean,
    default: false,
  },
  isDeleting: {
    type: Boolean,
    default: false,
  },
  copyMessage: {
    type: String,
    default: '',
  },
  placeholderCover: {
    type: String,
    required: true,
  },
  imageResolver: {
    type: Function,
    default: (value) => value,
  },
  formatDateTime: {
    type: Function,
    default: (value) => value || '尚無建立時間',
  },
})

const emit = defineEmits(['close', 'save', 'delete', 'copy-share-link', 'owner-click'])

const selectedPhotoFile = ref(null)
const draftPlan = reactive({
  TripID: '',
  TripName: '',
  TripTag: '',
  TripDesc: '',
  StartDate: '',
  EndDate: '',
  PrivacyStatus: 0,
  IsSuggest: false,
  ShortCode: '',
  ListId: null,
  PhotoURL: '',
  CreateAt: '',
  UpdateAt: '',
  Collaborators: [],
})

const isPublicTrip = computed(() => isPublicPrivacyStatus(draftPlan.PrivacyStatus))
const dateRange = ref([])
const datePickerOptions = {
  dateFormat: 'yyyy-MM-dd',
}

const getPlanValue = (item, ...keys) => {
  for (const key of keys) {
    const value = item?.[key]
    if (value !== undefined && value !== null) return value
  }

  return ''
}

const getPlanCreateAt = (item) =>
  getPlanValue(item, 'CreateAt', 'createAt', 'CreatedAt', 'createdAt')

const getPlanUpdateAt = (item) =>
  getPlanValue(item, 'UpdateAt', 'updateAt', 'UpdatedAt', 'updatedAt')

const formatCreateTime = (value) => (value ? props.formatDateTime(value) : '尚無建立時間')

const formatUpdateTime = (value) => (value ? props.formatDateTime(value) : '尚無更新時間')

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

const normalizeDateRange = (value) => {
  if (Array.isArray(value)) return value.map((item) => String(item || '').trim()).filter(Boolean)

  return String(value || '')
    .split(' ~ ')
    .map((item) => item.trim())
    .filter(Boolean)
}

const syncDraft = () => {
  const currentPlan = props.plan
  selectedPhotoFile.value = null
  if (!currentPlan) return

  const privacyStatus = normalizePrivacyStatus(
    getPlanValue(currentPlan, 'PrivacyStatus', 'privacyStatus'),
  )

  const startDate = toInputDate(
    getPlanValue(currentPlan, 'StartDate', 'startDate', 'StartDateTime', 'startDateTime'),
  )
  const endDate = toInputDate(
    getPlanValue(currentPlan, 'EndDate', 'endDate', 'EndDateTime', 'endDateTime') ||
      getPlanValue(currentPlan, 'StartDate', 'startDate', 'StartDateTime', 'startDateTime'),
  )

  Object.assign(draftPlan, {
    TripID: getPlanValue(currentPlan, 'TripID', 'tripId', 'id') || '',
    TripName: getPlanValue(currentPlan, 'TripName', 'tripName') || '',
    TripTag: getPlanValue(currentPlan, 'TripTag', 'tripTag') || '',
    TripDesc: getPlanValue(currentPlan, 'TripDesc', 'tripDesc') || '',
    StartDate: startDate,
    EndDate: endDate,
    PrivacyStatus: privacyStatus,
    IsSuggest: resolvePrivacySuggest(privacyStatus),
    ShortCode: getPlanValue(currentPlan, 'ShortCode', 'shortCode') || '',
    ListId: currentPlan.ListId ?? currentPlan.ListID ?? null,
    PhotoURL: getPlanValue(currentPlan, 'PhotoURL', 'PhotoUrl', 'photoUrl') || '',
    CreateAt: getPlanCreateAt(currentPlan) || '',
    UpdateAt: getPlanUpdateAt(currentPlan) || '',
    Collaborators: [...(currentPlan.Collaborators || [])],
  })
  dateRange.value = [startDate, endDate].filter(Boolean)
}

watch(
  () => [props.isOpen, props.plan],
  () => {
    if (props.isOpen) syncDraft()
  },
  { immediate: true },
)

watch(dateRange, (value) => {
  const [startDate, endDate] = normalizeDateRange(value)

  draftPlan.StartDate = startDate || ''
  draftPlan.EndDate = endDate || startDate || ''
})

const updatePrivacyStatus = (value) => {
  draftPlan.PrivacyStatus = normalizePrivacyStatus(value)
  draftPlan.IsSuggest = resolvePrivacySuggest(draftPlan.PrivacyStatus)
}

const updateIsSuggest = (value) => {
  if (!isPublicTrip.value) {
    draftPlan.IsSuggest = false
    return
  }

  draftPlan.PrivacyStatus = value ? PLAN_PRIVACY.SUGGEST : PLAN_PRIVACY.PUBLIC
  draftPlan.IsSuggest = resolvePrivacySuggest(draftPlan.PrivacyStatus)
}

const removeCollaborator = (collaboratorId) => {
  draftPlan.Collaborators = draftPlan.Collaborators.filter(
    (collaborator) => collaborator.CollaboratorID !== collaboratorId,
  )
}

const submitSave = () => {
  if (!draftPlan.TripName.trim()) {
    toast('請輸入旅行名稱', 'error')
    return
  }
  if (!draftPlan.StartDate || !draftPlan.EndDate) {
    toast('缺少行程日期，請重新整理後再試', 'error')
    return
  }
  if (draftPlan.EndDate < draftPlan.StartDate) {
    toast('結束日期不可早於開始日期', 'error')
    return
  }

  emit('save', {
    draftPlan: {
      ...draftPlan,
      Collaborators: [...draftPlan.Collaborators],
    },
    photoFile: selectedPhotoFile.value,
  })
}
</script>

<template>
  <PlanDialogShell
    v-if="props.isOpen"
    title-id="plan-settings-title"
    title="行程設定"
    kicker="Trip Settings"
    @close="emit('close')"
  >
    <div class="form-floating">
      <input
        id="trip-name-input"
        v-model.trim="draftPlan.TripName"
        class="form-control custom-border"
        type="text"
        placeholder="旅行名稱"
        :disabled="props.isSaving"
      />
      <label for="trip-name-input">旅行名稱</label>
    </div>

    <div class="form-floating plan-create-date-range">
      <fieldset :disabled="props.isSaving">
        <LuiuDatePickerRange
          v-model="dateRange"
          class="plan-create-date-picker"
          placeholder="請選擇開始與結束日期"
          :options="datePickerOptions"
        />
      </fieldset>
      <label class="plan-create-date-label">行程日期</label>
    </div>

    <div class="form-floating">
      <input
        id="trip-tag-input"
        v-model.trim="draftPlan.TripTag"
        class="form-control custom-border"
        type="text"
        maxlength="10"
        placeholder="行程標籤"
        :disabled="props.isSaving"
      />
      <label for="trip-tag-input">行程標籤</label>
    </div>

    <div class="form-floating">
      <textarea
        id="trip-desc-input"
        v-model.trim="draftPlan.TripDesc"
        class="form-control custom-border plan-settings-textarea"
        placeholder="旅行簡介"
        :disabled="props.isSaving"
      ></textarea>
      <label for="trip-desc-input">旅行簡介</label>
    </div>

    <div class="d-flex">
      <div class="plan-settings-readonly flex-fill me-3">
        <div>
          <span>建立時間</span>
          <br />
          <strong>{{ formatCreateTime(draftPlan.CreateAt) }}</strong>
        </div>
      </div>

      <div class="plan-settings-readonly flex-fill">
        <div>
          <span>更新時間</span>
          <br />
          <strong>{{ formatUpdateTime(draftPlan.UpdateAt) }}</strong>
        </div>
      </div>
    </div>

    <PlanPrivacySuggestControls
      :privacy-status="draftPlan.PrivacyStatus"
      :is-suggest="draftPlan.IsSuggest"
      :disabled="props.isSaving"
      @update:privacy-status="updatePrivacyStatus"
      @update:is-suggest="updateIsSuggest"
    />

    <!-- <section class="plan-settings-section">
      <h3>共同編輯者</h3>
      <div v-if="draftPlan.Collaborators.length" class="collaborator-list">
        <div
          v-for="collaborator in draftPlan.Collaborators"
          :key="collaborator.CollaboratorID"
          class="collaborator-item"
        >
          <button
            class="collaborator-profile"
            type="button"
            :aria-label="`${collaborator.Name || '共同編輯者'} profile`"
            @click="emit('owner-click')"
          >
            <LuiuAvator
              :avatar="
                collaborator.IconURL || collaborator.Avatar
                  ? props.imageResolver(collaborator.IconURL || collaborator.Avatar)
                  : undefined
              "
              :username="collaborator.Name"
              size="sm"
            />
          </button>
          <button
            class="collaborator-remove"
            type="button"
            :aria-label="`移除 ${collaborator.Name || '共同編輯者'}`"
            @click="removeCollaborator(collaborator.CollaboratorID)"
          >
            <i class="bi bi-trash"></i>
          </button>
        </div>
      </div>
      <p v-else class="plan-settings-empty">目前沒有共同編輯者</p>
    </section> -->

    <section class="plan-settings-section">
      <h3>分享行程資訊連結</h3>
      <button type="button" class="plan-share-button" @click="emit('copy-share-link', draftPlan)">
        <i class="bi bi-link-45deg"></i>
        點擊複製連結
      </button>
      <p v-if="props.copyMessage" class="plan-copy-message">{{ props.copyMessage }}</p>
    </section>

    <PlanCoverPhotoField
      :initial-url="draftPlan.PhotoURL"
      :placeholder-url="props.placeholderCover"
      :image-resolver="props.imageResolver"
      :disabled="props.isSaving"
      input-id="trip-photo-input"
      scale-id="trip-photo-scale"
      helper-text="未選擇新圖片時會保留目前封面。"
      @update:photo-file="selectedPhotoFile = $event"
    />

    <template #footer>
      <button
        type="button"
        class="luiu-btn-danger plan-delete-button"
        :disabled="props.isDeleting || props.isSaving"
        @click="emit('delete', draftPlan)"
      >
        <i class="bi bi-trash"></i>
        {{ props.isDeleting ? '刪除中...' : '刪除行程' }}
      </button>
      <div class="plan-settings-actions">
        <button
          type="button"
          class="luiu-btn-outline-primary"
          :disabled="props.isSaving"
          @click="emit('close')"
        >
          取消
        </button>
        <button
          type="button"
          class="luiu-btn-primary"
          :disabled="props.isSaving"
          @click="submitSave"
        >
          {{ props.isSaving ? '儲存中...' : '儲存' }}
        </button>
      </div>
    </template>
  </PlanDialogShell>
</template>

<style scoped lang="scss">
@import '@/assets/scss/pages/plan-list';
</style>
