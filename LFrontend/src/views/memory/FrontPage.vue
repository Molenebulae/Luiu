<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import LuiuPostCard from '@/components/post/LuiuPostcard.vue';
import LuiuPostOptionsModal from '@/components/post/LuiuPostOptionsModal.vue';
import LuiuCreateMemoryModal from '@/components/post/LuiuCreateMemoryModal.vue';
import LuiuEditMemoryModal from '@/components/post/LuiuEditMemoryModal.vue';
import { getMemories, followUser, unfollowUser, getRecommendedUsers } from '@/api/memory';
import { toast } from '@/utils/sweetAlert';
import defaultAvatar from '@/assets/Images/person.svg';

const router = useRouter();

const handleImageError = (e) => {
    e.target.src = defaultAvatar;
};

const MemoryDetailView = (id) => {
    router.push({ name: 'MemoryDetail', params: { id } });
};

const goToUserProfile = (userId) => {
    router.push({ name: 'MemberProfile', params: { userId } });
};
// 控制發文彈窗
const isCreateModalOpen = ref(false);

const goToCreatePage = () => {
    isCreateModalOpen.value = true;
};

// 控制編輯貼文彈窗
const isEditModalOpen = ref(false);
const editTargetMemoryId = ref(null);

const handleEditPost = (post) => {
    editTargetMemoryId.value = post.id;
    isEditModalOpen.value = true;
};

const handleDeletePost = (postId) => {
    posts.value = posts.value.filter(p => p.id !== postId);
};

const handleFollowChange = ({ userId, isFollowing }) => {
    const foundRecommend = recommendUsers.value.find(u => u.id === userId);
    if (foundRecommend) {
        foundRecommend.isFollowing = isFollowing;
    }
};

// 控制進階選項彈窗
const isOptionsModalOpen = ref(false);
const currentSelectedPost = ref(null);

const openPostOptions = (post) => {
    currentSelectedPost.value = post;
    isOptionsModalOpen.value = true;
};

// 貼文模擬資料 (暫時保留供參考)
/*
const mockPosts = ref([
    {
        id: 1,
        author: {
            name: '旅行規劃師 Amy',
            date: '4月15日 - 4月19日',
            avatar: ''
        },
        image: logoUrl,
        title: '台灣環島五日遊',
        days: 5,
        budget: 'NT$ 11,200',
        locations: ['台北', '台中', '台東', '+2'],
        likes: 342,
        commentsCount: parseInt(localStorage.getItem('luiu_mock_comments_count_1') || '2'),
        description: '台灣環島五日遊 🟢 台北 ➡️ 台中 ➡️ 台南 ➡️ 高雄 ➡️ 花蓮\n9 個景點 · 5 天行程'
    },
    {
        id: 2,
        author: {
            name: '海島控 Amy',
            date: '墾丁白沙灣, 台灣',
            avatar: '@/images/beach-avatar.jpg'
        },
        image: '@/images/beach-scene.jpg',
        title: '夏日墾丁三天兩夜',
        days: 3,
        budget: 'NT$ 6,500',
        locations: ['屏東', '墾丁', '恆春'],
        likes: 567,
        commentsCount: parseInt(localStorage.getItem('luiu_mock_comments_count_2') || '0'),
        description: '陽光、沙灘、比基尼！超推薦白沙灣的夕陽，行程排得比較鬆，純粹放空度假 🏖️'
    }
]);
*/

// 貧載中狀態
const isLoading = ref(false);
const fetchError = ref(false);

// 貼文資料陣列
const posts = ref([]);

