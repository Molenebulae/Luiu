<script setup>
import { computed } from 'vue';

const props = defineProps(['modelValue', 'min']);
const emit = defineEmits(['update:modelValue']);

const errorMsg = computed(() => {
  if (!props.modelValue) return '此欄位不可空白';
  if (props.modelValue.length < (props.min || 5)) return `至少需要 ${props.min || 5} 個字`;
  return '';
});

// 只有當有內容且長度不足時才顯示紅字提示
const showError = computed(() => !!errorMsg.value && props.modelValue.length > 0);
</script>

<template>
  <div class="col-12">
    <div class="form-floating">
      <textarea
        :value="modelValue"
        @input="$emit('update:modelValue', $event.target.value)"
        class="form-control bg-white custom-border"
        :class="{ 'is-invalid': showError }"
        placeholder="Special Request"
        id="message"
        style="height: 100px"
      ></textarea>
      <label for="message">Special Request</label>
      
      <div v-if="showError" class="invalid-feedback">
        {{ errorMsg }}
      </div>
    </div>
  </div>
</template>


