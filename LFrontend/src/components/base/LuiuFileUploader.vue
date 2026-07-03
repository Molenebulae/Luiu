<script setup>
import { onMounted, onUnmounted, ref, defineEmits } from 'vue'
// import { Dropzone } from 'dropzone';
import { useDropzene } from '@/utils/dropzone'
import { uploadFileApi } from '@/api/file'
import 'dropzone/dist/dropzone.css'

const props = defineProps({
  title: { type: String, default: '旅遊照片確認' },
  uploadType: { type: String, default: 'memory' }, // 後端要的檔案類型 policy
  maxFiles: { type: Number, default: 5 },
})

const emit = defineEmits(['update-pending', 'upload-success'])

const { initDropzene } = useDropzene()
const dropzoneElement = ref(null)
const isUploading = ref(false) // 控制按鈕讀取狀態
let myDropzone = null

onMounted(() => {
  myDropzone = initDropzene(
    dropzoneElement.value,
    {
      type: 'image',
      options: {
        url: '/Files', // 虛擬路徑以滿足 Dropzone 初始化需求，實際傳輸使用 uploadFileApi
        maxFiles: props.maxFiles,
        params: {
          type: props.uploadType,
        },
      },
    },
    () => {
      // 這個是之前的 refreshPending 邏輯
      emit('update-pending', myDropzone.getQueuedFiles().length)
    },
  )

  // 當「單張」上傳成功
  myDropzone.on('success', (file, response) => {
    emit('upload-success', response) // 把後端回傳的資料傳出去
    myDropzone.removeFile(file) // 上傳成功後移除預覽圖，保持畫面乾淨
  })

  // 當上傳失敗時，跳出伺服器的錯誤訊息
  myDropzone.on('error', (file, errorMessage) => {
    let msg = errorMessage
    if (typeof errorMessage === 'object' && errorMessage.message) {
      msg = errorMessage.message // 抓取後端的 JSON message
    } else if (typeof errorMessage === 'object' && errorMessage.errors) {
      msg = JSON.stringify(errorMessage.errors) // 抓取 ASP.NET Core 的 binding 錯誤
    }
    alert('上傳失敗: ' + msg)
    console.error('Upload Error:', errorMessage)
  })

  // 當「所有隊列」處理完畢
  myDropzone.on('queuecomplete', () => {
    isUploading.value = false
    // refreshPending();
    emit('update-pending', myDropzone.getQueuedFiles().length)
  })
  // }
})

// 按鈕點擊事件：啟動上傳
const handleConfirmUpload = async () => {
  if (!myDropzone) {
    console.log('myDropzone 尚未初始化')
    return
  }

  const totalFiles = myDropzone.files.length // 獲取目前所有檔案總數
  const rejectedFiles = myDropzone.getRejectedFiles().length // 格式不符或超出的檔案
  console.log(totalFiles, rejectedFiles)

  // 1. 檢查是否根本沒選檔案
  if (totalFiles <= 0) {
    alert('請先選擇照片！')
    return
  }

  // 2. 檢查總數是否超過上限 (關鍵修改)
  if (totalFiles > props.maxFiles) {
    alert(
      `目前選擇了 ${totalFiles} 張照片，已超過上限 ${props.maxFiles} 張。請移除標示紅色的檔案後再試。`,
    )
    return
  }

  // 3. 檢查是否有其他錯誤（例如格式不對，但數量沒超標）
  if (rejectedFiles > 0) {
    alert('部分檔案不符合條件（格式錯誤），請移除後再試。')
    return
  }

  // 4. 通過驗證，開始上傳
  isUploading.value = true

  const queuedFiles = myDropzone.getQueuedFiles()

  for (const file of queuedFiles) {
    file.status = 'uploading'
    myDropzone.emit('sending', file)

    const formData = new FormData()
    const fileName = file.upload?.filename || file.name
    formData.append('file', file, fileName)
    formData.append('type', props.uploadType)

    try {
      const response = await uploadFileApi(formData)
      
      file.status = 'success'
      myDropzone.emit('success', file, response)
      myDropzone.emit('complete', file)
    } catch (error) {
      file.status = 'error'
      
      let errMsg = '上傳失敗'
      if (typeof error === 'string') {
        errMsg = error
      } else if (error && typeof error === 'object') {
        errMsg = error.message || error.errors || JSON.stringify(error)
      }
      
      myDropzone.emit('error', file, errMsg)
      myDropzone.emit('complete', file)
    }
  }

  isUploading.value = false
  emit('update-pending', myDropzone.getQueuedFiles().length)
}

onUnmounted(() => {
  if (myDropzone) myDropzone.destroy()
})
</script>

<template>
  <div class="card border-0 shadow-sm rounded-4 mb-4">
    <div
      class="card-header bg-transparent border-0 p-4 pb-0 d-flex justify-content-between align-items-center"
    >
      <h5 class="card-title mb-0 fw-bold">{{ title }}</h5>

      <button
        type="button"
        class="btn btn-primary btn-sm rounded-pill px-4 shadow-sm"
        :disabled="isUploading"
        @click="handleConfirmUpload"
      >
        <i v-if="isUploading" class="spinner-border spinner-border-sm me-1"></i>
        <i v-else class="ri-check-double-line me-1"></i>
        確認上傳
      </button>
    </div>

    <div class="card-body p-4">
      <div ref="dropzoneElement" class="dropzone rounded-4 border-dashed"></div>
    </div>
  </div>
</template>

<style scoped>
.dropzone {
  border: 2px dashed #dee2e6 !important;
  background-color: #f8f9fa !important;
  min-height: 150px;
  border-radius: 1rem;
  padding: 20px;
  /*  加上內距，讓圖片不要貼著邊框 */
  display: flex !important;
  /*  關鍵：啟用彈性排版 */
  flex-wrap: wrap;
  /*  關鍵：允許自動換行 */
  gap: 15px;
  /*  關鍵：圖片之間的間隔 */
}

/* 隱藏黑線與細節 */
:deep(.dz-preview .dz-image:after),
:deep(.dz-preview .dz-details),
:deep(.dz-preview .dz-progress) {
  display: none !important;
}

/* 2. 解決錯誤提示擋住按鈕：必須加上 :deep() 才能生效 */
:deep(.dz-preview .dz-error-message) {
  top: -35px !important;
  /* 向上移出圖片區域 */
  transform: translateY(0) !important;
  bottom: auto !important;
  opacity: 1 !important;
  /* 確保錯誤出現時是清楚的 */
  z-index: 1000 !important;
  /* 確保它浮在最上層 */
}

/* 調整紅色錯誤氣泡的「小箭頭」，讓它朝下指著圖片 */
:deep(.dz-preview .dz-error-message::after) {
  top: auto !important;
  bottom: -6px !important;
  border-top-color: #be2626 !important;
  border-bottom-color: transparent !important;
}

/* 稍微把 Remove file 往下移一點點，更好點擊 */
:deep(.dz-preview .dz-remove) {
  margin-top: 10px !important;
  font-weight: bold;
}
</style>
