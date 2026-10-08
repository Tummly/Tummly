import { useCallback, useEffect, useRef } from "react"
import {
  Navigate,
  Outlet,
  useLocation,
  useNavigate,
  useSearchParams,
} from "react-router-dom"

import { ActivateTummlyPilotDialogHost } from "@/components/dashboard/operator/ActivateTummlyPilotDialogHost"
import { PendingPaymentInterstitialHost } from "@/components/dashboard/operator/PendingPaymentInterstitialHost"
import { DashboardShell } from "@/components/dashboard/operator/DashboardShell"
import {
  DashboardUiStoreProvider,
  useDashboardUiStoreApi,
} from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { planAssistantActionNavigate, planAssistantSendScheduleRoute } from "@/lib/operatorAiAssistant/assistantActionNavigate"
import { CapturePageModuleProvider } from "@/components/dashboard/operator/Capture/CapturePageModuleProvider"
import { HomePageModuleProvider } from "@/components/dashboard/operator/Home/HomePageModuleProvider"
import { CampaignsPageModuleProvider } from "@/components/dashboard/operator/Campaigns/CampaignsPageModuleProvider"
import { GuestsPageModuleProvider } from "@/components/dashboard/operator/Guests/GuestsPageModuleProvider"
import { FeedbackPageModuleProvider } from "@/components/dashboard/operator/Feedback/FeedbackPageModuleProvider"
import { OffersPageModuleProvider } from "@/components/dashboard/operator/Offers/OffersPageModuleProvider"
import { useHomePageModule } from "@/components/dashboard/operator/Home/utils/useHomePageModule"
import { useFeedbackPageModuleApi } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import { useGuestsPageModuleApi } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import { useOffersPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import { useAiAssistantModule } from "@/components/dashboard/operator/useAiAssistantModule"
import { useGlobalSearchModule } from "@/components/dashboard/operator/useGlobalSearchModule"
import { useNotificationsModule } from "@/components/dashboard/operator/useNotificationsModule"
import { useWorkspaceSession } from "@/components/dashboard/operator/useWorkspaceSession"
import { Button } from "@/components/ui/button"
import {
  bindExclusiveAssistantCloser,
  closeExclusivePeerRightDrawers,
} from "@/lib/operatorAiAssistant/assistantExclusiveOpen"
import { buildOperatorShellPresentation } from "@/lib/operatorHome/buildShellPresentation"
import {
  buildOperatorSidebarAreaAccess,
  isDeniedOperatorSidebarActiveId,
  resolveHiddenOperatorSidebarNavIds,
} from "@/lib/operatorHome/operatorAreaChromeAccess"
import type { BillingCreditsAccess } from "@/lib/operatorHome/parseOperatorProfile"
import { getOperatorFirstName } from "@/lib/operatorHome/operatorProfile"
import {
  operatorDashboardCampaignDetailsPath,
  operatorDashboardCapturePlacementDetailPath,
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  operatorDashboardOfferDetailsPath,
  operatorDashboardOffersRedeemPath,
  resolveOperatorSidebarActiveId,
} from "@/lib/operatorHome/operatorDashboardPaths"
import { clearAuthSession } from "@/pages/utils/authHelpers"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

export type DashboardProps = {
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
  const feedbackPage = useFeedbackPageModuleApi()
  const guestsPage = useGuestsPageModuleApi()
  const offersPage = useOffersPageModuleApi()
  const dashboardUiStore = useDashboardUiStoreApi()
  const selectedAssistantLocation = workspace.snapshot.locations.find(
    (location) => location.id === workspace.snapshot.selectedLocationId
  )
  const aiAssistant = useAiAssistantModule({
    mode,
    restaurantName: workspace.snapshot.restaurantName,
    selectedLocation:
      selectedAssistantLocation == null
        ? null
        : {
          id: selectedAssistantLocation.id,
          name: selectedAssistantLocation.locationName,
        },
    locations: workspace.snapshot.locations.map((location) => ({
      id: location.id,
      name: location.locationName,
    })),
    billingCreditsAccess: workspace.snapshot.billingCreditsAccess,
    navigateBillingHref: (href) => {
      navigate(href)
    },
    navigateAction: ({
      action,
      analysisScope,
      recoveryDraft,
      campaignDraft,
      catalogOffer,
      sendScheduleRoute,
    }) => {
      const plan = sendScheduleRoute
        ? planAssistantSendScheduleRoute({
          route: sendScheduleRoute,
          analysisScope,
          mode,
          recoveryDraft,
          campaignDraft,
        })
        : planAssistantActionNavigate({
            action,
            analysisScope,
            mode,
            recoveryDraft,
            campaignDraft,
            catalogOffer,
          })
      if (plan.selectLocationId != null) {
        workspace.selectLocation(plan.selectLocationId)
      }
      if (plan.feedbackDateRange) {
        dashboardUiStore
          .getState()
          .setFeedbackPageDateRange(plan.feedbackDateRange)
      }
      if (plan.feedbackInbox) {
        dashboardUiStore
          .getState()
          .setFeedbackInboxIntent(plan.feedbackInbox)
      }
      if (plan.guests) {
        dashboardUiStore.getState().setGuestsIntent(plan.guests)
      }
      if (plan.campaigns) {
        dashboardUiStore.getState().setCampaignsIntent(plan.campaigns)
      }
      if (plan.offers) {
        dashboardUiStore.getState().setOffersIntent(plan.offers)
      }
      if (plan.captureDateRange) {
        dashboardUiStore
          .getState()
          .setCapturePerformanceDateRange(plan.captureDateRange)
      }
      navigate(plan.path, {
        state: plan.recoveryDraft
          ? { recoveryDraft: plan.recoveryDraft }
          : undefined,
      })
    },
    openRecoveryFromDraftAction: (payload) =>
      feedbackPage.openFromDraftAction(payload),
    closePeerRightDrawers: () => {
      notifications.closeDrawer()
      home.closeFeedbackDetails()
      feedbackPage.closeFeedbackDetails()
      guestsPage.closeGuestDetails()
      guestsPage.closeFeedbackDetails()
      offersPage.closeCreateOfferDrawer()
      closeExclusivePeerRightDrawers()
    },
  })

  const aiAssistantRef = useRef(aiAssistant)
  aiAssistantRef.current = aiAssistant
  const notificationsRef = useRef(notifications)
  notificationsRef.current = notifications
  const profileFirstNameRef = useRef(workspace.snapshot.operatorDisplayName)
  profileFirstNameRef.current = workspace.snapshot.operatorDisplayName

  const globalSearch = useGlobalSearchModule({
    handoffSuggestionToAssistant: (prompt) => {
      notificationsRef.current.closeDrawer()
      // Soft lock / Dormant / zero credits still fill; Send stays gated in Assistant.
      aiAssistantRef.current.openDrawer({
        operatorFirstName: getOperatorFirstName(profileFirstNameRef.current),
      })
      aiAssistantRef.current.setComposerDraft(prompt)
    },
    getLocationId: () => workspace.snapshot.selectedLocationId,
    getAuthorisedLocationCount: () => workspace.snapshot.locations.length,
    navigateToGuestProfile: (guestId, locationId) => {
      navigate(operatorDashboardGuestProfilePath(mode, guestId, locationId))
    },
    navigateToFeedbackDetail: (feedbackId, locationId) => {
      const base = operatorDashboardNavPath(mode, "feedback", locationId)
      const separator = base.includes("?") ? "&" : "?"
      navigate(`${base}${separator}feedbackId=${feedbackId}`)
    },
    navigateToCampaignDetail: (campaignId, locationId) => {
      navigate(
        operatorDashboardCampaignDetailsPath(mode, campaignId, locationId)
      )
    },
    navigateToOfferDetails: (offerId, locationId) => {
      navigate(operatorDashboardOfferDetailsPath(mode, offerId, locationId))
    },
    navigateToCapturePlacementDetail: (qrCodeId, locationId) => {
      navigate(
        operatorDashboardCapturePlacementDetailPath(mode, locationId, qrCodeId)
      )
    },
    navigateToEntityList: ({ entity, q, locationId, scope }) => {
      const navKey =
        entity === "qr-codes"
          ? "capture"
          : entity === "guests"
            ? "guests"
            : entity === "feedback"
              ? "feedback"
              : entity === "campaigns"
                ? "campaigns"
                : "offers"
      const base = operatorDashboardNavPath(mode, navKey, locationId)
      const params = new URLSearchParams()
      params.set("q", q)
      if (scope === "all") {
        params.set("searchScope", "all")
      }
      const separator = base.includes("?") ? "&" : "?"
      navigate(`${base}${separator}${params.toString()}`)
    },
  })

  useEffect(() => {
    // Module no-ops when Search is closed; resets to current scope when open.
    globalSearch.notifyOwnedLocationChanged()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- shell location id only
  }, [workspace.snapshot.selectedLocationId])

  const loadRef = useRef(workspace.load)
  const preferRef = useRef(workspace.preferLocationFromQuery)
  const syncHomeRef = useRef(home.syncWorkspace)
  const bootstrappedRef = useRef(false)

  loadRef.current = workspace.load
  preferRef.current = workspace.preferLocationFromQuery
  syncHomeRef.current = home.syncWorkspace

  /** Picker-only shell sync — navigation URLs own `?location=` via preferLocationFromQuery. */
  const handleSelectLocation = useCallback(
    (locationId: number) => {
      workspace.selectLocation(locationId)
      if (mode !== "multi") {
        return
      }
      setSearchParams(
        (current) => {
          if (current.get("location") === String(locationId)) {
            return current
          }
          const next = new URLSearchParams(current)
          next.set("location", String(locationId))
          return next
        },
        { replace: true }
      )
    },
    [mode, setSearchParams, workspace.selectLocation]
  )

  useEffect(() => {
    return bindExclusiveAssistantCloser(() => {
      aiAssistant.closeDrawer()
    })
  }, [aiAssistant.closeDrawer])

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
    if (workspace.snapshot.status !== "loaded") {
      return
    }

    void syncHomeRef.current({
      locations: workspace.snapshot.locations,
      selectedLocationId: workspace.snapshot.selectedLocationId,
      billingCreditsAccess: workspace.snapshot.billingCreditsAccess,
      workspaceName: workspace.snapshot.restaurantName,
      captureAccess: workspace.snapshot.captureAccess,
    })
  }, [
    workspace.snapshot.status,
    workspace.snapshot.locations,
    workspace.snapshot.selectedLocationId,
    workspace.snapshot.billingCreditsAccess,
    workspace.snapshot.restaurantName,
    workspace.snapshot.captureAccess,
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

  const areaAccess = buildOperatorSidebarAreaAccess(workspace.snapshot)
  const activeNavId = resolveOperatorSidebarActiveId(pathname)
  const staffMayOpenOffers = !isDeniedOperatorSidebarActiveId(
    "offers",
    areaAccess
  )
  if (isDeniedOperatorSidebarActiveId(activeNavId, areaAccess)) {
    // Staff: Offers redeem when Offers is allowed; never bounce to a denied row
    // (Offers "none" + Offers fallback would Navigate-loop).
    const fallbackPath =
      workspace.snapshot.permissionRole === "Staff" && staffMayOpenOffers
        ? operatorDashboardOffersRedeemPath(mode, selectedLocationId)
        : operatorDashboardNavPath(mode, "home", selectedLocationId)
    return <Navigate to={fallbackPath} replace />
  }

  if (
    workspace.snapshot.permissionRole === "Staff"
    && activeNavId === "home"
    && staffMayOpenOffers
  ) {
    return (
      <Navigate
        to={operatorDashboardOffersRedeemPath(mode, selectedLocationId)}
        replace
      />
    )
  }

  const presentation = buildOperatorShellPresentation({
    operatorDisplayName: workspace.snapshot.operatorDisplayName,
    activationExpiresAt: workspace.snapshot.activationExpiresAt,
    subscriptionPlan: workspace.snapshot.subscriptionPlan,
    billingStatus: workspace.snapshot.billingStatus,
    permissionRole: workspace.snapshot.permissionRole,
    billingCreditsAccess: workspace.snapshot.billingCreditsAccess,
    locations: workspace.snapshot.locations.map((location) => {
      const paused = location.lifecycleStatus === "paused"
      return {
        id: location.id,
        name: location.locationName,
        address: location.address,
        isActive: !paused,
        showPausedBadge: paused,
      }
    }),
    selectedLocationId,
    locationSwitcherInteractive:
      workspace.snapshot.locationSwitcherInteractive,
    brandLogoPublicUrl: workspace.snapshot.brandLogoPublicUrl,
    activeNavId,
    navTargets: {
      mode,
      locationId: selectedLocationId,
    },
    hiddenNavIds: resolveHiddenOperatorSidebarNavIds(areaAccess),
  })

  return (
    <DashboardShell
      presentation={presentation}
      onSelectLocation={handleSelectLocation}
      onSignOut={handleSignOut}
      onOperatorProfileChange={workspace.applyOperatorProfile}
      notifications={{
        snapshot: notifications.snapshot,
        onOpen: () => {
          aiAssistant.closeDrawer()
          void notifications.openDrawer()
        },
        onOpenChange: (open) => {
          if (open) {
            aiAssistant.closeDrawer()
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
      aiAssistant={
        workspace.snapshot.aiAssistantAccess
          ? {
            snapshot: aiAssistant.snapshot,
            onOpen: () => {
              notifications.closeDrawer()
              aiAssistant.openDrawer({
                operatorFirstName: presentation.profileFirstName,
              })
            },
            onOpenChange: (open) => {
              if (open) {
                notifications.closeDrawer()
                aiAssistant.openDrawer({
                  operatorFirstName: presentation.profileFirstName,
                })
              } else {
                aiAssistant.closeDrawer()
              }
            },
            onStartNewChat: aiAssistant.startNewChat,
            onOpenRecent: aiAssistant.openRecent,
            onOpenArchive: aiAssistant.openArchive,
            onBackToConversation: aiAssistant.backToConversation,
            onSearchQueryChange: aiAssistant.setSearchQuery,
            onOpenConversation: aiAssistant.openConversation,
            onArchiveConversation: aiAssistant.archiveConversation,
            onUnarchiveConversation: aiAssistant.unarchiveConversation,
            onRequestDelete: aiAssistant.requestDelete,
            onCancelDelete: aiAssistant.cancelDelete,
            onConfirmDelete: aiAssistant.confirmDelete,
            onRetryList: aiAssistant.retryList,
            onRetryBody: aiAssistant.retryBody,
            onExpand: aiAssistant.expandDrawer,
            onLeaveExpand: aiAssistant.leaveExpand,
            onRouteDestination: aiAssistant.leaveExpand,
            onOpenChangeScope: aiAssistant.openChangeScope,
            onChangeScopeOpenChange: (open) => {
              if (open) {
                aiAssistant.openChangeScope()
              } else {
                aiAssistant.cancelChangeScope()
              }
            },
            onChangeScopeDraftLocation: aiAssistant.setChangeScopeDraftLocation,
            onChangeScopeDraftReportingPeriod:
              aiAssistant.setChangeScopeDraftReportingPeriod,
            onApplyChangeScope: aiAssistant.applyChangeScope,
            onSetComposerDraft: aiAssistant.setComposerDraft,
            onFillComposerFromChip: aiAssistant.fillComposerFromChip,
            onSend: aiAssistant.send,
            onStartMic: () => {
              void aiAssistant.startMic()
            },
            onConfirmMic: () => {
              void aiAssistant.confirmMic()
            },
            onCancelMic: () => {
              void aiAssistant.cancelMic()
            },
            onDismissMicError: aiAssistant.dismissMicError,
            micAudioLevelSource: aiAssistant.micAudioLevelSource,
            onRetry: aiAssistant.retry,
            onActivateAction: aiAssistant.clickAction,
            onDismissFromEscape: aiAssistant.dismissFromEscape,
            onRefreshCreditsChrome: aiAssistant.refreshCreditsChrome,
            onViewUsage: aiAssistant.viewUsage,
            onAddCredits: aiAssistant.addCredits,
            onFollowRestorationHelper: aiAssistant.followRestorationHelper,
          }
          : undefined
      }
      globalSearch={{
        snapshot: globalSearch.snapshot,
        onOpen: () => {
          notifications.closeDrawer()
          globalSearch.open()
        },
        onOpenChange: (open) => {
          if (open) {
            notifications.closeDrawer()
            globalSearch.open()
          } else {
            globalSearch.close()
          }
        },
        onQueryChange: globalSearch.setQuery,
        onSelectSuggestion: globalSearch.selectSuggestion,
        onSelectGuestHit: globalSearch.selectGuestHit,
        onSelectFeedbackHit: globalSearch.selectFeedbackHit,
        onSelectCampaignHit: globalSearch.selectCampaignHit,
        onSelectOfferHit: globalSearch.selectOfferHit,
        onSelectQrCodeHit: globalSearch.selectQrCodeHit,
        onViewAllGuests: globalSearch.viewAllGuests,
        onViewAllFeedback: globalSearch.viewAllFeedback,
        onViewAllCampaigns: globalSearch.viewAllCampaigns,
        onViewAllOffers: globalSearch.viewAllOffers,
        onViewAllQrCodes: globalSearch.viewAllQrCodes,
        onRetrySearch: globalSearch.retrySearch,
        onLocationScopeChange: globalSearch.setLocationScope,
        onWidenToAllLocations: globalSearch.widenToAllLocations,
      }}
    >
      <>
        <ActivateTummlyPilotDialogHost
          mode={mode}
          status={workspace.snapshot.status}
          subscriptionPlan={workspace.snapshot.subscriptionPlan}
          selectedLocationId={selectedLocationId}
          billingCreditsAccess={workspace.snapshot.billingCreditsAccess}
          pendingPaymentCheckoutUrl={
            workspace.snapshot.pendingPaymentCheckoutUrl
          }
          reloadWorkspace={() => workspace.load()}
        />
        <PendingPaymentInterstitialHost
          status={workspace.snapshot.status}
          pendingPaymentCheckoutUrl={
            workspace.snapshot.pendingPaymentCheckoutUrl
          }
          reloadWorkspace={() => workspace.load()}
        />
        <Outlet
          context={{
            activationPeriodBadge: presentation.activationPeriodBadge,
            billingCreditsAccess: workspace.snapshot.billingCreditsAccess,
            billingStatus: workspace.snapshot.billingStatus,
            subscriptionPlan: workspace.snapshot.subscriptionPlan,
            permissionRole: workspace.snapshot.permissionRole,
            chargebackRestricted: workspace.snapshot.chargebackRestricted,
            offersAccess: workspace.snapshot.offersAccess,
            captureAccess: workspace.snapshot.captureAccess,
            privacyConsentAccess: workspace.snapshot.privacyConsentAccess,
            selectedLocationId,
            locations: workspace.snapshot.locations,
            brandLogoPublicUrl: workspace.snapshot.brandLogoPublicUrl ?? null,
            mode,
            selectLocation: handleSelectLocation,
            applyRestaurantIdentity: workspace.applyRestaurantIdentity,
            reloadWorkspace: () => workspace.load(),
            summariseFeedbackWithAi: (reportingPeriod: HomePerformanceDateRange) => {
              aiAssistant.summariseFeedbackForPeriod({
                operatorFirstName: presentation.profileFirstName,
                reportingPeriod,
              })
            },
          }}
        />
      </>
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
                <OffersPageModuleProvider>
                  <DashboardContent mode={mode} />
                </OffersPageModuleProvider>
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
  billingCreditsAccess: BillingCreditsAccess
  billingStatus: string
  subscriptionPlan: string
  permissionRole: string
  /** Omit / false keeps purchase CTAs enabled. */
  chargebackRestricted: boolean
  /**
   * Offers Area chrome. Omit / manage keeps write + redemption-log export.
   * Only explicit `"none"` hides Offers (CODING_STANDARDS chrome omit).
   */
  offersAccess: "none" | "view" | "manage"
  /**
   * Capture Area chrome. Omit / view / manage keep Guest form preview.
   * Only explicit `"none"` hides it (CODING_STANDARDS chrome omit).
   */
  captureAccess: "none" | "view" | "manage"
  /** Privacy consent Area chrome — omit/manage keeps Guest consent export visible. */
  privacyConsentAccess: "none" | "view" | "manage"
  selectedLocationId: number
  locations: Array<{
    id: number
    locationName: string
    address: string
    lifecycleStatus?: "active" | "paused"
  }>
  brandLogoPublicUrl: string | null
  mode: DashboardProps["mode"]
  selectLocation: (locationId: number) => void
  applyRestaurantIdentity: (input: {
    restaurantName: string
    brandLogoPublicUrl: string | null
  }) => void
  reloadWorkspace: () => Promise<void>
  summariseFeedbackWithAi: (reportingPeriod: HomePerformanceDateRange) => void
}
