import type { PendingRequest } from "./types.js"

export interface PendingRegistry<TKey> {
    readonly enqueue: (
        key: TKey,
        entry: PendingRequest
    ) => void
    readonly resolve: (key: TKey, value: unknown) => boolean
    readonly remove: (key: TKey) => void
    readonly rejectAll: (reason: Error) => void
}

export const usePendingRegistry = <TKey>(): PendingRegistry<TKey> => {
    const queues = new Map<TKey, PendingRequest>()

    const enqueue = (key: TKey, entry: PendingRequest): void => {
        const request = queues.get(key)
        if (request) return
        else queues.set(key, entry)
    }

    const resolve = (key: TKey, value: unknown): boolean => {
        const request = queues.get(key)
        if (!request) return false
        request.resolve(value)
        queues.delete(key)
        return true
    }

    const remove = (key: TKey): void => {
        queues.delete(key)
    }

    const rejectAll = (reason: Error): void => {
        for (const [, request] of queues) {
            request.reject(reason)
        }
        queues.clear()
    }

    return { enqueue, resolve, remove, rejectAll }
}