export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: {
    enabled: true
  },
  runtimeConfig: {
    public: {
      wsUrl: 'ws://localhost:5096',
      apiUrl: 'http://localhost:5096',
    }
  },
  modules: [
    '@vueuse/nuxt',
    '@nuxtjs/tailwindcss'
  ],
  tailwindcss: {
    configPath: '~/tailwind.config.js',
    exposeConfig: false,
  }
})
