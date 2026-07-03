import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'
import HomeView from '@/views/HomeView.vue'
import TopNavLayout from '@/layouts/TopNavLayout.vue'
import SideNavLayout from '@/layouts/SideNavLayout.vue'
import OnlyTopNavLayout from '@/layouts/OnlyTopNavLayout.vue'
import { updateDocumentTitle } from '@/utils/titleHelper'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: TopNavLayout,
      children: [
        {
          path: '',
          name: 'Home',
          component: HomeView,
          alias: ['/index', '/main'],
          meta: { title: '首頁 | Luiu' },
        },
        {
          path: ':userId/plan-list',
          name: 'PlanList',
          component: () => import('@/views/plan/PlanListView.vue'),
          meta: { title: '行程列表 | Luiu', requiresAuth: true },
        },
        {
          path: ':userId/plan-info/:planId',
          name: 'PlanInfo',
          component: () => import('@/views/plan/PlanInfo.vue'),
          meta: { title: '行程資訊 | Luiu' , requiresAuth: true},
        },
        {
          path: 'member',
          children: [
            {
              path: 'profile/:userId',
              name: 'MemberProfile',
              component: () => import('@/views/member/ProfileView.vue'),
              props: true,
              meta: { title: '個人頁面 | Luiu' },
            },
            {
              path: 'settings',
              name: 'MemberSetting',
              component: () => import('@/views/member/SettingsView.vue'),
              meta: { title: '會員設定 | Luiu', requiresAuth: true },
            },
          ],
        },
      ],
    },

    {
      path: '/:userId/plan-list/',
      component: OnlyTopNavLayout,
      children: [
        {
          path: ':tripId',
          name: 'Plan',
          component: () => import('@/views/plan/PlanView.vue'),
          meta: { title: '編輯行程 | Luiu' },
        },
      ],
    },
    {
      path: '/auth/:mode',
      name: 'MemberAuth',
      component: () => import('@/views/member/AuthView.vue'),
      props: true,
    },
    {
      path: '/auth/google-auth',
      component: () => import('@/views/member/GoogleCallBack.vue'),
    },
    {
      path: '/memory',
      component: SideNavLayout,
      children: [
        {
          path: '',
          name: 'Memory',
          component: () => import('@/views/memory/FrontPage.vue'),
          meta: { title: '回憶錄 | Luiu' },
        },
      ],
    },

    // 記得在這裡加上 MemoryDetail 的路由設定，並且路徑要帶上 :id 參數，這樣才能在 MemoryDetailView.vue 裡面用 useRoute() 拿到這個 id 參數！
    {
      path: '/MemoryDetail/:id',
      name: 'MemoryDetail',
      component: () => import('@/views/memory/MemoryDetailView.vue'), // 這是你要新建立的頁面
      meta: { title: '回憶詳情 | Luiu' },
    },

    // 直接放404的內容，網址不變
    {
      path: '/:pathMatch(.*)*',
      component: TopNavLayout,
      children: [{ path: '', component: () => import('@/views/NotFoundView.vue') }],
      meta: { title: '找不到資源' },
    },
  ],
})

router.beforeEach(async (to, from) => {
  const userStore = useUserStore()
  await userStore.checkAuth()

  updateDocumentTitle(to);
  if (to.path === '/plan-list') {
    if (userStore.isLoggedIn && userStore.userInfo?.userId) {
      return {
        name: 'PlanList',
        params: { userId: userStore.userInfo.userId },
      }
    }

    return {
      name: 'MemberAuth',
      params: { mode: 'login' },
      query: { redirect: to.fullPath },
    }
  }

  // if (userStore.isInitialed === false) {
  //   await userStore.checkAuth()
  // }

  // 需要登入的頁面檢查權限
  if (to.matched.some((record) => record.meta.requiresAuth)) {
    const isAuthenticated = userStore.isLoggedIn

    // 沒登入，
    if (!isAuthenticated) {
      return {
        name: 'MemberAuth',
        params: { mode: 'login' },
        query: { redirect: to.fullPath },
      }
    }
    return true
  }

  // 不需要登入的頁面
  else {
    // 登入後避免前往登入畫面
    if (userStore.isLoggedIn && to.name === 'MemberAuth') {
      return { name: 'Home' }
    }
    return true
  }
})

// 先保留
// router.beforeEach(async (to, from) => {
//   const userStore = useUserStore()

//   if (to.matched.some((record) => record.meta.requiresAuth)) {
//     const isAuthenticated = userStore.isLoggedIn

//     if (!isAuthenticated) {
//       return {
//         name: 'MemberAuth',
//         params: { mode: 'login' },
//         query: { redirect: to.fullPath },
//       }
//     }
//     return true
//   } else {
//     // // 不需要登入的頁面
//     if (userStore.isLoggedIn && to.name === 'MemberAuth') {
//       // 如果有登入，檢查身份
//       return { name: 'Home' };
//     }
//     return true
//   }
// })

export default router
