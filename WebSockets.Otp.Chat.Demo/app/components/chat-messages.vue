<template>
  <div class="flex-1 overflow-y-auto p-4 bg-gray-800 rounded-lg shadow-md mb-5" ref="messagesContainer">
    <div v-for="(message, index) in messages" :key="index"
      class="mb-4 p-3 rounded-lg bg-blue-50 border-l-4 border-blue-500">
      <div class="flex justify-between mb-1">
        <span class="text-xs text-gray-600">{{ formatTimestamp(message.timestamp) }}</span>
      </div>
      <div class="text-sm text-gray-800 leading-relaxed">{{ message.content }}</div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import type { ChatMessage } from '~/types/message';

const props = defineProps<{
  messages: ChatMessage[]
}>()

const formatTimestamp = (timestamp: string) => new Date(timestamp).toLocaleTimeString()

const messagesContainer = ref<HTMLElement>()
watch(() => props.messages.length, () => {
  nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
    }
  })
}, { flush: 'post' })
</script>