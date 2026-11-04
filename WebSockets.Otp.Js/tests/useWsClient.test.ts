import {
    describe,
    it,
    expect,
    beforeEach,
    afterEach,
    jest,
} from "@jest/globals"
import type { ConnectionState, PendingRequest, Serializer } from "../src/types"
import type { PendingRegistry } from "../src/pending"
import { useWsClient, type WsClient, type WsClientOptions } from "../src/client"

type MessageHandler = (ev: { data: unknown }) => void
type SimpleHandler = (ev: unknown) => void
type CloseHandler = (ev: { code: number; reason: string }) => void

class MockSocket {
    static instances: MockSocket[] = []

    static reset(): void {
        MockSocket.instances = []
    }

    readyState = 0
    sent: string[] = []
    readonly url: string

    onopen: SimpleHandler | null = null
    onmessage: MessageHandler | null = null
    onerror: SimpleHandler | null = null
    onclose: CloseHandler | null = null

    constructor(url: string) {
        this.url = url
        MockSocket.instances.push(this)
    }

    send(data: string): void {
        if (this.readyState !== 1) throw new Error("MockSocket: not OPEN")
        this.sent.push(data)
    }

    close(code = 1000, reason = ""): void {
        if (this.readyState === 3) return
        this.readyState = 3
        this.onclose?.({ code, reason })
    }

    asWebSocket(): WebSocket {
        return this as unknown as WebSocket
    }

    open(): void {
        this.readyState = 1
        this.onopen?.({})
    }

    receive(data: string): void {
        this.onmessage?.({ data })
    }

    fireError(): void {
        this.onerror?.({})
    }

    serverClose(code = 1000, reason = ""): void {
        if (this.readyState === 3) return
        this.readyState = 3
        this.onclose?.({ code, reason })
    }
}

function getSocket(index = -1): MockSocket {
    const arr = MockSocket.instances
    const idx = index < 0 ? arr.length + index : index
    const s = arr[idx]
    if (!s) throw new Error(`MockSocket: no instance at index ${idx}`)
    return s
}

const mockSerializer: Serializer = {
    protocol: "test.v1",
    serialize: (value: unknown): string => JSON.stringify(value),
    deserialize: (data): Promise<unknown> => Promise.resolve(JSON.parse(data as string)),
}

type MockSchedule = {
    run: jest.Mock<(fn: () => Promise<void>) => Promise<void>>
    reset: jest.Mock<() => void>
}

function createSchedule(autoRun: boolean): MockSchedule {
    return {
        run: jest.fn<(fn: () => Promise<void>) => Promise<void>>((fn) =>
            autoRun ? fn() : Promise.resolve(),
        ),
        reset: jest.fn<() => void>(),
    }
}

type MockPending = PendingRegistry<number> & {
    readonly entries: Map<number, PendingRequest>
    readonly enqueue: jest.Mock<(id: number, entry: PendingRequest) => void>
    readonly remove: jest.Mock<(id: number) => void>
    readonly resolve: jest.Mock<(id: number, value: unknown) => void>
    readonly reject: jest.Mock<(id: number, error: unknown) => void>
    readonly rejectAll: jest.Mock<(error: unknown) => void>
}

function createPending(): MockPending {
    const entries = new Map<number, PendingRequest>()

    const api = {
        entries,
        enqueue: jest.fn<(id: number, entry: PendingRequest) => void>((id, entry) => {
            entries.set(id, entry)
        }),
        remove: jest.fn<(id: number) => void>((id) => {
            entries.delete(id)
        }),
        resolve: jest.fn<(id: number, value: unknown) => void>((id, value) => {
            const e = entries.get(id)
            if (e) {
                entries.delete(id)
                e.resolve(value)
            }
        }),
        reject: jest.fn<(id: number, error: unknown) => void>((id, error) => {
            const e = entries.get(id)
            if (e) {
                entries.delete(id)
                e.reject(error)
            }
        }),
        rejectAll: jest.fn<(error: unknown) => void>((error) => {
            const all = Array.from(entries.values())
            entries.clear()
            for (const e of all) e.reject(error)
        }),
    }

    return api as unknown as MockPending
}

async function flushPromises(): Promise<void> {
    for (let i = 0; i < 20; i++) await Promise.resolve()
}

type Harness = {
    client: WsClient
    schedule: MockSchedule
    pending: MockPending
}

function createClient(overrides: Partial<WsClientOptions> = {}): Harness {
    const schedule = createSchedule(false)
    const pending = createPending()

    const client = useWsClient({
        url: "ws://test",
        defaultSerializer: mockSerializer,
        serializer: mockSerializer,
        socketFactory: (url: string): WebSocket => new MockSocket(url).asWebSocket(),
        scheduleFactory: () => schedule,
        pendingFactory: () => pending,
        ...overrides,
    })

    return { client, schedule, pending }
}

