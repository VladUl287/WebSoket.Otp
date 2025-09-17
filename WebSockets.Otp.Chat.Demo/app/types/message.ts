export type BaseMessage = {
    route: string
}

export type ChatMessage = BaseMessage & {
    content: string
    username: string
    timestamp: string
}