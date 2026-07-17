import { useEffect, useRef } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { OperatorDashboardShell } from "@/components/dashboard/operator/OperatorDashboardShell"
import { OperatorHomeBody } from "@/components/dashboard/operator/OperatorHomeBody"
import { useOperatorHomePageModule } from "@/components/dashboard/operator/useOperatorHomePageModule"
import { useOperatorNotificationsModule } from "@/components/dashboard/operator/useOperatorNotificationsModule"
import { useOperatorWorkspaceSession } from "@/components/dashboard/operator/useOperatorWorkspaceSession"
import { Button } from "@/components/ui/button"
import { buildOperatorShellPresentation } from "@/lib/operatorHome/buildShellPresentation"
import { clearAuthSession } from "@/pages/utils/authHelpers"

type OperatorDashboardProps = {
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

export function OperatorDashboard({ mode }: OperatorDashboardProps) {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const queryLocationId = readQueryLocationId(searchParams)

  const workspace = useOperatorWorkspaceSession(mode)
  const home = useOperatorHomePageModule()
  const notifications = useOperatorNotificationsModule()

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

  const viewModel = home.snapshot.viewModel
  const presentation = buildOperatorShellPresentation({
    operatorDisplayName: workspace.snapshot.operatorDisplayName,
    activationExpiresAt: workspace.snapshot.activationExpiresAt,
    locations: workspace.snapshot.locations.map((location) => ({
      id: location.id,
      name: location.locationName,
    })),
    selectedLocationId,
    locationSwitcherInteractive:
      workspace.snapshot.locationSwitcherInteractive,
  })
  const feedbackState =
    home.snapshot.loadStatus === "idle" ||
    home.snapshot.loadStatus === "loading" ||
    viewModel == null
      ? "loading"
      : home.snapshot.loadStatus

  return (
    <OperatorDashboardShell
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
      {home.snapshot.actionError ? (
        <p className="mb-3 text-sm text-destructive" role="alert">
          {home.snapshot.actionError}
        </p>
      ) : null}
      {viewModel == null ? (
        <div
          className="flex min-h-48 items-center justify-center"
          role="status"
          aria-live="polite"
          aria-label="Loading home"
        >
          <div
            className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
            aria-hidden
          />
        </div>
      ) : (
        <OperatorHomeBody
          viewModel={viewModel}
          feedbackState={feedbackState}
          onRetryFeedback={() => {
            void home.retryLoad()
          }}
          previewBusy={home.snapshot.previewBusy}
          downloadBusy={home.snapshot.downloadBusy}
          onPreviewGuestForm={home.previewGuestForm}
          onDownloadQr={home.downloadQr}
          feedbackDetails={home.snapshot.feedbackDetails}
          onViewFeedback={(feedbackId) => {
            void home.openFeedbackDetails(feedbackId)
          }}
          onFeedbackDetailsOpenChange={(open) => {
            if (!open) {
              home.closeFeedbackDetails()
            }
          }}
          onRetryFeedbackDetails={() => {
            void home.retryFeedbackDetails()
          }}
          onStartClassificationCorrection={() => {
            home.startClassificationCorrection()
          }}
          onClassificationDraftSentimentChange={(sentiment) => {
            home.setClassificationDraftSentiment(sentiment)
          }}
          onCancelClassificationCorrection={() => {
            home.cancelClassificationCorrection()
          }}
          onSaveClassificationCorrection={() => {
            void home.saveClassificationCorrection()
          }}
        />
      )}
    </OperatorDashboardShell>
  )
}
