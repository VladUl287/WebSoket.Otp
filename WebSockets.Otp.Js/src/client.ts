import { usePendingRegistry, type PendingRegistry } from "./pending"
import { buildSchedule } from "./schedule"
import { jsonSerializer } from "./serializers"
import type { ConnectionState, PendingRequest, Serializer } from "./types"

const defaultFactory = (url: string): WebSocket => {
    if (typeof WebSocket === "undefined") {
        throw new Error("No global WebSocket. Pass `webSocketFactory`")
    }
    return new WebSocket(url)
}

type Handler<T> = (value: T) => void

export type WsClientOptions = {
    readonly url: string | (() => string),
    readonly defaultSerializer?: Serializer,
    readonly serializer?: Serializer,
    readonly socketFactory?: typeof defaultFactory,
    readonly scheduleFactory?: ReturnType<typeof buildSchedule>,
    readonly pendingFactory?: () => PendingRegistry<number>
    readonly handshakeTimeout?: number
}

export type WsClient = {
    readonly state: () => ConnectionState
    readonly connect: () => Promise<void>
    readonly close: (code?: number, reason?: string) => void
    readonly send: <TRequest, TResponse>(key: string, payload: TRequest) => Promise<TResponse>
    readonly notify: <TRequest>(key: string, payload: TRequest) => void
    readonly receive: <TValue>(key: string, callback: Handler<TValue>) => (() => void)
}

