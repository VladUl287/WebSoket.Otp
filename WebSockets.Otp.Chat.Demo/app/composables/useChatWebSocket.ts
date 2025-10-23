import type { WsMessage, ChatMessage } from "~/types/message"

export type WsHandler = (message: WsMessage) => void
export type WsOptions = {
  path: MaybeRefOrGetter<string>
  handshakePath: MaybeRefOrGetter<string>
  token?: MaybeRefOrGetter<string>
  handlers: Map<string, WsHandler>
  autoReconnect?: {
    retries?: number
    delay?: number
    onFailed?: () => void
  }
  onMessage?: (ws: WebSocket, event: MessageEvent) => void
  onConnected?: (ws: WebSocket) => void
  onDisconnected?: (ws: WebSocket, event: CloseEvent) => void
  onError?: (ws: WebSocket, event: Event) => void
}

interface WsReturn {
  status: Ref<string>
  send: (data: any) => void
  connect: () => Promise<boolean>
  disconnect: () => void
}

export const useOtpWebSocket = (options: WsOptions): WsReturn => {
  const connectionTokenId = ref('')
  const requestUrl = computed(() => {
    const url = toValue(options.path)
    const urlObj = new URL(url)
    urlObj.searchParams.set('id', encodeURIComponent(connectionTokenId.value))
    return urlObj.href
  })

  const { status, send, open, close } = useWebSocket(requestUrl, {
    ...options,
    immediate: false,
    autoConnect: false,
    onMessage: (ws, event) => {
      try {
        const message = JSON.parse(event.data) as WsMessage
        const key = message.key
        const handler = options.handlers.get(key)
        if (handler) {
          return handler(message)
        }
        options.onMessage?.(ws, event)
      } catch (error) {
        console.error('Error parsing message:', error, event.data)
      }
    }
  })

  const connect = async () => {
    connectionTokenId.value = await $fetch<string>(toValue(options.handshakePath), {
      headers: {
        Authorization: toValue(options.token) ?? ''
      }
    })
    await nextTick()
    open()
    return true
  }

  const disconnect = () => close()

  return {
    status,

    send,
    connect,
    disconnect
  }
}