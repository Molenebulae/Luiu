const getPrimaryMapColor = () =>
  getComputedStyle(document.documentElement).getPropertyValue('--bs-primary').trim()

const getBootstrapColor = (name) =>
  getComputedStyle(document.documentElement).getPropertyValue(`--bs-${name}`).trim()

export const usePlanMapMarkers = (options) => {
  let mapMarkers = []
  let favoriteSpotMarkers = []

  const getMap = () => options.getMap?.()
  const getMarkerConstructor = () => options.getMarkerConstructor?.()

  const createPinElement = (isSelected, label, variant = '') => {
    const el = document.createElement('div')
    el.className = [
      'map-marker',
      variant ? `map-marker--${variant}` : '',
      isSelected ? 'map-marker--selected' : '',
    ]
      .filter(Boolean)
      .join(' ')
    el.innerHTML = `<span class="map-marker-label">${label}</span>`
    return el
  }

  const createLegacyMarkerIcon = (isSelected, variant = '') => {
    const primaryColor = getPrimaryMapColor()
    const warningColor = getBootstrapColor('warning')
    const markerColor = variant === 'favorite' ? warningColor || primaryColor : primaryColor
    return {
      path: window.google.maps.SymbolPath.CIRCLE,
      scale: isSelected ? 8 : variant === 'favorite' ? 6 : 4,
      fillColor: markerColor || undefined,
      fillOpacity: 1,
      strokeColor: markerColor || undefined,
      strokeWeight: isSelected ? 3 : 2,
    }
  }

  const getMarkerLabelColor = (isSelected, variant = '') => {
    if (isSelected) return getBootstrapColor('white')
    if (variant === 'favorite') return getBootstrapColor('dark')
    return getPrimaryMapColor()
  }

  const updateMarkerStyles = () => {
    const allMarkers = [...mapMarkers, ...favoriteSpotMarkers]

    allMarkers.forEach(({ marker, item, label, variant = '' }) => {
      const isSelected = item.id === options.selectedMarkerId.value

      if (marker.content) {
        marker.content = createPinElement(isSelected, label, variant)
      } else if (marker.setIcon) {
        marker.setIcon(createLegacyMarkerIcon(isSelected, variant))
        marker.setLabel?.({
          text: label,
          color: getMarkerLabelColor(isSelected, variant),
          fontSize: isSelected ? '18px' : variant === 'favorite' ? '14px' : '16px',
          fontWeight: 'bold',
        })
      }
    })
  }

  const isAdvancedMarker = () => {
    const markerConstructor = getMarkerConstructor()
    return (
      markerConstructor?.name === 'AdvancedMarkerElement' ||
      markerConstructor?.toString?.().includes('AdvancedMarker')
    )
  }

  const createMarker = ({ item, label, variant = '', isSelected = false }) => {
    const googleMap = getMap()
    const markerConstructor = getMarkerConstructor()
    const position = options.getPlacePosition(item)
    if (!googleMap || !markerConstructor || !position) return null

    let marker
    if (isAdvancedMarker()) {
      const el = createPinElement(isSelected, label, variant)
      marker = new markerConstructor({ map: googleMap, position, title: item.title, content: el })
    } else {
      marker = new markerConstructor({
        map: googleMap,
        position,
        title: item.title,
        icon: createLegacyMarkerIcon(isSelected, variant),
        label: {
          text: label,
          color: getMarkerLabelColor(isSelected, variant),
          fontSize: isSelected ? '18px' : variant === 'favorite' ? '14px' : '16px',
          fontWeight: 'bold',
        },
      })
    }

    if (isAdvancedMarker() && marker.addEventListener) {
      marker.addEventListener('gmp-click', () => options.selectAndFocusPlace(item))
    } else {
      marker.addListener('click', () => options.selectAndFocusPlace(item))
    }

    return { marker, position, item, label, variant }
  }

  const clearMarkers = (markers) => {
    markers.forEach(({ marker }) => {
      marker.map = null
      if (marker.setMap) marker.setMap(null)
    })
  }

  const clearMapMarkers = () => {
    clearMarkers(mapMarkers)
    mapMarkers = []
  }

  const clearFavoriteSpotMarkers = () => {
    clearMarkers(favoriteSpotMarkers)
    favoriteSpotMarkers = []
  }

  const renderFavoriteSpotMarkers = () => {
    clearFavoriteSpotMarkers()
    if (!options.showFavoriteSpots.value || !getMap() || !getMarkerConstructor()) return

    const validFavoriteSpots = options.favoriteSpots.value.filter(
      (item) => options.getPlacePosition(item) !== null,
    )
    favoriteSpotMarkers = validFavoriteSpots
      .map((item) => createMarker({ item, label: '♥', variant: 'favorite' }))
      .filter(Boolean)

    if (!validFavoriteSpots.length && options.favoriteSpots.value.length) {
      options.showMapStatus('收藏地點缺少座標，暫時無法顯示在地圖上')
    }
  }

  const renderMapMarkers = () => {
    if (!getMap() || !getMarkerConstructor()) return
    clearMapMarkers()

    const validItems = options.activeItineraryItems.value.filter(
      (item) => options.getPlacePosition(item) !== null,
    )
    if (!validItems.length) {
      options.clearMapStatus()
      renderFavoriteSpotMarkers()
      return
    }

    options.clearMapStatus()
    mapMarkers = validItems
      .map((item, index) =>
        createMarker({
          item,
          label: String(index + 1),
          isSelected: item.id === options.selectedMarkerId.value,
        }),
      )
      .filter(Boolean)
    renderFavoriteSpotMarkers()
  }

  const cleanupMapMarkers = () => {
    clearMapMarkers()
    clearFavoriteSpotMarkers()
  }

  return {
    cleanupMapMarkers,
    clearFavoriteSpotMarkers,
    clearMapMarkers,
    createLegacyMarkerIcon,
    createPinElement,
    getPrimaryMapColor,
    renderFavoriteSpotMarkers,
    renderMapMarkers,
    updateMarkerStyles,
  }
}
