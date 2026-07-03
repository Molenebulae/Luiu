import request from "./index";

/**
 *
 * @param {string} userId
 * @returns
 */
export const getProfileApi = (userId) => {
  return request({
    url: `/Member/profile/${userId}`,
    method: 'get',
  })
}

export const updateProfileApi = (userId, data) => {
  return request({
    url: `/Member/profile/${userId}`,
    method: 'put',
    data: data
  });
}

export const updateMemberSettingsApi = (data) => {
  return request({
    url: '/Member/settings',
    method: 'put',
    data: data
  });
}

export const changePasswordApi = (data) => {
  return request({
    url: '/Member/password',
    method: 'put',
    data: data
  })
}

export const deleteAccountApi = (data) => {
  return request({
    url: '/Member/account',
    method: 'delete',
    data: data
  })
}
