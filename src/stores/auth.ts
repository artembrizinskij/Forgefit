import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { authApi, type PublicUser } from '../api/auth.js'
import { getToken, setToken, clearToken } from '../api/client.js'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<PublicUser | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const isAuthenticated = computed(() => !!user.value)

  function clearError() {
    error.value = null
  }

  async function login(email: string, password: string): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const { token, user: u } = await authApi.login(email, password)
      setToken(token)
      user.value = u
    } catch (e) {
      error.value = (e as Error).message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function register(email: string, password: string, name: string): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const { token, user: u } = await authApi.register(email, password, name)
      setToken(token)
      user.value = u
    } catch (e) {
      error.value = (e as Error).message
      throw e
    } finally {
      loading.value = false
    }
  }

  function logout(): void {
    clearToken()
    user.value = null
  }

  /** Called on app start — tries to restore session from saved token. */
  async function restoreSession(): Promise<void> {
    const token = getToken()
    if (!token) return
    loading.value = true
    try {
      const { user: u } = await authApi.me()
      user.value = u
    } catch {
      // Token invalid/expired — clean up
      clearToken()
      user.value = null
    } finally {
      loading.value = false
    }
  }

  return {
    user,
    loading,
    error,
    isAuthenticated,
    clearError,
    login,
    register,
    logout,
    restoreSession,
  }
})
