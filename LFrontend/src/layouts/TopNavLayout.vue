<script setup>
import LuiuFooter from '@/components/layout/LuiuFooter.vue'
import LuiuNavbar from '@/components/layout/LuiuNavbar.vue'
</script>

<template>
  <div class="wrapper">
    <header>
      <LuiuNavbar />
    </header>

    <div class="content-page">
      <main class="">
        <div class="container-fluid">
          <router-view />
        </div>
      </main>

      <LuiuFooter />
    </div>
  </div>
</template>

<style>
.wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.content-page {
  padding-top: 70px;
  flex: 1;
  display: flex;
  flex-direction: column;
}

main {
  flex: 1;
}
</style>
