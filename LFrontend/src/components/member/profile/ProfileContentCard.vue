<script setup>
const props = defineProps({
  id: [String, Number],
  title: String,
  tag: String,
  subTitle: String,
  coverUrl: String,
  icon: String, // 讓「規劃」與「紀錄」顯示不同圖標
  defaultImage: {
    type: String,
    default: defaultImageUrl,
  },
})

const emit = defineEmits(['click'])

const defaultImageUrl = 'https://images.unsplash.com/photo-1488646953014-85cb44e25828?q=80&w=400'

const handleImageError = (e) => {
  // 當圖片載入失敗時，將 src 指向預設圖
  e.target.src = defaultImageUrl
}

const handleClick = () => {
  console.log(props.id)
  emit('click', props.id)
}
</script>

<template>
  <div
    class="card content-card border-0 shadow-sm hover-up transition-all"
    @click="$emit('click', id)"
  >
    <div class="ratio ratio-16x9">
      <img
        :src="coverUrl || defaultImage"
        class="card-img-top"
        alt="cover"
        @click="handleClick"
        @error="handleImageError"
      />
      <span v-if="tag" class="badge text-bg-primary content-card-tag">
        {{ tag }}
      </span>
    </div>

    <div class="card-body px-3 py-2 content-card-body">
      <div class="info-group">
        <h6 class="content-title text-truncate" :title="title">
          {{ title }}
        </h6>
        <p class="content-date text-muted">
          <i :class="icon || 'bi bi-calendar3'" class="me-1"></i>
          {{ subTitle }}
        </p>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.content-card {
  border: none;
  border-radius: $luiu-card-radius;
  background-color: $luiu-bg-light;
  overflow: hidden;
  transform: $luiu-transition;
  cursor: pointer;
  margin-bottom: 0;

  &:hover {
    transform: translateY(-4px);
  }

  .card-img-top {
    object-fit: cover;
  }

  .content-card-tag {
    position: absolute;
    top: auto;
    right: auto;
    bottom: 0.75rem;
    left: 0.75rem;
    z-index: 1;
    width: auto;
    height: auto;
    max-width: calc(100% - 1.2rem);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    box-shadow: 0 0.35rem 0.85rem rgba(var(--bs-dark-rgb), 0.16);
  }

  .content-card-body {
    height: 60px;
    border-radius: $luiu-card-radius;
    background-color: transparent;
    overflow: hidden;
    border: none;

    .content-title {
      color: $luiu-text-main;
      font-size: 0.95rem;
      font-weight: 500;
      margin-bottom: 2px;
    }

    .content-date {
      color: $text-muted;
      font-size: 0.75rem;
      margin-bottom: 0;
      line-height: 1.2;
    }
  }
}
</style>
