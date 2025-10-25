<template>
  <div class="p-5 flex flex-col h-full bg-gray-900">
    <chat-header :status="status" />

    <chat-messages :messages="messages" />

    <chat-input :is-connected="isConnected" @send="sendMessage" />
  </div>
</template>

<script setup lang="ts">
import type { ChatMessage } from '~/types/message';

const props = defineProps<{
  chatId: string
}>()

const useHandlers = useChatWebSocketHandlers({
  onMessageReceived: (message) => {
    addMessage(message)
  }
})

const config = useRuntimeConfig()
const wsUrl = `${config.public.wsUrl}/ws`
const handshakeUrl = `${config.public.apiUrl}/ws/_handshake`

const token = useCookie('token')
const { status, connect, disconnect, send } = useOtpWebSocket({
  path: wsUrl,
  handshakePath: handshakeUrl,
  handlers: useHandlers.handlers,
  token: () => `Bearer ${token.value}`
})

onMounted(() => {
  connect()
})

const isConnected = computed(() => status.value === 'OPEN')

const { messages, createMessage, clearMessages, addMessage, loadMessages } = useChat({
  chatId: props.chatId,
  fetchUrl: () => `${config.public.apiUrl}/chats/getmessages/${props.chatId}`,
  token: () => token.value ?? ''
})
watch(() => props.chatId, loadMessages, { flush: 'post' })

const sendMessage = (content: string) => {
  if (status.value !== 'OPEN' || content.trim().length === 0) return
  const message = createMessage(content, 'chat/message/send')
  send(JSON.stringify(message))
}

onBeforeUnmount(() => {
  disconnect()
})
</script>