/**
 * 統一處理後端圖片路徑
 * @param {string} path 後端回傳的相對路徑 (例如: /members/avatars/abc.jpg)
 * @returns {string} 完整的圖片網址
 */
export const getImageUrl = (path) => {
  if (!path) return ''

  // 如果路徑已經是完整的 http 開頭，直接回傳
  if (path.startsWith('blob:') || path.startsWith('http')) {
    return path
  }

  // 取得環境變數中的 API 基礎網址 (例如 http://localhost:5161)
  const baseUrl = import.meta.env.VITE_STORAGE_URL || 'http://localhost:5161'

  // 確保路徑開頭有斜線，並拼接
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const fullPath = `${baseUrl}${normalizedPath}`

  return fullPath
}
