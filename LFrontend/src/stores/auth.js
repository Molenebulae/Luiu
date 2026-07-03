import { defineStore } from "pinia";

export const useAuthStore = defineStore('auth', {
  state: () => ({
    resetEmail: '',
    resetCode: ''
  }),

  actions: {
    setResetInfo(email, code) {
      this.resetEmail = email;
      this.resetCode = code;
    },
    clearResetInfo() {
      this.resetEmail = '',
        this.resetCode = ''
    }
  }
})
