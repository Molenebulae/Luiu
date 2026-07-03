<script setup>
// 載入拆好的Component 使用@開頭
import example from '@/components/base/example.vue'

//頭貼群組
import AvatarGroup from '@/components/base/LuiuAvatarGroup.vue'
import { ref, onMounted, inject } from 'vue'

// // 準備測試用的圖片資料 (模擬未來從 SQL Server 或 API 撈回來的行程圖片)
// const sampleImages = ref([
//     { src: 'https://picsum.photos/id/1018/800/400', alt: '自然風景' },
//     { src: 'https://picsum.photos/id/1015/800/400', alt: '河川' },
//     { src: 'https://picsum.photos/id/1019/800/400', alt: '海岸' },
// ]);
const props = defineProps({
  // 在 LuiuUserRow.vue 的 defineProps 裡面這樣改：
  avatar: {
    type: String,
    // required: true,  <-- 刪除或註解掉這行
    default: 'https://picsum.photos/id/1005/100/100', // 網路上的隨機大頭貼
  },
  username: {
    type: String,
    // required: true,  <-- 刪除或註解掉這行
    default: 'username',
  },
  subtitle: {
    type: String,
    default: '',
  },
  actionText: {
    type: String,
    default: '切換', // 如果傳入空字串 ''，按鈕就會隱藏
  },
  // 可以自訂頭像大小，預設為 48px
  avatarSize: {
    type: Number,
    default: 48,
  },
})

const emit = defineEmits(['action-click'])

// 假設這是你的資料流，當你 push 新資料進去，畫面會自動跳出頭像
const userList = ref([
  { id: 101, name: 'Alex', avatarUrl: 'https://i.pravatar.cc/150?u=101' }, // 有圖片
  { id: 102, name: 'Bella', avatarUrl: 'https://i.pravatar.cc/150?u=102' }, // 有圖片
  { id: 103, name: 'Charlie' }, // 故意不給，測試預設圖
  { id: 104, name: 'Doris', avatarUrl: '' }, // 空字串，測試預設圖
  { id: 105, name: 'Erik', avatarUrl: 'wrong-url' }, // 壞掉的連結，測試 handleImgError
  { id: 105, name: 'Erik', avatarUrl: 'wrong-url' }, // 壞掉的連結，測試 handleImgError
  { id: 105, name: 'Erik', avatarUrl: 'wrong-url' }, // 壞掉的連結，測試 handleImgError
  { id: 105, name: 'Erik', avatarUrl: 'wrong-url' }, // 壞掉的連結，測試 handleImgError
  { id: 105, name: 'Erik', avatarUrl: 'wrong-url' }, // 壞掉的連結，測試 handleImgError
  { id: 105, name: 'Erik', avatarUrl: 'wrong-url' }, // 壞掉的連結，測試 handleImgError
])

//Tag功能
import FormTag from '@/components/base/LuiuTagify.vue'

const mySkills = ref(['css', 'html']) // 初始值
const allSkills = ref(['javascript', 'vue', 'react', 'node.js', 'python', 'sql']) // 建議選單

//Input、 Textarea 輸入功能
import LuiuInput from '@/components/base/LuiuInput.vue'
import LuiuTextarea from '@/components/base/LuiuEditer.vue'

// Input 跟 texarea 用的
const userName = ref('')
const errorMsg = ref('') // 用來存錯誤訊息
// Textarea 用的
const userComment = ref('')
const commentError = ref('')

// 模擬一個驗證邏輯
const submitForm = () => {
  let isValid = true

  // 驗證姓名
  if (!userName.value) {
    errorMsg.value = '姓名不可以是空的喔！'
    isValid = false
  } else {
    errorMsg.value = ''
  }

  // 驗證內容
  if (!userComment.value) {
    commentError.value = '內容不能空白喔！'
    isValid = false
  } else {
    commentError.value = ''
  }

  if (isValid) {
    alert('全部驗證成功！')
  }
}

//Select 功能
import BaseSelect from '@/components/base/LuiuSelect.vue'

const formData = ref({ destination: '' })
const destinations = ref([
  { value: '1', label: 'Destination 1' },
  { value: '2', label: 'Destination 2' },
])

