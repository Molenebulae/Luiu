<script setup>
import { computed } from 'vue';

const props = defineProps({
  modelValue: String,    // 接收 v-model 的值
  label: String,         // 標籤文字
  placeholder: String,   // 佔位文字
  id: String,            // 用於 label 綁定
  type: {                // input 類型，預設為 text
    type: String,
    default: 'text'
  },
  errorMessage: {        // 錯誤訊息，有值時會自動亮紅框
    type: String,
    default: ''
  }
});

defineEmits(['update:modelValue']);

defineOptions({
  inheritAttrs: false
});

const hasError = computed(() => !! props.errorMessage);
</script>

<template>
  <div class="form-floating mb-3">
    <!-- 綁定 class，如果有 errorMessage 就亮紅框 (is-invalid) -->
    <input
      :id="id"
      :type="type"
      :class="['form-control custom-border', { 'hasError': errorMessage }]"
      :placeholder="placeholder"
      :value="modelValue"
      v-bind="$attrs"
      @input="$emit('update:modelValue', $event.target.value)"
    />
    <label :for="id">{{ label }}</label>

    <!-- 錯誤訊息顯示區 -->
    <transition name="shake">
      <div v-if="hasError" class="invalid-feedback">
        {{ errorMessage }}
      </div>
    </transition>
  </div>
</template>

<style lang="scss" scoped>
@import '@/assets/scss/components/floatingInput';
</style>
