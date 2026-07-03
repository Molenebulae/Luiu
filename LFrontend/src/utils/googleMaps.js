import { importLibrary, setOptions } from '@googlemaps/js-api-loader'

setOptions({
  key: import.meta.env.VITE_GOOGLE_MAPS_API_KEY,
  v: 'weekly',
  language: 'zh-TW',
  region: 'TW',
})

export const googleMaps = {
  importLibrary,
  loadMaps: () => importLibrary('maps'),
  loadPlaces: () => importLibrary('places'),
  loadMarker: () => importLibrary('marker'),
  loadGeometry: () => importLibrary('geometry'),
}

export default googleMaps
