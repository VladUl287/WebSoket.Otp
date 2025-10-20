<template>
  <div class="chat-container">
    <div class="chat-header">
      <h2>WebSocket Chat</h2>
      <div class="connection-status" :class="status.toLowerCase()">
        {{ status }}
      </div>
    </div>

    <div class="chat-messages" ref="messagesContainer">
      <div v-for="(message, index) in messages" :key="index" class="message">
        <div class="message-header">
          <span class="timestamp">{{ formatTimestamp(message.timestamp) }}</span>
        </div>
        <div class="message-content">{{ message.content }}</div>
      </div>
    </div>

    <div class="chat-controls">
      <div class="message-input-group" :class="{ disabled: !isConnected }">
        <input v-model="newMessage" placeholder="Type your message..." class="message-input" @keyup.enter="sendMessage"
          :disabled="!isConnected" />
        <button @click="sendMessage" :disabled="!isConnected || !newMessage.trim()" class="send-btn">
          Send
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useChatWebSocket } from '~/composables/useChatWebSocket'

defineProps<{
  chatId: string
}>()

const { status, isConnected, messages, newMessage, connect, disconnect, sendMessage } = useChatWebSocket()

const messagesContainer = ref<HTMLElement>()

onMounted(() => {
  connect()
})

onBeforeUnmount(() => {
  disconnect()
})

const formatTimestamp = (timestamp: string) => new Date(timestamp).toLocaleTimeString()

watch(messages, () => {
  nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
    }
  })
}, { deep: true })
</script>

<style scoped>
.chat-container {
  padding: 20px;
  height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: #f5f5f5;
}

.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  padding: 15px;
  background: white;
  border-radius: 10px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.connection-status {
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: bold;
  text-transform: uppercase;
}

.connection-status.open {
  background-color: #4caf50;
  color: white;
}

.connection-status.connecting {
  background-color: #ff9800;
  color: white;
}

.connection-status.closed {
  background-color: #f44336;
  color: white;
}

.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 15px;
  background: white;
  border-radius: 10px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.message {
  margin-bottom: 15px;
  padding: 12px;
  border-radius: 8px;
  background-color: #e3f2fd;
  border-left: 4px solid #2196f3;
}

.message.system {
  background-color: #fff3e0;
  border-left-color: #ff9800;
  text-align: center;
  font-style: italic;
}

.message-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 5px;
  font-size: 12px;
  color: #666;
}

.username {
  font-weight: bold;
  color: #1976d2;
}

.message-content {
  font-size: 14px;
  line-height: 1.4;
  color: #333;
}

.chat-controls {
  background: white;
  padding: 20px;
  border-radius: 10px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.connection-controls {
  display: flex;
  gap: 10px;
  margin-bottom: 15px;
}

.username-input {
  flex: 1;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 14px;
}

.connection-btn {
  padding: 10px 20px;
  border: none;
  border-radius: 6px;
  font-weight: bold;
  cursor: pointer;
  transition: background-color 0.2s;
}

.connection-btn.connect {
  background-color: #4caf50;
  color: white;
}

.connection-btn.disconnect {
  background-color: #f44336;
  color: white;
}

.connection-btn:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}

.message-input-group {
  display: flex;
  gap: 10px;
}

.message-input-group.disabled {
  opacity: 0.6;
}

.message-input {
  flex: 1;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 14px;
}

.send-btn {
  padding: 12px 20px;
  border: none;
  border-radius: 6px;
  background-color: #2196f3;
  color: white;
  font-weight: bold;
  cursor: pointer;
}

.send-btn:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}

.send-btn:hover:not(:disabled) {
  background-color: #1976d2;
}
</style>