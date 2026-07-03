<script setup>
import LuiuAvator from '@/components/base/LuiuAvator.vue'
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getProfileApi, updateProfileApi } from '@/api/member'
import { asArrayPayload, getPlanList } from '@/api/planning/plan'
import { uploadFileApi } from '@/api/file'
import ProfileTrips from '@/components/member/profile/ProfileTrips.vue'
import ProfileMemories from '@/components/member/profile/ProfileMemories.vue'
import ProfileCollections from '@/components/member/profile/ProfileCollections.vue'
import ProfileEditModal from '@/components/member/profile/ProfileEditModal.vue'
import PlanCreateDialog from '@/components/Planning/PlanCreateDialog.vue'

const props = defineProps({
  userId: { type: String, required: true },
})
const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

// 個人頁面資料
const profileData = ref({
  userId: '',
  name: '',
  bio: '',
  avatarUrl: undefined,
  tripCount: 0,
  memoryCount: 0,
  collectCount: 0,
  socialLinks: {
    facebook: '',
    google: '',
    // github: '',    // 擴充你想支援的平台
    // linkedin: ''
  },
})
const isMyOwnProfile = computed(() => {
  // 沒登入
  if (!userStore.userInfo) return false

  return userStore.userInfo.userId === route.params.userId
})
const socialMedialList = computed(() => ({
  facebook: profileData.value?.socialLinks?.facebook || '',
  google: profileData.value?.socialLinks?.google || '',
}))
const isEditModalOpen = ref(false)
const isCreateDialogOpen = ref(false)
const activeTripCount = ref(0)

const isDeletedTrip = (trip) => trip?.IsDelete === 1 || trip?.IsDelete === '1'

const onProfileSave = async (updateData) => {
  console.log(updateData)

  try {
    // 先更新圖片
    let finalAvatarUrl = profileData.value.avatarUrl
    if (updateData.rawFile) {
      const formData = new FormData()
      formData.append('file', updateData.rawFile)
      formData.append('type', 'avatar')

      const fileRes = await uploadFileApi(formData)
      finalAvatarUrl = fileRes.data
      console.log('儲存後的圖片路徑', finalAvatarUrl)
    }

    // TODO: 更新到可以修改所有的個人頁面的欄位
    // 目前需要更新的資料
    const newData = {
      userId: updateData.userId,
      name: updateData.name,
      bio: updateData.bio,
      avatarUrl: finalAvatarUrl,
    }

    // 更新
    // TODO: 待更新JWT
    const response = await updateProfileApi(props.userId, newData)
    const updateUser = response.data
    userStore.updateUserInfo({ ...updateUser, avatarUrl: finalAvatarUrl })

    profileData.value = { ...profileData.value, ...updateData } // 更新頁面上的資料
    if (props.userId !== response.data.userId) {
      router.replace({
        name: 'MemberProfile',
        params: {
          userId: updateUser.userId,
        },
      })
    }

    isEditModalOpen.value = false // 關閉修改頁面
  } catch (err) {
    console.error(err)
    alert(err.message || '更新失敗')
  }
}

const getIconPath = (platform) => {
  return new URL(`../../assets/icons/${platform}_48x48.svg`, import.meta.url).href
}

const handleSocialClick = (platform, url) => {
  console.log(platform, url)
}

const openCreateDialog = () => {
  isCreateDialogOpen.value = true
}

const closeCreateDialog = () => {
  isCreateDialogOpen.value = false
}

const handlePlanCreated = (tripId) => {
  closeCreateDialog()
  router.push({
    name: 'Plan',
    params: {
      userId: props.userId,
      tripId,
    },
  })
}

// 底下頁面切換
const activeTab = ref('trips')
const tabs = computed(() => {
  const allTabs = [
    { id: 'trips', name: '行程規劃', count: profileData.value?.tripCount ?? 0 },
    { id: 'memories', name: '旅遊回憶', count: profileData.value?.memoryCount ?? 0 },
    { id: 'collections', name: '收藏清單', count: profileData.value?.collectCount ?? 0 },
  ]

  if (!isMyOwnProfile.value) {
    return allTabs.filter((tab) => tab.id !== 'collections')
  }

  return allTabs
})

const changeTab = (tabId) => {
  // 如果試圖切換到收藏且不是本人，直接返回或重導向到行程
  if (tabId === 'collections' && !isMyOwnProfile.value) {
    activeTab.value = 'trips'
    return
  }
  activeTab.value = tabId
}

