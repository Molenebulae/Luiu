<script setup>
import SettingProfile from '@/components/member/settings/SettingProfile.vue'
import SettingSecurity from '@/components/member/settings/SettingSecurity.vue'
import SettingNotifications from '@/components/member/settings/SettingNotifications.vue'
import SettingDeleteAccount from '@/components/member/settings/SettingDeleteAccount.vue'
import { ref, computed } from 'vue'

const activeTab = ref('profile')

const nvaItems = [
  {
    id: 'profile',
    name: '帳戶資料',
    icon: 'bi-person-circle',
    component: SettingProfile,
    variant: 'default',
  },
  {
    id: 'security',
    name: '帳戶安全',
    icon: 'bi-shield-lock',
    component: SettingSecurity,
    variant: 'default',
  },
  {
    id: 'notifications',
    name: '通知設定',
    icon: 'bi-bell',
    component: SettingNotifications,
    variant: 'default',
  },
  {
    id: 'delete',
    name: '刪除帳號',
    icon: 'bi-trash',
    component: SettingDeleteAccount,
    variant: 'danger',
  }, // 宣告為危險配色
]

const currentComponent = computed(() => {
  const current = nvaItems.find((item) => item.id === activeTab.value)
  return current ? current.component : SettingProfile
})
</script>

<template>
  <div class="container-fluid py-5 settings-container">
    <div class="row">
      <div class="col-3">
        <div class="list-group list-group-flush shadow-sm rounded-3">
          <button
            v-for="item in nvaItems"
            :key="item.id"
            class="list-group-item list-group-item-action py-3 border-0 d-flex align-items-center custom-tab-item"
            :class="{
              active: activeTab === item.id,
              'text-danger': item.variant === 'danger',
            }"
            @click="activeTab = item.id"
          >
            <i :class="[item.icon, 'me-3 fs-5']"></i>{{ item.name }}
          </button>
        </div>
      </div>

      <div class="col-9">
        <div class="card">
          <!-- 放入畫面 -->
          <keep-alive>
            <component :is="currentComponent" />
          </keep-alive>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
@import '@/assets/scss/pages/settings';
</style>
