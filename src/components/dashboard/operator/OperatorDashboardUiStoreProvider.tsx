import {
  createContext,
  createElement,
  useContext,
  useRef,
  type ReactNode,
} from "react"
import { useStore } from "zustand"

import {
  createOperatorDashboardUiStore,
  type OperatorDashboardUiState,
  type OperatorDashboardUiStore,
} from "@/stores/createOperatorDashboardUiStore"

const OperatorDashboardUiStoreContext =
  createContext<OperatorDashboardUiStore | null>(null)

export function OperatorDashboardUiStoreProvider({
  children,
}: {
  children: ReactNode
}) {
  const storeRef = useRef<OperatorDashboardUiStore | null>(null)
  if (storeRef.current == null) {
    storeRef.current = createOperatorDashboardUiStore()
  }

  return createElement(
    OperatorDashboardUiStoreContext.Provider,
    { value: storeRef.current },
    children
  )
}

export function useOperatorDashboardUiStore<T>(
  selector: (state: OperatorDashboardUiState) => T
): T {
  const store = useContext(OperatorDashboardUiStoreContext)
  if (store == null) {
    throw new Error(
      "useOperatorDashboardUiStore must be used within OperatorDashboardUiStoreProvider"
    )
  }
  return useStore(store, selector)
}

export function useOperatorDashboardUiStoreApi(): OperatorDashboardUiStore {
  const store = useContext(OperatorDashboardUiStoreContext)
  if (store == null) {
    throw new Error(
      "useOperatorDashboardUiStoreApi must be used within OperatorDashboardUiStoreProvider"
    )
  }
  return store
}
