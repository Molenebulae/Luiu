<script setup>
import { computed, reactive, ref, watch } from 'vue'
import {
  createPackingCategory,
  createPackingItem,
  createPackingList,
  deletePackingCategory,
  deletePackingItem,
  deletePackingList,
  getPackingList,
  updatePackingCategory,
  updatePackingItem,
  updatePackingList,
} from '@/api/planning/packingList'
import { unwrapPlanPayload } from '@/api/planning/plan'
import { luiuNotify, toast } from '@/utils/sweetAlert'

const props = defineProps({
  isOpen: {
    type: Boolean,
    default: false,
  },
  userId: {
    type: [String, Number],
    required: true,
  },
  tripId: {
    type: [String, Number],
    required: true,
  },
  tripTitle: {
    type: String,
    default: '',
  },
})

const emit = defineEmits(['close'])

const packingList = ref(null)
const packingLoading = ref(false)
const packingNotCreated = ref(false)
const packingSavingList = ref(false)
const packingDeletingList = ref(false)
const packingSavingCategoryId = ref(null)
const packingDeletingCategoryId = ref(null)
const packingSavingItemId = ref(null)
const packingDeletingItemId = ref(null)
const packingEditingCategoryId = ref(null)
const packingEditingItemId = ref(null)
const selectedPackingCategoryId = ref(null)
const isAddingPackingCategory = ref(false)
const isAddingPackingItem = ref(false)
const packingForm = reactive({
  listName: '',
  newCategoryName: '',
  categoryNames: {},
  newItemNames: {},
  itemNames: {},
})

const defaultPackingListName = computed(() => `${props.tripTitle || '我的'}行李清單`)
const packingCategories = computed(() =>
  (packingList.value?.Categories || []).filter((category) => !category.IsDeleted),
)
const selectedPackingCategory = computed(
  () =>
    packingCategories.value.find(
      (category) => String(category.CategoryID) === String(selectedPackingCategoryId.value),
    ) ||
    packingCategories.value[0] ||
    null,
)
const selectedPackingItems = computed(() =>
  (selectedPackingCategory.value?.Items || []).filter((item) => !item.IsDeleted),
)

watch(
  () => props.isOpen,
  (isOpen) => {
    if (isOpen) loadPackingList()
  },
)

const getPackingApiErrorMessage = (error, fallback) =>
  error?.message ||
  error?.Message ||
  error?.response?.data?.message ||
  error?.response?.data?.Message ||
  fallback
const unwrapPackingPayload = (payload) =>
  payload?.data === null || payload?.Data === null ? null : unwrapPlanPayload(payload)

const resetPackingDrafts = () => {
  packingForm.listName = ''
  packingForm.newCategoryName = ''
  packingForm.categoryNames = {}
  packingForm.newItemNames = {}
  packingForm.itemNames = {}
  packingEditingCategoryId.value = null
  packingEditingItemId.value = null
  selectedPackingCategoryId.value = null
  isAddingPackingCategory.value = false
  isAddingPackingItem.value = false
}

const syncSelectedPackingCategory = (preferredCategoryId = selectedPackingCategoryId.value) => {
  const categories = packingCategories.value
  if (!categories.length) {
    selectedPackingCategoryId.value = null
    return
  }

  const preferredCategory = categories.find(
    (category) => String(category.CategoryID) === String(preferredCategoryId),
  )
  selectedPackingCategoryId.value = preferredCategory?.CategoryID || categories[0].CategoryID
}

const syncPackingDrafts = () => {
  const currentList = packingList.value
  packingForm.listName = currentList?.ListName || defaultPackingListName.value
  packingForm.categoryNames = {}
  packingForm.newItemNames = {}
  packingForm.itemNames = {}
  ;(currentList?.Categories || [])
    .filter((category) => !category.IsDeleted)
    .forEach((category) => {
      packingForm.categoryNames[category.CategoryID] = category.CategoryName || ''
      packingForm.newItemNames[category.CategoryID] = ''
      ;(category.Items || [])
        .filter((item) => !item.IsDeleted)
        .forEach((item) => {
          packingForm.itemNames[item.ItemID] = item.ItemName || ''
        })
    })
  syncSelectedPackingCategory()
}

