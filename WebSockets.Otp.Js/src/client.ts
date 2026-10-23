import { usePendingRegistry } from "./pending"
import { jsonSerializer } from "./serializers"
import type { ConnectionState, PendingRequest, Serializer } from "./types"

const defaultFactory = (url: string): WebSocket => {
    if (typeof WebSocket === "undefined") {
        throw new Error("No global WebSocket. Pass `webSocketFactory`")
    }
    return new WebSocket(url)
}

type Handler<T> = (value: T) => void

export type WsClient = {
    state: () => ConnectionState
    connect: () => Promise<void>
    close: (code?: number, reason?: string) => void
    send<TRequest, TResponse>(key: string, payload: TRequest): Promise<TResponse>
    notify<TRequest>(key: string, payload: TRequest): void
    receive: <TValue>(key: string, callback: Handler<TValue>) => (() => void),
}

export type WsReconnectOptions = {
    minDelay?: number,
    maxDelay?: number,
    factor?: number,
    jitter?: number,
    maxRetries?: number
}

type ScheduleReconnect = (callback: () => Promise<any>) => Promise<void>

const buildScheduleFactory = (options?: WsReconnectOptions) => (): ScheduleReconnect => {
    let retries = 0

    let {
        minDelay = 1000,
        maxDelay = 30000,
        maxRetries = Infinity,
        factor = 2,
        jitter = 0.3
    } = options ?? {}

    return (callback) => {
        if (retries >= maxRetries) {
            return Promise.reject()
        }

        const base = Math.min(maxDelay, minDelay * Math.pow(factor, retries))
        const base_jitter = base * jitter * (Math.random() * 2 - 1)
        const delay = Math.max(0, Math.round(base + base_jitter))

        retries += 1

        return new Promise((resolve, reject) => {
            setTimeout(() => callback().then(resolve, reject), delay)
        })
    }
}

export type WsClientOptions = {
    url: string,
    defaultSerializer?: Serializer,
    serializer?: Serializer,
    socketFactory?: typeof defaultFactory,
    scheduleFactory?: ReturnType<typeof buildScheduleFactory>,
}

export const useWsClient = (options: WsClientOptions): WsClient => {
    const {
        url,
        defaultSerializer = jsonSerializer(),
        serializer = defaultSerializer,
        socketFactory = defaultFactory,
        scheduleFactory = buildScheduleFactory()
    } = options

    let ws: WebSocket | null = null
    let state: ConnectionState = "disconnected"
    let manuallyClosed = false

    const setState = (next: ConnectionState): void => { state = next }

    let schedule = scheduleFactory?.()

    const connect = (): Promise<void> => {
        if (state === "connected" || state === "connecting") {
            return Promise.resolve()
        }

        manuallyClosed = false
        setState("connecting")

        return new Promise<void>((resolve, reject) => {
            let socket: WebSocket

            try {
                socket = socketFactory(url)
            } catch (err) {
                setState("disconnected")
                if (schedule) {
                    schedule(connect).then(resolve, reject)
                    return
                }
                reject(err)
                return
            }

            ws = socket

            socket.onopen = (): void => {
                sendHandshake(socket)
                    .then(() => {
                        socket.onmessage = (ev) => handleIncoming(ev.data as string | BufferSource | Blob)
                        setState("connected")
                        resolve()
                    })
                    .catch((err: unknown) => {
                        close(400, 'handshake failed')
                        reject(err)
                    })
            }

            socket.onmessage = (ev) => handleHandshake(ev.data as string | BufferSource | Blob)

            socket.onerror = (): void => {
                setState("disconnected")

                const error = new Error("WebSocket error")
                pending.rejectAll(error)

                ws = null

                if (schedule) {
                    schedule(connect).then(resolve, reject)
                    return
                }
                
                reject(error)
            }

            socket.onclose = (ev: { code: number; reason: string }): void => {
                setState("disconnected")

                const error = new Error(`Connection closed (code ${ev.code})`)
                pending.rejectAll(error)

                ws = null

                if (!manuallyClosed && schedule) {
                    schedule(connect).then(resolve, reject)
                    return
                }

                reject(error)
            }
        })
    }

    const close = (code?: number, reason?: string): void => {
        try {
            manuallyClosed = true
            ws?.close(code, reason)
        } catch { }
    }

    const pending = usePendingRegistry<number>()

    const handshakeId = 0
    const sendHandshake = (socket: WebSocket): Promise<{}> => {
        return new Promise<{}>((resolve, reject) => {
            if (state !== "connected" || !ws || ws.readyState !== 1) {
                return Promise.reject(new Error("Client is not connected"))
            }

            const message = defaultSerializer.serialize({ protocol: defaultSerializer.protocol })
            const entry: PendingRequest = {
                resolve: resolve as (value: unknown) => void,
                reject
            }
            try {
                pending.enqueue(handshakeId, entry)
                socket.send(message)
            } catch (err) {
                pending.remove(handshakeId)
                reject(err)
            }
        })
    }

    const handleHandshake = async (data: string | BufferSource | Blob): Promise<void> => {
        if (data === null) {
            pending.rejectAll(new Error('Handshake response is empty'))
            return
        }

        let value: unknown
        try {
            value = await defaultSerializer.deserialize(data)
        } catch (error) {
            pending.rejectAll(error instanceof Error ? error : new Error('Unable to deserializer handshake repsonse message'))
            return
        }

        if (value && typeof value === "object" && Object.keys(value).length === 0) {
            pending.resolve(0, value)
            return
        }

        pending.rejectAll(new Error('Handshake response has invalid format'))
    }

    let correlationId = 0
    const send = <TRequest, TResponse>(key: string, value: TRequest): Promise<TResponse> => {
        if (state !== "connected" || !ws || ws.readyState !== 1) {
            return Promise.reject(new Error("Client is not connected"))
        }

        const socket = ws
        correlationId = (correlationId + 1) >>> 0
        const message = serializer.serialize({ key, id: correlationId, value })

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

    const notify = <TRequest>(key: string, value: TRequest): void => {
        if (state !== "connected" || !ws || ws.readyState !== 1) {
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

    const handleIncoming = async (data: string | BufferSource | Blob): Promise<void> => {
        if (data === null) {
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

            if (typeof obj.id === "number") {
                const key = obj.id
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

    return {
        state: () => state,
        connect,
        close,
        send,
        notify,
        receive
    }
}