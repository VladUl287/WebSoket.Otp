import type { Serializer } from "./types"

export const asBytes = async (data: BufferSource | Blob): Promise<ArrayBuffer | ArrayBufferView> => {
    if (data instanceof ArrayBuffer) return data
    if (ArrayBuffer.isView(data)) return data
    return new Uint8Array(await data.arrayBuffer())
}

const decoder = new TextDecoder()
export const jsonSerializer = (): Serializer => ({
    protocol: 'json',
    serialize: (data) => JSON.stringify(data),
    deserialize: async (data) => JSON.parse(typeof data === "string" ? data : decoder.decode(await asBytes(data)))
})