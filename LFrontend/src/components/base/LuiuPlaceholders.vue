<!-- 這東西引用自 AdminLTE 3 / Bootstrap 4 還有Bootstrap 5擴展樣式 -->
<!-- 在模板的app.min.css檔案裡(讚嘆beautifier.io，有夠難讀的一大坨)，目前塞在這專案的app.min.css了 -->
<!-- 使用方法:外面包一層div.placeholder-body，裡面再塞.placeholder-start、.placeholder-line 與 .placeholder-end -->
<!-- 或者直接外面包div.placeholder-glow，裡面有placeholder標籤就會吃到了，會依照實際字串長度調整 -->
<script setup>
import { onMounted } from 'vue'
onMounted(() => {})
</script>

<template>
  <div>placeholder-body</div>
  <div class="placeholder-body">
    <div>placeholder-start square</div>
    <div class="placeholder-start square"></div>
    <div>placeholder-start circle</div>
    <div class="placeholder-start circle"></div>
    <div>placeholder-end</div>
    <div class="placeholder-end">
      <div>placeholder-line placeholder w-25</div>
      <div class="placeholder-line placeholder w-25"></div>
      <div>placeholder-line placeholder w-100</div>
      <div class="placeholder-line placeholder w-100"></div>
      <div>placeholder-line placeholder placeholder-xs w-100</div>
      <div class="placeholder-line placeholder placeholder-xs w-100"></div>
      <div>placeholder-line placeholder placeholder-xs w-50</div>
      <div class="placeholder-line placeholder placeholder-xs w-50"></div>
    </div>
  </div>
  <div>placeholder-glow</div>
  <div class="placeholder-glow">
    <span class="placeholder"
      >12323132121321111111111111111111111111111111111111111111111111111111111</span
    >
    <hr />
    <div class="placeholder">1</div>
  </div>
</template>

<style></style>
