import { asBytes } from "../../serializers"
import { Serializer } from "../../types"

export const makeFakeSerializer = (protocol = "json"): Serializer & {
    serializeCalls: number
    deserializeCalls: number
    throwOnDeserialize: Error | null
} => {
    const s = {
        protocol,
        serializeCalls: 0,
        deserializeCalls: 0,
        throwOnDeserialize: null as Error | null,
        serialize(data: unknown): string | BufferSource | Blob {
            s.serializeCalls++
            return JSON.stringify(data)
        },
        async deserialize(data: string | BufferSource | Blob): Promise<unknown> {
            s.deserializeCalls++
            if (s.throwOnDeserialize) throw s.throwOnDeserialize
            const text =
                typeof data === "string"
                    ? data
                    : new TextDecoder().decode(await asBytes(data))
            return JSON.parse(text)
        }
    }
    return s
}