export async function fetchPlan(userId, planId) {
  // Simulate async fetch with a small delay
  await new Promise(resolve => setTimeout(resolve, 200));
  // Return mock data matching the structure used in PlanInfo.vue
  return {
    id: planId,
    name: `示範行程 ${planId}`,
    coverImage: 'https://images.unsplash.com/photo-1522120690-21172e0b3c76?w=1200&auto=format',
    description: '這是一個示範行程的簡介。',
    days: [
      {
        day: 1,
        label: 'Day 1',
        date: '4月20日',
        spots: [
          {
            name: '台北101',
            description: '觀景台俯瞰全市',
            image: 'https://images.unsplash.com/photo-1522120690-21172e0b3c76?w=800&auto=format',
            transport: '捷運紅線',
          },
          {
            name: '國立故宮博物院',
            description: '欣賞中國古代藝術珍品',
            image: 'https://images.unsplash.com/photo-1519337265831-281ec6cc8514?w=800&auto=format',
            transport: '公車 304',
          },
        ],
      },
      {
        day: 2,
        label: 'Day 2',
        date: '4月21日',
        spots: [],
      },
    ],
  };
}

// Export as default for convenience in imports
export default { fetchPlan };
