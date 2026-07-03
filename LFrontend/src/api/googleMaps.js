import api from '@/api/index'

export async function getGoogleMapPlace(placeId) {
  const encodedPlaceId = encodeURIComponent(placeId)
  const response = await api.get(`/google-maps/places/${encodedPlaceId}`)
  return response.data ?? response.Data ?? response
}

export async function searchPlaces(query) {
  const response = await api.get('/google-maps/places/search', {
    params: { query },
  })
  return response.data ?? response.Data ?? response
}

export async function autocompletePlaces(input, sessionToken) {
  const response = await api.get('/google-maps/places/autocomplete', {
    params: { input, sessionToken },
  })
  return response.data ?? response.Data ?? response
}

export async function getPlaceByGoogleMapId(googleMapId) {
  const encodedGoogleMapId = encodeURIComponent(googleMapId)
  const response = await api.get(`/places/${encodedGoogleMapId}`)
  return response.data ?? response.Data ?? response
}
