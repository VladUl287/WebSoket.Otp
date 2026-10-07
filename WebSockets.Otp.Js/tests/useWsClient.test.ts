import { FakeWebSocket } from "./fakes/fakeWebSocket"
import { makeFakeSerializer } from "./fakes/fakeSerializer"
import { useWsClient } from "../src/client"

function setup(opts?: {
    protocol?: string
    serializer?: ReturnType<typeof makeFakeSerializer>
}) {
    const serializer = opts?.serializer ?? makeFakeSerializer(opts?.protocol)
    let socket!: FakeWebSocket
    const factory: (() => WebSocket) = () => {
        socket = new FakeWebSocket()
        return socket.asWebSocket()
    }
    const client = useWsClient({
        url: "wss://example.test",
        serializer,
        factory
    })
    return { client, get socket() { return socket }, serializer }
}

async function connectAndHandshake(
    ctx: ReturnType<typeof setup>
): Promise<void> {
    const p = ctx.client.connect()
    await Promise.resolve()
    ctx.socket.open()
    ctx.socket.message(JSON.stringify({ ok: true }))
    await p
}

describe("useWsClient  construction", () => {
    it("throws if no global WebSocket and no factory is provided", async () => {
        const original = (globalThis as any).WebSocket
        delete (globalThis as any).WebSocket
        try {
            const client = useWsClient({ url: "wss://x" })
            await expect(client.connect()).rejects.toThrow()
        } finally {
            (globalThis as any).WebSocket = original
        }
    })

    it("exposes initial state as disconnected", () => {
        const { client } = setup()
        expect(client.state()).toBe("disconnected")
        expect(client.isConnected()).toBe(false)
    })
})

describe("useWsClient  connect()", () => {
    it("resolves after open + handshake ack", async () => {
        const ctx = setup()
        const p = ctx.client.connect()
        await Promise.resolve()
        expect(ctx.client.state()).toBe("connecting")

        ctx.socket.open()
        expect(ctx.client.state()).toBe("handshaking")

        ctx.socket.message(JSON.stringify({ ok: true }))
        await expect(p).resolves.toBeUndefined()
        expect(ctx.client.state()).toBe("connected")
        expect(ctx.client.isConnected()).toBe(true)
    })

    it("is idempotent while connecting/handshaking/connected", async () => {
        const ctx = setup()
        const p1 = ctx.client.connect()
        await Promise.resolve()

        const p2 = ctx.client.connect()
        await Promise.resolve()
        expect(p1).not.toBe(p2)
        await expect(p2).resolves.toBeUndefined()

        ctx.socket.open()
        const p3 = ctx.client.connect()
        await expect(p3).resolves.toBeUndefined()

        ctx.socket.message(JSON.stringify({ ok: true }))
        await p1

        const p4 = ctx.client.connect()
        await expect(p4).resolves.toBeUndefined()
    })

    it("rejects if the factory throws", async () => {
        const client = useWsClient({
            url: "wss://x",
            serializer: makeFakeSerializer(),
            factory: () => {
                throw new Error("boom")
            }
        })
        await expect(client.connect()).rejects.toThrow("boom")
        expect(client.state()).toBe("disconnected")
    })

    it("rejects if the socket errors before handshake completes", async () => {
        const ctx = setup()
        const p = ctx.client.connect()
        await Promise.resolve()
        ctx.socket.open()
        ctx.socket.error()
        await expect(p).rejects.toThrow("WebSocket error")
        expect(ctx.client.state()).toBe("disconnected")
    })

    it("rejects if the socket closes before handshake completes", async () => {
        const ctx = setup()
        const p = ctx.client.connect()
        await Promise.resolve()
        ctx.socket.open()
        ctx.socket.closeFromServer(1006, "nope")
        await expect(p).rejects.toThrow(/Connection closed before handshake/)
        expect(ctx.client.state()).toBe("disconnected")
    })

    it("ignores non-parseable frames during handshaking", async () => {
        const ctx = setup()
        const p = ctx.client.connect()
        await Promise.resolve()
        ctx.socket.open()

        ctx.socket.message("not json")
        await Promise.resolve()
        expect(ctx.client.state()).toBe("handshaking")

        ctx.socket.message(JSON.stringify({ ok: true }))
        await expect(p).resolves.toBeUndefined()
    })

    it("ignores frames deserialize throws on during handshaking", async () => {
        const serializer = makeFakeSerializer()
        serializer.throwOnDeserialize = new Error("bad")
        const ctx = setup({ serializer })
        const p = ctx.client.connect()
        await Promise.resolve()
        ctx.socket.open()

        ctx.socket.message("anything")
        await Promise.resolve()
        expect(ctx.client.state()).toBe("handshaking")

        serializer.throwOnDeserialize = null
        ctx.socket.message(JSON.stringify({ ok: true }))
        await expect(p).resolves.toBeUndefined()
    })
})

