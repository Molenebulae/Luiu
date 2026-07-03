import request from '@/api/index'

export const getPlanRoute = (userId, tripId, payload) =>
  request({
    url: `/${encodeURIComponent(userId)}/plan-list/${encodeURIComponent(tripId)}/routes`,
    method: 'post',
    data: {
      dayNumber: payload.dayNumber,
      travelMode: payload.travelMode,
      stops: payload.stops,
    },
  })
