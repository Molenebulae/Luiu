<script setup>
import { useRouter } from 'vue-router'
import { ref } from 'vue'
import { RouterLink } from 'vue-router'

// 使用 useRouter(多一個r)的push 去到指定的頁面使用name，參數用params傳遞
const router = useRouter()
const number = ref(0)
function GoTo2() {
  router.push({
    name: 'ExampleInfo',
    params: {
      id: number.value,
    },
  })
}
</script>

<template>
  <h2>範例vue</h2>
  <input v-model="number" type="number" min="1" max="100" />
  <button @click="GoTo2">去範例二</button>

  <br />
  <RouterLink class="luiu-btn-primary" :to="{ name: 'ExampleComponent' }"
    >測試Component頁面</RouterLink
  >
</template>

<style lang="scss" scoped>
@import '@/assets/scss/components/buttons';
</style>
