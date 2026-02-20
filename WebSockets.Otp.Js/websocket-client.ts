import { WsMessage, HandshakeOptions } from "./types"

export type WsSecurityOptions = {
    token?: string | (() => string) | (() => Promise<string>)
    tokenRefreshThreshold?: number
    onTokenRefresh?: () => Promise<string>
}

export type WsReconnectOptions = {
    maxAttempts: number
    interval: number
    maxInterval?: number
    reconnectDecay?: number
    shouldReconnect?: (attempt: number, error: Event) => boolean | Promise<boolean>
    onReconnectAttempt?: (attempt: number) => void
    onReconnectSuccess?: (attempt: number) => void
    onReconnectFailed?: () => void
}

export type WsOptions = {
    url: string | URL
    protocols?: string | string[]
    reconnect?: WsReconnectOptions
    auth?: WsSecurityOptions
}

export function useWsEndpoints(options: WsOptions) {
    const socket: WebSocket = new WebSocket(options.url, options.protocols)
    const listeners = new Map<string, (message: any) => any>()

    socket.onmessage = (event) => {
        const data = JSON.parse(event.data) as WsMessage
        const listener = listeners.get(data.key)
        listener && listener(data)
    }

    const connect = (options: HandshakeOptions) => {
        socket.send(JSON.stringify(options))
    }

    const disconnect = () => {
        socket.close()
    }

    const send = <T>(key: string, message: T) => {
        socket.send(JSON.stringify({
            key,
            ...message
        }))
    }

    // const request = <T, TResult>(key: string, message: T): Promise<TResult> => {}

    const receive = <T>(key: string, callback: (message: T) => any) => {
        listeners.set(key, callback)
    }

    return {
        connect,
        disconnect,
        send,
        receive,
    }
}