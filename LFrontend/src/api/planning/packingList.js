import request from '@/api/index'

const normalizeApiResponse = (data) => {
  let parsedData = data

  if (typeof data === 'string') {
    try {
      parsedData = data ? JSON.parse(data) : data
    } catch {
      return data
    }
  }

  if (parsedData?.Success !== undefined && parsedData.success === undefined) {
    return {
      ...parsedData,
      success: parsedData.Success,
      message: parsedData.Message,
      data: parsedData.Data,
    }
  }

  return parsedData
}

const packingListPath = (userId, tripId) =>
  `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}/packing-list`

const packingListSummariesPath = (userId) => `/${encodeURIComponent(userId)}/packing-list`

const categoryPath = (userId, tripId, categoryId = '') => {
  const basePath = `${packingListPath(userId, tripId)}/categories`
  return categoryId ? `${basePath}/${encodeURIComponent(categoryId)}` : basePath
}

const itemPath = (userId, tripId, itemId) =>
  `${packingListPath(userId, tripId)}/items/${encodeURIComponent(itemId)}`

export const getPackingList = (userId, tripId) =>
  request({
    url: packingListPath(userId, tripId),
    method: 'get',
    transformResponse: [normalizeApiResponse],
  })

export const getPackingListSummaries = (userId) =>
  request({
    url: packingListSummariesPath(userId),
    method: 'get',
    transformResponse: [normalizeApiResponse],
  })

export const createPackingList = (userId, tripId, ListName) =>
  request({
    url: packingListPath(userId, tripId),
    method: 'post',
    data: { ListName },
    transformResponse: [normalizeApiResponse],
  })

export const updatePackingList = (userId, tripId, ListName) =>
  request({
    url: packingListPath(userId, tripId),
    method: 'put',
    data: { ListName },
    transformResponse: [normalizeApiResponse],
  })

export const deletePackingList = (userId, tripId) =>
  request({
    url: packingListPath(userId, tripId),
    method: 'delete',
    transformResponse: [normalizeApiResponse],
  })

export const createPackingCategory = (userId, tripId, CategoryName) =>
  request({
    url: categoryPath(userId, tripId),
    method: 'post',
    data: { CategoryName },
    transformResponse: [normalizeApiResponse],
  })

export const updatePackingCategory = (userId, tripId, categoryId, CategoryName) =>
  request({
    url: categoryPath(userId, tripId, categoryId),
    method: 'put',
    data: { CategoryName },
    transformResponse: [normalizeApiResponse],
  })

export const deletePackingCategory = (userId, tripId, categoryId) =>
  request({
    url: categoryPath(userId, tripId, categoryId),
    method: 'delete',
    transformResponse: [normalizeApiResponse],
  })

export const createPackingItem = (userId, tripId, categoryId, ItemName, IsCheck = false) =>
  request({
    url: `${categoryPath(userId, tripId, categoryId)}/items`,
    method: 'post',
    data: {
      ItemName,
      IsCheck,
    },
    transformResponse: [normalizeApiResponse],
  })

export const updatePackingItem = (userId, tripId, itemId, payload) =>
  request({
    url: itemPath(userId, tripId, itemId),
    method: 'put',
    data: payload,
    transformResponse: [normalizeApiResponse],
  })

export const deletePackingItem = (userId, tripId, itemId) =>
  request({
    url: itemPath(userId, tripId, itemId),
    method: 'delete',
    transformResponse: [normalizeApiResponse],
  })
