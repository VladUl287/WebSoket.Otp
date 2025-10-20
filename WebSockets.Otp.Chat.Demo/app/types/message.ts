export type BaseMessage = {
    key: string
}

export type ChatMessage = BaseMessage & {
    content: string
    timestamp: string
}