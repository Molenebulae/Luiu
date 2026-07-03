const getValue = (source, ...keys) => {
  for (const key of keys) {
    const value = source?.[key]
    if (value !== undefined && value !== null) return value
  }

  return ''
}

const toInputDate = (value) => {
  if (!value) return ''
  const normalized = String(value).replaceAll('/', '-')
  const dateOnlyMatch = normalized.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/)
  if (dateOnlyMatch) {
    const [, year, month, day] = dateOnlyMatch
    return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? normalized.slice(0, 10) : date.toISOString().slice(0, 10)
}

const formatTime = (value) => {
  if (!value || value === '--:--') return ''
  const timeOnlyMatch = String(value).match(/(\d{1,2}):(\d{2})/)
  if (!timeOnlyMatch) return ''

  const [, hour, minute] = timeOnlyMatch
  return `${hour.padStart(2, '0')}:${minute}`
}

const formatDuration = (value) => {
  if (value === undefined || value === null || value === '') return ''
  if (typeof value === 'number') return `${value} 分鐘`

  return String(value)
}

export const createMemoryEventId = () =>
  globalThis.crypto?.randomUUID?.() || `memory-event-${Date.now()}-${Math.random()}`

const getPlanCoverImage = (plan = {}) =>
  getValue(
    plan,
    'PhotoURL',
    'PhotoUrl',
    'photoURL',
    'photoUrl',
    'CoverImage',
    'coverImage',
    'CoverImageUrl',
    'coverImageUrl',
  )

const buildDateRange = (plan = {}) => {
  const startDate = toInputDate(
    getValue(plan, 'StartDate', 'startDate', 'StartDateTime', 'startDateTime'),
  )
  const endDate = toInputDate(getValue(plan, 'EndDate', 'endDate', 'EndDateTime', 'endDateTime'))

  if (startDate && endDate) return [startDate, endDate]
  if (startDate) return [startDate, startDate]

  return ''
}

const normalizeMemoryEvent = (item = {}) => ({
  id: item.id || item.detailId || item.DetailID || createMemoryEventId(),
  time: formatTime(getValue(item, 'time', 'ArrivalTime', 'arrivalTime')),
  title: getValue(item, 'title', 'SpotAlias', 'SpotName', 'spotName') || '未命名景點',
  location: getValue(item, 'location', 'address', 'Address', 'SpotName', 'spotName', 'title') || '',
  duration: formatDuration(getValue(item, 'duration', 'StayDuration', 'stayDuration')),
  description: getValue(item, 'description', 'Notes', 'notes') || '',
  expense: Number(getValue(item, 'expense', 'cost', 'Budget', 'budget', 'Cost')) || 0,
  rating: 0,
  imageUrls: [],
  videoUrl: '',
})

const buildDailyEvents = (itineraryItems = []) => {
  const dailyEvents = {}

  itineraryItems
    .filter((item) => item?.draftStatus !== 'deleted' && !(item?.IsDeleted ?? item?.isDeleted))
    .forEach((item) => {
      const dayNumber = Number(getValue(item, 'dayNumber', 'DayNumber')) || 1
      if (!dailyEvents[dayNumber]) dailyEvents[dayNumber] = []
      dailyEvents[dayNumber].push(normalizeMemoryEvent(item))
    })

  Object.keys(dailyEvents).forEach((dayNumber) => {
    dailyEvents[dayNumber].sort((a, b) => a.time.localeCompare(b.time))
  })

  return dailyEvents
}

export const buildPlanMemoryForm = ({ plan = {}, itineraryItems = [], tripId = null } = {}) => {
  const tripTag = String(getValue(plan, 'TripTag', 'tripTag')).trim()

  return {
    title: getValue(plan, 'TripName', 'tripName') || '',
    dateRange: buildDateRange(plan),
    coverImage: getPlanCoverImage(plan) || '',
    destinations: tripTag ? [tripTag] : [],
    sourceTripId: getValue(plan, 'TripID', 'tripId') || tripId || null,
    dailyEvents: buildDailyEvents(itineraryItems),
  }
}
