<template>
  <header class="flex justify-between items-center mb-5 p-4 bg-gray-800 text-white rounded-lg shadow-md">
    <h2 class="text-xl font-semibold">WebSocket Chat</h2>
    <div class="px-3 py-1.5 rounded-full text-xs font-bold uppercase" :class="statusClasses[wsStatus]">
      {{ wsStatus }}
    </div>
  </header>
</template>

<script setup lang="ts">
const props = withDefaults(defineProps<{ status: WebSocketStatus }>(), {
  status: 'CLOSED'
})

const wsStatus = ref<WebSocketStatus>('CLOSED')
const setStatus = (status: WebSocketStatus) => (wsStatus.value = status)
const debounceStatus = debounce(setStatus, 100)
watch(() => props.status, debounceStatus)

const statusClasses = Object.freeze<Record<WebSocketStatus, string>>({
  'CONNECTING': 'bg-yellow-800 animate-pulse',
  'OPEN': 'bg-green-800',
  'CLOSED': 'bg-red-800'
})
</script>