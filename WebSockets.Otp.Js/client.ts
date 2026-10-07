import { usePendingRegistry } from "./pending"
import { jsonSerializer } from "./serializers"
import { ConnectionState, PendingRequest, Serializer } from "./types"

const defaultFactory = (url: string): WebSocket => {
    if (typeof WebSocket === "undefined") {
        throw new Error("No global WebSocket. Pass `webSocketFactory`")
    }
    return new WebSocket(url)
}

type Handler<T> = (value: T) => void

export type WsClient = {
    state: () => ConnectionState
    isConnected: () => boolean
    connect: () => Promise<void>
    close: (code?: number, reason?: string) => void
    send<TRequest, TResponse>(key: string, payload: TRequest): Promise<TResponse>
    send<TResponse>(key: string): Promise<TResponse>
    notify(key: string): void
    receive: <TValue>(key: string, callback: Handler<TValue>) => (() => void)
}

export type WsClientOptions = {
    url: string,
    serializer?: Serializer,
    factory?: typeof defaultFactory,
}

export const useWsClient = (options: WsClientOptions): WsClient => {
    const jsSerializer = jsonSerializer()

    const {
        url,
        serializer = jsSerializer,
        factory = defaultFactory
    } = options

    let ws: WebSocket | null = null
    let state: ConnectionState = "disconnected"
    let handshakeResolve: (() => void) | null = null
    let handshakeReject: ((err: Error) => void) | null = null
    let manuallyClosed = false

    const setState = (next: ConnectionState): void => {
        if (state === next) return
        state = next
    }

    const clearHandshake = (): void => {
        handshakeResolve = null
        handshakeReject = null
    }

    const performHandshake = (socket: WebSocket): Promise<void> =>
        new Promise<void>((resolve, reject) => {
            handshakeResolve = resolve
            handshakeReject = reject
            socket.send(jsSerializer.serialize({ protocol: jsSerializer.protocol }))
        })

    const safeClose = (socket: WebSocket, code?: number, reason?: string): void => {
        try {
            pending.rejectAll(new Error(reason))
            socket.close(code, reason)
        } catch { }
    }

    const connect = (): Promise<void> => {
        if (
            state === "connected" ||
            state === "connecting" ||
            state === "handshaking"
        ) {
            return Promise.resolve()
        }

        manuallyClosed = false
        setState("connecting")

        return new Promise<void>((resolve, reject) => {
            let settled = false
            let socket: WebSocket

            try {
                socket = factory(url)
            } catch (err) {
                setState("disconnected")
                reject(err instanceof Error ? err : new Error(String(err)))
                return
            }

            ws = socket

            socket.onopen = (): void => {
                setState("handshaking")

                performHandshake(socket)
                    .then(() => {
                        if (settled) return
                        settled = true
                        setState("connected")
                        resolve()
                    })
                    .catch((err: unknown) => {
                        if (settled) return
                        settled = true
                        setState("disconnected")
                        reject(err instanceof Error ? err : new Error(String(err)))
                    })
            }

            socket.onmessage = (ev: { data: unknown }) => handleIncoming(ev.data as string | BufferSource | Blob)

            socket.onerror = (): void => {
                clearHandshake()
                setState("disconnected")

                const error = new Error("WebSocket error")
                pending.rejectAll(error)

                ws = null
                if (!settled) {
                    settled = true
                    reject(error)
                }
            }

            socket.onclose = (ev: { code: number; reason: string }): void => {
                clearHandshake()
                setState("disconnected")

                const error = new Error(`Connection closed before handshake completed (code ${ev.code})`)
                pending.rejectAll(error)

                ws = null
                if (!settled) {
                    settled = true
                    reject(error)
                }
            }
        })
    }

    const close = (code?: number, reason?: string): void => {
        manuallyClosed = true
        clearHandshake()

        if (ws) {
            safeClose(ws, code, reason)
            ws = null
        }

        setState("disconnected")
    }

    const pending = usePendingRegistry<number>()

    const handlers = new Map<string, Set<Handler<unknown>>>()

    const handleIncoming = async (data: string | BufferSource | Blob): Promise<void> => {
        if (data === null) {
            return
        }

        if (state === "handshaking") {
            try {
                await jsSerializer.deserialize(data)
            } catch {
                return
            }

            if (handshakeResolve) {
                const resolve = handshakeResolve
                clearHandshake()
                resolve()
            }
            return
        }

        let parsed: unknown
        try {
            parsed = await serializer.deserialize(data)
        } catch {
            return
        }

        if (parsed && typeof parsed === "object") {
            const obj = parsed as Record<string, unknown>

            if (typeof obj.correlationId === "number") {
                const key = obj.correlationId
                pending.resolve(key, obj.value)
                return
            }

            if (typeof obj.key === "string") {
                const key = obj.key
                const set = handlers.get(key)
                if (!set) return
                for (const handler of set) {
                    try {
                        handler(obj.value)
                    }
                    catch (err) { }
                }
            }
        }
    }

    let correlationId = 0
    const send = <TRequest, TResponse>(key: string, payload?: TRequest): Promise<TResponse> => {
        if (state !== "connected" || !ws || ws.readyState !== 1) {
            return Promise.reject(new Error("Client is not connected"))
        }

        const socket = ws
        correlationId = (correlationId + 1) >>> 0
        const message = serializer.serialize({ key, correlationId, value: payload })

        return new Promise<TResponse>((resolve, reject) => {
            const entry: PendingRequest = {
                resolve: resolve as (value: unknown) => void,
                reject
            }
            try {
                pending.enqueue(correlationId, entry)
                socket.send(message)
            } catch (err) {
                pending.remove(correlationId)
                reject(err)
            }
        })
    }

    const notify = (key: string): Promise<void> => send(key)

    const receive = <TValue>(key: string, callback: Handler<TValue>): (() => void) => {
        const cb = callback as Handler<unknown>

        let store = handlers.get(key)
        if (!store) {
            handlers.set(key, (store = new Set()))
        }
        store.add(cb)

        return () => {
            const store = handlers.get(key)
            if (!store) { return }
            store.delete(cb)
            if (store.size === 0) {
                handlers.delete(key)
            }
        }
    }

    return {
        state: () => state,
        isConnected: () => state === "connected" && ws?.readyState === 1,
        connect,
        close,
        send,
        notify,
        receive,
    }
}