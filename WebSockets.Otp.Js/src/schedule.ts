export type ScheduleOptions = {
    readonly minDelay?: number,
    readonly maxDelay?: number,
    readonly factor?: number,
    readonly jitter?: number,
    readonly maxRetries?: number
}

export type ScheduleReconnect = (callback: () => Promise<any>) => Promise<void>
export type Schedule = {
    readonly run: ScheduleReconnect
    readonly reset: () => void
}

export const buildSchedule = (options?: ScheduleOptions) => (): Schedule => {
    let retries = 0

    const {
        minDelay = 1000,
        maxDelay = 30000,
        maxRetries = Infinity,
        factor = 2,
        jitter = 0.3,
    } = options ?? {}

    const run: ScheduleReconnect = (callback) => {
        if (retries >= maxRetries) {
            return Promise.reject(new Error("Maximum reconnect attempts reached"))
        }

        const base = Math.min(maxDelay, minDelay * Math.pow(factor, retries))
        const jitterOffset = base * jitter * (Math.random() * 2 - 1)
        const delay = Math.max(0, Math.round(base + jitterOffset))
        
        retries += 1

        return new Promise((resolve, reject) => {
            setTimeout(() => callback().then(resolve, reject), delay)
        })
    }

    const reset = (): void => { retries = 0 }
    return { run, reset }
}