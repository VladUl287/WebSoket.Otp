import { ref, type Ref } from '#imports'
import type { ChatMessage } from '~/types/message'

export interface UseChatOptions {
    chatId: MaybeRefOrGetter<string>,
    fetchUrl: MaybeRefOrGetter<string>,
    token: MaybeRefOrGetter<string>
}

export function useChat(options: UseChatOptions) {
    const messages = ref<ChatMessage[]>([])
    const newMessage = ref('')
    const isLoading = ref(false)

    const addMessage = (message: ChatMessage) => {
        messages.value.push(message)
    }

    const clearMessages = () => {
        messages.value = []
    }

    const createMessage = (content: string, key: string): ChatMessage => ({
        key: key,
        content: content.trim(),
        timestamp: new Date().toISOString(),
        chatId: toValue(options.chatId)
    })

    const loadMessages = async () => {
        isLoading.value = true
        try {
            const fetchedMessages = await $fetch<ChatMessage[]>(toValue(options.fetchUrl), {
                headers: {
                    Authorization: `Bearer ${toValue(options.token)}`
                }
            })
            messages.value = fetchedMessages
        } catch (error) {
            console.error('Failed to load messages:', error)
            throw error
        } finally {
            isLoading.value = false
        }
    }

    return {
        // messages: readonly(messages) as Readonly<Ref<ChatMessage[]>>,
        messages: messages,
        newMessage,
        isLoading: readonly(isLoading),

        addMessage,
        clearMessages,
        createMessage,
        loadMessages
    }
}