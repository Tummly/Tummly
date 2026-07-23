import { useEffect, useRef } from "react"
import { useOutletContext, useParams } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { GuestProfilePage } from "@/components/dashboard/operator/GuestProfile/GuestProfilePage"
import { useGuestProfilePageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"
import { parseLocationGuestRouteId } from "@/components/dashboard/operator/GuestProfile/utils/parseLocationGuestRouteId"

export function GuestProfileRoute() {
  const { guestId: guestIdParam } = useParams<{ guestId: string }>()
  const { selectedLocationId, mode } =
    useOutletContext<DashboardOutletContext>()
  const pageModule = useGuestProfilePageModuleApi()
  const syncRef = useRef(pageModule.syncWorkspace)
  syncRef.current = pageModule.syncWorkspace

  const guestId = parseLocationGuestRouteId(guestIdParam)

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
