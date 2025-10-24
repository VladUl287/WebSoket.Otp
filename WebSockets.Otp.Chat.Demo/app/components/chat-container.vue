<template>
  <div class="p-5 flex flex-col h-full bg-gray-900">
    <header class="flex justify-between items-center mb-5 p-4 bg-gray-800 text-white rounded-lg shadow-md">
      <h2>WebSocket Chat</h2>
      <div class="px-3 py-1.5 rounded-full text-xs font-bold uppercase" :class="{
        'bg-green-800': wsStatus === 'OPEN',
        'bg-red-800': wsStatus === 'CLOSED',
        'bg-yellow-800': wsStatus === 'CONNECTING'
      }">
        {{ wsStatus }}
      </div>
    </header>

    <div class="flex-1 overflow-y-auto p-4 bg-gray-800 rounded-lg shadow-md mb-5" ref="messagesContainer">
      <div v-for="(message, index) in messages" :key="index" class="message">
        <div class="message-header">
          <span class="timestamp">{{ formatTimestamp(message.timestamp) }}</span>
        </div>
        <div class="message-content">{{ message.content }}</div>
      </div>
    </div>

    <div class="p-5 rounded-lg bg-gray-800 shadow-md">
      <div class="message-input-group" :class="{ disabled: !isConnected }">
        <input v-model="newMessage" placeholder="Type your message..." class="message-input"
          @keyup.enter="sendMessage({ chatId })" :disabled="!isConnected" />
        <button @click="sendMessage({ chatId })" :disabled="!isConnected || !newMessage.trim()" class="send-btn">
          Send
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { type WsHandler } from '~/composables/useChatWebSocket'
import type { ChatMessage } from '~/types/message';

const props = defineProps<{
  chatId: string
}>()

const handlers = new Map<string, WsHandler>()
handlers.set('chat/message/receive', (message) => {
  messages.value.push(message as ChatMessage)
})

const token = useCookie('token')

const config = useRuntimeConfig()
const wsUrl = `${config.public.wsUrl}/ws`
const handshakeUrl = `${config.public.apiUrl}/ws/_handshake`

const debounce = <T extends (...args: any[]) => any>(fn: T, delay: number) => {
  let timeoutId: ReturnType<typeof setTimeout> | undefined
  return (...args: Parameters<T>) => {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => fn(...args), delay)
  }
}

const { status, connect, disconnect, send } = useOtpWebSocket({
  path: wsUrl,
  handshakePath: handshakeUrl,
  handlers: handlers,
  token: () => `Bearer ${token.value}`
})

const wsStatus = ref<WebSocketStatus>('CLOSED')
const setStatus = (status: WebSocketStatus) => (wsStatus.value = status)
const debounceStatus = debounce(setStatus, 200)
watch(() => status.value, debounceStatus)

const newMessage = ref('')

const isConnected = computed(() => status.value === 'OPEN')

const messages = ref<ChatMessage[]>([])
const messagesUrl = `${config.public.apiUrl}/chats/GetMessages/`
watch(() => props.chatId, async (chatId) => {
  messages.value = []
  messages.value = await $fetch<ChatMessage[]>(messagesUrl + chatId, {
    headers: {
      Authorization: `Bearer ${token.value}`
    }
  })
})

const sendMessage = (data: any) => {
  if (!newMessage.value.trim() || status.value !== 'OPEN') return

  const message: ChatMessage = {
    key: 'chat/message/send',
    content: newMessage.value.trim(),
    timestamp: new Date().toISOString(),
    ...data
  }

  send(JSON.stringify(message))

  newMessage.value = ''
}

const messagesContainer = ref<HTMLElement>()

onMounted(() => {
  connect()
})

onBeforeUnmount(() => {
  disconnect()
})

const formatTimestamp = (timestamp: string) => new Date(timestamp).toLocaleTimeString()

watch(messages, () => {
  nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
    }
  })
}, { deep: true })
</script>

<style scoped>
.message {
  margin-bottom: 15px;
  padding: 12px;
  border-radius: 8px;
  background-color: #e3f2fd;
  border-left: 4px solid #2196f3;
}

.message-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 5px;
  font-size: 12px;
  color: #666;
}

.message-content {
  font-size: 14px;
  line-height: 1.4;
  color: #333;
}

.connection-controls {
  display: flex;
  gap: 10px;
  margin-bottom: 15px;
}

.message-input-group {
  display: flex;
  gap: 10px;
}

.message-input-group.disabled {
  opacity: 0.6;
}

.message-input {
  flex: 1;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 14px;
}

.send-btn {
  padding: 12px 20px;
  border: none;
  border-radius: 6px;
  background-color: #2196f3;
  color: white;
  font-weight: bold;
  cursor: pointer;
}

.send-btn:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}

.send-btn:hover:not(:disabled) {
  background-color: #1976d2;
}
</style>