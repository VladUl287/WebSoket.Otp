export type Protocol = "json" | (string & {})

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