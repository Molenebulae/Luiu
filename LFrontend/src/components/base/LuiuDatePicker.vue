<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import AirDatepicker from 'air-datepicker'
import 'air-datepicker/air-datepicker.css'
import { localeZh } from '@/utils/airDatepicker.js'

const props = defineProps({
  modelValue: { type: String, default: '' },
  title: { type: String, default: '' },
  placeholder: { type: String, default: '請選擇日期' },
  options: { type: Object, default: () => ({}) },
})

const emit = defineEmits(['update:modelValue'])
const dateInput = ref(null)
let dp = null

onMounted(() => {
  if (!dateInput.value) return

  dp = new AirDatepicker(dateInput.value, {
    locale: localeZh,
    autoClose: true,
    ...props.options,
    onSelect({ date, formattedDate }) {
      emit('update:modelValue', formattedDate)
    },
  })

  if (props.modelValue) {
    const dates = Array.isArray(props.modelValue) ? props.modelValue : [props.modelValue]
    dp.selectDate(dates)
  }
})

watch(
  () => props.modelValue,
  (newVal) => {
    if (!dp) return
    if (!newVal || (Array.isArray(newVal) && newVal.length === 0)) {
      dp.clear()
    } else {
      dp.selectDate([newVal], { updateTime: true })
    }
  },
)

onUnmounted(() => {
  if (dp) dp.destroy()
})
</script>

<template>
  <div class="datepicker-container">
    <input
      ref="dateInput"
      type="text"
      class="form-control"
      :placeholder="placeholder"
      autocomplete="off"
    />
  </div>
</template>

<style scoped>
.datepicker-container :deep(.air-datepicker) {
  --adp-accent-color: var(--bs-primary);
  --adp-border-radius: 4px;
  --adp-font-size: 14px;
  z-index: 1050;
}
</style>
