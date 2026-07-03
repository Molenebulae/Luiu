<script setup>
import { useRoute } from 'vue-router';

// 使用useRoute(結尾沒有r) 取得網址上資料在example 的params 的id
const coute = useRoute();
const number = coute.params.id;
</script>

<template>
    <h2>範例二</h2>
    <label for="">{{ number }}</label>
</template>
