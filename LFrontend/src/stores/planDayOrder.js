import { defineStore } from 'pinia'

export const usePlanDayOrderStore = defineStore('planDayOrder', () => {
  /**
   * Store: reorder days after drag order changes.
   * Day number labels come from the visible index in PlanView, while the card
   * content, date, and itinerary dayNumber stay with the dragged day.
   */
  function normalizeDaysByPosition(currentDays, orderedIds) {
    const dayById = new Map(currentDays.map((day) => [String(day.id), day]))
    const orderedDays = orderedIds
      .map((id) => dayById.get(String(id)))
      .filter(Boolean)

    const missingDays = currentDays.filter((day) => !orderedIds.includes(String(day.id)))
    const nextDays = [...orderedDays, ...missingDays]

    return nextDays
  }

  return { normalizeDaysByPosition }
})
