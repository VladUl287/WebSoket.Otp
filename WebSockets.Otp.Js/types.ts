export type Protocol = "json" | (string & {})

export type Serializer = {
    readonly protocol: Protocol,
    readonly serialize: (data: unknown) => string | BufferSource | Blob
    readonly deserialize: (data: string | BufferSource | Blob) => unknown | Promise<unknown>
}

export interface HandshakeMessage {
    protocol: Protocol
}

export interface EndpointMessage<T = unknown> {
    key: string
    correlationId?: number,
    [field: string]: unknown
}

export type ConnectionState =
    | "disconnected"
    | "connecting"
    | "handshaking"
    | "connected"
    | "reconnecting"

export interface PendingRequest {
    readonly resolve: (value: unknown) => void
    readonly reject: (reason?: unknown) => void
}