// 取得真實貧文
const fetchMemories = async () => {
    isLoading.value = true;
    fetchError.value = false;
    try {
        const response = await getMemories({});
        if (response && response.success) {
            // 將後端回傳的 DTO 轉換為前端元件需要的格式
            posts.value = response.data.map(mem => {
                const startDateStr = mem.startDate ? mem.startDate.replace(/-/g, '/') : '';
                const endDateStr = mem.endDate ? mem.endDate.replace(/-/g, '/') : '';
                const dateRange = startDateStr && endDateStr ? `${startDateStr} - ${endDateStr}` : '日期未定';

                let daysCount = 0;
                if (mem.startDate && mem.endDate) {
                    const s = new Date(mem.startDate);
                    const e = new Date(mem.endDate);
                    daysCount = Math.floor((e - s) / (1000 * 60 * 60 * 24)) + 1;
                }

                // 自動組成行程摘要描述
                const description = daysCount > 0
                    ? `${mem.title || '旅遊回憶'} · ${daysCount} 天行程 · ${dateRange}`
                    : (mem.title || '旅遊回憶');

                return {
                    id: mem.memoryId,
                    userId: mem.authorUserId,
                    sourceTripId: mem.sourceTripId,
                    author: {
                        name: mem.authorName || '無名旅人',
                        date: dateRange,
                        avatar: mem.authorAvatarUrl || ''
                    },
                    image: mem.coverImage || '',
                    title: mem.title || '無標題回憶',
                    days: daysCount,
                    budget: null,
                    locations: [],
                    likes: mem.likeCount || 0,
                    commentsCount: 0,
                    description
                };
            });
        }
    } catch (error) {
        // Memory API 錄遭異常：只顯示空狀態，不觸發登入狀態變更
        console.error('[Memory] 取得貧文列表失敗:', error);
        posts.value = [];
        fetchError.value = true;
    } finally {
        isLoading.value = false;
    }
};

// 推薦追蹤資料 (加入 isFollowing 狀態)
const recommendUsers = ref([]);

const fetchRecommendedUsers = async () => {
    try {
        const response = await getRecommendedUsers();
        if (response && response.success) {
            recommendUsers.value = response.data.map(u => ({
                id: u.userId,
                name: u.name || '旅人',
                desc: u.bio || '熱愛旅行的生活家',
                avatar: u.avatarUrl || '',
                isFollowing: u.isFollowing || false
            }));
        }
    } catch (error) {
        console.error('取得推薦用戶失敗:', error);
        recommendUsers.value = [];
    }
};

onMounted(() => {
    fetchMemories();
    fetchRecommendedUsers();
});

// 切換追蹤狀態的函式
const toggleFollow = async (user) => {
    // 儲存原始狀態以便失敗時還原
    const originalState = user.isFollowing;

    // 樂觀更新 UI (Optimistic UI Update)
    user.isFollowing = !user.isFollowing;

    try {
        let response;
        if (originalState) {
            response = await unfollowUser(user.id);
        } else {
            response = await followUser(user.id);
        }

        if (response && response.success) {
            toast(originalState ? '已取消追蹤' : '追蹤成功', 'success');
        } else {
            throw new Error(response?.message || '操作失敗');
        }
    } catch (error) {
        console.error('追蹤狀態切換失敗:', error);
        // 還原 UI 狀態
        user.isFollowing = originalState;

        // 如果是因為假資料 (Target 不存在) 造成的錯誤，我們特別提示
        if (error.response?.status === 400 || error.message.includes('不存在')) {
            toast('這目前是模擬資料，無法真實追蹤唷！', 'warning');
        } else {
            toast('切換追蹤狀態失敗，請稍後再試', 'error');
        }
    }
};
</script>

