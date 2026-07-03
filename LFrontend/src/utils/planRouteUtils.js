export const MAX_ROUTE_STOPS = 12

export const transportOptions = [
  { value: 'DRIVE', label: '汽車', icon: 'fa-car', iconName: 'car' },
  { value: 'TWO_WHEELER', label: '機車', icon: 'fa-motorcycle', iconName: 'motorcycle' },
  { value: 'WALK', label: '步行', icon: 'fa-person-walking', iconName: 'person-walking' },
  { value: 'BICYCLE', label: '自行車', icon: 'fa-bicycle', iconName: 'bicycle' },
  { value: 'TRANSIT', label: '大眾運輸', icon: 'fa-bus', iconName: 'bus' },
]

export const transportModeAliases = {
  DRIVING: 'DRIVE',
  DRIVE: 'DRIVE',
  TWO_WHEELER: 'TWO_WHEELER',
  MOTORCYCLE: 'TWO_WHEELER',
  WALKING: 'WALK',
  WALK: 'WALK',
  BICYCLING: 'BICYCLE',
  BICYCLE: 'BICYCLE',
  TRANSIT: 'TRANSIT',
}

export const transportModeApiValues = {
  DRIVE: 1,
  TWO_WHEELER: 2,
  WALK: 3,
  BICYCLE: 4,
  TRANSIT: 5,
}

const transportModeApiModes = Object.fromEntries(
  Object.entries(transportModeApiValues).map(([mode, value]) => [String(value), mode]),
)

export const transitModeOptions = {
  SUBWAY: { label: '捷運', icon: 'fa-train-subway', iconName: 'train-subway' },
  BUS: { label: '公車', icon: 'fa-bus', iconName: 'bus' },
  FERRY: { label: '渡輪', icon: 'fa-ship', iconName: 'ship' },
  RAIL: { label: '鐵路', icon: 'fa-train', iconName: 'train' },
  TRAIN: { label: '火車', icon: 'fa-train', iconName: 'train' },
  TRAM: { label: '輕軌', icon: 'fa-train-tram', iconName: 'train-tram' },
}

export const normalizeTransportMode = (mode) =>
  transportModeAliases[String(mode || '').toUpperCase()] || 'DRIVE'

export const toApiTransportMode = (mode) => transportModeApiValues[normalizeTransportMode(mode)] ?? null

export const fromApiTransportMode = (mode) =>
  transportModeApiModes[String(mode)] || transportModeAliases[String(mode || '').toUpperCase()] || ''

export const resolveResponseTransportMode = (mode, fallbackMode = 'DRIVE') =>
  transportModeAliases[String(mode || '').toUpperCase()] || normalizeTransportMode(fallbackMode)

export const getTransportOption = (mode) =>
  transportOptions.find((o) => o.value === normalizeTransportMode(mode)) ?? transportOptions[0]

export const getTransportIconName = (mode) => getTransportOption(mode).iconName

export const formatDistanceLabel = (meters = 0) => {
  const distance = Number(meters || 0)
  if (!distance) return ''
  return distance >= 1000 ? `${(distance / 1000).toFixed(1)} 公里` : `${Math.round(distance)} 公尺`
}

export const getTransitStepOption = (step = {}) =>
  transitModeOptions[String(step.transitMode || '').toUpperCase()] || getTransportOption(step.type)

export const getTransitStepIconName = (step = {}) => getTransitStepOption(step).iconName

export const isRouteWalkStep = (step = {}) => normalizeTransportMode(step.type) === 'WALK'

export const isTransitVehicleStep = (step = {}) => step.type === 'TRANSIT' || Boolean(step.transitMode)

export const getRouteStepTitle = (step = {}) => {
  if (step.type === 'SUMMARY') return step.title
  const option = getTransitStepOption(step)
  if (step.type === 'TRANSIT') {
    return [option.label, step.lineName].filter(Boolean).join(' ')
  }
  return option.label
}

export const getRouteStepDescription = (step = {}) => {
  if (step.type === 'SUMMARY') return step.description
  const distance = formatDistanceLabel(step.distanceMeters)
  if (isRouteWalkStep(step)) return distance || '步行路段'
  if (step.type === 'TRANSIT') {
    const stops = [step.departureStop, step.arrivalStop].filter(Boolean).join(' → ')
    return stops || step.instruction || '大眾運輸路段'
  }
  return [step.instruction, distance].filter(Boolean).join(' · ')
}

export const getRouteStepTimeLabel = (step = {}) =>
  [formatDistanceLabel(step.distanceMeters), step.minutes ? `${step.minutes} 分鐘` : '']
    .filter(Boolean)
    .join(' · ')

