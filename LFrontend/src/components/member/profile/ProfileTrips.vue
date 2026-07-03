<script setup>
import { onMounted, ref } from 'vue'
import ProfileContentCard from './ProfileContentCard.vue'
import { useRouter } from 'vue-router'
import { asArrayPayload, getPlanList } from '@/api/planning/plan.js'

const props = defineProps({
  userId: { type: String, required: true },
})

defineEmits(['create-plan'])

const router = useRouter()
const trips = ref([])
const isLoading = ref(false)
const errorMessage = ref('')

const isDeletedTrip = (trip) => trip?.IsDelete === 1 || trip?.IsDelete === '1'

const loadTrips = async () => {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const payload = await getPlanList(props.userId)
    trips.value = asArrayPayload(payload).filter((trip) => !isDeletedTrip(trip))
  } catch (error) {
    errorMessage.value = error?.message || '行程規劃載入失敗'
    trips.value = []
  } finally {
    isLoading.value = false
  }
}

const handleTripClick = (tripId) => {
  if (!tripId) return

  router.push({
    name: 'PlanInfo',
    params: {
      userId: props.userId,
      planId: tripId,
    },
  })
}

onMounted(loadTrips)
</script>

<template>
  <div v-if="isLoading" class="row g-4">
    <div class="col-12 text-center py-5 text-muted">行程規劃載入中...</div>
  </div>

  <div v-else-if="errorMessage" class="row g-4">
    <div class="col-12 text-center py-5 text-danger">{{ errorMessage }}</div>
  </div>

  <div v-else-if="trips.length > 0">
    <div class="row g-4 mt-1">
      <div v-for="trip in trips" :key="trip.TripID" class="col-4">
        <ProfileContentCard
          :id="trip.TripID"
          :title="trip.TripName"
          :tag="trip.TripTag"
          :sub-title="`${trip.StartDate} ~ ${trip.EndDate}`"
          :cover-url="trip.PhotoURL ? $img(trip.PhotoURL) : ''"
          @click="handleTripClick"
        />
      </div>
    </div>
  </div>

  <div v-else class="row g-4">
    <div class="col-12 text-center py-5">
      <i class="bi bi-geo-alt fs-1 text-muted"></i>
      <p class="mt-2 text-muted">目前沒有行程規劃</p>
    </div>
  </div>
</template>

<style scoped></style>
