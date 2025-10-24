import type { ChatMessage } from '~/types/message'

export interface ChatWebSocketHandlers {
  onMessageReceived: (message: ChatMessage) => void
}

export function useChatWebSocketHandlers(handlers: ChatWebSocketHandlers) {
  const wsHandlers = new Map<string, WsHandler>()

  wsHandlers.set('chat/message/receive', (message) => {
    handlers.onMessageReceived(message as ChatMessage)
  })

  const registerHandler = (key: string, handler: WsHandler) => {
    wsHandlers.set(key, handler)
  }

  return {
    handlers: wsHandlers,
    registerHandler
  }
}