<script setup>
import { computed } from 'vue'

const props = defineProps({
  name: String,
  image: String,
  category: String,
  address: String,
  rating: { type: Number, default: 0 },
  userRatingCount: { type: Number, default: 0 },
})
defineEmits(['click'])

const normalizedRating = computed(() => Math.max(0, Math.min(5, Number(props.rating) || 0)))
const roundedRating = computed(() => Math.round(normalizedRating.value * 2) / 2)

const getStarClass = (starIndex) => {
  if (starIndex <= Math.floor(roundedRating.value)) return 'bi-star-fill'
  if (starIndex - 0.5 === roundedRating.value) return 'bi-star-half'

  return 'bi-star'
}
</script>

<template>
  <div class="spot-card h-100 shadow-sm border-0" @click="$emit('click')">
    <div class="position-relative overflow-hidden rounded-top" style="height: 180px">
      <img :src="image" class="w-100 h-100 object-fit-cover transition-img" alt="Spot Image" />
      <span class="badge bg-dark bg-opacity-75 position-absolute top-2 start-2 rounded-pill px-3">
        <i class="bi bi-tag-fill me-1"></i> {{ category }}
      </span>
    </div>
    <div class="p-3">
      <h6 class="mb-1 text-truncate fw-bold">{{ name }}</h6>
      <div class="d-flex align-items-center text-muted small mb-2">
        <i class="bi bi-geo-alt-fill text-danger me-1"></i>
        <span class="text-truncate">{{ address }}</span>
      </div>
      <div class="d-flex justify-content-between align-items-center mt-3">
        <div class="text-warning small">
          <span class="text-muted ms-1">{{ rating }}</span
          >&nbsp;
          <i v-for="i in 5" :key="i" class="bi" :class="getStarClass(i)"></i>
          <span class="text-muted ms-1">({{ userRatingCount }})</span>
        </div>
        <i class="bi bi-heart-fill text-danger"></i>
      </div>
    </div>
  </div>
</template>

<style scoped>
.spot-card {
  cursor: pointer;
  transition: transform 0.3s ease;
  background: #fff;
  border-radius: 15px;
}
.spot-card:hover {
  transform: translateY(-5px);
}
.transition-img {
  transition: transform 0.5s ease;
}
.spot-card:hover .transition-img {
  transform: scale(1.1);
}
.top-2 {
  top: 0.75rem;
}
.start-2 {
  start: 0.75rem;
}
</style>