const loadPackingList = async ({ silent = false } = {}) => {
  packingLoading.value = true
  try {
    const payload = await getPackingList(props.userId, props.tripId)
    const currentPackingList = unwrapPackingPayload(payload)

    if (currentPackingList === null) {
      packingList.value = null
      packingNotCreated.value = true
      resetPackingDrafts()
      packingForm.listName = defaultPackingListName.value
      return
    }

    packingList.value = currentPackingList
    packingNotCreated.value = false
    syncPackingDrafts()
  } catch (error) {
    if (!silent) {
      toast(getPackingApiErrorMessage(error, '行李清單載入失敗，請稍後再試'), 'error')
    }
  } finally {
    packingLoading.value = false
  }
}

const closePackingDialog = () => {
  packingSavingCategoryId.value = null
  packingDeletingCategoryId.value = null
  packingSavingItemId.value = null
  packingDeletingItemId.value = null
  packingEditingCategoryId.value = null
  packingEditingItemId.value = null
  isAddingPackingCategory.value = false
  isAddingPackingItem.value = false
  emit('close')
}

const createCurrentPackingList = async () => {
  const listName = packingForm.listName.trim()
  if (!listName) {
    toast('請輸入行李清單名稱', 'error')
    return
  }

  packingSavingList.value = true
  try {
    await createPackingList(props.userId, props.tripId, listName)
    await loadPackingList({ silent: true })
    toast('行李清單已建立')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '建立行李清單失敗，請稍後再試'), 'error')
  } finally {
    packingSavingList.value = false
  }
}

const updateCurrentPackingList = async () => {
  const listName = packingForm.listName.trim()
  if (!listName) {
    toast('請輸入行李清單名稱', 'error')
    return
  }

  packingSavingList.value = true
  try {
    await updatePackingList(props.userId, props.tripId, listName)
    await loadPackingList({ silent: true })
    toast('行李清單已更新')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '更新行李清單失敗，請稍後再試'), 'error')
  } finally {
    packingSavingList.value = false
  }
}

const deleteCurrentPackingList = async () => {
  if (packingDeletingList.value) return

  const result = await luiuNotify.confirmDelete(
    '確定要刪除此行李清單嗎?',
    '刪除後行李清單將不會顯示在此行程中。',
  )
  if (!result.isConfirmed) return

  packingDeletingList.value = true
  try {
    await deletePackingList(props.userId, props.tripId)
    packingList.value = null
    packingNotCreated.value = true
    resetPackingDrafts()
    packingForm.listName = defaultPackingListName.value
    toast('行李清單已刪除')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '刪除行李清單失敗，請稍後再試'), 'error')
  } finally {
    packingDeletingList.value = false
  }
}

const selectPackingCategory = (categoryId) => {
  selectedPackingCategoryId.value = categoryId
  packingEditingItemId.value = null
  isAddingPackingItem.value = false
}

const createCurrentPackingCategory = async () => {
  const categoryName = packingForm.newCategoryName.trim()
  if (!categoryName) {
    toast('請輸入分類名稱', 'error')
    return
  }
  if (categoryName.length > 20) {
    toast('分類名稱不能超過 20 個字', 'error')
    return
  }

  packingSavingCategoryId.value = 'new'
  try {
    const payload = await createPackingCategory(props.userId, props.tripId, categoryName)
    const createdCategory = unwrapPlanPayload(payload)
    packingForm.newCategoryName = ''
    isAddingPackingCategory.value = false
    await loadPackingList({ silent: true })
    syncSelectedPackingCategory(
      createdCategory?.CategoryID || packingCategories.value.at(-1)?.CategoryID,
    )
    toast('分類已新增')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '新增分類失敗，請稍後再試'), 'error')
  } finally {
    packingSavingCategoryId.value = null
  }
}

const startEditPackingCategory = (category) => {
  packingEditingCategoryId.value = category.CategoryID
  packingForm.categoryNames[category.CategoryID] = category.CategoryName || ''
}

