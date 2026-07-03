import request from "./index";

// 1. 取得收藏列表
export const getFavoritesApi = () => {
  return request({
    url: '/Favorite',
    method: 'get'
  });
};

// 2. 新增收藏
export const addFavoriteApi = (data) => {
  return request({
    url: '/Favorite',
    method: 'post',
    data: data
  });
};

// 3. 移除收藏
export const removeFavoriteApi = (targetId, type) => {
  return request({
    url: `/Favorite/${targetId}/${type}`,
    method: 'delete'
  });
};
