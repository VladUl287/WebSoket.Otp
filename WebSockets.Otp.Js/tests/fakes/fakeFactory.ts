import { FakeWebSocket } from "./fakeWebSocket"

type WebSocketFactory = (url: string) => WebSocket

export function makeFakeFactory(): {
    factory: WebSocketFactory
    get socket(): FakeWebSocket
} {
    let socket!: FakeWebSocket
    const factory: WebSocketFactory = () => {
        socket = new FakeWebSocket()
        return socket.asWebSocket()
    }
    return {
        factory,
        get socket() {
            return socket
        }
    }
}