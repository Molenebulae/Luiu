<script setup>
import LuiuNavbar from '@/components/layout/LuiuNavbar.vue'
</script>

<template>
  <div class="wrapper only-top-nav-layout">
    <header>
      <LuiuNavbar />
    </header>

    <div class="content-page">
      <main class="layout-main">
        <div class="container-fluid">
          <router-view />
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
.only-top-nav-layout {
  display: grid;
  grid-template-rows: auto minmax(0, 1fr);
  height: 100dvh;
  overflow: hidden;
}

.content-page {
  min-height: 0;
  overflow: hidden;
  padding: 0;
}

.layout-main,
.content-page .container-fluid {
  height: 100%;
  min-height: 0;
  overflow: hidden;
}

.content-page .container-fluid {
  padding: 0;
}
</style>