const tabComponents = {
  trips: ProfileTrips,
  memories: ProfileMemories,
  collections: ProfileCollections,
}

const loadProfile = async () => {
  try {
    // 取得個人頁面資料
    const response = await getProfileApi(props.userId)

    profileData.value = {
      ...profileData.value,
      ...response.data,
      userId: props.userId,
    }
    console.log(profileData.value)
  } catch (error) {
    console.error('載入失敗', error)
  }
}

const loadActiveTripCount = async () => {
  try {
    const payload = await getPlanList(props.userId)
    activeTripCount.value = asArrayPayload(payload).filter((trip) => !isDeletedTrip(trip)).length
  } catch (error) {
    console.error('行程數量載入失敗', error)
    activeTripCount.value = 0
  }
}

onMounted(() => {
  loadProfile()
  loadActiveTripCount()
})
</script>

<template>
  <div class="profile-container">
    <section class="profile-header container py-4">
      <div class="d-flex gap-5">
        <!-- 頭像跟基本資料 -->
        <div class="profile-main-info d-flex">
          <div class="avatar-warpper">
            <LuiuAvator :avatar="$img(profileData.avatarUrl)" size="xl" />
          </div>

          <div class="user-details d-flex flex-column ms-4">
            <h4 class="user-name">{{ profileData.name || '未命名用戶' }}</h4>
            <!-- <div>
              <p>{{ user.name }}</p>
            </div> -->
            <div>
              <button
                v-if="isMyOwnProfile"
                class="btn btn-outline-primary"
                @click="isEditModalOpen = true"
              >
                <i class="bi bi-pencil"></i> 編輯個人頁面
              </button>

              <ProfileEditModal
                v-if="isEditModalOpen"
                :initial-data="profileData"
                @close="isEditModalOpen = false"
                @save="onProfileSave"
              />
            </div>
          </div>
        </div>

        <!-- 社群統計 -->
        <div class="profile-info-column d-flex flex-column">
          <div class="profile-stats">
            <ul class="stats-list d-flex mb-0 list-unstyled">
              <li class="stat-item">
                <span class="stat-value fw-bold">{{ profileData.followerCount || 0 }}</span>
                <span class="stat-label">粉絲</span>
              </li>
              <li class="stat-item">
                <span class="stat-value fw-bold">{{ profileData.followingCount || 0 }}</span>
                <span class="stat-label">追蹤中</span>
              </li>
            </ul>
          </div>

          <div class="profile-socials mt-2">
            <ul class="social-links d-flex gap-3 mb-0 list-unstyled">
              <li v-for="(url, platform) in socialMedialList" :key="platform">
                <a
                  href="javascript:void(0)"
                  class="social-icon-link"
                  :class="{ 'is-active': url }"
                  @click="handleSocialClick(platform, url)"
                >
                  <img class="svg-icon" :src="getIconPath(platform)" :alt="platform" />
                </a>
              </li>
            </ul>
          </div>

          <div class="profile-bio mt-2">
            <p class="bio-text">{{ profileData.bio || '這傢伙很懶，什麼都沒留下。' }}</p>
          </div>
        </div>
      </div>
    </section>

    <nav class="profile-tabs-nav sticky-top bg-white border-bottom">
      <div class="container">
        <ul class="nav nav-tabs border-bottom-0">
          <li v-for="tab in tabs" :key="tab.id" class="nav-item">
            <a
              class="nav-link"
              :class="{ active: activeTab === tab.id }"
              @click="changeTab(tab.id)"
            >
              <span>{{ tab.count }}</span>
              {{ tab.name }}
            </a>
          </li>
        </ul>
      </div>
    </nav>

    <div class="profile-content-wrapper">
      <div class="container pb-4">
        <!-- 將各頁面的資料儲存在記憶體中 -->
        <keep-alive>
          <component
            :is="tabComponents[activeTab]"
            :user-id="props.userId"
            @create-plan="openCreateDialog"
          />
        </keep-alive>
      </div>
    </div>

    <PlanCreateDialog
      v-if="isCreateDialogOpen"
      :user-id="props.userId"
      @close="closeCreateDialog"
      @created="handlePlanCreated"
    />
  </div>
</template>

<style lang="scss">
@import '@/assets/scss/pages/profile';
</style>
