import { usePendingRegistry } from "./pending"
import { ConnectionState, PendingRequest, Protocol } from "./types"

const defaultFactory = (url: string): WebSocket => {
    if (typeof WebSocket === "undefined") {
        throw new Error("No global WebSocket. Pass `webSocketFactory`")
    }
    return new WebSocket(url)
}

export const useWsClient = (options: { url: string, protocol?: Protocol, factory?: typeof defaultFactory }) => {
    const {
        url,
        protocol = "json",
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
            socket.send(JSON.stringify({ protocol }))
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
                setState("disconnected");
                reject(err instanceof Error ? err : new Error(String(err)));
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
                        reject(err instanceof Error ? err : new Error(String(err)));
                    })
            }

            socket.onmessage = (ev: { data: unknown }): void => handleIncoming(ev.data)

            socket.onerror = (): void => {
                clearHandshake()
                ws = null
                const error = new Error("WebSocket error")
                if (!settled) {
                    settled = true
                    setState("disconnected")
                    reject(error)
                }
            }

            socket.onclose = (ev: { code: number; reason: string }): void => {
                clearHandshake()
                setState("disconnected")
                ws = null
                if (!settled) {
                    settled = true
                    reject(new Error(`Connection closed before handshake completed (code ${ev.code})`))
                    return
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

    type Handler<T> = (value: T) => void

    const handlers = new Map<string, Set<Handler<unknown>>>()

    const handleIncoming = (data: unknown): void => {
        const text =
            typeof data === "string"
                ? data
                : data instanceof ArrayBuffer
                    ? new TextDecoder().decode(data)
                    : null

        if (text === null) {
            return
        }

        let parsed: unknown
        try {
            parsed = JSON.parse(text)
        } catch {
            return
        }

        if (state === "handshaking") {
            if (handshakeResolve) {
                const resolve = handshakeResolve
                clearHandshake()
                resolve()
            }
            return
        }

        if (parsed && typeof parsed === "object" && 'value' in parsed) {
            const obj = parsed as Record<string, unknown>

            if (typeof obj.correlationId === "number") {
                const key = obj.correlationId
                pending.resolve(key, parsed.value)
            }
            else if (typeof obj.key === "string") {
                const key = obj.key
                const set = handlers.get(key)
                if (!set) return
                for (const handler of set) {
                    try {
                        handler(parsed.value)
                    }
                    catch (err) { }
                }
            }
        }
    }

    let correlationId = 0
    const send = <TRequest, TResponse>(key: string, payload: TRequest): Promise<TResponse> => {
        if (state !== "connected" || !ws || ws.readyState !== 1) {
            return Promise.reject(new Error("Client is not connected"))
        }

        const socket = ws
        correlationId = (correlationId + 1) >>> 0
        const message = JSON.stringify({ key, correlationId, value: payload })

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
        receive,
    }
}