async function finishHandshake(socket: MockSocket): Promise<void> {
    socket.open()
    await flushPromises()
    socket.receive("{}")
    await flushPromises()
}

async function connectAndHandshake(client: WsClient): Promise<MockSocket> {
    const p = client.connect()
    const socket = getSocket()
    await finishHandshake(socket)
    await p
    return socket
}

function stateOf(client: WsClient): ConnectionState {
    return client.state()
}

beforeEach(() => {
    MockSocket.reset()
    jest.useRealTimers()
})

afterEach(() => {
    jest.restoreAllMocks()
})

describe("connect()", () => {
    it("completes handshake and transitions to connected", async () => {
        const { client, schedule } = createClient()
        const socket = await connectAndHandshake(client)

        expect(stateOf(client)).toBe("connected")
        expect(socket.url).toBe("ws://test")
        expect(socket.sent).toHaveLength(1)
        expect(JSON.parse(socket.sent[0]!)).toEqual({ protocol: "test.v1" })
        expect(schedule.reset).toHaveBeenCalledTimes(1)
    })

    it("is a no-op when already connected", async () => {
        const { client } = createClient()
        await connectAndHandshake(client)
        const before = MockSocket.instances.length

        await client.connect()

        expect(MockSocket.instances.length).toBe(before)
        expect(stateOf(client)).toBe("connected")
    })

    it("resolves a concurrent call before the handshake completes", async () => {
        const { client } = createClient()

        const first = client.connect()
        const second = client.connect()

        await second
        expect(stateOf(client)).toBe("connecting")

        await finishHandshake(getSocket())
        await first
        expect(stateOf(client)).toBe("connected")
    })

    it("rejects when the handshake times out", async () => {
        jest.useFakeTimers()
        const { client } = createClient({
            handshakeTimeoutMs: 1_000,
            scheduleFactory: undefined,
        })

        const p = client.connect()
        p.catch(() => { })

        const socket = getSocket()
        socket.open()
        await flushPromises()
        expect(socket.sent).toHaveLength(1)

        await jest.advanceTimersByTimeAsync(1_000)

        await expect(p).rejects.toThrow()
        expect(socket.readyState).toBe(3)
        expect(stateOf(client)).toBe("disconnected")
    })

    it("rejects handshake replies that are not a plain object", async () => {
        const { client } = createClient({ scheduleFactory: undefined })
        const p = client.connect()
        const socket = getSocket()

        socket.open()
        await flushPromises()
        socket.receive(`"not-an-object"`)
        await flushPromises()

        await expect(p).rejects.toThrow(/invalid format/i)
        expect(socket.readyState).toBe(3)
        expect(stateOf(client)).toBe("disconnected")
    })

    it("accepts an empty array as a handshake reply", async () => {
        const { client } = createClient({ scheduleFactory: undefined })
        const p = client.connect()
        const socket = getSocket()

        socket.open()
        await flushPromises()
        socket.receive("[]")
        await flushPromises()

        await p
        expect(stateOf(client)).toBe("connected")
    })

    it("reports a connection error when the socket factory throws", async () => {
        const { client, pending } = createClient({
            socketFactory: () => {
                throw new Error("factory exploded")
            },
            scheduleFactory: undefined,
        })

        await expect(client.connect()).rejects.toThrow(/WebSocket connection error/i)
        expect(pending.rejectAll).not.toHaveBeenCalled()
        expect(stateOf(client)).toBe("disconnected")
    })

    it("rejects on a socket error before handshake", async () => {
        const { client } = createClient({ scheduleFactory: undefined })
        const p = client.connect()
        const socket = getSocket()

        socket.fireError()

        await expect(p).rejects.toThrow(/connection error/i)
        expect(stateOf(client)).toBe("disconnected")
    })
})

