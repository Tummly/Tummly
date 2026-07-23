import { useEffect, useRef } from "react"
import { useOutletContext, useParams } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { GuestEditPage } from "@/components/dashboard/operator/GuestProfile/GuestEditPage"
import { useGuestEditPageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestEditPageModuleContext"

function parseGuestId(raw: string | undefined): number | null {
  if (raw == null || raw.trim() === "") {
    return null
  }

  const parsed = Number(raw)
  if (!Number.isInteger(parsed) || parsed <= 0) {
    return null
  }

  return parsed
}

export function GuestEditRoute() {
  const { guestId: guestIdParam } = useParams<{ guestId: string }>()
  const { selectedLocationId, mode } =
    useOutletContext<DashboardOutletContext>()
  const pageModule = useGuestEditPageModuleApi()
  const syncRef = useRef(pageModule.syncWorkspace)
  syncRef.current = pageModule.syncWorkspace

  const guestId = parseGuestId(guestIdParam)

  useEffect(() => {
    void syncRef.current({
      guestId,
      selectedLocationId,
    })
  }, [guestId, selectedLocationId])

  return (
    <GuestEditPage mode={mode} selectedLocationId={selectedLocationId} />
  )
}
