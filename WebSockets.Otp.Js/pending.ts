import type { PendingRequest } from "./types.js"

export interface PendingRegistry {
    readonly enqueue: (
        key: string,
        entry: PendingRequest
    ) => void
    readonly resolveNext: (key: string, value: unknown) => boolean
    readonly remove: (key: string, entry: PendingRequest) => void
    readonly rejectAll: (reason: Error) => void
}

export const usePendingRegistry = (): PendingRegistry => {
    const queues = new Map<string, PendingRequest[]>()

    const enqueue = (key: string, entry: PendingRequest): void => {
        const q = queues.get(key)
        if (q) q.push(entry)
        else queues.set(key, [entry])
    }

    const resolveNext = (key: string, value: unknown): boolean => {
        const q = queues.get(key)
        if (!q || q.length === 0) return false
        const entry = q.shift()!
        if (q.length === 0) queues.delete(key)
        entry.resolve(value)
        return true
    }

    const remove = (key: string, entry: PendingRequest): void => {
        const q = queues.get(key)
        if (!q) return
        const idx = q.indexOf(entry)
        if (idx >= 0) q.splice(idx, 1)
        if (q.length === 0) queues.delete(key)
    }

    const rejectAll = (reason: Error): void => {
        for (const [, q] of queues) {
            for (const entry of q) {
                entry.reject(reason)
            }
        }
        queues.clear()
    }

    return { enqueue, resolveNext, remove, rejectAll }
}