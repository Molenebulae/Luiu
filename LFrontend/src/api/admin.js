import request from "./index";

export const updateRecommendationApi = (type, id, isRecommended) => {
  return request({
    url: `Admin/${type}/${id}/recommendation`,
    method: 'post',
    data: isRecommended
  });
};
