export const debounce = <T extends (...args: any[]) => any>(fn: T, delay: number) => {
  let timeoutId: ReturnType<typeof setTimeout> | undefined
  return (...args: Parameters<T>) => {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => fn(...args), delay)
  }
}