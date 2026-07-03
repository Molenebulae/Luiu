import request from '@/api/index'

const commentPath = (userId, tripId, commentId = '') => {
  const basePath = `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}/comments`
  return commentId ? `${basePath}/${encodeURIComponent(commentId)}` : basePath
}

export const getTripComments = (userId, tripId) =>
  request({
    url: commentPath(userId, tripId),
    method: 'get',
  })

export const createTripComment = (userId, tripId, payload) =>
  request({
    url: commentPath(userId, tripId),
    method: 'post',
    data: {
      Content: payload.Content,
      ParentID: payload.ParentID ?? null,
    },
  })

export const updateTripComment = (userId, tripId, commentId, payload) =>
  request({
    url: commentPath(userId, tripId, commentId),
    method: 'put',
    data: {
      Content: payload.Content,
      ParentID: payload.ParentID ?? null,
    },
  })

export const deleteTripComment = (userId, tripId, commentId) =>
  request({
    url: commentPath(userId, tripId, commentId),
    method: 'delete',
  })