describe("send()", () => {
    it("sends immediately when connected and resolves with the reply", async () => {
        const { client } = createClient()
        const socket = await connectAndHandshake(client)

        const p = client.send<{ foo: string }, { ok: boolean }>("req", { foo: "bar" })

        expect(socket.sent).toHaveLength(2)
        const msg = JSON.parse(socket.sent[1]!) as {
            key: string
            id: number
            value: unknown
        }
        expect(msg.key).toBe("req")
        expect(msg.value).toEqual({ foo: "bar" })
        expect(typeof msg.id).toBe("number")
        expect(msg.id).not.toBe(0)

        socket.receive(JSON.stringify({ id: msg.id, value: { ok: true } }))
        await flushPromises()
        await expect(p).resolves.toEqual({ ok: true })
    })

    it("queues while connecting and flushes after handshake", async () => {
        const { client } = createClient()
        const connectP = client.connect()
        const socket = getSocket()

        const sendP = client.send<unknown, { result: number }>("req", { x: 1 })
        expect(socket.sent).toHaveLength(0)

        await finishHandshake(socket)
        await connectP

        expect(socket.sent).toHaveLength(2)
        const msg = JSON.parse(socket.sent[1]!) as { id: number }
        socket.receive(JSON.stringify({ id: msg.id, value: { result: 42 } }))
        await flushPromises()

        await expect(sendP).resolves.toEqual({ result: 42 })
    })

    it("keeps queued requests pending when the connection attempt fails", async () => {
        const { client } = createClient({ scheduleFactory: undefined })
        const connectP = client.connect()
        const socket = getSocket()

        const sendP = client.send("req", {})
        let settled = false
        void sendP.then(
            () => { settled = true },
            () => { settled = true },
        )

        socket.fireError()
        await expect(connectP).rejects.toThrow()
        await flushPromises()

        expect(settled).toBe(false)
    })

    it("throws synchronously when the serializer fails", async () => {
        const throwingSerializer: Serializer = {
            protocol: "test",
            serialize: () => {
                throw new Error("cannot serialize")
            },
            deserialize: () => Promise.resolve(undefined),
        }
        const { client } = createClient({ serializer: throwingSerializer })
        await connectAndHandshake(client)

        expect(() => client.send("req", {})).toThrow("cannot serialize")
    })

    it("rejects an in-flight request when the socket closes unexpectedly", async () => {
        const { client } = createClient()
        const socket = await connectAndHandshake(client)

        const p = client.send("req", {})
        socket.serverClose(1006, "abnormal")

        await expect(p).rejects.toThrow(/Connection closed/i)
    })

    it("assigns a unique nonzero correlation id to every request", async () => {
        const { client } = createClient()
        const socket = await connectAndHandshake(client)

        const a = client.send("a", {})
        const b = client.send("b", {})
        const c = client.send("c", {})
        a.catch(() => { }); b.catch(() => { }); c.catch(() => { })

        const ids = socket.sent
            .slice(1)
            .map((s) => (JSON.parse(s) as { id: number }).id)
        expect(new Set(ids).size).toBe(ids.length)
        expect(ids.every((id) => id > 0)).toBe(true)
    })
})

describe("notify()", () => {
    it("throws when not connected", () => {
        const { client } = createClient()
        expect(() => client.notify("evt", {})).toThrow(/not connected/i)
    })

    it("sends a key/value message without an id", async () => {
        const { client } = createClient()
        const socket = await connectAndHandshake(client)

        client.notify("evt", { a: 1 })

        expect(socket.sent).toHaveLength(2)
        expect(JSON.parse(socket.sent[1]!)).toEqual({ key: "evt", value: { a: 1 } })
    })
})

describe("receive()", () => {
    it("dispatches keyed messages to handlers", async () => {
        const { client } = createClient()
        const got: unknown[] = []
        client.receive("evt", (v) => got.push(v))

        const socket = await connectAndHandshake(client)
        socket.receive(JSON.stringify({ key: "evt", value: { a: 1 } }))
        await flushPromises()

        expect(got).toEqual([{ a: 1 }])
    })

    it("delivers to multiple subscribers", async () => {
        const { client } = createClient()
        const a: unknown[] = []
        const b: unknown[] = []
        client.receive("evt", (v) => a.push(v))
        client.receive("evt", (v) => b.push(v))

        const socket = await connectAndHandshake(client)
        socket.receive(JSON.stringify({ key: "evt", value: 7 }))
        await flushPromises()

        expect(a).toEqual([7])
        expect(b).toEqual([7])
    })

    it("stops delivery after unsubscribe", async () => {
        const { client } = createClient()
        const got: unknown[] = []
        const off = client.receive("evt", (v) => got.push(v))

        const socket = await connectAndHandshake(client)
        off()
        socket.receive(JSON.stringify({ key: "evt", value: 1 }))
        await flushPromises()

        expect(got).toEqual([])
    })

    it("isolates a throwing handler from the next", async () => {
        const { client } = createClient()
        const got: unknown[] = []
        client.receive("evt", () => {
            throw new Error("boom")
        })
        client.receive("evt", (v) => got.push(v))

        const socket = await connectAndHandshake(client)
        socket.receive(JSON.stringify({ key: "evt", value: 7 }))
        await flushPromises()

        expect(got).toEqual([7])
    })

    it("ignores messages for unknown keys", async () => {
        const { client } = createClient()
        const socket = await connectAndHandshake(client)
        expect(() => socket.receive(JSON.stringify({ key: "nope", value: 1 }))).not.toThrow()
    })
})

