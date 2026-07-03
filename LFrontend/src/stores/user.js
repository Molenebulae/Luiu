import { defineStore } from "pinia";
import { checkAuthApi } from "@/api/auth";
import { luiuNotify } from "@/utils/sweetAlert";
import router from '@/router'

export const useUserStore = defineStore('user', {
  state: () => ({
    userInfo: JSON.parse(localStorage.getItem('Luiu_Member')) || null,
    isInitialized: false
  }),
  getters: {
    isLoggedIn: (state) => !!state.userInfo,

    hasRole: (state) => (roles) => {
      if (!state.userInfo || !state.userInfo.role) return false;

      const requiredRoles = Array.isArray(roles) ? roles : [roles];
      return requiredRoles.includes(state.userInfo.role)
    }
  },

  actions: {
    // 登入用
    login(memberDto) {
      this.userInfo = memberDto;
      this.isInitialized = true
      localStorage.setItem('Luiu_Member', JSON.stringify(memberDto));
    },

    // 登出用
    logout(isForced = false) {
      if (isForced && this.userInfo) {
        luiuNotify.forcedLogout().then((result) => {
          if (result.isConfirmed) {
            // 使用者點選「確定並重新登入」後，安全導向登入頁
            router.push({
              name: 'MemberAuth',
              params: { mode: 'login' },
              query: { redirect: router.currentRoute.value.fullPath }
            })
          }
        })
      }
      // 移除 Pinia
      this.userInfo = null;
      this.isInitialized = false
      // 移除 LocalStorage
      localStorage.removeItem('Luiu_Member');
    },

    updateUserInfo(partialInfo) {
      // 避免userInfo為null
      if (!this.userInfo) {
        this.userInfo = {};
      }

      this.userInfo = {
        ...this.userInfo,
        ...partialInfo
      };

      // 儲存
      localStorage.setItem("Luiu_Member", JSON.stringify(this.userInfo));
    },

    async checkAuth() {
      // 本地沒有資料 -> 登出
      if (!localStorage.getItem('Luiu_Member')) {
        this.logout();
        return false;
      }

      try {
        const response = await checkAuthApi();

        if (response && response.success) {
          // 更新本地資料
          this.userInfo = response.data;
          this.isInitialized = true;
          localStorage.setItem('Luiu_Member', JSON.stringify(this.userInfo));
          return true;
        }

        // 如果 success 為 false，執行登出清空
        this.logout();
        return false;
      } catch (error) {
        if (error === 'RATE_LIMIT_HIT') {
          // 如果原本就有登入資料，就允許他繼續使用舊資料操作網頁（回傳 true）
          if (this.userInfo) {
            this.isInitialized = true;
            return true;
          }
        }

        // 其他無法處理的嚴重網路錯誤，才清空登入
        console.error('[Auth] 驗證遇到未知錯誤:', error);
        this.logout();
        return false;
      }
    },

    clearUserData() {
      this.userInfo = null
      localStorage.removeItem('Luiu_Member')
    }

  }
})