onMounted(async () => {
  // API 邏輯照舊，撈完資料塞進 destinations.value 即可
})

const selectedCityId = ref('')

import LuiuDropdown from '@/components/base/LuiuDropdown.vue'

// 隨時在這裡新增或修改項目
const pagesList = [
  { label: 'About Us', link: '/about' },
  { label: 'Our Team', link: '/team' },
  { divider: true }, // 甚至可以設計分隔線
  { label: 'FAQ', link: '/faq' },
]

const serviceList = [
  { label: 'Web Design', link: '/web' },
  { label: 'SEO Optimization', link: '/seo' },
]

import PlanCards from '@/components/Planning/PlanCards.vue'
import LuiuPlaceholders from '@/components/base/LuiuPlaceholders.vue'
import LuiuSeparator from '@/components/base/LuiuSeparator.vue'
import LuiuCheck from '@/components/base/LuiuCheck.vue'
import LuiuInputSpin from '@/components/base/LuiuInputSpin.vue'
import LuiuDatePicker from '@/components/base/LuiuDatePicker.vue'
import LuiuDatePickerRange from '@/components/base/LuiuDatePickerRange.vue'
import LuiuDraggableContainer from '@/components/base/LuiuDraggableContainer.vue'
import LuiuDraggableCards from '@/components/base/LuiuDraggableCards.vue'

const testData = [
  {
    TripID: 1,
    TripName: '高雄五日遊',
    OwnerName: '路人a',
    StartDate: '2024-06-10',
    EndDate: '2024-06-18',
    Desc: '深度體驗高雄文化',
  },
  {
    TripID: 2,
    TripName: '台中美食團',
    OwnerName: '路人b',
    StartDate: '2024-07-19',
    EndDate: '2024-07-31',
    Desc: '逢甲夜市吃到飽',
  },
  {
    TripID: 3,
    TripName: '宜蘭美食團777777777777777777777777777777777777777777',
    OwnerName: '路人CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC',
    StartDate: '2024-07-19',
    EndDate: '2024-07-31',
    Desc: '33333333333333333333333333333333333333333333333333333星蔥吃到飽',
  },
  {
    TripID: 16,
    TripName: '台南美食團',
    OwnerName: '路人D',
    StartDate: '2024-07-19',
    EndDate: '2024-07-31',
    Desc: '台糖二砂吃到飽',
  },
  {
    TripID: 6,
    TripName: '離島美食團',
    OwnerName: '路人E',
    StartDate: '2024-07-19',
    EndDate: '2024-07-31',
    Desc: '海鮮吃到飽',
  },
]

const date1 = ref('')
const date2 = ref([])
// 定義日期限制
const datepickerOptions = {
  // 最小日期：今天(maybe 看你們)
  minDate: new Date(),
  // 最大日期
  maxDate: new Date('2028-12-31'),
}

const list = ref([
  { id: 1, title: '卡片 1', content: '這是第一個測試內容' },
  { id: 2, title: '卡片 2', content: '這是第二個測試內容' },
  { id: 3, title: '卡片 3', content: '這是第三個測試內容' },
])
const listA = ref([
  { id: 'A1', title: '項目 A1', content: '內容 A1' },
  { id: 'A2', title: '項目 A2', content: '內容 A2' },
])

const listB = ref([
  { id: 'B1', title: '項目 B1', content: '內容 B1' },
  { id: 'B2', title: '項目 B2', content: '內容 B2' },
])

const checkName = '隨便放點文字'
const separatorName = '我是新分隔線名稱'
import WithControls from '@/components/base/LuiuCarousel.vue'
import LuiuStatCard from '@/components/base/LuiuStatCard.vue'
import LuiuButtonBadge from '@/components/base/LuiuBadge.vue'
import LuiuVideoPlayer from '@/components/base/LuiuVideoPlayer.vue'
import LuiuAudioPlayer from '@/components/base/LuiuAudioPlayer.vue'
import LuiuFileUploader from '@/components/base/LuiuFileUploader.vue'
import LuiuUserRow from '@/components/base/LuiuAvator.vue'