const updateCurrentPackingCategory = async (categoryId) => {
  const categoryName = String(packingForm.categoryNames[categoryId] || '').trim()
  if (!categoryName) {
    toast('請輸入分類名稱', 'error')
    return
  }
  if (categoryName.length > 20) {
    toast('分類名稱不能超過 20 個字', 'error')
    return
  }

  packingSavingCategoryId.value = categoryId
  try {
    await updatePackingCategory(props.userId, props.tripId, categoryId, categoryName)
    packingEditingCategoryId.value = null
    await loadPackingList({ silent: true })
    toast('分類已更新')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '更新分類失敗，請稍後再試'), 'error')
  } finally {
    packingSavingCategoryId.value = null
  }
}

const deleteCurrentPackingCategory = async (categoryId) => {
  if (packingDeletingCategoryId.value) return

  const result = await luiuNotify.confirmDelete(
    '確定要刪除此分類嗎?',
    '刪除後此分類與其中項目將不會顯示。',
  )
  if (!result.isConfirmed) return

  packingDeletingCategoryId.value = categoryId
  try {
    await deletePackingCategory(props.userId, props.tripId, categoryId)
    await loadPackingList({ silent: true })
    syncSelectedPackingCategory()
    toast('分類已刪除')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '刪除分類失敗，請稍後再試'), 'error')
  } finally {
    packingDeletingCategoryId.value = null
  }
}

const createCurrentPackingItem = async (categoryId) => {
  const itemName = String(packingForm.newItemNames[categoryId] || '').trim()
  if (!itemName) {
    toast('請輸入項目名稱', 'error')
    return
  }
  if (itemName.length > 20) {
    toast('項目名稱不能超過 20 個字', 'error')
    return
  }

  packingSavingItemId.value = `new-${categoryId}`
  try {
    await createPackingItem(props.userId, props.tripId, categoryId, itemName)
    packingForm.newItemNames[categoryId] = ''
    isAddingPackingItem.value = false
    await loadPackingList({ silent: true })
    toast('項目已新增')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '新增項目失敗，請稍後再試'), 'error')
  } finally {
    packingSavingItemId.value = null
  }
}

const startEditPackingItem = (item) => {
  packingEditingItemId.value = item.ItemID
  packingForm.itemNames[item.ItemID] = item.ItemName || ''
}

const updateCurrentPackingItemName = async (itemId) => {
  const itemName = String(packingForm.itemNames[itemId] || '').trim()
  if (!itemName) {
    toast('請輸入項目名稱', 'error')
    return
  }
  if (itemName.length > 20) {
    toast('項目名稱不能超過 20 個字', 'error')
    return
  }
  packingSavingItemId.value = itemId
  try {
    await updatePackingItem(props.userId, props.tripId, itemId, { ItemName: itemName })
    packingEditingItemId.value = null
    await loadPackingList({ silent: true })
    toast('項目已更新')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '更新項目失敗，請稍後再試'), 'error')
  } finally {
    packingSavingItemId.value = null
  }
}

const togglePackingItemCheck = async (item) => {
  const nextChecked = !item.IsCheck
  const previousChecked = item.IsCheck
  item.IsCheck = nextChecked
  packingSavingItemId.value = item.ItemID

  try {
    await updatePackingItem(props.userId, props.tripId, item.ItemID, { IsCheck: nextChecked })
  } catch (error) {
    item.IsCheck = previousChecked
    toast(getPackingApiErrorMessage(error, '更新項目狀態失敗，請稍後再試'), 'error')
  } finally {
    packingSavingItemId.value = null
  }
}

const deleteCurrentPackingItem = async (itemId) => {
  if (packingDeletingItemId.value) return

  const result = await luiuNotify.confirmDelete(
    '確定要刪除此項目嗎?',
    '刪除後此項目將不會顯示在行李清單中。',
  )
  if (!result.isConfirmed) return

  packingDeletingItemId.value = itemId
  try {
    await deletePackingItem(props.userId, props.tripId, itemId)
    await loadPackingList({ silent: true })
    toast('項目已刪除')
  } catch (error) {
    toast(getPackingApiErrorMessage(error, '刪除項目失敗，請稍後再試'), 'error')
  } finally {
    packingDeletingItemId.value = null
  }
}