<template>
    <div class="container py-4">



        <!-- 最外層的 row -->
        <div class="row justify-content-center">

            <!-- 左側動態牆 (我幫你調整成 col-lg-8，這樣版面比較寬裕) -->
            <div class="col-lg-8 col-md-12">
                <!-- ！！！關鍵修正：LuiuPostCard 必須放在這個 col-lg-8 裡面！！！ -->
                <LuiuPostCard v-for="post in posts" :key="post.id" :post="post" class="mb-4"
                    @click="MemoryDetailView(post.id)" @open-options="openPostOptions" @edit-post="handleEditPost" />
            </div>

            <!-- 右側推薦追蹤區塊 -->
            <div class="col-lg-4 d-none d-lg-block">

                <!--  關鍵：我們把 sticky-top 放在最外層，把「按鈕」跟「卡片」包在一起 -->
                <div class="sticky-top" style="top: 20px;">

                    <!-- 1. 搬過來的：建立貼文按鈕 (加上 w-100 讓它與下方卡片等寬，並加上 mb-3 產生下邊距) -->
                    <button
                        class="btn btn-primary rounded-pill py-2 fw-bold text-white shadow-sm d-flex align-items-center justify-content-center w-100 mb-3"
                        style="letter-spacing: 1px;" @click="goToCreatePage">
                        <i class="ri-add-line fs-5 me-2"></i> 建立貼文
                    </button>

                    <!-- 2. 原本的：推薦追蹤白色卡片 -->
                    <div class="p-4 bg-white rounded-4 shadow-sm border-0">
                        <h6 class="fw-bold mb-4" style="color: #1a2b4c;">推薦追蹤</h6>

                        <div v-for="user in recommendUsers" :key="user.id"
                            class="d-flex align-items-center justify-content-between mb-3">
                            <div class="d-flex align-items-center gap-2" style="cursor: pointer;" @click="goToUserProfile(user.id)">
                                <div class="rounded-circle d-flex align-items-center justify-content-center overflow-hidden"
                                    style="width: 40px; height: 40px; background-color: #f0f2f5; color: #1a2b4c; font-size: 12px;">
                                    <img v-if="user.avatar" :src="$img(user.avatar)" style="width: 100%; height: 100%; object-fit: cover;" alt="avatar" @error="handleImageError">
                                    <span v-else>User</span>
                                </div>
                                <div>
                                    <p class="mb-0 fw-bold small text-hover-underline" style="color: #1a2b4c;">{{ user.name }}</p>
                                    <p class="mb-0 text-muted" style="font-size: 0.7rem;">{{ user.desc }}</p>
                                </div>
                            </div>

                            <button class="btn btn-sm rounded-pill px-3 transition-all"
                                :class="user.isFollowing ? 'btn-light text-muted border' : 'btn-outline-navy'"
                                style="font-size: 0.75rem; font-weight: 600;" @click="toggleFollow(user)">
                                {{ user.isFollowing ? '追蹤中' : '追蹤' }}
                            </button>
                        </div>

                        <p class="text-muted small mt-4 mb-0" style="font-size: 0.75rem;">© 2026 LUIU 旅遊社群平台</p>
                    </div>

                </div>
            </div>

        </div>

        <!-- IG風格貼文選項彈窗 (全域共用一個) -->
        <LuiuPostOptionsModal v-model="isOptionsModalOpen" :post="currentSelectedPost" @edit="handleEditPost" @delete="handleDeletePost" @follow-change="handleFollowChange" />

        <!-- IG風格建立貼文彈窗 -->
        <LuiuCreateMemoryModal :show="isCreateModalOpen" @close="isCreateModalOpen = false" @success="fetchMemories" />

        <!-- IG風格編輯貼文彈窗 -->
        <LuiuEditMemoryModal v-if="editTargetMemoryId" :show="isEditModalOpen" :memoryId="editTargetMemoryId" @close="isEditModalOpen = false; editTargetMemoryId = null" @success="fetchMemories" />
    </div>
</template>

<style scoped>
/* 加入一些專屬的日韓極簡風樣式 */
.transition-all {
    transition: all 0.2s ease-in-out;
}

/* 藏青色風格按鈕 (避免使用純黑) */
.btn-outline-navy {
    color: #1a2b4c;
    border: 1px solid #1a2b4c;
    background-color: transparent;
}

.btn-outline-navy:hover {
    background-color: #1a2b4c;
    color: #ffffff;
}

/* 追蹤中按鈕樣式 */
.btn-light.border {
    background-color: #f8f9fa;
    border-color: #e0e0e0 !important;
}

.text-hover-underline:hover {
    text-decoration: underline;
}
</style>
