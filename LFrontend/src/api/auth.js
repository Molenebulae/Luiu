import request from "./index";

export const loginApi = (data) => {
  return request({
    url: '/Auth/login',
    method: 'post',
    data: data
  })
};

export const demoLoginApi = () => {
  return request({
    url: '/Auth/login/demo',
    method: 'post'
  })
};

export const logoutApi = (data) => {
  return request({
    url: '/Auth/logout',
    method: 'post',
    data: data
  })
};

export const registerSendCodeApi = (data) => {
  return request({
    url: '/Auth/register/send-code',
    method: 'post',
    data: data
  })
};

export const registerConfirmCodeApi = (data) => {
  return request({
    url: '/Auth/register/confirm-code',
    method: 'post',
    data: data
  })
}

export const resetSendCodeApi = (data) => {
  return request({
    url: '/Auth/forgot-password/send-code',
    method: 'post',
    data: data
  })
};

export const resetConfirmCodeApi = (data) => {
  return request({
    url: '/Auth/forgot-password/confirm-code',
    method: 'post',
    data: data
  })
}

export const resetPasswordApi = (data) => {
  return request({
    url: '/Auth/forgot-password/reset',
    method: 'post',
    data: data
  })
}

export const googleLoginApi = (data) => {
  return request({
    url: '/Auth/login/google',
    method: 'post',
    data: data
  })
}

export const checkAuthApi = () => {
  return request({
    url: '/Auth/me',
    method: 'get',
  })
}
