<script setup>
import { computed } from 'vue';

const props = defineProps({
  modelValue: [String, Number], // 綁定的值
  options: {
    type: Array,
    default: () => [] // 預期格式: { label: '顯示文字', value: '值' }
  },
  label: { type: String, default: '' },
  id: {
    type: String,
    default: 'custom-select'
  },
  placeholder: {
    type: String,
    default: '請選擇'
  }
});

const emit = defineEmits(['update:modelValue']);
const hasError = computed(() => !!props.errorMessage);

defineOptions({
  inheritAttrs: false
});

const handleChange = (event) => {
  const val = event.target.value;
  const output = (typeof props.modelValue === 'number') ? Number(val) : val;
  emit('update:modelValue', output);
};
</script>

<template>
  <div class="form-floating">
    <select
      :id="id"
      :class="['form-select custom-border', { 'is-invalid': hasError }]"
      :value="modelValue"
      v-bind="$attrs"
      @change="handleChange"
    >
      <option value="" disabled selected hidden>{{ placeholder }}</option>
      <option v-for="item in options" :key="item.value" :value="item.value">
        {{ item.label }}
      </option>
    </select>
    <label :for="id">{{ label }}</label>

    <transition name="shake">
      <div v-if="hasError" class="invalid-feedback">
        {{ errorMessage }}
      </div>
    </transition>
  </div>
</template>

<style lang="scss" scoped>
@import "@/assets/scss/components/select";
</style>
