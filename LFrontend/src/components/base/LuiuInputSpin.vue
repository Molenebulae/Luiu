<!-- 目前沒有分離各input輸入 -->
<script setup>
import { onMounted } from 'vue';

const props = defineProps({
  modelValue: { type: Number, default: 0 },
  max: { type: Number, default: 100 },
  min: { type: Number, default: 0 },
});

const emit = defineEmits(['update:modelValue']);

// 更新數值
const updateValue = (newValue) => {
  let finalValue = newValue;

  // 限制範圍
  if (finalValue > props.max) finalValue = props.max;
  if (finalValue < props.min) finalValue = props.min;

  emit('update:modelValue', finalValue);
}

// 處理手動輸入的邏輯
const handleManualInput = (event) => {
  const val = event.target.value;
  if (val === '') return;

  let num = parseInt(val);
  if (num > props.max) {
    num = props.max;
    event.target.value = props.max;
  } else if (num < props.min) {
    num = props.min;
    event.target.value = props.min;
  }

  updateValue(num);
}
// 輸入框失去焦點時確保不是空的
const handleBlur = (event) => {
  if (event.target.value === '') {
    event.target.value = props.min;
    updateValue(props.min);
  }
}

onMounted(() => { })
</script>

<template>
  <div class="row g-5">
    <div class="col-12 col-md-4 col-xl-3">
      <div class="qty-input qty-input-primary">
        <button :disabled="modelValue <= min" @click="updateValue(modelValue - 1)">
          <i class="ri-subtract-line"> </i>
        </button>
        <input id="default-touchspin" type="number" :value="modelValue" @input="handleManualInput" @blur="handleBlur" />
        <button :disabled="modelValue >= max" @click="updateValue(modelValue + 1)">
          <i class="ri-add-line"> </i>
        </button>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.qty-input {
  display: flex;
  align-items: center;
  gap: 3px;

  --local-primary: #{$primary};

  input {
    width: 85px;
    height: 34px;
    text-align: center;
    background: transparent;
    border: 1px solid var(--bs-border-color);
    border-radius: var(--bs-border-radius);

    // 移除原生 number 箭頭
    // 針對Chrome
    &::-webkit-outer-spin-button,
    &::-webkit-inner-spin-button {
      -webkit-appearance: none;
      margin: 0;
    }

    // 針對 Firefox
    &[type='number'] {
      -moz-appearance: textfield;
    }

    &:focus-visible {
      outline: 0;
      border-color: var(--local-primary);
    }
  }

  button {
    width: 34px;
    height: 34px;
    display: grid;
    place-items: center;
    background-color: var(--bs-light);
    border: none;
    border-radius: var(--bs-border-radius);
    transition: all 0.2s ease;

    &:hover {
      opacity: .8;
    }

    &:disabled {
      background-color: var(--bs-gray-400, #ccc) !important;
      cursor: not-allowed;
      opacity: 0.6;
    }
  }

  &.qty-input-primary {
    button {
      background-color: var(--local-primary);
      color: #fff;
    }

    input {
      border: 1px solid var(--local-primary);
    }
  }
}
</style>
