import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const mocks = vi.hoisted(() => ({
  login: vi.fn(),
  register: vi.fn(),
  refresh: vi.fn(),
  logout: vi.fn(),
  setAccessToken: vi.fn(),
}))

vi.mock('../src/api/client', () => ({
  authApi: {
    login: mocks.login,
    register: mocks.register,
    refresh: mocks.refresh,
    logout: mocks.logout,
  },
  setAccessToken: mocks.setAccessToken,
  userApi: { update: vi.fn() },
}))

import { useAuthStore } from '../src/stores/auth'

describe('authentication store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('stores the user and access token after login', async () => {
    const user = { id: 'user-1', email: 'home@example.com', displayName: 'Home', role: 'User' }
    mocks.login.mockResolvedValue({ data: { accessToken: 'access-token', user } })
    const store = useAuthStore()

    await store.login('home@example.com', 'Secret123')

    expect(mocks.login).toHaveBeenCalledWith('home@example.com', 'Secret123')
    expect(store.user).toEqual(user)
    expect(store.accessToken).toBe('access-token')
    expect(store.isAuthenticated).toBe(true)
    expect(mocks.setAccessToken).toHaveBeenCalledWith('access-token')
  })

  it('clears local session even when logout request fails', async () => {
    mocks.logout.mockRejectedValue(new Error('network unavailable'))
    const store = useAuthStore()
    store.clearSession()
    store.user = { id: 'user-1', email: 'home@example.com', displayName: 'Home', role: 'User' }
    store.accessToken = 'access-token'

    await expect(store.logout()).rejects.toThrow('network unavailable')

    expect(store.user).toBeNull()
    expect(store.accessToken).toBeNull()
    expect(store.isAuthenticated).toBe(false)
    expect(mocks.setAccessToken).toHaveBeenLastCalledWith(null)
  })
})
