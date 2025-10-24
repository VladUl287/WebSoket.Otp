<template>
  <div class="min-h-screen bg-gray-900 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
    <div class="sm:mx-auto sm:w-full sm:max-w-md">
      <h2 class="mt-6 text-center text-3xl font-extrabold text-white">
        Sign in to your account
      </h2>
    </div>

    <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
      <div class="bg-gray-800 py-8 px-4 shadow sm:rounded-lg sm:px-10 border border-gray-700">
        <form class="space-y-6" @submit.prevent="handleLogin">
          <div>
            <label for="name" class="block text-sm font-medium text-gray-200">
              Name
            </label>
            <div class="mt-1">
              <input 
                id="name" 
                v-model="form.name" 
                name="name" 
                type="text" 
                required
                class="appearance-none block w-full px-3 py-2 border border-gray-600 rounded-md shadow-sm placeholder-gray-400 bg-gray-700 text-white focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              >
            </div>
          </div>

          <div>
            <label for="password" class="block text-sm font-medium text-gray-200">
              Password
            </label>
            <div class="mt-1">
              <input 
                id="password" 
                v-model="form.password" 
                name="password" 
                type="password" 
                required
                class="appearance-none block w-full px-3 py-2 border border-gray-600 rounded-md shadow-sm placeholder-gray-400 bg-gray-700 text-white focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              >
            </div>
          </div>

          <div>
            <button 
              type="submit"
              class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-500 hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 focus:ring-offset-gray-800 transition-colors"
            >
              Sign in
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const form = reactive({
  name: '',
  password: ''
})

const config = useRuntimeConfig()
const loginUrl = `${config.public.apiUrl}/auth/login`

const token = useCookie('token')

const handleLogin = async () => {
  const result = await $fetch<{ token: string }>(loginUrl, {
    method: 'POST',
    body: form,
  })
  token.value = result.token
  navigateTo('/')
}
</script>