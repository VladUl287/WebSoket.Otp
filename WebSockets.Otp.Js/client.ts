import { usePendingRegistry } from "./pending"
import { ConnectionState, PendingRequest, Protocol } from "./types"

const defaultFactory = (url: string): WebSocket => {
    if (typeof WebSocket === "undefined") {
        throw new Error("No global WebSocket. Pass `webSocketFactory`")
    }
    return new WebSocket(url)
}

export const useWsClient = (options: { url: string, protocol: Protocol, factory: typeof defaultFactory }) => {
    const {
        url,
        protocol = "json",
        factory = defaultFactory
    } = options

    let ws: WebSocket | null = null
    let state: ConnectionState = "disconnected"
    let handshakeTimer: ReturnType<typeof setTimeout> | null = null
    let handshakeResolve: (() => void) | null = null
    let handshakeReject: ((err: Error) => void) | null = null
    let manuallyClosed = false


    const setState = (next: ConnectionState): void => {
        if (state === next) return
        state = next
    }

    const clearHandshake = (): void => {
        if (handshakeTimer) {
            clearTimeout(handshakeTimer)
            handshakeTimer = null
        }
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
            socket.close(code, reason);
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
                const error = new Error("WebSocket error")
                if (!settled) {
                    settled = true
                    setState("disconnected")
                    reject(error)
                }
            }

            socket.onclose = (ev: { code: number; reason: string }): void => {
                clearHandshake()
                const wasConnected = state === "connected"
                setState("disconnected");
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

    const pending = usePendingRegistry()

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

        if (parsed && typeof parsed === "object" && "correlationId" in parsed) {
            const key = parsed.correlationId as number
            pending.resolve(key, parsed)
        }
    }

    let correlationId = 0
    const send = <TRequest, TResponse>(key: string, payload: TRequest): Promise<TResponse> => {
        if (state !== "connected" || !ws || ws.readyState !== 1) {
            return Promise.reject(new Error("Client is not connected"))
        }

        const socket = ws
        correlationId = (correlationId + 1) >>> 0
        const message = JSON.stringify({ key, correlationId, value: { ...payload } })

        return new Promise<TResponse>((resolve, reject) => {
            const entry: PendingRequest = {
                resolve: resolve as (value: unknown) => void,
                reject
            }
            try {
                pending.enqueue(correlationId, entry)
                socket.send(message)
            } catch (err) {
                pending.remove(correlationId, entry)
                reject(err)
            }
        })
    }

    return {
        state: () => state,
        isConnected: () => state === "connected" && ws?.readyState === 1,
        connect,
        close,
        send,
    }
}