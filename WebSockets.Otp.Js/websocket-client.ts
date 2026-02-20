import { WsMessage, HandshakeOptions } from "./types"

export type WsSecurityOptions = {
    token?: string | (() => string) | (() => Promise<string>)
    tokenRefreshThreshold?: number
    onTokenRefresh?: () => Promise<string>
}

export type WsMultiplexingOptions = {
    connectionsCount: number
    // mode: 'round-robin' | 'least-loaded' | 'parallel'
    // minConnections: number
    // maxConnections: number
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
    multiplexing?: WsMultiplexingOptions
    reconnect?: WsReconnectOptions
}

export function useWsEndpoints(options: WsOptions) {
    const sockets: WebSocket[] = []
    const listeners = new Map<string, (message: any) => any>()

    const initConnections = (options: WsOptions) => {
        const connectionsCount = Math.max(options.multiplexing?.connectionsCount ?? 1, 1)

        for (let i = 0; i < connectionsCount; i++) {
            const socket = new WebSocket(options.url, options.protocols);

            socket.onmessage = (event) => {
                const data = JSON.parse(event.data) as WsMessage
                const listener = listeners.get(data.key)
                listener && listener(data)
            }

            sockets.push(socket)
        }
    }

    initConnections(options)

    let currentConnectionIndex = 0

    const getNextConnection = (options: WsOptions) => {
        const connection = sockets[currentConnectionIndex]
        currentConnectionIndex = (currentConnectionIndex + 1) % (options.multiplexing?.connectionsCount ?? 1)
        return connection
    }

    const connect = (options: HandshakeOptions) => {
        sockets.forEach((socket) => {
            socket.send(JSON.stringify(options))
        })
    }

    const send = <T>(key: string, message: T) => {
        const webSocket = getNextConnection(options)
        webSocket.send(JSON.stringify({
            key,
            ...message
        }))
    }

    // const request = <T, TResult>(key: string, message: T): Promise<TResult> => {}

    const receive = <T>(key: string, callback: (message: T) => any) => {
        listeners.set(key, callback)
    }

    const disconnect = () => {
        sockets.forEach((socket) => {
            socket.close()
        })
    }

    return {
        connect,
        send,
        receive,
        disconnect
    }
}