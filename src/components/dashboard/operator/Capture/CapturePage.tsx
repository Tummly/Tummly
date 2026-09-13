import { useEffect, useRef } from "react"
import { useLocation, useNavigate } from "react-router-dom"

import { CaptureLoadingState } from "@/components/dashboard/operator/Capture/CaptureLoadingState"
import { CaptureSingleShell } from "@/components/dashboard/operator/Capture/CaptureSingleShell"
import { useCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useCapturePageModule"
import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import {
  capturePlacementDetailOpenReplacePath,
  parseCapturePlacementDetailOpenQuery,
} from "@/lib/operatorCapture/captureLocationHandoff"

/** Single-location Capture page — spinner until the Capture module is ready. */
export function CapturePage() {
  const navigate = useNavigate()
  const location = useLocation()
  const capturePageModule = useCapturePageModuleApi()
  const { snapshot } = useCapturePageModule()
  const openQueryConsumedRef = useRef(false)

  useEffect(() => {
    openQueryConsumedRef.current = false
  }, [location.pathname])

  useEffect(() => {
    if (openQueryConsumedRef.current) {
      return
    }
    if (
      snapshot.loadStatus !== "loaded"
      || snapshot.viewModel == null
    ) {
      return
    }

    const searchParams = new URLSearchParams(location.search)
    const qrCodeId = parseCapturePlacementDetailOpenQuery(searchParams)
    if (qrCodeId == null) {
      return
    }

    openQueryConsumedRef.current = true
    // Missing / archived codes fail safely as noop inside the page module.
    capturePageModule.openPlacementDetail(qrCodeId)

    navigate(
      capturePlacementDetailOpenReplacePath(location.pathname, searchParams),
      { replace: true }
    )
  }, [
    snapshot.loadStatus,
    snapshot.viewModel,
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

  return <CaptureSingleShell />
}
