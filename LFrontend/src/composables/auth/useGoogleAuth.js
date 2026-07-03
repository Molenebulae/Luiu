import { ref } from "vue";
import { useRouter } from "vue-router";
import { useUserStore } from "@/stores/user";

export const useGoogleAuth = () => {
  const router = useRouter();
  const userStore = useUserStore();
  const isGoogleLoading = ref(false);

  const init = async () => {
    return Promise.resolve();
  }

  const login = () => {
    isGoogleLoading.value = true;
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
    const redirectUri = import.meta.env.VITE_GOOGLE_REDIRECT_URI;
    const scope = 'email profile';
    const responseType = 'code';
    const googleAuthUrl = `https://accounts.google.com/o/oauth2/v2/auth?client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=${responseType}&scope=${encodeURIComponent(scope)}&access_type=offline&prompt=select_account`;

    const popup = window.open(googleAuthUrl, 'GoogleLoginPopup', 'width=500,height=600');

    return new Promise((resolve, reject) => {
      const messageHandler = async (event) => {
        // 只接受自己網域的訊息
        if (event.origin !== window.location.origin) return;

        if (event.data && event.data.type === 'GOOGLE_AUTH_SUCCESS') {
          userStore.login(event.data.data); // 儲存登入結果
          isGoogleLoading.value = false; // 恢復狀態
          window.removeEventListener('message', messageHandler);
          router.push({ name: 'Home' });
          resolve(event.data.data)
        } else if (event.data && event.data.type === 'GOOGLE_AUTH_FAILED') {
          window.removeEventListener('message', messageHandler);
          isGoogleLoading.value = false;
          reject(event.data.message)
        }
      }
      window.addEventListener('message', messageHandler);
    })
  }

  const checkStatus = async () => {
    return { status: 'unknown' }
  }

  return { init, login, checkStatus, isGoogleLoading }
}
