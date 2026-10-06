import type { PendingRequest } from "./types.js"

export interface PendingRegistry {
    readonly enqueue: (
        id: number,
        entry: PendingRequest
    ) => void
    readonly resolve: (id: number, value: unknown) => boolean
    readonly remove: (id: number) => void
    readonly rejectAll: (reason: Error) => void
}

export const usePendingRegistry = (): PendingRegistry => {
    const queues = new Map<number, PendingRequest>()

    const enqueue = (id: number, entry: PendingRequest): void => {
        const request = queues.get(id)
        if (request) return
        else queues.set(id, entry)
    }

    const resolve = (id: number, value: unknown): boolean => {
        const request = queues.get(id)
        if (!request) return false
        request.resolve(value)
        queues.delete(id)
        return true
    }

    const remove = (key: number): void => {
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