describe("close()", () => {
    it("rejects in-flight requests and marks disconnected", async () => {
        const { client } = createClient()
        const socket = await connectAndHandshake(client)

        const p = client.send("req", {})
        client.close()

        expect(stateOf(client)).toBe("disconnected")
        expect(socket.readyState).toBe(3)
        await expect(p).rejects.toThrow(/Connection closed/i)
    })

    it("does not schedule a reconnect", async () => {
        const { client, schedule } = createClient()
        await connectAndHandshake(client)
        client.close()
        await flushPromises()
        expect(schedule.run).not.toHaveBeenCalled()
    })

    it("throws when not connected", () => {
        const { client } = createClient()
        expect(() => client.close()).toThrow(/not connected/i)
    })
})

describe("reconnect", () => {
    it("schedules a retry after an unexpected close", async () => {
        const autoSchedule = createSchedule(true)
        const { client } = createClient({ scheduleFactory: () => autoSchedule })

        const socketA = await connectAndHandshake(client)
        socketA.serverClose(1006, "network")
        await flushPromises()

        expect(autoSchedule.run).toHaveBeenCalledTimes(1)
        expect(MockSocket.instances).toHaveLength(2)
    })

    it("resets the schedule after a successful reconnect", async () => {
        const autoSchedule = createSchedule(true)
        const { client } = createClient({ scheduleFactory: () => autoSchedule })

        const socketA = await connectAndHandshake(client)
        socketA.serverClose(1006, "network")
        await flushPromises()

        const socketB = getSocket(1)
        await finishHandshake(socketB)
        await flushPromises()

        expect(stateOf(client)).toBe("connected")
        expect(autoSchedule.reset).toHaveBeenCalledTimes(2)
    })

    it("keeps the new connection when the stale socket closes late", async () => {
        const autoSchedule = createSchedule(true)
        const { client } = createClient({ scheduleFactory: () => autoSchedule })

        const connectP = client.connect()
        const socketA = getSocket(0)
        socketA.fireError()
        await expect(connectP).rejects.toThrow()
        await flushPromises()

        const socketB = getSocket(1)
        await finishHandshake(socketB)
        await flushPromises()
        expect(stateOf(client)).toBe("connected")

        socketA.serverClose(1006, "late")
        await flushPromises()

        expect(MockSocket.instances.length).toBeGreaterThanOrEqual(2)
    })

    it("does not deliver stale socket messages to user handlers", async () => {
        const autoSchedule = createSchedule(true)
        const { client } = createClient({ scheduleFactory: () => autoSchedule })

        const connectP = client.connect()
        const socketA = getSocket(0)
        socketA.fireError()
        await expect(connectP).rejects.toThrow()
        await flushPromises()

        const socketB = getSocket(1)
        await finishHandshake(socketB)
        await flushPromises()

        const got: unknown[] = []
        client.receive("evt", (v) => got.push(v))

        socketA.receive(JSON.stringify({ key: "evt", value: "stale" }))
        await flushPromises()

        expect(got).toEqual([])
    })
})

describe("handleMessage()", () => {
    it("rejects a request whose reply carries an error", async () => {
        const { client } = createClient()
        const socket = await connectAndHandshake(client)

        const p = client.send("req", {})
        const msg = JSON.parse(socket.sent[1]!) as { id: number }
        socket.receive(JSON.stringify({ id: msg.id, error: "bad" }))
        await flushPromises()

        await expect(p).rejects.toBe("bad")
    })

    it("calls both reject and resolve when error and value coexist", async () => {
        const { client, pending } = createClient()
        const socket = await connectAndHandshake(client)

        const p = client.send("req", {})
        const msg = JSON.parse(socket.sent[1]!) as { id: number }
        socket.receive(JSON.stringify({ id: msg.id, error: "bad", value: "ok" }))
        await flushPromises()

        expect(pending.reject).toHaveBeenCalledWith(msg.id, "bad")
        expect(pending.resolve).toHaveBeenCalledWith(msg.id, "ok")
        await expect(p).rejects.toBe("bad")
    })

    it("ignores malformed JSON", async () => {
        const { client } = createClient()
        const socket = await connectAndHandshake(client)
        expect(() => socket.receive("<<not json>>")).not.toThrow()
        await flushPromises()
        expect(stateOf(client)).toBe("connected")
    })
})

describe("schedule usage", () => {
    it("resets on successful connect", async () => {
        const { client, schedule } = createClient()
        await connectAndHandshake(client)
        expect(schedule.reset).toHaveBeenCalledTimes(1)
    })

    it("does not run on a clean manual close", async () => {
        const { client, schedule } = createClient()
        await connectAndHandshake(client)
        client.close()
        expect(schedule.run).not.toHaveBeenCalled()
    })
})