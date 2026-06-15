import { create } from "zustand"
import { createJSONStorage, persist } from "zustand/middleware"

import type { AuthSessionRole } from "@/types/auth"

const AUTH_PERSIST_KEY = "tummly-auth"

/** Legacy keys — migrated once on first hydrate. */
const LEGACY_TOKEN_KEY = "token"
const LEGACY_ROLE_KEY = "role"

interface AuthState {
  token: string | null
  role: AuthSessionRole | null
  _hasHydrated: boolean
  setSession: (token: string, role: AuthSessionRole) => void
  clearSession: () => void
  setHasHydrated: (value: boolean) => void
}

function readLegacySession(): {
  token: string | null
  role: AuthSessionRole | null
} {
  const token = localStorage.getItem(LEGACY_TOKEN_KEY)
  const role = localStorage.getItem(LEGACY_ROLE_KEY)

  if (
    token?.trim() &&
    (role === "ADMIN" || role === "USER")
  ) {
    localStorage.removeItem(LEGACY_TOKEN_KEY)
    localStorage.removeItem(LEGACY_ROLE_KEY)
    return { token, role }
  }

  return { token: null, role: null }
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      role: null,
      _hasHydrated: false,
      setSession: (token, role) => set({ token, role }),
      clearSession: () => set({ token: null, role: null }),
      setHasHydrated: (value) => set({ _hasHydrated: value }),
    }),
    {
      name: AUTH_PERSIST_KEY,
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        token: state.token,
        role: state.role,
      }),
      onRehydrateStorage: () => (state, error) => {
        if (error) {
          useAuthStore.getState().setHasHydrated(true)
          return
        }

        if (!state) {
          useAuthStore.getState().setHasHydrated(true)
          return
        }

        if (!state.token) {
          const legacy = readLegacySession()
          if (legacy.token && legacy.role) {
            state.setSession(legacy.token, legacy.role)
          }
        }

        state.setHasHydrated(true)
      },
    }
  )
)

/** Non-React access for axios and fetch helpers. */
export function getAuthToken(): string | null {
  const token = useAuthStore.getState().token
  return token?.trim() ? token : null
}

export function getAuthRole(): AuthSessionRole | null {
  return useAuthStore.getState().role
}

export function hasAuthSession(): boolean {
  return Boolean(getAuthToken())
}

/** Test-only reset — clears in-memory state and persisted auth slice. */
export function resetAuthStore() {
  useAuthStore.persist.clearStorage()
  useAuthStore.setState({
    token: null,
    role: null,
    _hasHydrated: true,
  })
}
