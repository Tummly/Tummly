import { useEffect, useRef } from "react"
import { Outlet, useLocation, useNavigate, useSearchParams } from "react-router-dom"

import { DashboardShell } from "@/components/dashboard/operator/DashboardShell"
import {
  DashboardUiStoreProvider,
} from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { CapturePageModuleProvider } from "@/components/dashboard/operator/Capture/CapturePageModuleProvider"
import { HomePageModuleProvider } from "@/components/dashboard/operator/Home/HomePageModuleProvider"
import { CampaignsPageModuleProvider } from "@/components/dashboard/operator/Campaigns/CampaignsPageModuleProvider"
import { GuestsPageModuleProvider } from "@/components/dashboard/operator/Guests/GuestsPageModuleProvider"
import { FeedbackPageModuleProvider } from "@/components/dashboard/operator/Feedback/FeedbackPageModuleProvider"
import { useHomePageModule } from "@/components/dashboard/operator/Home/utils/useHomePageModule"
import { useNotificationsModule } from "@/components/dashboard/operator/useNotificationsModule"
import { useWorkspaceSession } from "@/components/dashboard/operator/useWorkspaceSession"
import { Button } from "@/components/ui/button"
import { buildOperatorShellPresentation } from "@/lib/operatorHome/buildShellPresentation"
import { resolveOperatorSidebarActiveId } from "@/lib/operatorHome/operatorDashboardPaths"
import { clearAuthSession } from "@/pages/utils/authHelpers"

type DashboardProps = {
  mode: "single" | "multi"
}

function readQueryLocationId(
  searchParams: URLSearchParams
): number | null {
  const raw = searchParams.get("location")
  if (!raw) {
    return null
  }
  const parsed = Number.parseInt(raw, 10)
  return Number.isFinite(parsed) ? parsed : null
}

function DashboardContent({ mode }: DashboardProps) {
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()
  const queryLocationId = readQueryLocationId(searchParams)

  const workspace = useWorkspaceSession(mode)
  const home = useHomePageModule()
  const notifications = useNotificationsModule()

  const loadRef = useRef(workspace.load)
  const preferRef = useRef(workspace.preferLocationFromQuery)
  const syncHomeRef = useRef(home.syncWorkspace)
  const bootstrappedRef = useRef(false)

  loadRef.current = workspace.load
  preferRef.current = workspace.preferLocationFromQuery
  syncHomeRef.current = home.syncWorkspace

  useEffect(() => {
    if (bootstrappedRef.current) {
      return
    }
    bootstrappedRef.current = true
    void loadRef.current({ queryLocationId })
  }, [queryLocationId])

  useEffect(() => {
    if (!bootstrappedRef.current) {
      return
    }
    if (workspace.snapshot.status !== "loaded") {
      return
    }
    preferRef.current(queryLocationId)
  }, [queryLocationId, workspace.snapshot.status])

  useEffect(() => {
    const selectedLocationId = workspace.snapshot.selectedLocationId
    if (selectedLocationId == null) {
      return
    }
    if (searchParams.get("location") === String(selectedLocationId)) {
      return
    }
    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current)
        next.set("location", String(selectedLocationId))
        return next
      },
      { replace: true }
    )
  }, [workspace.snapshot.selectedLocationId, searchParams, setSearchParams])

  useEffect(() => {
    if (workspace.snapshot.status !== "loaded") {
      return
    }

    void syncHomeRef.current({
      locations: workspace.snapshot.locations,
      selectedLocationId: workspace.snapshot.selectedLocationId,
    })
  }, [
    workspace.snapshot.status,
    workspace.snapshot.locations,
    workspace.snapshot.selectedLocationId,
  ])

  const handleSignOut = () => {
    clearAuthSession()
    navigate("/login", { replace: true })
  }

  if (
    workspace.snapshot.status === "idle" ||
    workspace.snapshot.status === "loading"
  ) {
    return (
      <div
        className="flex min-h-dvh items-center justify-center bg-background"
        role="status"
        aria-live="polite"
        aria-label="Loading dashboard"
      >
        <div
          className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
          aria-hidden
        />
      </div>
    )
  }

  if (workspace.snapshot.status === "error") {
    return (
      <div className="flex min-h-dvh flex-col items-center justify-center gap-4 bg-background">
        <p className="text-destructive">
          Could not load your dashboard. Please try again.
        </p>
        <Button
          variant="outline"
          size="sm"
          onClick={() => void workspace.retry()}
        >
          Retry
        </Button>
      </div>
    )
  }

  const selectedLocationId = workspace.snapshot.selectedLocationId

  if (selectedLocationId == null) {
    return (
      <div className="flex min-h-dvh items-center justify-center bg-background">
        <p className="text-muted-foreground">
          No location found for your account.
        </p>
      </div>
    )
  }

  const presentation = buildOperatorShellPresentation({
    operatorDisplayName: workspace.snapshot.operatorDisplayName,
    activationExpiresAt: workspace.snapshot.activationExpiresAt,
    selfRole: workspace.snapshot.selfRole,
    locations: workspace.snapshot.locations.map((location) => ({
      id: location.id,
      name: location.locationName,
      address: location.address,
      // Deactivation is not shipped yet — every Owned location is Active.
      isActive: true,
    })),
    selectedLocationId,
    locationSwitcherInteractive:
      workspace.snapshot.locationSwitcherInteractive,
    activeNavId: resolveOperatorSidebarActiveId(pathname),
    navTargets: {
      mode,
      locationId: selectedLocationId,
    },
  })

  return (
    <DashboardShell
      presentation={presentation}
      onSelectLocation={workspace.selectLocation}
      onSignOut={handleSignOut}
      notifications={{
        snapshot: notifications.snapshot,
        onOpen: () => {
          void notifications.openDrawer()
        },
        onOpenChange: (open) => {
          if (open) {
            void notifications.openDrawer()
          } else {
            notifications.closeDrawer()
          }
        },
        onSetTab: notifications.setTab,
        onMarkOneRead: notifications.markOneRead,
        onMarkVisibleRead: notifications.markVisibleRead,
        onActivateCta: notifications.activateCta,
        onOpenSettings: () => {
          void notifications.openSettings()
        },
        onCloseSettings: notifications.closeSettings,
        onSetPreference: notifications.setPreference,
      }}
    >
      <Outlet
        context={{
          activationPeriodBadge: presentation.activationPeriodBadge,
          selectedLocationId,
          locations: workspace.snapshot.locations,
          mode,
          selectLocation: workspace.selectLocation,
        }}
      />
    </DashboardShell>
  )
}

export function Dashboard({ mode }: DashboardProps) {
  return (
    <DashboardUiStoreProvider>
      <HomePageModuleProvider>
        <GuestsPageModuleProvider>
          <CapturePageModuleProvider>
            <FeedbackPageModuleProvider>
              <CampaignsPageModuleProvider>
                <DashboardContent mode={mode} />
              </CampaignsPageModuleProvider>
            </FeedbackPageModuleProvider>
          </CapturePageModuleProvider>
        </GuestsPageModuleProvider>
      </HomePageModuleProvider>
    </DashboardUiStoreProvider>
  )
}

export type DashboardOutletContext = {
  activationPeriodBadge: ReturnType<
    typeof buildOperatorShellPresentation
  >["activationPeriodBadge"]
  selectedLocationId: number
  locations: Array<{ id: number; locationName: string; address: string }>
  mode: DashboardProps["mode"]
  selectLocation: (locationId: number) => void
}