// 準備測試用的儀表板資料 (未來這裡可以接 API 撈取 Luiu 平台的真實數據)
const dashboardStats = ref([
  {
    id: 1,
    title: '總結算金額',
    value: '14,500',
    prefix: '$',
    suffix: '',
    icon: 'ri-money-dollar-circle-line', // 金錢圖示
  },
  {
    id: 2,
    title: '平台總行程數',
    value: '5,340',
    prefix: '',
    suffix: ' 趟',
    icon: 'ri-flight-takeoff-line', // 飛機圖示
  },
  {
    id: 3,
    title: '本月新增會員',
    value: '1,230',
    prefix: '',
    suffix: ' 人',
    icon: 'ri-user-add-line', // 新增會員圖示
  },
  {
    id: 4,
    title: '系統處理時間',
    value: '10.2',
    prefix: '',
    suffix: ' ms',
    icon: 'ri-timer-flash-line', // 碼表圖示
  },
])
// 狀態管理
const pendingCount = ref(0) // 待上傳
const uploadedCount = ref(0) // 已成功連動

//  建立一個反應式變數來存張數，預設為 0
const photoCount = ref(0)

//  定義處理更新的函式
const handleCountUpdate = (newCount) => {
  photoCount.value = newCount
}
// 處理「選取中」的張數連動
const handlePendingUpdate = (count) => {
  pendingCount.value = count
}

// 處理「上傳成功」的連動
const handleUploadSuccess = (response) => {
  // 每成功一張，右邊數字就 +1
  uploadedCount.value++
  console.log('伺服器回應：', response)
}

const count = ref(0)
</script>

