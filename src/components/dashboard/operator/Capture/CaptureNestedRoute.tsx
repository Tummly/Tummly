import { useEffect, useRef } from "react"
import {
  useLocation,
  useNavigate,
  useOutletContext,
  useParams,
} from "react-router-dom"

import { CaptureLoadingState } from "@/components/dashboard/operator/Capture/CaptureLoadingState"
import { CaptureNestedShell } from "@/components/dashboard/operator/Capture/CaptureNestedShell"
import { useCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useCapturePageModule"
import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  decideCaptureNestedLocationSync,
  parseCaptureNestedLocationId,
} from "@/lib/operatorCapture/captureNestedLocationSync"
import {
  captureLocationHandoffHasIntent,
  readCaptureLocationHandoff,
} from "@/lib/operatorCapture/captureLocationHandoff"
import {
  operatorDashboardCaptureLocationPath,
  operatorDashboardNavPath,
} from "@/lib/operatorHome/operatorDashboardPaths"

export function CaptureNestedRoute() {
  const { locationId: rawLocationId } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const { selectedLocationId, locations, selectLocation } =
    useOutletContext<DashboardOutletContext>()
  const capturePageModule = useCapturePageModuleApi()
  const { snapshot } = useCapturePageModule()

  const pathLocationId = parseCaptureNestedLocationId(rawLocationId)
  const previousPathLocationIdRef = useRef<number | null>(null)
  const syncRef = useRef(capturePageModule.syncWorkspace)
  syncRef.current = capturePageModule.syncWorkspace
  const handoffConsumedRef = useRef(false)

  useEffect(() => {
    handoffConsumedRef.current = false
  }, [pathLocationId])

  useEffect(() => {
    const decision = decideCaptureNestedLocationSync({
      pathLocationId,
      previousPathLocationId: previousPathLocationIdRef.current,
      selectedLocationId,
      ownedLocationIds: locations.map((location) => location.id),
    })

    previousPathLocationIdRef.current = pathLocationId

    switch (decision.action) {
      case "sync_workspace_to_path":
        selectLocation(decision.locationId)
        break
      case "sync_path_to_workspace":
      case "redirect_invalid_path":
        navigate(operatorDashboardCaptureLocationPath(decision.locationId), {
          replace: true,
        })
        break
      case "noop":
        break
    }
  }, [
    pathLocationId,
    selectedLocationId,
    locations,
    navigate,
    selectLocation,
  ])

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncRef.current({
      selectedLocationId,
      locations: locations.map((item) => ({
        id: item.id,
        locationName: item.locationName,
        address: item.address,
      })),
    })
  }, [selectedLocationId, locations])

  useEffect(() => {
    if (handoffConsumedRef.current) {
      return
    }
    if (
      snapshot.loadStatus !== "loaded"
      || snapshot.placementsLoadStatus !== "loaded"
      || snapshot.viewModel == null
    ) {
      return
    }

    const handoff = readCaptureLocationHandoff(location.state)
    if (!captureLocationHandoffHasIntent(handoff)) {
      return
    }

    handoffConsumedRef.current = true
    const qrCodeId = handoff.openPlacementDetailQrCodeId
    if (qrCodeId != null) {
      capturePageModule.openPlacementDetail(qrCodeId)
    }
    navigate(`${location.pathname}${location.search}`, {
      replace: true,
      state: null,
    })
  }, [
    snapshot.loadStatus,
    snapshot.placementsLoadStatus,
    snapshot.viewModel,
    location.state,
    location.pathname,
    location.search,
    navigate,
    capturePageModule,
  ])

  if (
    snapshot.viewModel == null &&
    (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return <CaptureLoadingState label="Loading Capture" />
  }

  if (snapshot.viewModel == null) {
    return null
  }

  const captureRootPath = operatorDashboardNavPath(
    "multi",
    "capture",
    selectedLocationId
  )

  return (
    <CaptureNestedShell
      locationName={snapshot.viewModel.locationName}
      selectedLocationId={snapshot.viewModel.locationId}
      locations={locations}
      captureRootPath={captureRootPath}
      onSelectLocation={selectLocation}
    />
  )
}
