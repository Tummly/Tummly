import { useEffect, useRef } from "react"
import { useOutletContext, useParams } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { GuestProfilePage } from "@/components/dashboard/operator/GuestProfile/GuestProfilePage"
import { useGuestProfilePageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"

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

export function GuestProfileRoute() {
  const { guestId: guestIdParam } = useParams<{ guestId: string }>()
  const { selectedLocationId, mode } =
    useOutletContext<DashboardOutletContext>()
  const pageModule = useGuestProfilePageModuleApi()
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
    <GuestProfilePage mode={mode} selectedLocationId={selectedLocationId} />
  )
}