export const useWsClient = (options: WsClientOptions): WsClient => {
    const {
        url,
        defaultSerializer = jsonSerializer(),
        serializer = defaultSerializer,
        socketFactory = defaultFactory,
        scheduleFactory,
        pendingFactory = usePendingRegistry,
        handshakeTimeout = 30_000,
    } = options

    let ws: WebSocket | null = null
    let state: ConnectionState = "disconnected"
    let manuallyClosed = false

    let schedule = scheduleFactory?.()

    const connect = (): Promise<void> => {
        if (state === "connected" || state === "connecting") {
            return Promise.resolve()
        }

        state = "connecting"
        ws = null
        manuallyClosed = false

        return new Promise<void>((resolve, reject) => {
            let socket: WebSocket

            const onConnectError = (): void => {
                state = "disconnected"

                if (schedule) {
                    schedule.run(connect).then(resolve, reject)
                    return
                }

                const error = new Error("WebSocket error")
                reject(error)
            }

            const onRuntimeError = (): void => {
                state = "disconnected"

                if (schedule) {
                    schedule.run(connect)
                    return
                }

                const error = new Error("WebSocket error")
                pending.rejectAll(error)
            }

            try {
                const URL = typeof url === 'string' ? url : url()
                ws = socket = socketFactory(URL)
            } catch (err) {
                state = "disconnected"
                schedule?.run(connect).then(resolve, reject)
                return
            }

            socket.onopen = (): void => {
                sendHandshake(socket)
                    .then(() => {
                        state = "connected"

                        schedule?.reset()

                        socket.onerror = onRuntimeError
                        socket.onopen = null
                        socket.onmessage = (ev) => handleMessage(ev.data as string | BufferSource | Blob)

                        resolve()

                        resolveQueue(socket)
                    })
                    .catch((error: unknown) => {
                        state = "disconnected"

                        try { socket.close(4000, "handshake fail") } catch { }

                        reject(error)
                    })
            }

            socket.onmessage = (ev) => handleHandshakeMessage(ev.data as string | BufferSource | Blob)
            socket.onerror = onConnectError

            socket.onclose = (ev: { code: number; reason: string }): void => {
                state = "disconnected"

                const error = new Error(`Connection closed (code ${ev.code})`)
                pending.rejectAll(error)

                ws = null

                if (!manuallyClosed && schedule) {
                    schedule.run(connect).then(resolve, reject)
                    return
                }

                reject(error)
            }
        })
    }

    const close = (code?: number, reason?: string): void => {
        if (state !== "connected" || ws?.readyState !== WebSocket.OPEN) {
            throw new Error('Client not connected')
        }

        state = "disconnected"

        const socket = ws
        manuallyClosed = true
        ws = null

        try { socket?.close(code ?? 1000, reason) } catch { }

        const error = new Error(
            `Connection closed${reason ? ` (${reason})` : code ? ` (code ${code})` : ''}`,
        )
        pending.rejectAll(error)
        rejectQueue(error)
    }

    const pending = usePendingRegistry<number>()

    const HANDSHAKE_ID = 0
    const sendHandshake = (socket: WebSocket): Promise<{}> => {
        return new Promise<{}>((resolve, reject) => {
            if (socket.readyState !== WebSocket.OPEN) {
                reject(new Error("Socket is not open"))
                return
            }

            let message: string | BufferSource | Blob
            try {
                message = defaultSerializer.serialize({ protocol: defaultSerializer.protocol })
            } catch (err) {
                reject(err)
                return
            }

            try {
                pending.enqueue(HANDSHAKE_ID, { resolve, reject })
                socket.send(message)
            } catch (err) {
                pending.remove(HANDSHAKE_ID)
                reject(err)
            }
        })
    }

    const handleHandshakeMessage = async (data: string | BufferSource | Blob): Promise<void> => {
        let value: unknown
        try {
            value = await defaultSerializer.deserialize(data)
        } catch (error) {
            pending.reject(HANDSHAKE_ID, error instanceof Error ? error : new Error("Unable to deserialize handshake response"))
            return
        }
        if (value && typeof value === "object" && Object.keys(value as object).length === 0) {
            pending.resolve(HANDSHAKE_ID, value)
            return
        }
        pending.reject(HANDSHAKE_ID, new Error("Handshake response has invalid format"))
    }

    const buildCorrelationId = () => {
        let correlationId = 0
        return () => (correlationId = (correlationId + 1) >>> 0)
    }
    const nextCorrelationId = buildCorrelationId()

    type QueueItem = {
        key: string
        value: unknown
        resolve: (value: any) => void
        reject: (reason?: unknown) => void
    }
    const queue: QueueItem[] = []
    const rejectQueue = (error: unknown) => queue.splice(0).forEach(a => a.reject(error))
    const resolveQueue = (socket: WebSocket) => {
        if (state !== "connected" || !ws || ws.readyState !== 1) return

        queue.splice(0).forEach(({ key, value, resolve, reject }) => {
            const correlationId = nextCorrelationId()
            try {
                const message = serializer.serialize({ key, id: correlationId, value })
                pending.enqueue(correlationId, { resolve, reject })
                socket.send(message)
            } catch (err) {
                pending.remove(correlationId)
                reject(err)
            }
        })
    }

    const send = <TRequest, TResponse>(key: string, value: TRequest): Promise<TResponse> => {
        if (state !== "connected" || ws?.readyState !== WebSocket.OPEN) {
            return new Promise<TResponse>((resolve, reject) => queue.push({ key, value, resolve, reject }))
        }

        const socket = ws
        const correlationId = nextCorrelationId()
        const message = serializer.serialize({ key, id: correlationId, value })

        return new Promise<TResponse>((resolve, reject) => {
            try {
                pending.enqueue(correlationId, { resolve, reject })
                socket.send(message)
            } catch (error) {
                pending.remove(correlationId)
                reject(error)
            }
        })
    }

    const notify = <TRequest>(key: string, value: TRequest): void => {
        if (state !== "connected" || ws?.readyState !== WebSocket.OPEN) {
            throw new Error("Client is not connected")
        }

        const request: Record<string, any> = { key, value }
        const message = serializer.serialize(request)
        ws.send(message)
    }

    const handlers = new Map<string, Set<Handler<unknown>>>()

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

    const handleMessage = async (data: string | BufferSource | Blob): Promise<void> => {
        let parsed: unknown
        try {
            parsed = await serializer.deserialize(data)
        } catch {
            // log
            return
        }

        if (parsed && typeof parsed === "object") {
            const obj = parsed as Record<string, unknown>

            if (typeof obj.id === "number") {
                const key = obj.id
                if (obj.error) {
                    pending.reject(key, obj.error)
                }
                if ('value' in obj) {
                    pending.resolve(key, obj.value)
                }
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

    return {
        state: () => state,
        connect,
        close,
        send,
        notify,
        receive
    }
}