<template>
  <div class="container py-5">
    <h1>Component 測試</h1>
    <hr />

    <!-- 從下面開始放 Component 做測試-->
    <example>測試</example>
    <div class="card shadow-sm">
      <div>旅行卡片測試</div>
      <PlanCards :items="testData"></PlanCards>
    </div>
    <div class="card shadow-sm">
      <div>連結樣式測試</div>
      <a href="#" class="luiu-a-primary">新的連結樣式-------------------</a>
    </div>
    <div class="card shadow-sm">
      <div>分割線樣式測試</div>
      <LuiuSeparator>
        {{ separatorName }}
      </LuiuSeparator>
    </div>
    <div class="card shadow-sm">
      <div>placeholder載入樣式測試</div>
      <LuiuPlaceholders />
    </div>
    <div class="card shadow-sm">
      <div>check載入樣式測試</div>
      <LuiuCheck>
        {{ checkName }}
      </LuiuCheck>
      <LuiuCheck variant="sec">
        {{ checkName }}
      </LuiuCheck>
    </div>
    <div class="card shadow-sm">
      <div>InputSpin載入樣式測試</div>
      <LuiuInputSpin v-model="count" />
    </div>

    <div class="card shadow-sm">
      <div>DatePicker測試</div>
      <LuiuDatePicker v-model="date1" :options="datepickerOptions" />
    </div>

    <div class="card shadow-sm">
      <div>DatePickerRange測試</div>
      <LuiuDatePickerRange v-model="date2" :options="datepickerOptions" />
    </div>

    <div class="card shadow-sm">
      <div>Draggable測試 單一容器</div>
      <LuiuDraggableContainer>
        <!-- 第一層必須是 col，這樣 Bootstrap 佈局才不會崩潰 -->
        <div v-for="item in list" :key="item.id" class="col-xl-4">
          <LuiuDraggableCards :title="item.title">
            <p>{{ item.content }}</p>
          </LuiuDraggableCards>
        </div>
      </LuiuDraggableContainer>
      <div>Draggable測試 多容器</div>
      <div class="card-body">
        <div class="row">
          <div class="col-md-6">
            <h6 class="text-center">容器 A</h6>
            <div class="p-3 border bg-light rounded">
              <LuiuDraggableContainer group="shared-group">
                <div v-for="item in listA" :key="item.id" class="col-12">
                  <LuiuDraggableCards :title="item.title" variant="bg-white">
                    <p>{{ item.content }}</p>
                  </LuiuDraggableCards>
                </div>
              </LuiuDraggableContainer>
            </div>
          </div>

          <div class="col-md-6">
            <h6 class="text-center">容器 B</h6>
            <div class="p-3 border bg-light rounded">
              <LuiuDraggableContainer group="shared-group">
                <div v-for="item in listB" :key="item.id" class="col-12">
                  <LuiuDraggableCards :title="item.title" variant="bg-primary-subtle">
                    <p>{{ item.content }}</p>
                  </LuiuDraggableCards>
                </div>
              </LuiuDraggableContainer>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <WithControls :interval="5000" />
  <!-- <div class="container py-5">
        <h1>Component 測試實驗室</h1>
        <hr>

        <div class="row">
            <div class="col-md-8 mx-auto">
                <LuiuCarousel :images="sampleImages" />
            </div>
        </div>
    </div> -->

  <div class="container py-5">
    <h4 class="mb-4">使用者列 (純淨版) 測試</h4>
    <hr class="mb-4" />

    <div class="row g-4 align-items-center">
      <div class="col-12">
        <span class="text-muted small mb-2 d-block">小尺寸 (sm - 36px)</span>
        <LuiuUserRow
          size="sm"
          username="Buxiu"
          avatar="https://ui-avatars.com/api/?name=Buxiu&background=0D8ABC&color=fff"
        />
      </div>

      <div class="col-12">
        <span class="text-muted small mb-2 d-block">預設中尺寸 (md - 48px)</span>
        <LuiuUserRow
          username="Milan Olsen"
          avatar="https://ui-avatars.com/api/?name=Milan+Olsen&background=F56A79&color=fff"
        />
      </div>

      <div class="col-12">
        <span class="text-muted small mb-2 d-block">大尺寸 (lg - 64px)</span>
        <LuiuUserRow
          size="lg"
          username="System Admin"
          avatar="https://ui-avatars.com/api/?name=Admin&background=2c3e50&color=fff"
        />
      </div>

      <div class="col-12">
        <span class="text-muted small mb-2 d-block">特大尺寸 (xl - 120px)</span>
        <LuiuUserRow
          size="xl"
          username="超級VIP會員"
          avatar="https://ui-avatars.com/api/?name=VIP&background=f1c40f&color=fff"
        />
      </div>

      <div class="col-12 mt-5">
        <span class="text-muted small mb-2 d-block">特大尺寸 (只傳頭像，不傳 username)</span>
        <LuiuUserRow
          size="xl"
          avatar="https://ui-avatars.com/api/?name=None&background=95a5a6&color=fff"
        />
      </div>
    </div>
  </div>

  <div class="container py-5">
    <h1>Component 測試實驗室</h1>
    <hr />

    <div class="card bg-light mb-4">
      <div class="card-header">
        <h5 class="card-title mb-0">平台營運數據</h5>
      </div>
      <div class="card-body">
        <p class="text-muted mb-4">以下顯示 Luiu 系統目前的即時營運狀況：</p>

        <div class="row g-5">
          <div v-for="stat in dashboardStats" :key="stat.id" class="col-12 col-md-6 col-xl-3">
            <LuiuStatCard
              :title="stat.title"
              :value="stat.value"
              :prefix="stat.prefix"
              :suffix="stat.suffix"
              :iconClass="stat.icon"
            />
          </div>
        </div>
      </div>
    </div>
  </div>

  <div class="container py-5">
    <h4 class="mb-4">萬能 Badge 測試 (精緻版)</h4>

    <div class="d-flex gap-5 align-items-center flex-wrap pt-3">
      <LuiuButtonBadge Message="99+" position="top-right">
        <button class="btn btn-primary">我的訊息</button>
      </LuiuButtonBadge>

      <LuiuButtonBadge Message="4" badgeClass="bg-warning text-dark" position="top-right">
        <i class="ri-notification-3-line fs-3"></i>
      </LuiuButtonBadge>

      <LuiuButtonBadge Message="New" badgeClass="bg-success" position="bottom-right">
        <img
          src="https://ui-avatars.com/api/?name=Buxiu&background=0D8ABC&color=fff"
          class="rounded-circle shadow-sm"
          width="48"
        />
      </LuiuButtonBadge>

      <LuiuButtonBadge Message="審核中" badgeClass="bg-secondary" position="inline">
        <span class="fw-bold fs-5">Luiu 旅遊企劃案</span>
      </LuiuButtonBadge>
    </div>
  </div>

  <div class="container py-5">
    <h1>多媒體播放器測試</h1>
    <hr />

    <div class="container py-5">
      <LuiuVideoPlayer videoId="5caEKdSKB-M" title="測試影片播放" showInput />
    </div>
    <div class="col-lg-4">
      <LuiuAudioPlayer title="" />
    </div>
  </div>

  <div class="container py-5">
    <div class="row g-4">
      <div class="col-lg-4">
        <LuiuStatCard
          title="正式存檔照片"
          :value="uploadedCount"
          suffix=" 張"
          iconClass="ri-cloud-upload-line"
          iconBgClass="bg-success bg-gradient"
        />
      </div>

      <div class="col-lg-8">
        <LuiuFileUploader
          title="上傳行程回憶"
          uploadUrl="https://httpbin.org/post"
          max-files="10"
          @update-pending="handlePendingUpdate"
          @upload-success="handleUploadSuccess"
        />
      </div>
    </div>
  </div>

  <!-- 模擬圖片背景色 -->
  <nav style="background: #002347; display: flex">
    <!-- 頁面選單 -->
    <LuiuDropdown title="Pages" :menuItems="pagesList" />

    <!-- 你可以重複使用，傳入不同資料 -->
    <LuiuDropdown title="Services" :menuItems="serviceList" />
  </nav>

  <h3>群組頭貼</h3>
  <!-- 頭貼 -->
  <AvatarGroup :users="userList" :max="4" />

  <br />

  <h3>tag</h3>
  <div>
    <label>技能標籤</label>
    <FormTag v-model="mySkills" :suggestions="allSkills" placeholder="請輸入技能..." />
    <p>目前選擇：{{ mySkills }}</p>
  </div>

  <br />

  <h3>輸入Input功能</h3>
  <div class="row">
    <div class="col-md-6">
      <!-- 直接使用封裝好的 Component -->
      <LuiuInput
        id="nameInput"
        v-model="userName"
        label="Your Name"
        placeholder="Enter your name"
        :error-message="errorMsg"
      />
    </div>

    <div class="col-12 mt-2">
      <button class="btnLuiu" @click="submitForm">送出測試</button>
    </div>
  </div>

  <br />

  <h3>輸入Textarea功能</h3>
  <div class="col-md-6">
    <LuiuTextarea
      id="commentInput"
      v-model="userComment"
      label="留言內容"
      placeholder="請輸入您的意見..."
      height="150px"
      :error-message="commentError"
    />
  </div>

  <br />

  <h3>下拉選單(select)</h3>
  <BaseSelect
    id="select1"
    v-model="formData.destination"
    :options="destinations"
    label="Destination"
    placeholder="請選擇目的地"
    style="width: 500px"
  />

  <br />

  <h3>按鈕</h3>
  <button class="btn btn-primary">Button</button>
  <br />

  <button class="luiu-btn-primary w-100">按鈕</button>
  <br />
  <button class="luiu-btn-light w-100">按鈕</button>
  <br />
  <button class="luiu-btn-lg-square">按鈕</button>

  <br />

  <br />

  <p class="mt-3">名字 {{ userName }}</p>
  <p class="mt-3">目前選擇的 ID: {{ formData }}</p>

  <!-- 下拉選單 -->
  <Dropdown>
    <template #trigger>
      <button class="btn btn-primary rounded-pill dropdown-toggle" data-bs-toggle="dropdown">
        管理選單 ({{ menuItems.length }})
      </button>
    </template>

    <li
      v-for="item in menuItems"
      :key="item.id"
      class="dropdown-item"
      @click="handleMenuClick(item)"
    >
      {{ item.label }}
    </li>
  </Dropdown>

  <br />
</template>

<style lang="scss" scoped>
@import '@/assets/scss/components/buttons';
</style>
