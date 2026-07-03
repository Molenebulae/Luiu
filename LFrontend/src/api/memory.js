// 從同一個資料夾的 index.js 拿出你設定好的「超級版 axios」
import request from './index'

// ==========================================
//  1. 行程與貼文相關 (Memory API)
// ==========================================
export const getMemories = (params) => {
  return request({
    url: '/Memory',
    method: 'get',
    params: params, // GET 請求通常用 params 來帶查詢條件 (?key=value)
  })
}

export const getMemoryDetail = (id) => {
  return request({
    url: `/Memory/${id}`,
    method: 'get',
  })
}

export const createMemory = (data) => {
  return request({
    url: '/Memory',
    method: 'post',
    data: data, // POST 請求把資料放在 body，用 data
  })
}

export const updateMemory = (id, data) => {
  return request({
    url: `/Memory/${id}`,
    method: 'put',
    data: data,
  })
}

export const toggleMemoryLike = (id, isLike) => {
  return request({
    url: `/Memory/${id}/like`,
    method: 'post',
    params: { isLike },
  })
}

export const getMemoriesByUserId = async (userId) => {
  return request({
    url: `/Memory/user/${userId}`,
    method: 'get'
  })
}

export const getHomeHotMemoriesApi = async () => {
  return request({
    url: '/Memory/hot',
    method: 'get'
  })
}

// ==========================================
//  2. 社群與使用者相關 (Social API)
// ==========================================
export const getRecommendedUsers = () => {
  return request({
    url: '/Recommends',
    method: 'get',
  })
}

export const followUser = (userId) => {
  return request({
    url: `/Follows/${userId}`,
    method: 'post',
  })
}

export const unfollowUser = (userId) => {
  return request({
    url: `/Follows/${userId}`,
    method: 'delete',
  })
}

export const checkFollowStatus = (userId) => {
  return request({
    url: `/Follows/${userId}/status`,
    method: 'get',
  })
}

export const deleteMemory = (id) => {
  return request({
    url: `/Memory/${id}`,
    method: 'delete',
  })
}

// ==========================================
//  3. 檔案上傳相關 (Upload API)
// ==========================================
export const uploadFile = (data) => {
  return request({
    url: '/upload',
    method: 'post',
    data: data,
    headers: { 'Content-Type': 'multipart/form-data' }, // 檔案上傳專屬的 Header 照樣放進來
  })
}
