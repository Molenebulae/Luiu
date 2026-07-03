<script setup>
const props = defineProps({
    title: {
        type: String,
        required: true,
        default: 'Title'
    },
    value: {
        type: [String, Number],
        required: true,
        default: '0'
    },
    iconClass: {
        type: String,
        default: 'ri-map-pin-line' 
    },
    iconBgClass: {
        type: String,
        default: 'bg-primary bg-gradient'
    },
    prefix: {
        type: String,
        default: ''
    },
    suffix: {
        type: String,
        default: ''
    }
});
</script>

<template>
    <div class="card border-0 shadow-sm p-4 rounded-4 h-100 luiu-stat-card">
        <div class="d-flex align-items-center gap-3">
            <div 
                class="rounded-4 d-flex align-items-center justify-content-center text-white flex-shrink-0"
                :class="iconBgClass"
                style="width: 60px; height: 60px;"
            >
                <i :class="[iconClass, 'fs-2']"></i>
            </div>
            
            <div>
                <p class="text-muted mb-1 fs-6">{{ title }}</p>
                <h4 class="fw-bold mb-0 text-dark">
                    {{ prefix }}{{ value }}{{ suffix }}
                </h4>
            </div>
        </div>
    </div>
</template>

<style scoped>
.luiu-stat-card {
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
}
.luiu-stat-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 .5rem 1rem rgba(0,0,0,.1) !important;
}
</style>