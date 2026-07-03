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

export const getPlanList = (userId) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list`,
    method: 'get',
  })

export const getPlan = (userId, tripId) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}`,
    method: 'get',
  })

export const createPlan = (userId, formData) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list`,
    method: 'post',
    data: formData,
    headers: {
      'Content-Type': 'multipart/form-data',
    },
    transformResponse: [normalizeApiResponse],
  })

export const deletePlan = (userId, tripId) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}`,
    method: 'delete',
  })

export const updatePlan = (userId, tripId, formData) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}`,
    method: 'put',
    data: formData,
    headers: {
      'Content-Type': 'multipart/form-data',
    },
    transformResponse: [normalizeApiResponse],
  })

export const updatePlanSuggest = (userId, tripId, officeOper) => {
  const payload = {
    OfficeOper: officeOper,
  }

  return request({
    url: `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}`,
    method: 'patch',
    data: payload,
    transformResponse: [normalizeApiResponse],
  })
}

export const createPlanDetail = (userId, tripId, payload) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}`,
    method: 'post',
    data: payload,
    transformResponse: [normalizeApiResponse],
  })

export const updatePlanDetail = (userId, tripId, detailId, payload) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}/${encodeURIComponent(detailId)}`,
    method: 'put',
    data: payload,
    transformResponse: [normalizeApiResponse],
  })

export const syncPlanDetails = (userId, tripId, payload) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}/details/sync`,
    method: 'put',
    data: payload,
    transformResponse: [normalizeApiResponse],
  })

export const unwrapPlanPayload = (payload) => payload?.data ?? payload?.Data ?? payload

export const asArrayPayload = (payload) => {
  const data = unwrapPlanPayload(payload)

  if (Array.isArray(data)) {
    return data
  }

  if (Array.isArray(data?.items)) {
    return data.items
  }

  if (Array.isArray(data?.Items)) {
    return data.Items
  }

  return []
}

export const getHomeRecommendPlanApi = async () => {
  return request({
    url: '/plans/recommended',
    method: 'get'
  })
}

