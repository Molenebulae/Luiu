<script setup>
import { computed } from 'vue'
import LuiuCheck from '@/components/base/LuiuCheck.vue'
import {
  PLAN_PRIVACY,
  isPublicPrivacyStatus,
  isSuggestPrivacyStatus,
} from '@/utils/planPrivacy'

const props = defineProps({
  privacyStatus: {
    type: [Number, String, Boolean],
    default: 0,
  },
  isSuggest: {
    type: Boolean,
    default: false,
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  variant: {
    type: String,
    default: 'bootstrap',
  },
})

const emit = defineEmits(['update:privacy-status', 'update:is-suggest'])

const isPublicTrip = computed(() => isPublicPrivacyStatus(props.privacyStatus))
const isSuggestTrip = computed(() => isSuggestPrivacyStatus(props.privacyStatus))

const setPrivacyStatus = (checked) => {
  emit('update:privacy-status', checked ? PLAN_PRIVACY.PUBLIC : PLAN_PRIVACY.PRIVATE)
  if (!checked) emit('update:is-suggest', false)
}

const setSuggestStatus = (checked) => {
  if (!isPublicTrip.value) {
    emit('update:is-suggest', false)
    return
  }

  emit('update:privacy-status', checked ? PLAN_PRIVACY.SUGGEST : PLAN_PRIVACY.PUBLIC)
  emit('update:is-suggest', checked)
}

const handlePublicChange = (event) => {
  setPrivacyStatus(event.target.checked)
}

const handleSuggestChange = (event) => {
  setSuggestStatus(event.target.checked)
}
</script>

<template>
  <div class="plan-settings-switches">
    <template v-if="props.variant === 'check'">
      <fieldset class="plan-switch-fieldset" :disabled="props.disabled">
        <LuiuCheck
          class="plan-switch-control"
          :class="{ 'is-active': isPublicTrip }"
          @change="handlePublicChange"
        >
          {{ isPublicTrip ? '公開行程' : '私密行程' }}
        </LuiuCheck>
      </fieldset>
      <fieldset
        v-if="isPublicTrip"
        class="plan-switch-fieldset"
        :disabled="props.disabled || !isPublicTrip"
      >
        <LuiuCheck
          class="plan-switch-control"
          :class="{ 'is-active': isSuggestTrip }"
          @change="handleSuggestChange"
        >
          願意被推薦到首頁
        </LuiuCheck>
      </fieldset>
    </template>

    <template v-else>
      <label class="plan-switch-control" :class="{ 'is-active': isPublicTrip }">
        <span class="form-check form-switch mb-0">
          <input
            class="form-check-input me-2"
            type="checkbox"
            :checked="isPublicTrip"
            :disabled="props.disabled"
            @change="handlePublicChange"
          />
          <span class="form-check-label">
            {{ isPublicTrip ? '公開行程' : '私密行程' }}
          </span>
        </span>
      </label>
      <label
        v-if="isPublicTrip"
        class="plan-switch-control"
        :class="{ 'is-active': isSuggestTrip }"
      >
        <span class="form-check form-switch mb-0">
          <input
            class="form-check-input me-2"
            type="checkbox"
            :checked="isSuggestTrip"
            :disabled="props.disabled || !isPublicTrip"
            @change="handleSuggestChange"
          />
          <span class="form-check-label">願意被推薦到首頁</span>
        </span>
      </label>
    </template>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/pages/plan-list';
</style>
