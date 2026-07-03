import request from "./index";

export const uploadFileApi = (formData) => {
  return request({
    url: '/Files',
    method: 'post',
    data: formData,
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}
