import { computed, nextTick, ref } from 'vue'
import { getPlanRoute } from '@/api/planning/planRoute'
import {
  MAX_ROUTE_STOPS,
  fromApiTransportMode,
  getItemRouteKey,
  getRouteDrawingSegments,
  getStopRouteKey,
  getTransportOption,
  normalizeRouteLeg,
  normalizeTransportMode,
  toApiTransportMode,
} from '@/utils/planRouteUtils'

export const usePlanRoutePlanner = (options) => {
  const showRouteMap = ref(false)
  const isRouteShown = ref(false)
  const isRoutePlanningActive = ref(false)
  const routeResult = ref(null)
  const isRouteLoading = ref(false)
  const transferRows = ref([])
  const defaultTravelMode = ref('DRIVE')
  const editingTransferId = ref(null)
  const editingTransferRoute = ref(null)

  const activeTransferRows = computed(() =>
    options.activeItineraryItems.value.length
      ? transferRows.value
          .filter((row) => Number(row.dayNumber || 1) === Number(options.activeDayNumber.value))
          .sort((a, b) => Number(a.index || 0) - Number(b.index || 0))
      : [],
  )

  const defaultTransportOption = computed(() => getTransportOption(defaultTravelMode.value))

  const setRouteMapVisibility = (visible) => {
    showRouteMap.value = Boolean(visible)
    const input = options.routeMapSwitchRef.value?.$el?.querySelector?.('input[type="checkbox"]')
    if (input && input.checked !== showRouteMap.value) input.checked = showRouteMap.value
  }

  const clearRouteState = (dayNumber = options.activeDayNumber.value, resetPlanning = true) => {
    options.clearRoutePolyline()
    routeResult.value = null
    isRouteShown.value = false
    if (resetPlanning) {
      isRoutePlanningActive.value = false
      setRouteMapVisibility(false)
    }
    transferRows.value = transferRows.value.filter(
      (row) => Number(row.dayNumber || 1) !== Number(dayNumber),
    )
  }

  const buildRouteStops = () =>
    options.activeItineraryItems.value
      .slice()
      .sort((a, b) => Number(a.sortOrder || 0) - Number(b.sortOrder || 0))
      .map((item, index) => ({
        detailId: item.detailId ?? null,
        spotId: item.spotId ?? null,
        googleMapId: item.placeId || '',
        name: item.title,
        latitude: item.lat,
        longitude: item.lng,
        sortOrder: index + 1,
      }))

  const getTransferAfterTripDetailItem = (item) => {
    if (!item) return null
    const dayItems = options.getDayItineraryItems({ dayNumber: item.dayNumber })
    const index = dayItems.findIndex((dayItem) => String(dayItem.id) === String(item.id))
    const nextItem = dayItems[index + 1]
    if (index < 0 || !nextItem) return null

    const fromRouteKey = getItemRouteKey(item)
    const toRouteKey = getItemRouteKey(nextItem)
    const dayRows = transferRows.value.filter(
      (row) => Number(row.dayNumber || 1) === Number(item.dayNumber || 1),
    )
    const matchedRow = dayRows.find(
      (row) => row.fromRouteKey === fromRouteKey && row.toRouteKey === toRouteKey,
    )
    if (matchedRow) return matchedRow

    return dayRows.find(
      (row) => !row.fromRouteKey && !row.toRouteKey && Number(row.index || 0) === Number(index),
    )
  }

  const getTripDetailTransportPayload = (item) => {
    const transfer = getTransferAfterTripDetailItem(item)
    if (!transfer) {
      return {
        TransportMode: item.transportMode ?? null,
        TransportTime: item.transportTime ?? null,
      }
    }

    return {
      TransportMode: toApiTransportMode(transfer.mode),
      TransportTime: transfer.minutes ? Number(transfer.minutes) : null,
    }
  }

  const getTransferAfterItem = (item, index) => {
    const nextItem = options.activeItineraryItems.value[index + 1]
    if (!item || !nextItem) return null
    const fromRouteKey = getItemRouteKey(item)
    const toRouteKey = getItemRouteKey(nextItem)
    const matchedRow = activeTransferRows.value.find(
      (row) => row.fromRouteKey === fromRouteKey && row.toRouteKey === toRouteKey,
    )
    if (matchedRow) return matchedRow

    return activeTransferRows.value.find(
      (row) => !row.fromRouteKey && !row.toRouteKey && Number(row.index || 0) === Number(index),
    )
  }

  const hasStoredTransferData = (item = {}) =>
    item.transportMode !== null ||
    item.transportTime !== null ||
    Boolean(item.polylineEncoded)

  const buildStoredTransferRows = (items = []) => {
    const itemsByDay = new Map()
    items
      .filter((item) => item.draftStatus !== options.DRAFT_STATUS.deleted)
      .forEach((item) => {
        const dayNumber = Number(item.dayNumber || 1)
        if (!itemsByDay.has(dayNumber)) itemsByDay.set(dayNumber, [])
        itemsByDay.get(dayNumber).push(item)
      })

    const rows = []
    itemsByDay.forEach((dayItems, dayNumber) => {
      const orderedItems = dayItems.sort(
        (a, b) => Number(a.sortOrder || 0) - Number(b.sortOrder || 0),
      )
      orderedItems.slice(0, -1).forEach((item, index) => {
        const nextItem = orderedItems[index + 1]
        if (!hasStoredTransferData(item)) return
        const mode = fromApiTransportMode(item.transportMode) || defaultTravelMode.value
        const minutes = Number(item.transportTime || 0)
        rows.push({
          id: `stored-${dayNumber}-${index}-${getItemRouteKey(item)}-${getItemRouteKey(nextItem)}`,
          dayNumber,
          index,
          visible: true,
          mode,
          minutes,
          distanceMeters: 0,
          durationSeconds: minutes * 60,
          fromSpotId: item.spotId ?? null,
          toSpotId: nextItem.spotId ?? null,
          fromRouteKey: getItemRouteKey(item),
          toRouteKey: getItemRouteKey(nextItem),
          polyline: item.polylineEncoded || '',
          steps: [],
        })
      })
    })
    return rows
  }

  const buildRouteResultFromTransferRows = (rows = activeTransferRows.value) => {
    const visibleRows = rows.filter((row) => row.visible)
    return {
      legs: visibleRows,
      overviewPolyline: '',
      totalDurationMinutes: visibleRows.reduce((sum, row) => sum + Number(row.minutes || 0), 0),
      totalDistanceMeters: visibleRows.reduce((sum, row) => sum + Number(row.distanceMeters || 0), 0),
    }
  }

  const showStoredRouteRows = async () => {
    routeResult.value = buildRouteResultFromTransferRows()
    isRouteShown.value = Boolean(activeTransferRows.value.length)
    isRoutePlanningActive.value = true
    if (showRouteMap.value) {
      await options.drawRoutePolylines(getRouteDrawingSegments(routeResult.value))
    }
    options.scheduleRenderKey.value += 1
  }

  const getMatchedTransferRowsForCurrentStops = (previousRows = activeTransferRows.value) => {
    const stops = buildRouteStops()
    const matchedRows = []

    stops.slice(0, -1).forEach((stop, index) => {
      const fromRouteKey = getStopRouteKey(stop)
      const toRouteKey = getStopRouteKey(stops[index + 1])
      const matchedRow = previousRows.find(
        (row) => row.fromRouteKey === fromRouteKey && row.toRouteKey === toRouteKey,
      )
      if (!matchedRow) return

      matchedRows.push({
        ...matchedRow,
        id: `${options.activeDayNumber.value}-${index}-${fromRouteKey}-${toRouteKey}`,
        dayNumber: options.activeDayNumber.value,
        index,
        fromRouteKey,
        toRouteKey,
      })
    })

    return matchedRows
  }

  const showMatchedRouteRows = async (previousRows = activeTransferRows.value) => {
    const matchedRows = getMatchedTransferRowsForCurrentStops(previousRows)
    transferRows.value = [
      ...transferRows.value.filter(
        (row) => Number(row.dayNumber || 1) !== Number(options.activeDayNumber.value),
      ),
      ...matchedRows,
    ]
    routeResult.value = matchedRows.length ? buildRouteResultFromTransferRows(matchedRows) : null
    isRouteShown.value = Boolean(matchedRows.length)
    isRoutePlanningActive.value = true

    if (showRouteMap.value && routeResult.value) {
      await options.drawRoutePolylines(getRouteDrawingSegments(routeResult.value))
    } else {
      options.clearRoutePolyline()
    }

    options.scheduleRenderKey.value += 1
    recalculateActiveItineraryTimes()
    return true
  }

  const buildCurrentTravelModes = () => {
    const routeLegCount = Math.max(options.activeItineraryItems.value.length - 1, 0)
    return Array.from({ length: routeLegCount }, (_, index) => {
      const item = options.activeItineraryItems.value[index]
      return normalizeTransportMode(
        getTransferAfterItem(item, index)?.mode ||
          fromApiTransportMode(item?.transportMode) ||
          defaultTravelMode.value,
      )
    })
  }

  const buildTravelModesForCurrentStops = (previousRows = activeTransferRows.value) => {
    const stops = buildRouteStops()
    return Array.from({ length: Math.max(stops.length - 1, 0) }, (_, index) => {
      const fromRouteKey = getStopRouteKey(stops[index])
      const toRouteKey = getStopRouteKey(stops[index + 1])
      const matchedRow = previousRows.find(
        (row) => row.fromRouteKey === fromRouteKey && row.toRouteKey === toRouteKey,
      )
      return normalizeTransportMode(
        matchedRow?.mode ||
          fromApiTransportMode(options.activeItineraryItems.value[index]?.transportMode) ||
          defaultTravelMode.value,
      )
    })
  }

  const normalizeRouteResult = (payload) => {
    const result = options.unwrapApiData(payload)
    const firstRoute = result?.routes?.[0] || result?.Routes?.[0]
    const route = Array.isArray(result?.legs) || Array.isArray(result?.Legs) ? result : firstRoute
    if (!route) return result

    const legs = route.legs || route.Legs || []
    return {
      ...result,
      ...route,
      legs,
      overviewPolyline:
        route.overviewPolyline ||
        route.OverviewPolyline ||
        route.overview_polyline?.points ||
        result?.overviewPolyline ||
        result?.OverviewPolyline ||
        '',
      totalDurationMinutes:
        route.totalDurationMinutes ??
        route.TotalDurationMinutes ??
        result?.totalDurationMinutes ??
        result?.TotalDurationMinutes ??
        legs.reduce((sum, leg) => sum + (leg.durationMinutes ?? leg.DurationMinutes ?? 0), 0),
      totalDistanceMeters:
        route.totalDistanceMeters ??
        route.TotalDistanceMeters ??
        result?.totalDistanceMeters ??
        result?.TotalDistanceMeters ??
        legs.reduce((sum, leg) => sum + (leg.distanceMeters ?? leg.DistanceMeters ?? 0), 0),
    }
  }

  const recalculateActiveItineraryTimes = () => {
    const activeItems = options.activeItineraryItems.value
      .slice()
      .sort((a, b) => Number(a.sortOrder || 0) - Number(b.sortOrder || 0))
    if (!activeItems.length) return

    let nextTime =
      activeItems[0].time && activeItems[0].time !== '--:--' ? activeItems[0].time : '09:00'
    const nextTimes = new Map()
    activeItems.forEach((item, index) => {
      nextTimes.set(item.id, nextTime)
      const stayMinutes = options.parseDurationMinutes(item.duration)
      const transferMinutes = getTransferAfterItem(item, index)?.minutes || 0
      nextTime = options.addMinutesToTime(nextTime, stayMinutes + transferMinutes)
    })

    let hasTimeChanged = false
    options.itineraryItems.value = options.itineraryItems.value.map((item) => {
      if (!nextTimes.has(item.id) || item.time === nextTimes.get(item.id)) return item
      hasTimeChanged = true
      return options.markDraftUpdated({ ...item, time: nextTimes.get(item.id) })
    })
    if (hasTimeChanged) options.scheduleRenderKey.value += 1
  }

  const applyRouteResult = async (result, travelModes = []) => {
    const stops = buildRouteStops()
    routeResult.value = result
    isRouteShown.value = true
    isRoutePlanningActive.value = true
    transferRows.value = [
      ...transferRows.value.filter(
        (row) => Number(row.dayNumber || 1) !== Number(options.activeDayNumber.value),
      ),
      ...(Array.isArray(result?.legs)
        ? result.legs.map((leg, index) =>
            normalizeRouteLeg(leg, index, {
              currentStops: stops,
              dayNumber: options.activeDayNumber.value,
              travelModes,
            }),
          )
        : []),
    ]
    if (showRouteMap.value) {
      await options.drawRoutePolylines(getRouteDrawingSegments(result))
    } else {
      options.clearRoutePolyline()
    }
    await nextTick()
    recalculateActiveItineraryTimes()
    options.scheduleRenderKey.value += 1
  }

  const requestRoute = async (travelMode) => {
    const stops = buildRouteStops()
    if (stops.length > MAX_ROUTE_STOPS) {
      options.showMapStatus(`目前路線最多支援 ${MAX_ROUTE_STOPS} 個景點，請先減少景點數量`)
      return false
    }

    if (stops.some((stop) => stop.latitude == null || stop.longitude == null)) {
      options.showMapStatus('部分景點缺少座標，無法規劃路線')
      return false
    }

    if (travelMode.length !== Math.max(stops.length - 1, 0)) {
      options.showMapStatus('路線交通方式數量與景點數量不一致，請重新規劃')
      return false
    }

    isRouteLoading.value = true
    try {
      const payload = await getPlanRoute(options.userId.value, options.tripId.value, {
        dayNumber: options.activeDayNumber.value,
        travelMode,
        stops,
      })
      const result = normalizeRouteResult(payload)
      if (travelMode.length && !Array.isArray(result?.legs)) {
        throw new Error('Google 路線服務未回傳路線資料')
      }
      await applyRouteResult(result, travelMode)
      return true
    } catch (error) {
      clearRouteState(options.activeDayNumber.value, false)
      options.showMapStatus(options.getApiErrorMessage(error, '路線計算失敗，請稍後再試'))
      return false
    } finally {
      isRouteLoading.value = false
    }
  }

  const ensureCurrentScheduleOrder = async () => {
    await options.syncItineraryOrderFromDom({ reroute: false })
  }

  const rerouteIfPlanningActive = async (previousRows = activeTransferRows.value) => {
    editingTransferId.value = null
    if (options.activeDialog.value === 'transfer') options.closeDialog()
    if (!isRoutePlanningActive.value) {
      options.clearRoutePolyline()
      return null
    }
    setRouteMapVisibility(true)
    if (options.activeItineraryItems.value.length < 2) {
      clearRouteState(options.activeDayNumber.value, false)
      options.showMapStatus('至少需要兩個景點才能規劃路線')
      return false
    }
    return showMatchedRouteRows(previousRows)
  }

  const handleRouteButton = async () => {
    if (isRouteLoading.value) return
    await ensureCurrentScheduleOrder()
    isRoutePlanningActive.value = true
    setRouteMapVisibility(true)

    if (options.activeItineraryItems.value.length < 2) {
      options.showMapStatus('至少需要兩個景點才能規劃路線')
      setRouteMapVisibility(false)
      return
    }

    const didRoute = await requestRoute(buildCurrentTravelModes())
    if (!didRoute) setRouteMapVisibility(false)
  }

  const handleRouteMapToggle = async (event) => {
    isRoutePlanningActive.value = Boolean(event?.target?.checked)
    setRouteMapVisibility(Boolean(event?.target?.checked))
    if (!showRouteMap.value) {
      options.clearRoutePolyline()
      return
    }

    if (isRouteLoading.value) return
    if (routeResult.value && getRouteDrawingSegments(routeResult.value).length) {
      await options.drawRoutePolylines(getRouteDrawingSegments(routeResult.value))
      return
    }

    if (options.activeItineraryItems.value.length < 2) {
      options.showMapStatus('至少需要兩個景點才能規劃路線')
      setRouteMapVisibility(false)
      return
    }

    await ensureCurrentScheduleOrder()
    if (activeTransferRows.value.length) {
      await showStoredRouteRows()
      return
    }

    const didRoute = await requestRoute(buildCurrentTravelModes())
    if (!didRoute) setRouteMapVisibility(false)
  }

  const setDefaultTravelMode = async (mode) => {
    if (isRouteLoading.value) return
    const normalizedMode = normalizeTransportMode(mode)
    defaultTravelMode.value = normalizedMode
    await ensureCurrentScheduleOrder()
    isRoutePlanningActive.value = true
    setRouteMapVisibility(true)

    if (options.activeItineraryItems.value.length < 2) {
      options.showMapStatus('至少需要兩個景點才能規劃路線')
      setRouteMapVisibility(false)
      return
    }

    const didRoute = await requestRoute(buildCurrentTravelModes())
    if (!didRoute) setRouteMapVisibility(false)
  }

  const getTransferLabel = (transfer) =>
    `${getTransportOption(transfer.mode).label} ${transfer.minutes || 0} 分鐘`

  const shouldShowRouteSteps = (transfer = {}) =>
    normalizeTransportMode(transfer.mode) === 'TRANSIT' &&
    Array.isArray(transfer.steps) &&
    transfer.steps.length

  const openTransferDialog = (transfer) => {
    if (isRouteLoading.value) return
    editingTransferId.value = transfer.id
    editingTransferRoute.value = {
      id: transfer.id,
      index: Number(transfer.index || 0),
      fromRouteKey: transfer.fromRouteKey || '',
      toRouteKey: transfer.toRouteKey || '',
    }
    options.transferForm.mode = transfer.mode || 'DRIVE'
    options.activeDialog.value = 'transfer'
  }

  const updateTransfer = async () => {
    const targetRoute = editingTransferRoute.value
    await ensureCurrentScheduleOrder()
    const targetTransfer = activeTransferRows.value.find(
      (row) =>
        row.id === editingTransferId.value ||
        (targetRoute?.fromRouteKey &&
          targetRoute?.toRouteKey &&
          row.fromRouteKey === targetRoute.fromRouteKey &&
          row.toRouteKey === targetRoute.toRouteKey) ||
        Number(row.index || 0) === Number(targetRoute?.index),
    )
    if (!targetTransfer) {
      options.closeDialog()
      return
    }
    const targetMode = normalizeTransportMode(options.transferForm.mode)
    const travelMode = buildCurrentTravelModes().map((mode, index) => {
      const item = options.activeItineraryItems.value[index]
      const nextItem = options.activeItineraryItems.value[index + 1]
      const isSameRoute =
        item &&
        nextItem &&
        getItemRouteKey(item) === targetTransfer.fromRouteKey &&
        getItemRouteKey(nextItem) === targetTransfer.toRouteKey
      const isSameIndex = Number(index) === Number(targetTransfer.index)
      return isSameRoute || isSameIndex ? targetMode : mode
    })
    options.closeDialog()
    await requestRoute(travelMode)
  }

  return {
    activeTransferRows,
    buildCurrentTravelModes,
    buildRouteStops,
    buildStoredTransferRows,
    buildTravelModesForCurrentStops,
    clearRouteState,
    defaultTransportOption,
    defaultTravelMode,
    editingTransferId,
    editingTransferRoute,
    getTransferAfterItem,
    getTransferLabel,
    getTripDetailTransportPayload,
    handleRouteButton,
    handleRouteMapToggle,
    isRouteLoading,
    isRoutePlanningActive,
    isRouteShown,
    openTransferDialog,
    recalculateActiveItineraryTimes,
    rerouteIfPlanningActive,
    requestRoute,
    routeResult,
    setDefaultTravelMode,
    shouldShowRouteSteps,
    showRouteMap,
    transferRows,
    updateTransfer,
  }
}
