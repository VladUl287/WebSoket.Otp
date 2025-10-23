<template>
 <div class="flex h-screen bg-gray-900">
    <div class="w-1/4 bg-gray-800 border-r border-gray-700 flex flex-col">
      <div class="flex-1 overflow-y-auto">
        <div v-for="chat in chats.data.value" :key="chat.id"
          class="p-4 border-b border-gray-700 hover:bg-gray-750 cursor-pointer transition-colors"
          :class="{ 'bg-blue-900/30': chat.id === activeChat }" @click="setActiveChat(chat.id)">
          <div class="flex items-center space-x-3">
            <div class="relative">
              <div class="w-12 h-12 rounded-full border-2 border-gray-600"></div>
              <div class="absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-gray-800 bg-gray-400"></div>
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex justify-between items-start">
                <h3 class="text-sm font-medium text-gray-100 truncate">
                  {{ chat.name }}
                </h3>
                <span class="text-xs text-gray-400 whitespace-nowrap">
                  {{ 'last message time' }}
                </span>
              </div>
              <p class="text-sm text-gray-400 truncate mt-1">
                {{ 'last message' }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="flex-1 flex flex-col">
      <chat-container v-if="activeChat" :chat-id="activeChat"></chat-container>
    </div>
  </div>
</template>

<script setup lang="ts">
const config = useRuntimeConfig()
const chatsUrl = `${config.public.apiUrl}/chats/getall`

const token = useCookie('token')
const chats = await useAsyncData<{ id: string, name: string }[]>('chats', () => {
  return $fetch(chatsUrl, {
    headers: {
      'Authorization': `Bearer ${token.value}`
    }
  })
})

const route = useRoute()
const activeChat = computed(() => route.query.chat?.toString())

const setActiveChat = (chatId: string) => {
  navigateTo({
    path: '/',
    query: {
      chat: chatId
    }
  })
}
</script>