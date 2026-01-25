import { WsMessage, HandshakeOptions } from "./types"

export function useWsEndpoints(webSocket: WebSocket) {
    const listeners = new Map<string, (message: any) => any>()

    webSocket.onmessage = (event) => {
        const data = JSON.parse(event.data) as WsMessage
        const listener = listeners.get(data.key)
        listener && listener(data)
    }

    const connect = (options: HandshakeOptions) => {
        webSocket.send(JSON.stringify(options))
    }

    const send = <T>(key: string, message: T) => {
        webSocket.send(JSON.stringify({
            key,
            ...message
        }))
    }

    const receive = <T>(key: string, callback: (message: T) => any) => {
        listeners.set(key, callback)
    }

    return {
        connect,
        send,
        receive
    }
}