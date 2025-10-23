export type WsMessage = {
    key: string
}

export type ChatMessage = WsMessage & {
    content: string
    chatId: string
    timestamp: string
}