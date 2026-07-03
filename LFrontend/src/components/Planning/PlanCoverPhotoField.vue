<script setup>
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue'
import { PLAN_PLACEHOLDER_COVER_URL } from '@/constants/planDefaults'
import { toast } from '@/utils/sweetAlert'

const props = defineProps({
  initialUrl: {
    type: String,
    default: '',
  },
  placeholderUrl: {
    type: String,
    default: PLAN_PLACEHOLDER_COVER_URL,
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  inputId: {
    type: String,
    default: 'trip-photo-input',
  },
  scaleId: {
    type: String,
    default: 'trip-photo-scale',
  },
  inputLabel: {
    type: String,
    default: '選擇一張新封面圖片',
  },
  helperText: {
    type: String,
    default: '未選擇新圖片時會保留目前封面。',
  },
  imageResolver: {
    type: Function,
    default: (value) => value,
  },
})

const emit = defineEmits(['update:photo-file'])

const maxPhotoSize = 10 * 1024 * 1024
const allowedPhotoTypes = ['image/jpeg', 'image/png']
const cropRatio = 16 / 9
const minCropQuality = 0.72

const selectedPhotoFile = ref(null)
const selectedPhotoPreview = ref('')
const pendingPhotoFile = ref(null)
const cropSourceUrl = ref('')
const photoInputRef = ref(null)
const cropImageRef = ref(null)
const cropFrameRef = ref(null)
const cropState = reactive({
  sourceWidth: 0,
  sourceHeight: 0,
  baseWidth: 0,
  baseHeight: 0,
  scale: 1,
  offsetX: 0,
  offsetY: 0,
  isDragging: false,
  dragStartX: 0,
  dragStartY: 0,
  dragOriginX: 0,
  dragOriginY: 0,
})

const currentCoverUrl = computed(
  () =>
    selectedPhotoPreview.value ||
    (props.initialUrl ? props.imageResolver(props.initialUrl) : props.placeholderUrl),
)
const hasPendingCrop = computed(() => Boolean(cropSourceUrl.value && pendingPhotoFile.value))
const cropImageStyle = computed(() => ({
  width: `${cropState.baseWidth}px`,
  height: `${cropState.baseHeight}px`,
  transform: `translate(-50%, -50%) translate(${cropState.offsetX}px, ${cropState.offsetY}px) scale(${cropState.scale})`,
}))

watch(
  () => props.initialUrl,
  () => {
    resetPhotoState()
  },
)

const resetSelectedPhoto = () => {
  if (selectedPhotoPreview.value) URL.revokeObjectURL(selectedPhotoPreview.value)
  selectedPhotoFile.value = null
  selectedPhotoPreview.value = ''
  emit('update:photo-file', null)
}

const resetCropPhoto = () => {
  if (cropSourceUrl.value) URL.revokeObjectURL(cropSourceUrl.value)
  if (photoInputRef.value) photoInputRef.value.value = ''

  pendingPhotoFile.value = null
  cropSourceUrl.value = ''
  Object.assign(cropState, {
    sourceWidth: 0,
    sourceHeight: 0,
    baseWidth: 0,
    baseHeight: 0,
    scale: 1,
    offsetX: 0,
    offsetY: 0,
    isDragging: false,
    dragStartX: 0,
    dragStartY: 0,
    dragOriginX: 0,
    dragOriginY: 0,
  })
}

const resetPhotoState = () => {
  resetSelectedPhoto()
  resetCropPhoto()
}

const loadImage = (url) =>
  new Promise((resolve, reject) => {
    const image = new Image()
    image.onload = () => resolve(image)
    image.onerror = () => reject(new Error('圖片讀取失敗'))
    image.src = url
  })

const clamp = (value, min, max) => Math.min(Math.max(value, min), max)

const clampCropOffset = () => {
  const frame = cropFrameRef.value
  if (!frame || !cropState.baseWidth || !cropState.baseHeight) return

  const scaledWidth = cropState.baseWidth * cropState.scale
  const scaledHeight = cropState.baseHeight * cropState.scale
  const maxOffsetX = Math.max(0, (scaledWidth - frame.clientWidth) / 2)
  const maxOffsetY = Math.max(0, (scaledHeight - frame.clientHeight) / 2)

  cropState.offsetX = clamp(cropState.offsetX, -maxOffsetX, maxOffsetX)
  cropState.offsetY = clamp(cropState.offsetY, -maxOffsetY, maxOffsetY)
}

const setCropImageSize = (image) => {
  const frame = cropFrameRef.value
  if (!frame) return

  const frameRatio = frame.clientWidth / frame.clientHeight
  const imageRatio = image.naturalWidth / image.naturalHeight

  cropState.sourceWidth = image.naturalWidth
  cropState.sourceHeight = image.naturalHeight

  if (imageRatio > frameRatio) {
    cropState.baseHeight = frame.clientHeight
    cropState.baseWidth = frame.clientHeight * imageRatio
  } else {
    cropState.baseWidth = frame.clientWidth
    cropState.baseHeight = frame.clientWidth / imageRatio
  }

  cropState.scale = 1
  cropState.offsetX = 0
  cropState.offsetY = 0
  clampCropOffset()
}

const handlePhotoChange = async (event) => {
  const [file] = event.target.files || []
  resetPhotoState()

  if (!file) return
  if (!allowedPhotoTypes.includes(file.type)) {
    toast('只支援 JPG、JPEG、PNG', 'error')
    event.target.value = ''
    return
  }

  if (file.size > maxPhotoSize) {
    toast('圖片不可超過 10MB', 'error')
    event.target.value = ''
    return
  }

  const objectUrl = URL.createObjectURL(file)

  try {
    const image = await loadImage(objectUrl)

    pendingPhotoFile.value = file
    cropSourceUrl.value = objectUrl
    await nextTick()
    setCropImageSize(image)
  } catch (error) {
    URL.revokeObjectURL(objectUrl)
    toast(error?.message || '圖片讀取失敗', 'error')
    event.target.value = ''
  }
}

const handleCropImageLoad = (event) => {
  setCropImageSize(event.target)
}

const startCropDrag = (event) => {
  if (!hasPendingCrop.value) return

  cropState.isDragging = true
  cropState.dragStartX = event.clientX
  cropState.dragStartY = event.clientY
  cropState.dragOriginX = cropState.offsetX
  cropState.dragOriginY = cropState.offsetY
  event.currentTarget.setPointerCapture(event.pointerId)
}

const moveCropDrag = (event) => {
  if (!cropState.isDragging) return

  cropState.offsetX = cropState.dragOriginX + event.clientX - cropState.dragStartX
  cropState.offsetY = cropState.dragOriginY + event.clientY - cropState.dragStartY
  clampCropOffset()
}

const endCropDrag = (event) => {
  if (!cropState.isDragging) return

  cropState.isDragging = false
  if (event.currentTarget.hasPointerCapture(event.pointerId)) {
    event.currentTarget.releasePointerCapture(event.pointerId)
  }
}

const handleCropScaleChange = () => {
  cropState.scale = Number(cropState.scale)
  clampCropOffset()
}

const createCroppedFileName = (fileName) => {
  const baseName = fileName.replace(/\.[^/.]+$/, '') || 'trip-cover'
  return `${baseName}-cover.jpg`
}

const canvasToBlob = (canvas, quality) =>
  new Promise((resolve) => {
    canvas.toBlob((blob) => resolve(blob), 'image/jpeg', quality)
  })

const getCropSourceRect = () => {
  const frame = cropFrameRef.value
  const scaledWidth = cropState.baseWidth * cropState.scale
  const scaledHeight = cropState.baseHeight * cropState.scale
  const cropLeft = scaledWidth / 2 - frame.clientWidth / 2 - cropState.offsetX
  const cropTop = scaledHeight / 2 - frame.clientHeight / 2 - cropState.offsetY
  const sourceWidth = (frame.clientWidth / scaledWidth) * cropState.sourceWidth
  const sourceHeight = (frame.clientHeight / scaledHeight) * cropState.sourceHeight

  return {
    x: clamp(
      (cropLeft / scaledWidth) * cropState.sourceWidth,
      0,
      cropState.sourceWidth - sourceWidth,
    ),
    y: clamp(
      (cropTop / scaledHeight) * cropState.sourceHeight,
      0,
      cropState.sourceHeight - sourceHeight,
    ),
    width: sourceWidth,
    height: sourceHeight,
  }
}

const applyPhotoCrop = async () => {
  if (!pendingPhotoFile.value || !cropSourceUrl.value) return

  const sourceImage = cropImageRef.value
  if (!sourceImage || !sourceImage.complete) {
    toast('圖片讀取失敗', 'error')
    return
  }

  clampCropOffset()

  const sourceRect = getCropSourceRect()
  const canvas = document.createElement('canvas')
  const outputWidth = Math.max(1, Math.min(1600, Math.round(sourceRect.width)))
  const outputHeight = Math.max(1, Math.round(outputWidth / cropRatio))
  const context = canvas.getContext('2d')

  if (!context) {
    toast('圖片裁切失敗，請稍後再試', 'error')
    return
  }

  canvas.width = outputWidth
  canvas.height = outputHeight
  context.drawImage(
    sourceImage,
    sourceRect.x,
    sourceRect.y,
    sourceRect.width,
    sourceRect.height,
    0,
    0,
    outputWidth,
    outputHeight,
  )

  let quality = 0.92
  let croppedBlob = await canvasToBlob(canvas, quality)

  while (croppedBlob && croppedBlob.size > maxPhotoSize && quality > minCropQuality) {
    quality = Math.max(minCropQuality, quality - 0.08)
    croppedBlob = await canvasToBlob(canvas, quality)
  }

  if (!croppedBlob || croppedBlob.size > maxPhotoSize) {
    toast('裁切後圖片仍超過 2MB，請重新選擇或縮小圖片', 'error')
    return
  }

  resetSelectedPhoto()
  selectedPhotoFile.value = new File(
    [croppedBlob],
    createCroppedFileName(pendingPhotoFile.value.name),
    {
      type: 'image/jpeg',
    },
  )
  selectedPhotoPreview.value = URL.createObjectURL(croppedBlob)
  emit('update:photo-file', selectedPhotoFile.value)
  resetCropPhoto()
}

const handlePreviewError = (event) => {
  if (event.target.src !== props.placeholderUrl) event.target.src = props.placeholderUrl
}

onBeforeUnmount(() => {
  resetPhotoState()
})
</script>

<template>
  <section class="plan-settings-section">
    <h3>行程封面圖片</h3>
    <div class="plan-cover-upload">
      <img :src="currentCoverUrl" alt="行程封面預覽" @error="handlePreviewError" />
      <div class="plan-cover-upload-control">
        <label class="form-label" :for="props.inputId">{{ props.inputLabel }}</label>
        <input
          :id="props.inputId"
          ref="photoInputRef"
          class="form-control custom-border"
          type="file"
          accept="image/jpeg,image/png"
          :disabled="props.disabled"
          @change="handlePhotoChange"
        />
        <p class="plan-settings-empty mb-0">{{ props.helperText }}</p>
      </div>
      <div v-if="hasPendingCrop" class="plan-cover-crop">
        <div
          ref="cropFrameRef"
          class="plan-cover-crop-frame"
          :class="{ 'is-dragging': cropState.isDragging }"
          role="application"
          aria-label="調整行程封面裁切位置"
          @pointerdown="startCropDrag"
          @pointermove="moveCropDrag"
          @pointerup="endCropDrag"
          @pointercancel="endCropDrag"
        >
          <img
            ref="cropImageRef"
            :src="cropSourceUrl"
            alt="待裁切封面"
            draggable="false"
            :style="cropImageStyle"
            @load="handleCropImageLoad"
          />
        </div>
        <label class="form-label" :for="props.scaleId">縮放圖片</label>
        <input
          :id="props.scaleId"
          v-model="cropState.scale"
          class="form-range"
          type="range"
          min="1"
          max="3"
          step="0.01"
          :disabled="props.disabled"
          @input="handleCropScaleChange"
        />
        <div class="plan-cover-crop-actions">
          <button
            type="button"
            class="luiu-btn-outline-primary"
            :disabled="props.disabled"
            @click="resetCropPhoto"
          >
            重新選擇
          </button>
          <button
            type="button"
            class="luiu-btn-primary"
            :disabled="props.disabled"
            @click="applyPhotoCrop"
          >
            套用裁切
          </button>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped lang="scss">
@import '@/assets/scss/pages/plan-list';
</style>
