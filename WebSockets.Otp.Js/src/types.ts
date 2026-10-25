export type Protocol = "json" | (string & {})

export type Serializer = {
    readonly protocol: Protocol,
    readonly serialize: (data: unknown) => string | BufferSource | Blob
    readonly deserialize: (data: string | BufferSource | Blob) => unknown | Promise<unknown>
}

export interface HandshakeMessage {
    protocol: Protocol
}

export interface EndpointMessage {
    key: string
    correlationId?: number,
    [field: string]: unknown
}

export type ConnectionState =
    | "disconnected"
    | "connecting"
    | "connected"

export interface PendingRequest {
    readonly resolve: (value: any) => void
    readonly reject: (reason?: unknown) => void
}