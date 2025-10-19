export type BaseMessage = {
    key: string
}

export type ChatMessage = BaseMessage & {
    content: string
    username: string
    timestamp: string
}