<!-- 在使用說明上不用給props即可使用，屬於default樣式 -->
<!-- 是很純粹的formcheck -->
<!-- 目前增加自定義可以於div class增加lg或sm來更換大小 -->
<script setup>
import { onMounted } from 'vue'
const props = defineProps({
  checkName: {
    type: String,
    default: '預設文字',
  },
  //改顏色用
  variant: {
    type: String,
    default: 'primary', // 預設為 primary 藍
  },
})
onMounted(() => {})
</script>

<template>
  <div class="form-check form-switch luiu-switch mb-3" :class="variant">
    <input class="form-check-input me-2" type="checkbox" id="s1" />
    <label class="form-check-label" for="s1">
      <slot>預設標籤內容</slot>
    </label>
  </div>
</template>

<style scoped>
/* 統整成一個全能的開關類別 */
.luiu-switch {
  /* 定義預設尺寸變數 (中等大小) */
  --sw-width: 2.5rem;
  --sw-height: 1.25rem;
  --sw-font: 1rem;
  --sw-active-color: var(--bs-primary);

  display: inline-flex;
  align-items: center;
  gap: 0.5rem; /* 控制開關與文字的間距 */
  vertical-align: middle;
  padding-left: 0;
}

.luiu-switch .form-check-input {
  float: none;
  margin: 0;
  width: var(--sw-width);
  height: var(--sw-height);
  cursor: pointer;
  flex-shrink: 0;
}

.luiu-switch .form-check-label {
  font-size: var(--sw-font);
  cursor: pointer;
  line-height: var(--sw-height);
}

/* 簡略的尺寸切換 */
.luiu-switch.sm {
  --sw-width: 1.8rem;
  --sw-height: 0.9rem;
  --sw-font: 0.85rem;
}
.luiu-switch.lg {
  --sw-width: 3.5rem;
  --sw-height: 1.75rem;
  --sw-font: 1.25rem;
}
/*嘗試看看能不能換顏色 */
.luiu-switch .form-check-input:checked {
  background-color: var(--sw-active-color);
  border-color: var(--sw-active-color);
}

.luiu-switch.sec {
  --sw-active-color: var(--bs-secondary);
}
</style>
