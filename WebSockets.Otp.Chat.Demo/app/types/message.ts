export type BaseMessage = {
    key: string
}

export type ChatMessage = BaseMessage & {
    content: string
    chatId: string
    timestamp: string
}