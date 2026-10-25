import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { authApi, setAccessToken, userApi } from '../api/client'
import type { User } from '../types/finance'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const accessToken = ref<string | null>(null)
  const initialized = ref(false)
  const isAuthenticated = computed(() => !!accessToken.value && !!user.value)

  function setSession(token: string, currentUser: User) {
    accessToken.value = token
    user.value = currentUser
    setAccessToken(token)
  }

  function clearSession() {
    accessToken.value = null
    user.value = null
    setAccessToken(null)
  }

  async function login(email: string, password: string) {
    const { data } = await authApi.login(email, password)
    setSession(data.accessToken, data.user)
  }

  async function register(email: string, displayName: string, password: string) {
    await authApi.register(email, displayName, password)
    await login(email, password)
  }

  async function initialize() {
    if (initialized.value) return
    initialized.value = true
    try {
      const { data } = await authApi.refresh()
      setSession(data.accessToken, data.user)
    } catch {
      clearSession()
    }
  }

  async function updateProfile(displayName: string) {
    const { data } = await userApi.update(displayName)
    user.value = data
  }

  async function logout() {
    try {
      await authApi.logout()
    } finally {
      clearSession()
    }
  }

  window.addEventListener('session-expired', clearSession)

  return {
    user,
    accessToken,
    initialized,
    isAuthenticated,
    login,
    register,
    initialize,
    updateProfile,
    logout,
    clearSession,
  }
})