describe("useWsClient close()", () => {
    it("closes the socket and transitions to disconnected", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        ctx.client.close(1000, "bye")
        expect(ctx.socket.closeCode).toBe(1000)
        expect(ctx.socket.closeReason).toBe("bye")
        expect(ctx.client.state()).toBe("disconnected")
        expect(ctx.client.isConnected()).toBe(false)
    })

    it("rejects all pending requests on close", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const p = ctx.client.send("k", { a: 1 })
        ctx.client.close(1000, "bye")
        await expect(p).rejects.toThrow("bye")
    })

    it("is safe to call when never connected", () => {
        const { client } = setup()
        expect(() => client.close()).not.toThrow()
        expect(client.state()).toBe("disconnected")
    })
})

describe("useWsClient send()", () => {
    it("rejects when not connected", async () => {
        const { client } = setup()
        await expect(client.send("k")).rejects.toThrow("not connected")
    })

    it("rejects when the socket is not OPEN", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)
        ctx.socket.readyState = 3
        await expect(ctx.client.send("k")).rejects.toThrow("not connected")
    })

    it("writes the serialized frame and resolves on matching response", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const p = ctx.client.send<{ a: number }, { b: number }>("k", { a: 1 })

        expect(ctx.socket.sent).toHaveLength(2)
        const wire = JSON.parse(ctx.socket.sent[1] as string)
        expect(wire.key).toBe("k")
        expect(wire.value).toEqual({ a: 1 })
        expect(typeof wire.correlationId).toBe("number")


        ctx.socket.message(
            JSON.stringify({ correlationId: wire.correlationId, value: { b: 2 } })
        )
        await expect(p).resolves.toEqual({ b: 2 })
    })

    it("resolves with undefined value when response has no value", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)
        const p = ctx.client.send("k")
        const wire = JSON.parse(ctx.socket.sent[1] as string)
        ctx.socket.message(JSON.stringify({ correlationId: wire.correlationId }))
        await expect(p).resolves.toBeUndefined()
    })

    it("rejects if socket.send throws", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        ctx.socket.sendThrows = new Error("write failed")
        await expect(ctx.client.send("k", { a: 1 })).rejects.toThrow("write failed")
    })

    it("uses incrementing correlationIds", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const p1 = ctx.client.send("a")
        const p2 = ctx.client.send("b")
        const [_, f1, f2] = ctx.socket.sent.map((s) => JSON.parse(s as string))
        expect(f2.correlationId).toBe(f1.correlationId + 1)

        ctx.socket.message(JSON.stringify({ correlationId: f1.correlationId, value: 1 }))
        ctx.socket.message(JSON.stringify({ correlationId: f2.correlationId, value: 2 }))
        await expect(p1).resolves.toBe(1)
        await expect(p2).resolves.toBe(2)
    })

    it("rejects all in-flight requests when the connection drops", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const p1 = ctx.client.send("a")
        const p2 = ctx.client.send("b")

        ctx.socket.closeFromServer(1006, "server died")
        await expect(p1).rejects.toThrow()
        await expect(p2).rejects.toThrow()
    })
})

describe("useWsClient notify()", () => {
    it("sends a frame and resolves void when server replies", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const p = ctx.client.notify("evt")
        const wire = JSON.parse(ctx.socket.sent[1] as string)
        expect(wire.key).toBe("evt")

        ctx.socket.message(JSON.stringify({ correlationId: wire.correlationId }))
        await expect(p).resolves.toBeUndefined()
    })

    it("rejects when not connected", async () => {
        const { client } = setup()
        await expect(client.notify("x")).rejects.toThrow("not connected")
    })
})