export const getTransitStepLabel = (step = {}) => {
  const option = getTransitStepOption(step)
  if (isRouteWalkStep(step)) return option.label
  return [step.lineName, option.label].filter(Boolean).join(' ') || option.label
}

export const getTransitVehicleSteps = (transfer = {}) =>
  (Array.isArray(transfer.steps) ? transfer.steps : []).filter(isTransitVehicleStep)

export const getVisibleRouteSteps = (transfer = {}) => {
  const steps = Array.isArray(transfer.steps) ? transfer.steps : []
  const vehicleSteps = getTransitVehicleSteps(transfer)
  const walkSteps = steps.filter(isRouteWalkStep)
  if (steps.length && walkSteps.length === steps.length) {
    const totalMinutes = steps.reduce((sum, step) => sum + Number(step.minutes || 0), 0)
    const totalDistanceMeters = steps.reduce((sum, step) => sum + Number(step.distanceMeters || 0), 0)
    return [
      {
        type: 'SUMMARY',
        title: '步行',
        description: formatDistanceLabel(totalDistanceMeters) || '步行路段',
        hideDescription: true,
        minutes: totalMinutes,
        distanceMeters: totalDistanceMeters,
        detailSteps: steps,
      },
    ]
  }
  if (vehicleSteps.length <= 2) return vehicleSteps.length ? vehicleSteps : steps

  const firstStep = vehicleSteps[0]
  const lastStep = vehicleSteps[vehicleSteps.length - 1]
  const middleVehicleSteps = vehicleSteps.slice(1, -1)
  const middleTransitLabels = middleVehicleSteps.map(getTransitStepLabel)

  return [
    firstStep,
    {
      type: 'SUMMARY',
      title: `... ${middleTransitLabels.join('、')} ...`,
      description: `已簡略 ${steps.length - 2} 段路程`,
      hiddenSteps: middleVehicleSteps,
      detailSteps: steps,
    },
    lastStep,
  ]
}

const toNumberOrNull = (value) => {
  const number = Number(value)
  return Number.isFinite(number) ? number : null
}

export const getStopRouteKey = (stop = {}) =>
  String(
    stop.detailId ??
      stop.spotId ??
      stop.googleMapId ??
      `${stop.name || ''}-${stop.latitude || ''}-${stop.longitude || ''}`,
  )

export const getItemRouteKey = (item = {}) =>
  getStopRouteKey({
    detailId: item.detailId ?? null,
    spotId: item.spotId ?? null,
    googleMapId: item.placeId || '',
    name: item.title,
    latitude: item.lat,
    longitude: item.lng,
  })

export const getStepPolyline = (step = {}) =>
  step.encodedPolyline ||
  step.EncodedPolyline ||
  step.polyline?.points ||
  step.polyline?.Points ||
  step.Polyline?.Points ||
  step.Polyline?.points ||
  (typeof step.polyline === 'string' ? step.polyline : '') ||
  (typeof step.Polyline === 'string' ? step.Polyline : '') ||
  ''

export const getPolylineValue = (polyline) => {
  if (typeof polyline === 'string') return polyline
  if (!polyline || typeof polyline !== 'object') return ''
  return (
    polyline.points || polyline.Points || polyline.encodedPolyline || polyline.EncodedPolyline || ''
  )
}

export const toLatLngLiteral = (value = {}) => {
  const lat = toNumberOrNull(value.lat ?? value.Lat ?? value.latitude ?? value.Latitude)
  const lng = toNumberOrNull(value.lng ?? value.Lng ?? value.longitude ?? value.Longitude)
  return lat === null || lng === null ? null : { lat, lng }
}

export const getRouteDrawingSegments = (result = null) => {
  const segments = []
  if (!Array.isArray(result?.legs)) {
    const overviewPolyline = getPolylineValue(result?.overviewPolyline)
    return overviewPolyline ? [{ polyline: overviewPolyline }] : []
  }
  result.legs.forEach((leg) => {
    const steps = Array.isArray(leg.steps) ? leg.steps : []
    const legPolyline = getPolylineValue(leg.polyline)
    if (!steps.length) {
      if (legPolyline) segments.push({ polyline: legPolyline })
      return
    }
    let hasStepPolyline = false
    steps.forEach((step) => {
      if (normalizeTransportMode(step.type) === 'WALK') {
        const polyline = getStepPolyline(step)
        if (polyline) hasStepPolyline = true
        segments.push({
          type: 'WALK',
          polyline,
          startLocation: step.startLocation,
          endLocation: step.endLocation,
        })
        return
      }
      const polyline = getStepPolyline(step)
      if (polyline) {
        hasStepPolyline = true
        segments.push({ polyline })
      }
    })
    if (!hasStepPolyline && legPolyline) segments.push({ polyline: legPolyline })
  })
  const overviewPolyline = getPolylineValue(result?.overviewPolyline)
  return segments.length ? segments : overviewPolyline ? [{ polyline: overviewPolyline }] : []
}

