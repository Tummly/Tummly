import { useEffect, useRef } from "react"
import { useOutletContext, useParams } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { GuestEditPage } from "@/components/dashboard/operator/GuestProfile/GuestEditPage"
import { useGuestProfilePageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"
import { parseLocationGuestRouteId } from "@/components/dashboard/operator/GuestProfile/utils/parseLocationGuestRouteId"

export function GuestEditRoute() {
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
    <GuestEditPage mode={mode} selectedLocationId={selectedLocationId} />
  )
}