const isCategoryCompleted = (category) => {
  const activeItems = (category.Items || []).filter((item) => !item.IsDeleted)
  if (activeItems.length === 0) return false
  return activeItems.every((item) => item.IsCheck)
}
</script>

<template>
  <div v-if="props.isOpen" class="plan-dialog-backdrop" @click.self="closePackingDialog">
    <section
      class="plan-dialog packing-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="packing-dialog-title"
      @click.stop
    >
      <header>
        <div>
          <p class="packing-dialog-kicker mb-1">Packing List</p>
          <h2 id="packing-dialog-title">行李清單</h2>
        </div>
        <button type="button" class="dialog-close" aria-label="關閉" @click="closePackingDialog">
          <i class="fa-solid fa-xmark"></i>
        </button>
      </header>

      <div v-if="packingLoading" class="packing-list-state">
        <span class="spinner-border spinner-border-sm" aria-hidden="true"></span>
        <span>行李清單載入中...</span>
      </div>

      <form
        v-else-if="packingNotCreated"
        class="packing-create"
        aria-label="建立行李清單"
        @submit.prevent="createCurrentPackingList"
      >
        <p class="packing-list-empty">這趟行程尚未建立行李清單。</p>
        <div class="form-floating">
          <input
            id="packing-create-list-name"
            v-model.trim="packingForm.listName"
            class="form-control custom-border"
            type="text"
            placeholder="例如：日本五日行李清單"
            :disabled="packingSavingList"
          />
          <label for="packing-create-list-name">清單名稱</label>
        </div>
        <footer>
          <button type="button" class="btn btn-light" @click="closePackingDialog">取消</button>
          <button type="submit" class="btn btn-primary" :disabled="packingSavingList">
            {{ packingSavingList ? '建立中...' : '建立行李清單' }}
          </button>
        </footer>
      </form>

      <div v-else-if="packingList" class="packing-list-content">
        <form class="packing-list-name-panel" @submit.prevent="updateCurrentPackingList">
          <div class="form-floating">
            <input
              id="packing-list-name"
              v-model.trim="packingForm.listName"
              class="form-control custom-border"
              type="text"
              placeholder="清單名稱"
              :disabled="packingSavingList || packingDeletingList"
            />
            <label for="packing-list-name">清單名稱</label>
          </div>
          <div class="packing-list-actions">
            <button
              type="submit"
              class="packing-icon-button"
              :disabled="packingSavingList || packingDeletingList"
              aria-label="儲存清單名稱"
            >
              <i class="fa-regular fa-pen-to-square"></i>
            </button>
            <button
              type="button"
              class="packing-icon-button"
              :disabled="packingSavingList || packingDeletingList"
              aria-label="刪除清單"
              @click="deleteCurrentPackingList"
            >
              <i class="fa-solid fa-xmark"></i>
            </button>
          </div>
        </form>

        <div class="packing-workspace">
          <section class="packing-category-panel" aria-label="行李分類">
            <div v-if="packingCategories.length" class="packing-category-grid">
              <!-- 新增分類按鈕 / 表單固定在最前 (左上角) -->
              <form
                v-if="isAddingPackingCategory"
                class="packing-category-card packing-category-card--form"
                @submit.prevent="createCurrentPackingCategory"
              >
                <div class="form-floating">
                  <input
                    id="packing-new-category-name"
                    v-model.trim="packingForm.newCategoryName"
                    class="form-control custom-border"
                    type="text"
                    placeholder="新增分類"
                    :disabled="packingSavingCategoryId === 'new'"
                  />
                  <label for="packing-new-category-name">新增分類</label>
                </div>
                <div class="packing-card-actions">
                  <button
                    type="submit"
                    class="packing-icon-button"
                    :disabled="packingSavingCategoryId === 'new'"
                    aria-label="新增分類"
                  >
                    <i class="fa-solid fa-plus"></i>
                  </button>
                  <button
                    type="button"
                    class="packing-icon-button"
                    aria-label="取消新增分類"
                    @click="isAddingPackingCategory = false"
                  >
                    <i class="fa-solid fa-xmark"></i>
                  </button>
                </div>
              </form>
              <button
                v-else
                type="button"
                class="packing-category-card packing-category-add"
                @click="isAddingPackingCategory = true"
              >
                <i class="fa-solid fa-plus"></i>
              </button>

              <!-- 現有分類列表 -->
              <article
                v-for="category in packingCategories"
                :key="category.CategoryID"
                class="packing-category-card"
                :class="{
                  'is-selected': selectedPackingCategory?.CategoryID === category.CategoryID,
                  'is-completed': isCategoryCompleted(category),
                }"
              >
                <form
                  v-if="packingEditingCategoryId === category.CategoryID"
                  class="packing-card-edit"
                  @submit.prevent="updateCurrentPackingCategory(category.CategoryID)"
                >
                  <div class="form-floating">
                    <input
                      :id="`packing-category-name-${category.CategoryID}`"
                      v-model.trim="packingForm.categoryNames[category.CategoryID]"
                      class="form-control custom-border"
                      type="text"
                      placeholder="分類名稱"
                      :disabled="packingSavingCategoryId === category.CategoryID"
                    />
                    <label :for="`packing-category-name-${category.CategoryID}`">分類名稱</label>
                  </div>
                  <div class="packing-card-actions">
                    <button
                      type="submit"
                      class="packing-icon-button"
                      :disabled="packingSavingCategoryId === category.CategoryID"
                      aria-label="儲存分類"
                    >
                      <i class="fa-regular fa-pen-to-square"></i>
                    </button>
                    <button
                      type="button"
                      class="packing-icon-button"
                      aria-label="取消編輯分類"
                      @click="packingEditingCategoryId = null"
                    >
                      <i class="fa-solid fa-xmark"></i>
                    </button>
                  </div>
                </form>
                <template v-else>
                  <button
                    type="button"
                    class="packing-category-select"
                    @click="selectPackingCategory(category.CategoryID)"
                  >
                    <span>{{ category.CategoryName }}</span>
                  </button>
                  <div class="packing-card-actions">
                    <button
                      type="button"
                      class="packing-icon-button"
                      :disabled="packingSavingCategoryId === category.CategoryID"
                      aria-label="編輯分類"
                      @click="startEditPackingCategory(category)"
                    >
                      <i class="fa-regular fa-pen-to-square"></i>
                    </button>
                    <button
                      type="button"
                      class="packing-icon-button"
                      :disabled="packingDeletingCategoryId === category.CategoryID"
                      aria-label="刪除分類"
                      @click="deleteCurrentPackingCategory(category.CategoryID)"
                    >
                      <i class="fa-solid fa-xmark"></i>
                    </button>
                  </div>
                </template>
              </article>
            </div>
            <div v-else>
              <p class="packing-list-empty mb-3">目前沒有分類，先新增一個分類吧。</p>
              <form
                v-if="isAddingPackingCategory"
                class="packing-category-card packing-category-card--form"
                @submit.prevent="createCurrentPackingCategory"
              >
                <div class="form-floating">
                  <input
                    id="packing-new-category-name"
                    v-model.trim="packingForm.newCategoryName"
                    class="form-control custom-border"
                    type="text"
                    placeholder="新增分類"
                    :disabled="packingSavingCategoryId === 'new'"
                  />
                  <label for="packing-new-category-name">新增分類</label>
                </div>
                <div class="packing-card-actions">
                  <button
                    type="submit"
                    class="packing-icon-button"
                    :disabled="packingSavingCategoryId === 'new'"
                    aria-label="新增分類"
                  >
                    <i class="fa-solid fa-plus"></i>
                  </button>
                  <button
                    type="button"
                    class="packing-icon-button"
                    aria-label="取消新增分類"
                    @click="isAddingPackingCategory = false"
                  >
                    <i class="fa-solid fa-xmark"></i>
                  </button>
                </div>
              </form>
              <button
                v-else
                type="button"
                class="packing-category-card packing-category-add"
                @click="isAddingPackingCategory = true"
              >
                <i class="fa-solid fa-plus"></i>
              </button>
            </div>
          </section>

          <section class="packing-item-panel" aria-label="行李項目">
            <template v-if="selectedPackingCategory">
              <header class="packing-item-panel-header">
                <h3>{{ selectedPackingCategory.CategoryName }}</h3>
              </header>
              <ul v-if="selectedPackingItems.length" class="packing-item-list">
                <li
                  v-for="item in selectedPackingItems"
                  :key="item.ItemID"
                  class="packing-item-card"
                >
                  <template v-if="packingEditingItemId === item.ItemID">
                    <form
                      class="packing-item-edit"
                      @submit.prevent="updateCurrentPackingItemName(item.ItemID)"
                    >
                      <div class="form-floating">
                        <input
                          :id="`packing-item-name-${item.ItemID}`"
                          v-model.trim="packingForm.itemNames[item.ItemID]"
                          class="form-control custom-border"
                          type="text"
                          placeholder="項目名稱"
                          :disabled="packingSavingItemId === item.ItemID"
                        />
                        <label :for="`packing-item-name-${item.ItemID}`">項目名稱</label>
                      </div>
                      <button
                        type="submit"
                        class="packing-icon-button"
                        :disabled="packingSavingItemId === item.ItemID"
                        aria-label="儲存項目"
                      >
                        <i class="fa-regular fa-pen-to-square"></i>
                      </button>
                      <button
                        type="button"
                        class="packing-icon-button"
                        aria-label="取消編輯項目"
                        @click="packingEditingItemId = null"
                      >
                        <i class="fa-solid fa-xmark"></i>
                      </button>
                    </form>
                  </template>
                  <template v-else>
                    <label class="form-check packing-item-check">
                      <input
                        class="form-check-input"
                        type="checkbox"
                        :checked="item.IsCheck"
                        :disabled="packingSavingItemId === item.ItemID"
                        @change="togglePackingItemCheck(item)"
                      />
                      <span class="form-check-label" :class="{ 'is-checked': item.IsCheck }">
                        {{ item.ItemName }}
                      </span>
                    </label>
                    <div class="packing-item-actions">
                      <button
                        type="button"
                        class="packing-icon-button"
                        :disabled="packingSavingItemId === item.ItemID"
                        aria-label="編輯項目"
                        @click="startEditPackingItem(item)"
                      >
                        <i class="fa-regular fa-pen-to-square"></i>
                      </button>
                      <button
                        type="button"
                        class="packing-icon-button"
                        :disabled="packingDeletingItemId === item.ItemID"
                        aria-label="刪除項目"
                        @click="deleteCurrentPackingItem(item.ItemID)"
                      >
                        <i class="fa-solid fa-xmark"></i>
                      </button>
                    </div>
                  </template>
                </li>
              </ul>
              <p v-else class="packing-list-empty">這個分類還沒有項目。</p>

              <form
                v-if="isAddingPackingItem"
                class="packing-add-item-form"
                @submit.prevent="createCurrentPackingItem(selectedPackingCategory.CategoryID)"
              >
                <div class="form-floating">
                  <input
                    :id="`packing-new-item-name-${selectedPackingCategory.CategoryID}`"
                    v-model.trim="packingForm.newItemNames[selectedPackingCategory.CategoryID]"
                    class="form-control custom-border"
                    type="text"
                    placeholder="新增項目"
                    :disabled="packingSavingItemId === `new-${selectedPackingCategory.CategoryID}`"
                  />
                  <label :for="`packing-new-item-name-${selectedPackingCategory.CategoryID}`">
                    新增項目
                  </label>
                </div>
                <button
                  type="submit"
                  class="packing-icon-button"
                  :disabled="packingSavingItemId === `new-${selectedPackingCategory.CategoryID}`"
                  aria-label="新增項目"
                >
                  <i class="fa-solid fa-plus"></i>
                </button>
                <button
                  type="button"
                  class="packing-icon-button"
                  aria-label="取消新增項目"
                  @click="isAddingPackingItem = false"
                >
                  <i class="fa-solid fa-xmark"></i>
                </button>
              </form>
              <button
                v-else
                type="button"
                class="packing-add-item-button"
                @click="isAddingPackingItem = true"
              >
                <i class="fa-solid fa-plus"></i>
              </button>
            </template>
            <p v-else class="packing-list-empty">請先新增或選取分類。</p>
          </section>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/pages/plan';
</style>
