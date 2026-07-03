<script setup>
import { computed, ref, watch } from 'vue'
import Swal from 'sweetalert2'
import MemoryStep1BasicInfo from '@/components/post/MemoryStep1BasicInfo.vue'
import MemoryStep2Timeline from '@/components/post/MemoryStep2Timeline.vue'
import { createMemory } from '@/api/memory'
import { buildPlanMemoryForm, createMemoryEventId } from '@/utils/planMemoryForm'

const props = defineProps({
  isOpen: {
    type: Boolean,
    default: false,
  },
  plan: {
    type: Object,
    default: null,
  },
  itineraryItems: {
    type: Array,
    default: () => [],
  },
  tripId: {
    type: [String, Number],
    default: null,
  },
  userId: {
    type: [String, Number],
    default: '',
  },
})

const emit = defineEmits(['close', 'success'])

const step = ref(1)
const form = ref(buildPlanMemoryForm())

const totalDaysCount = computed(() => {
  if (!form.value.dateRange) return 0

  const parts = Array.isArray(form.value.dateRange)
    ? form.value.dateRange
    : String(form.value.dateRange).split(' ~ ')
  if (parts.length < 2) return 0

  const startDate = new Date(parts[0])
  const endDate = new Date(parts[1])
  if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime())) return 0

  const diffTime = Math.abs(endDate - startDate)
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1

  return diffDays > 0 ? diffDays : 0
})

const resetForm = () => {
  form.value = buildPlanMemoryForm({
    plan: props.plan || {},
    itineraryItems: props.itineraryItems,
    tripId: props.tripId,
  })
  step.value = 1
}

const closeDialog = () => {
  emit('close')
}

const handleClose = async () => {
  if (step.value === 2) {
    step.value = 1
    return
  }

  const result = await Swal.fire({
    title: '確定要關閉嗎？',
    text: '目前已帶入行程資料，關閉後本次編輯內容將不會保留。',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: '關閉',
    cancelButtonText: '繼續編輯',
    reverseButtons: true,
    customClass: {
      popup: 'rounded-4',
    },
  })

  if (result.isConfirmed) {
    closeDialog()
  }
}

const goToStep2 = () => {
  if (!form.value.title || !form.value.dateRange || !form.value.coverImage) {
    Swal.fire({
      icon: 'warning',
      title: '資料不完整',
      text: '請填寫必填欄位 (標題、日期、封面圖片)',
      customClass: { popup: 'rounded-4' },
    })
    return
  }

  if (!form.value.dailyEvents || Object.keys(form.value.dailyEvents).length === 0) {
    form.value.dailyEvents = {
      1: [
        {
          id: createMemoryEventId(),
          time: '09:00',
          title: '',
          location: '',
          duration: '1小時',
          description: '',
          expense: 0,
          rating: 5,
          imageUrls: [],
          videoUrl: '',
        },
      ],
    }
  }

  step.value = 2
}

const normalizeMemoryRating = (value) => {
  const numericValue = Number(value)
  if (!Number.isFinite(numericValue)) return 0

  return Math.min(5, Math.max(0, Math.round(numericValue)))
}

const buildPayloadDays = () => {
  const days = []

  for (let dayNumber = 1; dayNumber <= totalDaysCount.value; dayNumber += 1) {
    const stopsForDay = form.value.dailyEvents[dayNumber] || []
    const stops = stopsForDay.map((stop) => ({
      title: stop.title || '未命名景點',
      time: stop.time ? `${stop.time}:00` : null,
      location: stop.location,
      duration: stop.duration,
      description: stop.description,
      expense: Number(stop.expense) || 0,
      rating: normalizeMemoryRating(stop.rating),
      videoUrl: stop.videoUrl,
      imageUrls: stop.imageUrls || [],
    }))

    days.push({
      dayNumber,
      stops,
    })
  }

  return days
}

const handleSubmit = async () => {
  try {
    const [startDate, endDate] = Array.isArray(form.value.dateRange)
      ? form.value.dateRange
      : String(form.value.dateRange || '').split(' ~ ')
    const payload = {
      title: form.value.title,
      coverImage: form.value.coverImage,
      startDate: startDate || null,
      endDate: endDate || null,
      sourceTripId: form.value.sourceTripId || null,
      days: buildPayloadDays(),
    }

    Swal.fire({
      title: '發佈中...',
      text: '正在將您的回憶寫入系統',
      allowOutsideClick: false,
      didOpen: () => {
        Swal.showLoading()
      },
      customClass: { popup: 'rounded-4' },
    })

    const response = await createMemory(payload)

    if (response && (response.success || response.Success)) {
      Swal.fire({
        icon: 'success',
        title: '發佈成功！',
        text: '您的旅遊回憶已經成功送出',
        showConfirmButton: false,
        timer: 1500,
        customClass: { popup: 'rounded-4' },
      })
      emit('success')
      closeDialog()
    } else {
      Swal.fire({
        icon: 'error',
        title: '建立失敗',
        text: response?.message || response?.Message || '發生未知錯誤',
        customClass: { popup: 'rounded-4' },
      })
    }
  } catch (error) {
    console.error('Plan 建立旅遊回憶失敗:', error)
    Swal.fire({
      icon: 'error',
      title: '系統錯誤',
      text: '無法連線到伺服器或發生未預期的錯誤！',
      customClass: { popup: 'rounded-4' },
    })
  }
}

watch(
  () => props.isOpen,
  (isOpen) => {
    if (isOpen) resetForm()
  },
)
</script>

<template>
  <div
    v-if="isOpen"
    class="plan-create-memory-backdrop d-flex align-items-center justify-content-center"
  >
    <div
      class="plan-create-memory-dialog rounded-4 shadow overflow-hidden bg-white d-flex flex-column"
      @click.stop
    >
      <div class="d-flex justify-content-between align-items-center border-bottom px-3 py-2 bg-white sticky-top z-3">
        <button class="btn btn-link text-dark text-decoration-none p-0" type="button" @click="handleClose">
          <i v-if="step === 1" class="ri-close-line fs-3"></i>
          <i v-if="step === 2" class="ri-arrow-left-line fs-3"></i>
        </button>
        <h6 class="fw-bold mb-0 m-auto">建立旅遊回憶</h6>
        <button
          v-if="step === 1"
          class="btn btn-link fw-bold text-primary text-decoration-none p-0"
          type="button"
          @click="goToStep2"
        >
          下一步
        </button>
        <button
          v-if="step === 2"
          class="btn btn-link fw-bold text-primary text-decoration-none p-0"
          type="button"
          @click="handleSubmit"
        >
          發佈
        </button>
      </div>

      <div class="plan-create-memory-body d-flex flex-column">
        <MemoryStep1BasicInfo
          v-if="step === 1"
          v-model="form"
          :total-days-count="totalDaysCount"
        />

        <MemoryStep2Timeline
          v-if="step === 2"
          v-model="form"
          :total-days-count="totalDaysCount"
          :user-id="String(userId || '')"
        />
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
@import '@/assets/scss/pages/plan-create-memory';
</style>
