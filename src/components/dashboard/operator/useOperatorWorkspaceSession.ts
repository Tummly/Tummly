import { useRef, useSyncExternalStore } from "react"

import { getLocations } from "@/api/dashboardApi"
import { fetchCurrentUser } from "@/api/loginContextClient"
import {
  createOperatorWorkspaceSession,
  type OperatorWorkspaceMode,
  type OperatorWorkspaceSession,
  type OperatorWorkspaceSnapshot,
} from "@/lib/operatorWorkspace/createOperatorWorkspaceSession"
import {
  getSelectedLocationId,
  persistSelectedLocation,
} from "@/pages/utils/authHelpers"

export type OperatorWorkspaceSessionApi = {
  snapshot: OperatorWorkspaceSnapshot
  load: OperatorWorkspaceSession["load"]
  retry: OperatorWorkspaceSession["retry"]
  selectLocation: OperatorWorkspaceSession["selectLocation"]
  preferLocationFromQuery: OperatorWorkspaceSession["preferLocationFromQuery"]
}

export function useOperatorWorkspaceSession(
  mode: OperatorWorkspaceMode
): OperatorWorkspaceSessionApi {
  const sessionRef = useRef<OperatorWorkspaceSession | null>(null)

  if (sessionRef.current == null) {
    sessionRef.current = createOperatorWorkspaceSession(
      { mode },
      {
        getLocations,
        fetchCurrentUser,
        getPersistedLocationId: getSelectedLocationId,
        persistSelectedLocation,
      }
    )
  }

  const session = sessionRef.current
  const snapshot = useSyncExternalStore(
    session.subscribe,
    session.getSnapshot,
    session.getSnapshot
  )

  return {
    snapshot,
    load: session.load,
    retry: session.retry,
    selectLocation: session.selectLocation,
    preferLocationFromQuery: session.preferLocationFromQuery,
  }
}