describe("useWsClient  receive()", () => {
    it("invokes the handler for matching key, with the value", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const cb = jest.fn()
        clientReceive(ctx, "chat", cb)
        ctx.socket.message(JSON.stringify({ key: "chat", value: { text: "hi" } }))

        await Promise.resolve()
        await Promise.resolve()
        expect(cb).toHaveBeenCalledWith({ text: "hi" })
    })

    it("supports multiple subscribers per key", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const a = jest.fn()
        const b = jest.fn()
        ctx.client.receive("chat", a)
        ctx.client.receive("chat", b)

        ctx.socket.message(JSON.stringify({ key: "chat", value: 1 }))
        await flush()
        expect(a).toHaveBeenCalledWith(1)
        expect(b).toHaveBeenCalledWith(1)
    })

    it("unsubscribe removes only that handler", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const a = jest.fn()
        const b = jest.fn()
        const offA = ctx.client.receive("chat", a)
        ctx.client.receive("chat", b)

        offA()
        ctx.socket.message(JSON.stringify({ key: "chat", value: 1 }))
        await flush()
        expect(a).not.toHaveBeenCalled()
        expect(b).toHaveBeenCalledWith(1)
    })

    it("does not invoke handlers for other keys", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const cb = jest.fn()
        ctx.client.receive("chat", cb)
        ctx.socket.message(JSON.stringify({ key: "other", value: 1 }))
        await flush()
        expect(cb).not.toHaveBeenCalled()
    })

    it("a throwing handler does not prevent other handlers", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const bad = jest.fn(() => {
            throw new Error("bad")
        })
        const good = jest.fn()
        ctx.client.receive("chat", bad)
        ctx.client.receive("chat", good)

        ctx.socket.message(JSON.stringify({ key: "chat", value: 1 }))
        await flush()
        expect(bad).toHaveBeenCalled()
        expect(good).toHaveBeenCalled()
    })

    it("routes correlationId messages to pending, not to receive()", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)

        const cb = jest.fn()
        ctx.client.receive("k", cb)

        const p = ctx.client.send("k", { a: 1 })
        const wire = JSON.parse(ctx.socket.sent[1] as string)
        ctx.socket.message(
            JSON.stringify({ correlationId: wire.correlationId, key: "k", value: 1 })
        )
        await expect(p).resolves.toBe(1)
        expect(cb).not.toHaveBeenCalled()
    })

    it("ignores frames whose deserialize throws", async () => {
        const serializer = makeFakeSerializer()
        const ctx = setup({ serializer })
        await connectAndHandshake(ctx)

        const cb = jest.fn()
        ctx.client.receive("chat", cb)

        serializer.throwOnDeserialize = new Error("bad")
        ctx.socket.message("garbage")
        await flush()
        expect(cb).not.toHaveBeenCalled()
    })
})

describe("useWsClient  lifecycle edge cases", () => {
    it("can reconnect after close", async () => {
        const ctx = setup()
        await connectAndHandshake(ctx)
        ctx.client.close()

        const p = ctx.client.connect()
        await Promise.resolve()
        ctx.socket.open()
        ctx.socket.message(JSON.stringify({ ok: true }))
        await expect(p).resolves.toBeUndefined()
        expect(ctx.client.isConnected()).toBe(true)
    })

    it("state transitions are exactly connecting → handshaking → connected", async () => {
        const ctx = setup()
        const seen: string[] = []
        const p = ctx.client.connect()
        seen.push(ctx.client.state())
        await Promise.resolve()
        ctx.socket.open()
        seen.push(ctx.client.state())
        ctx.socket.message(JSON.stringify({ ok: true }))
        await p
        seen.push(ctx.client.state())
        expect(seen).toEqual(["connecting", "handshaking", "connected"])
    })
})

async function flush(n = 3): Promise<void> {
    for (let i = 0; i < n; i++) await Promise.resolve()
}

function clientReceive(
    ctx: ReturnType<typeof setup>,
    key: string,
    cb: (v: unknown) => void
): void {
    ctx.client.receive(key, cb)
}