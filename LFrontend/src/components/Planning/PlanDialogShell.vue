<script setup>
const props = defineProps({
  titleId: {
    type: String,
    required: true,
  },
  title: {
    type: String,
    required: true,
  },
  kicker: {
    type: String,
    default: '',
  },
  backdropClass: {
    type: String,
    default: 'plan-settings-backdrop',
  },
  panelClass: {
    type: String,
    default: 'plan-settings-panel',
  },
  closeClass: {
    type: String,
    default: 'plan-settings-close',
  },
  closeIconClass: {
    type: String,
    default: 'bi bi-x-lg',
  },
})

const emit = defineEmits(['close'])
</script>

<template>
  <div :class="props.backdropClass" @click.self="emit('close')">
    <section
      :class="props.panelClass"
      role="dialog"
      aria-modal="true"
      :aria-labelledby="props.titleId"
      @click.stop
    >
      <header class="plan-settings-header">
        <div>
          <p v-if="props.kicker" class="plan-settings-kicker mb-1">{{ props.kicker }}</p>
          <h2 :id="props.titleId">{{ props.title }}</h2>
        </div>
        <button :class="props.closeClass" type="button" aria-label="關閉" @click="emit('close')">
          <i :class="props.closeIconClass"></i>
        </button>
      </header>

      <slot></slot>

      <footer v-if="$slots.footer" class="plan-settings-footer">
        <slot name="footer"></slot>
      </footer>
    </section>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/pages/plan-list';
</style>
