import type { ChatMessage } from "~/types/message"

export const useChatWebSocket = (path: string = '/ws') => {
  const config = useRuntimeConfig()
  const wsUrl = `${config.public.wsUrl}${path}`

  const messages = ref<ChatMessage[]>([])
  const newMessage = ref('')

  const token = useCookie('token')

  const url = computed(() => `${wsUrl}?token=${encodeURIComponent(token.value ?? '')}`)

  const { status, data, send, open, close } = useWebSocket(wsUrl, {
    immediate: false,
    autoReconnect: {
      retries: 3,
      delay: 1000,
      onFailed() {
        console.error('Failed to connect after 3 retries')
      }
    },
    onConnected: (ws) => {
      console.log('WebSocket connected successfully')
    },
    onDisconnected: (ws, event) => {
      console.log('WebSocket disconnected', event)
    },
    onError: (ws, event) => {
      console.error('WebSocket error:', event)
    },
    onMessage: (ws, event) => {
      try {
        const message = JSON.parse(event.data)
        console.log(message)
        messages.value.push(message)
      } catch (error) {
        console.error('Error parsing message:', error, event.data)
      }
    }
  })

  const connect = () => {
    open()
    return true
  }

  const disconnect = () => {
    if (status.value === 'OPEN') {
      send(JSON.stringify({
        route: '/chat/leave',
        timestamp: new Date().toISOString()
      }))
    }
    close()
  }

  const sendChatMessage = () => {
    if (!newMessage.value.trim() || status.value !== 'OPEN') return

    const message: ChatMessage = {
      key: 'chat/message',
      content: newMessage.value.trim(),
      timestamp: new Date().toISOString()
    }

    send(JSON.stringify(message))

    newMessage.value = ''
  }

  return {
    status,
    isConnected: computed(() => status.value === 'OPEN'),

    messages,
    newMessage,

    connect,
    disconnect,
    sendMessage: sendChatMessage,
    sendRaw: send
  }
}