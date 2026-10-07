export class FakeWebSocket {
    readyState = 0
    binaryType: BinaryType = "arraybuffer"
    sent: Array<string | ArrayBufferLike | Blob | ArrayBufferView> = []
    closeCode?: number
    closeReason?: string
    sendThrows: Error | null = null

    onopen: ((ev: Event) => void) | null = null
    onmessage: ((ev: MessageEvent) => void) | null = null
    onerror: ((ev: Event) => void) | null = null
    onclose: ((ev: CloseEvent) => void) | null = null

    send(data: string | ArrayBufferLike | Blob | ArrayBufferView): void {
        if (this.sendThrows) {
            const err = this.sendThrows
            this.sendThrows = null
            throw err
        }
        this.sent.push(data)
    }

    close(code?: number, reason?: string): void {
        this.closeCode = code
        this.closeReason = reason
        this.readyState = 3
    }

    open(): void {
        this.readyState = 1
        this.onopen?.({} as Event)
    }

    message(data: unknown): void {
        this.onmessage?.({ data } as MessageEvent)
    }

    error(): void {
        this.onerror?.({} as Event)
    }

    closeFromServer(code = 1006, reason = ""): void {
        this.readyState = 3
        this.onclose?.({ code, reason } as CloseEvent)
    }

    asWebSocket(): WebSocket {
        return this as unknown as WebSocket
    }
}