import type { ChatMessage } from "~/types/message"

export const useChatWebSocket = (path: string = '/ws') => {
  const config = useRuntimeConfig()
  const wsUrl = `${config.public.wsUrl}${path}`

  const messages = ref<ChatMessage[]>([])
  const newMessage = ref('')

  const token = useCookie('token')

  const connectionToken = ref('')
  const url = computed(() => `${wsUrl}?id=${encodeURIComponent(connectionToken.value ?? '')}`)

  const { status, data, send, open, close } = useWebSocket(url, {
    immediate: false,
    autoConnect: false,
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
        messages.value.push(message)
      } catch (error) {
        console.error('Error parsing message:', error, event.data)
      }
    }
  })

  const connect = async () => {
    connectionToken.value = await $fetch<string>(config.public.apiUrl + '/ws/_handshake', {
      headers: {
        Authorization: `Bearer ${token.value}`
      }
    })
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

  const sendChatMessage = (data: any) => {
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