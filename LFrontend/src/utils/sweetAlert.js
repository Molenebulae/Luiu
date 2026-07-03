import Swal from "sweetalert2";


const baseOptions = {
  customClass: {
    confirmButton: "luiu-btn-primary mx-2",
    cancelButton: "luiu-btn-outline-secondary mx-2",
    denyButton: "luiu-btn-danger mx-2",
    popup: "rounded-4",
  },
  buttonsStyling: false,
  reverseButtons: true
};
export const LuiuAlert = Swal.mixin(baseOptions);

const ToastInstance = Swal.mixin({
  toast: true,
  position: 'top-end',
  showConfirmButton: false,
  timer: 2000,
  timerProgressBar: true,
  customClass: {
    popup: 'luiu-toast-popup'
  }
});
export const toast = (title, icon = 'success') => {
  ToastInstance.fire({ icon, title });
};

export const luiuNotify = {
  success: (title, text = "") => {
    return LuiuAlert.fire({
      icon: 'success',
      title,
      text,
      confirmButtonText: '確定'
    })
  },

  confirmCancel: (title = "確定要放棄修改嗎？", text = "您填寫的變更資料將不會被儲存。") => {
    return LuiuAlert.fire({
      icon: 'warning',
      title,
      text,
      showCancelButton: true,
      confirmButtonText: '確定放棄',
      cancelButtonText: '繼續修改',
      customClass: {
        ...baseOptions.customClass,
        confirmButton: 'luiu-btn-primary mx-2',       // 活力橘主鈕
        cancelButton: 'luiu-btn-outline-dark mx-2'   // 灰黑細框輔助鈕
      }
    });
  },

  confirmDelete: (title = "確定要刪除嗎?", text = "此動作無法還原！") => {
    return LuiuAlert.fire({
      icon: 'warning',
      title,
      text,
      showCancelButton: true,
      confirmButtonText: '是的，刪除它',
      cancelButtonText: '取消',
      customClass: {
        ...baseOptions.customClass, // 繼承基礎 class
        confirmButton: 'luiu-btn-danger mx-2' // 覆寫確認鈕為紅色
      }
    });
  },

  logout: () => {
    return LuiuAlert.fire({
      title: '確定要登出嗎?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: '確定登出',
      cancelButtonText: '我再待會',
      customClass: {
        confirmButton: "luiu-btn-danger mx-2",
        cancelButton: "luiu-btn-outline-secondary mx-2",
        popup: "rounded-4"
      }
    })
  },

  forcedLogout: (
    title = "登入連線已過期",
    text = "基於安全性考量或帳號已在其他裝置登入，請重新登入。"
  ) => {
    return LuiuAlert.fire({
      icon: 'warning',
      title,
      text,
      confirmButtonText: '確定並重新登入',
      allowOutsideClick: false, // 禁用點擊外部關閉
      allowEscapeKey: false,     // 禁用 ESC 鍵關閉
      customClass: {
        ...baseOptions.customClass,
        confirmButton: 'luiu-btn-primary mx-2' // 使用你們專屬的活力橘主鈕
      }
    });
  }
}
