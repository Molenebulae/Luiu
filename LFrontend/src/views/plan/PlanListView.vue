<script setup>
import PlanCreateDialog from '@/components/Planning/PlanCreateDialog.vue'
import PlanCards from '@/components/Planning/PlanCards.vue'
import { asArrayPayload, getPlanList } from '@/api/planning/plan'
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const planItems = ref([])
const isLoading = ref(false)
const errorMessage = ref('')
const isCreateDialogOpen = ref(false)

const userId = computed(() => route.params.userId)

const getPlanValue = (plan, ...keys) => {
  for (const key of keys) {
    const value = plan?.[key]
    if (value !== undefined && value !== null) return value
  }

  return ''
}

const getPlanCreateAt = (plan) =>
  getPlanValue(plan, 'CreateAt', 'createAt', 'CreatedAt', 'createdAt')

const getPlanUpdateAt = (plan) =>
  getPlanValue(plan, 'UpdateAt', 'updateAt', 'UpdatedAt', 'updatedAt')

const loadPlans = async () => {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const payload = await getPlanList(userId.value)
    planItems.value = asArrayPayload(payload).map((plan) => ({
      ...plan,
      CreateAt: getPlanCreateAt(plan),
      UpdateAt: getPlanUpdateAt(plan),
      OwnerIconURL: plan.OwnerIconURL || '',
    }))
  } catch (error) {
    errorMessage.value = error?.message || 'Unable to load plan list.'
    planItems.value = []
  } finally {
    isLoading.value = false
  }
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
      userId: userId.value,
      tripId,
    },
  })
}

const handlePlanUpdate = (updatedPlan) => {
  planItems.value = planItems.value.map((plan) =>
    plan.TripID === updatedPlan.TripID ? { ...plan, ...updatedPlan } : plan,
  )
}

const handlePlanDelete = (tripId) => {
  planItems.value = planItems.value.filter((plan) => plan.TripID !== tripId)
}

onMounted(loadPlans)
</script>

<template>
  <section class="plan-list-heading" aria-labelledby="plan-list-title">
    <div class="heading-kicker">
      <span></span>
      <strong>Plan List</strong>
      <span></span>
    </div>
    <div class="heading-titles">
      <a href="javascript:void(0)" class="plan-title" @click="openCreateDialog">新增行程</a>
      <span class="plan-divider p-3">/</span>
      <a href="javascript:void(0)" class="plan-title" @click="router.push({ name: 'Home' })">
        回首頁
      </a>
      <span class="plan-divider p-3">/</span>
      <a id="plan-list-title" class="plan-title" style="text-decoration: underline">我的行程</a>
    </div>
  </section>

  <div v-if="isLoading" class="plan-list-state">Loading plans...</div>
  <div v-else-if="errorMessage" class="plan-list-state plan-list-state-error">
    {{ errorMessage }}
  </div>
  <div v-else-if="!planItems.length" class="plan-list-state">目前還沒有建立任何行程喔!</div>
  <PlanCards
    v-else
    :items="planItems"
    :user-id="userId"
    @update-plan="handlePlanUpdate"
    @delete-plan="handlePlanDelete"
  />

  <PlanCreateDialog
    v-if="isCreateDialogOpen"
    :user-id="userId"
    @close="closeCreateDialog"
    @created="handlePlanCreated"
  />
</template>

<style lang="scss" scoped>
@import '@/assets/scss/pages/plan-list';
</style>
