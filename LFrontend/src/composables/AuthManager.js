import { useGoogleAuth } from "./auth/useGoogleAuth";
import { computed } from "vue";

export const useAuthManager = () => {
  const google = useGoogleAuth();

  const login = async (provider) => {
    switch (provider) {
      case 'google': return await google.login();
      default: throw new Error('未知的登入方式');
    }
  }

  const isAnyProviderLoading = computed(() => {
    return google.isGoogleLoading.value;
  })

  return {
    login,
    isAnyProviderLoading
  }
}
