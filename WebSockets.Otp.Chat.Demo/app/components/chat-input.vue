<template>
  <div class="p-5 rounded-lg bg-gray-800 shadow-md">
    <div class="flex gap-3" :class="{ 'opacity-60': !isConnected }">
      <input v-model="newMessage" placeholder="Type your message..."
        class="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        @keyup.enter="$emit('send', newMessage)" :disabled="!isConnected" />
      <button @click="$emit('send', newMessage)" :disabled="!isConnected || !newMessage.trim()"
        class="px-4 bg-blue-500 text-white rounded-md hover:bg-blue-600 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors">
        Send
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">

defineProps<{
  isConnected: boolean
}>()

defineEmits<{
  (e: 'send', data: string): void
}>()

const newMessage = ref('')
</script>