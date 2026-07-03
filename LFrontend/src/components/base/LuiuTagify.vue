<script setup>
import { ref, computed } from 'vue';

const props = defineProps({
  modelValue: { type: Array, default: () => [] },
  suggestions: { type: Array, default: () => [] },
  placeholder: { type: String, default: '新增標籤...' }
});

const emit = defineEmits(['update:modelValue']);

const newTag = ref('');
const tagInput = ref(null);

// 過濾建議清單
const filteredSuggestions = computed(() => {
  const query = newTag.value.toLowerCase().trim();
  if (!query) return [];
  return props.suggestions.filter(s =>
    s.toLowerCase().includes(query) && !props.modelValue.includes(s)
  );
});

const focusInput = () => tagInput.value.focus();

// 新增標籤邏輯
const addTag = () => {
  const val = newTag.value.trim();
  if (val && !props.modelValue.includes(val)) {
    emit('update:modelValue', [...props.modelValue, val]);
  }
  newTag.value = ''; // 清空輸入
};

// 刪除最後一個標籤 (當輸入框為空且按退格鍵)
const handleBackspace = () => {
  if (!newTag.value && props.modelValue.length > 0) {
    removeTag(props.modelValue.length - 1);
  }
};

const removeTag = (index) => {
  const updatedTags = [...props.modelValue];
  updatedTags.splice(index, 1);
  emit('update:modelValue', updatedTags);
};

const selectSuggestion = (item) => {
  if (!props.modelValue.includes(item)) {
    emit('update:modelValue', [...props.modelValue, item]);
  }
  newTag.value = '';
  focusInput();
};
</script>

<template>
  <div
    class="form-control tagify-wrapper"
    @click="focusInput"
  >
    <!-- 1. 顯示已選標籤 -->
    <div
      v-for="(tag, index) in modelValue"
      :key="index"
      class="tag-item"
    >
      <span class="tag-text">{{ tag }}</span>
      <span class="tag-remove" @click.stop="removeTag(index)">×</span>
    </div>

    <!-- 2. 行內輸入框 -->
    <input
      ref="tagInput"
      v-model="newTag"
      type="text"
      class="inline-input"
      :placeholder="modelValue.length === 0 ? placeholder : ''"
      @keydown.enter.prevent="addTag"
      @keydown.backspace="handleBackspace"
      @blur="addTag"
    />

    <!-- 3. 建議選單 (選用，根據輸入內容顯示) -->
    <div v-if="newTag && filteredSuggestions.length" class="tag-dropdown">
      <div
        v-for="item in filteredSuggestions"
        :key="item"
        class="dropdown-item"
        @mousedown.prevent="selectSuggestion(item)"
      >
        {{ item }}
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
@import '@/assets/scss/components/tagify';
</style>