export const normalizeRouteStep = (step = {}) => {
  const transitDetails = step.transitDetails || step.TransitDetails || {}
  const vehicle = transitDetails.vehicle || transitDetails.Vehicle || {}
  const line = transitDetails.line || transitDetails.Line || {}
  const transitMode = String(
    step.transitMode ||
      step.TransitMode ||
      step.vehicleType ||
      step.VehicleType ||
      vehicle.type ||
      vehicle.Type ||
      '',
  ).toUpperCase()
  const travelMode = normalizeTransportMode(
    step.type || step.Type || step.travelMode || step.TravelMode || step.mode || step.Mode,
  )
  return {
    type: transitMode ? 'TRANSIT' : travelMode,
    instruction:
      step.instruction ||
      step.Instruction ||
      step.htmlInstructions ||
      step.HtmlInstructions ||
      step.instructions ||
      step.Instructions ||
      '',
    minutes:
      step.durationMinutes ??
      step.DurationMinutes ??
      Math.round(((step.durationSeconds ?? step.DurationSeconds) || 0) / 60),
    distanceMeters: step.distanceMeters ?? step.DistanceMeters ?? 0,
    transitMode,
    lineName:
      step.lineName ||
      step.LineName ||
      step.lineShortName ||
      step.LineShortName ||
      step.routeName ||
      step.RouteName ||
      line.shortName ||
      line.ShortName ||
      line.name ||
      line.Name ||
      '',
    departureStop:
      step.departureStop ||
      step.DepartureStop ||
      step.departureStopName ||
      step.DepartureStopName ||
      transitDetails.departureStop?.name ||
      transitDetails.DepartureStop?.Name ||
      '',
    arrivalStop:
      step.arrivalStop ||
      step.ArrivalStop ||
      step.arrivalStopName ||
      step.ArrivalStopName ||
      transitDetails.arrivalStop?.name ||
      transitDetails.ArrivalStop?.Name ||
      '',
    stopCount:
      step.stopCount ??
      step.StopCount ??
      transitDetails.stopCount ??
      transitDetails.StopCount ??
      null,
    polyline: getStepPolyline(step),
    startLocation:
      toLatLngLiteral(
        step.startLocation || step.StartLocation || step.start_location || step.Start_Location,
      ) ||
      toLatLngLiteral(
        transitDetails.departureStop?.location || transitDetails.DepartureStop?.Location,
      ),
    endLocation:
      toLatLngLiteral(
        step.endLocation || step.EndLocation || step.end_location || step.End_Location,
      ) ||
      toLatLngLiteral(transitDetails.arrivalStop?.location || transitDetails.ArrivalStop?.Location),
    routeDetails: step.routeDetails || step.RouteDetails || step.stops || step.Stops || [],
  }
}

export const normalizeRouteLeg = (leg = {}, index, options = {}) => {
  const currentStops = options.currentStops || []
  const travelModes = options.travelModes || []
  const dayNumber = options.dayNumber || 1
  const fromRouteKey = getStopRouteKey(currentStops[index])
  const toRouteKey = getStopRouteKey(currentStops[index + 1])
  const steps = leg.steps || leg.Steps || []
  const hasRequestedMode =
    travelModes[index] !== undefined && travelModes[index] !== null && travelModes[index] !== ''
  const requestedMode = normalizeTransportMode(travelModes[index])
  const responseMode = leg.travelMode || leg.TravelMode || leg.mode || leg.Mode
  return {
    id: `${dayNumber}-${index}-${fromRouteKey}-${toRouteKey}`,
    dayNumber,
    index,
    visible: true,
    mode: hasRequestedMode ? requestedMode : resolveResponseTransportMode(responseMode, requestedMode),
    minutes:
      leg.durationMinutes ??
      leg.DurationMinutes ??
      Math.round(((leg.durationSeconds ?? leg.DurationSeconds) || 0) / 60),
    distanceMeters: leg.distanceMeters ?? leg.DistanceMeters ?? 0,
    durationSeconds: leg.durationSeconds ?? leg.DurationSeconds ?? 0,
    fromSpotId: leg.fromSpotId ?? leg.FromSpotId ?? leg.FromSpotID,
    toSpotId: leg.toSpotId ?? leg.ToSpotId ?? leg.ToSpotID,
    fromRouteKey,
    toRouteKey,
    polyline:
      getPolylineValue(leg.polyline || leg.Polyline) ||
      leg.encodedPolyline ||
      leg.EncodedPolyline ||
      '',
    steps: Array.isArray(steps) ? steps.map(normalizeRouteStep) : [],
  }
}
