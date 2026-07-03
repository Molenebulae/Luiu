import { ref } from 'vue'
import { addFavoriteApi, getFavoritesApi, removeFavoriteApi } from '@/api/favorite'

export const toFavoriteTargetId = (value) => {
  const number = Number(value)
  return Number.isFinite(number) && number > 0 ? number : null
}

const unwrapFavoritePayload = (payload) => payload?.data ?? payload?.Data ?? payload

const asFavoriteArray = (payload) => {
  const data = unwrapFavoritePayload(payload)

  if (Array.isArray(data)) return data
  if (Array.isArray(data?.items)) return data.items
  if (Array.isArray(data?.Items)) return data.Items

  return []
}

export const useFavoriteTargets = (type) => {
  const favoriteTargetIds = ref([])
  const isLoadingFavorites = ref(false)
  const isSavingFavorite = ref(false)

  const isFavoriteTarget = (targetId) => {
    const normalizedTargetId = toFavoriteTargetId(targetId)
    return normalizedTargetId !== null && favoriteTargetIds.value.includes(normalizedTargetId)
  }

  const loadFavoriteTargets = async () => {
    isLoadingFavorites.value = true

    try {
      const payload = await getFavoritesApi()
      favoriteTargetIds.value = asFavoriteArray(payload)
        .filter((item) => item?.type === type)
        .map((item) => toFavoriteTargetId(item.targetId))
        .filter((targetId) => targetId !== null)
    } catch {
      favoriteTargetIds.value = []
    } finally {
      isLoadingFavorites.value = false
    }
  }

  const addFavoriteTarget = async (targetId) => {
    const normalizedTargetId = toFavoriteTargetId(targetId)
    if (normalizedTargetId === null) throw new Error('收藏目標編號格式錯誤')

    await addFavoriteApi({ targetId: normalizedTargetId, type, ownerUserId: null })
    if (!favoriteTargetIds.value.includes(normalizedTargetId)) {
      favoriteTargetIds.value = [...favoriteTargetIds.value, normalizedTargetId]
    }

    return true
  }

  const removeFavoriteTarget = async (targetId) => {
    const normalizedTargetId = toFavoriteTargetId(targetId)
    if (normalizedTargetId === null) throw new Error('收藏目標編號格式錯誤')

    await removeFavoriteApi(normalizedTargetId, type)
    favoriteTargetIds.value = favoriteTargetIds.value.filter((id) => id !== normalizedTargetId)

    return false
  }

  const toggleFavoriteTarget = async (targetId) => {
    const normalizedTargetId = toFavoriteTargetId(targetId)
    if (normalizedTargetId === null) throw new Error('收藏目標編號格式錯誤')

    isSavingFavorite.value = true

    try {
      return isFavoriteTarget(normalizedTargetId)
        ? await removeFavoriteTarget(normalizedTargetId)
        : await addFavoriteTarget(normalizedTargetId)
    } finally {
      isSavingFavorite.value = false
    }
  }

  return {
    favoriteTargetIds,
    isLoadingFavorites,
    isSavingFavorite,
    isFavoriteTarget,
    loadFavoriteTargets,
    addFavoriteTarget,
    removeFavoriteTarget,
    toggleFavoriteTarget,
  }
}
