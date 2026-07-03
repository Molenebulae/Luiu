<script setup>
import { ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import LuiuAvator from '@/components/base/LuiuAvator.vue';
import { toggleMemoryLike } from '@/api/memory';
import { addFavoriteApi, removeFavoriteApi } from '@/api/favorite';
import { useUserStore } from '@/stores/user';
import logoUrl from '@/assets/Images/P1.jpg';

const props = defineProps({
    post: {
        type: Object,
        required: true,
        default: () => ({
            id: 1,
            author: { name: '旅行規劃師 Amy', date: '4月15日 - 4月19日', avatar: '' },
            image: 'https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=800&q=80',
            title: '台灣環島五日遊',
            days: 5,
            budget: 'NT$ 11,200',
            locations: ['台北', '台中', '台東', '+2'],
            likes: 342,
            commentsCount: 28,
            description: '台灣環島五日遊 🟢 台北 ➡️ 台中 ➡️ 台南 ➡️ 高雄 ➡️ 花蓮\n9 個景點 · 5 天行程'
        })
    }
});

const emit = defineEmits(['click', 'open-options', 'edit-post']);

const router = useRouter();
const userStore = useUserStore();

const isAuthor = computed(() => {
    if (!userStore.isLoggedIn || !userStore.userInfo) return false;

    // 檢查作者 ID 是否與當前登入使用者的 ID 相符
    // (因後端可能傳整數 MemberId 或是字串 UserId，這邊先強制轉型統一比較)
    if (String(props.post.author?.id) === String(userStore.userInfo.userId)) {
        return true;
    }

    // 暫時備案：若 ID 格式對不上，可透過名字比對 (因假資料或舊資料可能沒給對)
    if (props.post.author?.name === userStore.userInfo.name) {
        return true;
    }

    return false;
});

const isLiked = ref(false);
const isSaved = ref(false);

// 元件掛載時，從 localStorage 檢查此貼文是否已由該用戶按過讚與收藏
onMounted(() => {
    try {
        const likedList = JSON.parse(localStorage.getItem('luiu_liked_memories') || '[]');
        if (Array.isArray(likedList) && props.post.id) {
            isLiked.value = likedList.includes(props.post.id);
        }

        const savedList = JSON.parse(localStorage.getItem('luiu_saved_memories') || '[]');
        if (Array.isArray(savedList) && props.post.id) {
            isSaved.value = savedList.includes(props.post.id);
        }
    } catch (e) {
        console.error('讀取按讚或收藏快取失敗:', e);
    }
});

// 切換按讚狀態並串接 API 與同步 localStorage
const toggleLike = async () => {
    if (!props.post.id) return;
    const targetState = !isLiked.value;

    try {
        // 發送 API 請求到後端
        const response = await toggleMemoryLike(props.post.id, targetState);
        if (response && response.success) {
            isLiked.value = targetState;

            // 更新本機的 localStorage 紀錄
            const likedList = JSON.parse(localStorage.getItem('luiu_liked_memories') || '[]');
            if (targetState) {
                if (!likedList.includes(props.post.id)) {
                    likedList.push(props.post.id);
                }
            } else {
                const index = likedList.indexOf(props.post.id);
                if (index > -1) {
                    likedList.splice(index, 1);
                }
            }
            localStorage.setItem('luiu_liked_memories', JSON.stringify(likedList));
        } else {
            alert(response?.message || '按讚操作失敗，請確認是否已登入！');
        }
    } catch (error) {
        console.error('按讚請求發生錯誤:', error);
        alert('❤️ 操作失敗，請先登入會員以進行按讚！');
    }
};

const toggleSave = async () => {
    if (!props.post.id) return;

    // 如果當前未登入，擋住
    if (!userStore.isLoggedIn) {
        alert('🔖 請先登入會員以進行收藏！');
        return;
    }

    const targetState = !isSaved.value;

    try {
        let response;
        if (targetState) {
            const ownerUserId = props.post.author?.id ? String(props.post.author.id) : null;
            response = await addFavoriteApi({ targetId: props.post.id, type: 'Memory', ownerUserId });
        } else {
            response = await removeFavoriteApi(props.post.id, 'Memory');
        }

        if (response && response.success) {
            isSaved.value = targetState;

            // 更新 localStorage
            const savedList = JSON.parse(localStorage.getItem('luiu_saved_memories') || '[]');
            if (targetState) {
                if (!savedList.includes(props.post.id)) {
                    savedList.push(props.post.id);
                }
            } else {
                const index = savedList.indexOf(props.post.id);
                if (index > -1) {
                    savedList.splice(index, 1);
                }
            }
            localStorage.setItem('luiu_saved_memories', JSON.stringify(savedList));
        } else {
            alert(response?.message || '收藏操作失敗，請確認是否已登入！');
        }
    } catch (error) {
        console.error('收藏請求發生錯誤:', error);
        
        // 從 Axios 攔截器拿到的錯誤訊息
        const errMsg = error?.message || error || '';
        
        if (typeof errMsg === 'string' && errMsg.includes('項目已被收藏')) {
            // 狀態不同步：其實已經收藏過，直接修正前端顯示狀態
            isSaved.value = true;
            const savedList = JSON.parse(localStorage.getItem('luiu_saved_memories') || '[]');
            if (!savedList.includes(props.post.id)) {
                savedList.push(props.post.id);
                localStorage.setItem('luiu_saved_memories', JSON.stringify(savedList));
            }
        } else if (typeof errMsg === 'string' && (errMsg.includes('找不到符合條件') || errMsg.includes('無法移除'))) {
            // 狀態不同步：其實早就沒有收藏，直接修正前端顯示狀態
            isSaved.value = false;
            const savedList = JSON.parse(localStorage.getItem('luiu_saved_memories') || '[]');
            const index = savedList.indexOf(props.post.id);
            if (index > -1) {
                savedList.splice(index, 1);
                localStorage.setItem('luiu_saved_memories', JSON.stringify(savedList));
            }
        } else {
            alert('🔖 操作失敗，請稍後再試！');
        }
    }
};

// 跳轉到回憶明細頁
const goToDetail = () => {
    if (props.post.id) {
        router.push({ name: 'MemoryDetail', params: { id: props.post.id } });
    }
};

// 點擊留言按鈕：跳轉至明細頁，並自動捲動到留言區塊
const handleCommentClick = () => {
    if (props.post.id) {
        router.push({
            name: 'MemoryDetail',
            params: { id: props.post.id },
            hash: '#comment-input-section' // 配合明細頁的 ID，實現自動定位
        });
    }
};

// 點擊分享按鈕：優先使用原生分享，否則複製連結至剪貼簿
const handleShareClick = () => {
    const shareUrl = `${window.location.origin}/MemoryDetail/${props.post.id}`;
    if (navigator.share) {
        navigator.share({
            title: props.post.title,
            text: props.post.description || '分享我的旅遊回憶！',
            url: shareUrl
        }).catch(err => {
            console.log('取消分享或分享失敗:', err);
        });
    } else {
        navigator.clipboard.writeText(shareUrl)
            .then(() => {
                alert('🔗 已成功複製此回憶的分享連結！');
            })
            .catch(err => {
                console.error('無法複製連結:', err);
            });
    }
};

const handleImageError = (e) => {
    e.target.src = logoUrl;
};
</script>

<template>
    <div class="card border-0 shadow-sm rounded-4 mb-4 overflow-hidden bg-white">

        <!-- 1. 卡片頭部：作者資訊 -->
        <div class="card-header bg-white border-0 py-3 px-3 d-flex justify-content-between align-items-center">
            <!-- 直接使用我們做好的 LuiuUserRow！ -->
            <LuiuAvator :username="post.author.name" :subtitle="post.author.date" :avatar="$img(post.author.avatar)"
                size="md" />
            <div class="d-flex align-items-center">
                <button v-if="isAuthor" class="btn btn-sm text-muted me-1" title="編輯貼文"
                    @click.stop="emit('edit-post', post)">
                    <i class="ri-edit-box-line fs-5"></i>
                </button>
                <!-- 右上角的三個點選單 (可選) -->
                <button class="btn btn-sm text-muted" @click.stop="emit('open-options', post)">
                    <i class="ri-more-fill fs-5"></i>
                </button>
            </div>
        </div>

        <!-- 2. 卡片圖片區 (Hover 縮放效果) -->
        <div class="post-image-container position-relative overflow-hidden cursor-pointer" style="aspect-ratio: 4/5;"
            @click="goToDetail">
            <img :src="$img(post.image) || logoUrl" class="post-image w-100 h-100 object-fit-cover" alt="Post Image"
                @error="handleImageError">

            <!-- 覆蓋在圖片上的標籤 -->
            <div class="position-absolute top-0 start-0 w-100 p-3 d-flex justify-content-between pointer-events-none">
                <span class="badge bg-dark bg-opacity-75 rounded-pill px-3 py-2">{{ post.days }} 天</span>
                <!-- budget 為 null 或空時隱藏 -->
                <span v-if="post.budget" class="badge bg-dark bg-opacity-75 rounded-pill px-3 py-2">{{ post.budget
                }}</span>
            </div>

            <!-- 中間的標題 -->
            <div
                class="position-absolute top-50 start-50 translate-middle text-center w-100 pointer-events-none text-white text-shadow">
                <h3 class="fw-bold mb-2">{{ post.title }}</h3>
                <p class="small mb-0 opacity-75">點擊查看完整行程</p>
            </div>

            <!-- 底部的目的地標籤 -->
            <div class="position-absolute bottom-0 start-0 p-3 d-flex gap-2 pointer-events-none">
                <span v-for="loc in post.locations" :key="loc" class="badge bg-white text-dark rounded-pill shadow-sm">
                    <i class="ri-map-pin-line small me-1"></i>{{ loc }}
                </span>
            </div>
        </div>

        <!-- 3. 卡片底部互動區 (按讚、留言、分享) -->
        <div class="card-body px-3 pt-3 pb-2">
            <div class="d-flex justify-content-between mb-2 align-items-center">
                <div class="d-flex gap-1">
                    <button class="btn btn-link text-dark p-1 text-decoration-none" @click.stop="toggleLike">
                        <i :class="isLiked ? 'ri-heart-3-fill text-danger fs-4' : 'ri-heart-3-line fs-4'"></i>
                    </button>
                    <button class="btn btn-link text-dark p-1 text-decoration-none" @click.stop="handleCommentClick">
                        <i class="ri-chat-3-line fs-4"></i>
                    </button>
                    <button class="btn btn-link text-dark p-1 text-decoration-none" @click.stop="handleShareClick">
                        <i class="ri-send-plane-line fs-4"></i>
                    </button>
                </div>
                <!-- 收藏按鈕 -->
                <button class="btn btn-link text-dark p-1 text-decoration-none" @click.stop="toggleSave">
                    <i :class="isSaved ? 'ri-bookmark-fill fs-4' : 'ri-bookmark-line fs-4'"></i>
                </button>
            </div>

            <!-- 按讚數 -->
            <p class="fw-bold mb-1 small">{{ isLiked ? post.likes + 1 : post.likes }} 個讚</p>

            <!-- 貼文描述 -->
            <p class="mb-1 small text-dark" style="white-space: pre-wrap;">
                <span class="fw-bold me-2">{{ post.author.name }}</span>{{ post.description }}
            </p>

            <!-- 查看留言 (有留言才顯示) -->
            <a v-if="post.commentsCount > 0" href="#" class="text-muted small text-decoration-none"
                @click.stop.prevent="goToDetail">查看全部 {{ post.commentsCount }} 則留言</a>
        </div>

        <!-- 4. 查看完整行程按鈕 -->
        <div class="card-footer bg-white border-0 px-3 pb-3 pt-0">
            <button class="btn btn-light w-100 fw-bold border text-dark" @click="goToDetail">
                查看完整行程
            </button>
        </div>
    </div>
</template>

<style scoped>
/* 卡片整體懸停效果 (Premium Lift Effect) */
.card {
    transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.3s ease;
}

.card:hover {
    transform: translateY(-6px);
    box-shadow: 0 12px 24px rgba(0, 0, 0, 0.08) !important;
}

/* Hover 圖片縮放效果 */
.post-image-container .post-image {
    transition: transform 0.4s ease;
}

.post-image-container:hover .post-image {
    transform: scale(1.05);
    /* Hover 時稍微放大 */
}

/* 確保浮動在圖片上的元素不會擋住滑鼠事件 (例如圖片縮放) */
.pointer-events-none {
    pointer-events: none;
}

/* 指標手勢 */
.cursor-pointer {
    cursor: pointer;
}

/* 文字陰影，讓白色文字在圖片上更清楚 */
.text-shadow {
    text-shadow: 1px 1px 4px rgba(0, 0, 0, 0.6);
}
</style>