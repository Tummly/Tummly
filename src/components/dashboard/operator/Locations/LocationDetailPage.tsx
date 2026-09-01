import { useSyncExternalStore } from "react"
import { ChevronRightIcon } from "lucide-react"
import {
  Link,
  useNavigate,
  useOutletContext,
} from "react-router-dom"

import { useWriteActiveTabToSearchParams } from "@/hooks/useWriteActiveTabToSearchParams"

import { LocationDetailGuestLoopTab } from "@/components/dashboard/operator/Locations/LocationDetailGuestLoopTab"
import { LocationDetailLocationControlsTab } from "@/components/dashboard/operator/Locations/LocationDetailLocationControlsTab"
import { LocationDetailOverviewTab } from "@/components/dashboard/operator/Locations/LocationDetailOverviewTab"
import { LocationDetailSetupDetailsTab } from "@/components/dashboard/operator/Locations/LocationDetailSetupDetailsTab"
import { LocationDetailTeamAccessTab } from "@/components/dashboard/operator/Locations/LocationDetailTeamAccessTab"
import { useLocationDetailPageModuleApi } from "@/components/dashboard/operator/Locations/utils/locationDetailPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
  ACCOUNT_WORKSPACE_FULL_BLEED_X,
  ACCOUNT_WORKSPACE_PAGE_STACK_CLASS,
  ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
  ACCOUNT_WORKSPACE_SHELL_PAD_X,
  ACCOUNT_WORKSPACE_TAB_BODY_CLASS,
  ACCOUNT_WORKSPACE_TAB_LIST_CLASS,
  ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  CAPTURE_BREADCRUMB_CURRENT_CLASS,
  CAPTURE_BREADCRUMB_LINK_CLASS,
  CAPTURE_BREADCRUMB_NAV_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  operatorDashboardCaptureForLocationPath,
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  operatorDashboardOffersRedemptionLogPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import {
  LOCATION_DETAIL_ACTION_BUTTON_CLASS,
  LOCATION_DETAIL_PAGE_COPY,
  type LocationDetailTabId,
} from "@/lib/operatorLocations/locationDetailPresentation"
import {
  LOCATION_LIFECYCLE_LABELS,
  locationLifecycleBadgeVariant,
} from "@/lib/operatorLocations/locationsPresentation"
import { cn } from "@/lib/utils"

export function LocationDetailPage() {
  const pageModule = useLocationDetailPageModuleApi()
  const snap = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const navigate = useNavigate()
  const { mode } = useOutletContext<DashboardOutletContext>()
  const copy = LOCATION_DETAIL_PAGE_COPY

  useWriteActiveTabToSearchParams(snap.activeTabId, { defaultTabId: "overview" })

  const locationsListPath = operatorDashboardNavPath(
    mode,
    "locations",
    snap.locationId
  )
  const createQrPath = operatorDashboardCaptureForLocationPath(
    mode,
    snap.locationId
  )
  const feedbackPath = operatorDashboardNavPath(
    mode,
    "feedback",
    snap.locationId
  )
  const guestsPath = operatorDashboardNavPath(mode, "guests", snap.locationId)
  const offersPath = operatorDashboardNavPath(mode, "offers", snap.locationId)
  const campaignsPath = operatorDashboardNavPath(
    mode,
    "campaigns",
    snap.locationId
  )
  const redemptionsPath = operatorDashboardOffersRedemptionLogPath(
    mode,
    snap.locationId
  )
  const guestProfilePathFor = (locationGuestId: number) =>
    operatorDashboardGuestProfilePath(mode, locationGuestId, snap.locationId)
  const teamPermissionsPath = operatorDashboardNavPath(
    mode,
    "team-permissions",
    snap.locationId
  )

  const lifecycleVariant = locationLifecycleBadgeVariant(snap.lifecycleStatus)
  const lifecycleLabel = LOCATION_LIFECYCLE_LABELS[snap.lifecycleStatus]

  if (snap.loadStatus === "not-found") {
    return (
      <div className={ACCOUNT_WORKSPACE_PAGE_STACK_CLASS}>
        <p className="m-0 text-base font-medium text-op-text-primary">
          {copy.notFound}
        </p>
        <Button
          type="button"
          variant="op-tertiary"
          onClick={() => navigate(locationsListPath)}
        >
          {copy.breadcrumbLocations}
        </Button>
      </div>
    )
  }

  if (snap.loadStatus === "idle" || snap.loadStatus === "loading") {
    return (
      <div
        className="flex flex-1 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading location detail"
      >
        <Spinner />
      </div>
    )
  }

  if (snap.loadStatus === "error") {
    return (
      <div
        className={cn(
          ACCOUNT_WORKSPACE_PAGE_STACK_CLASS,
          "flex flex-1 flex-col items-center justify-center gap-3"
        )}
      >
        <p className="m-0 text-base font-medium text-[var(--op-color-red-550)]">
          {copy.loadError}
        </p>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <Button
            type="button"
            variant="op-secondary"
            onClick={() => {
              void pageModule.load()
            }}
          >
            {copy.retryLoad}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            onClick={() => navigate(locationsListPath)}
          >
            {copy.breadcrumbLocations}
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className={ACCOUNT_WORKSPACE_PAGE_STACK_CLASS}>
      <nav aria-label="Breadcrumb" className={CAPTURE_BREADCRUMB_NAV_CLASS}>
        <Link to={locationsListPath} className={CAPTURE_BREADCRUMB_LINK_CLASS}>
          {copy.breadcrumbLocations}
        </Link>
        <ChevronRightIcon
          className="size-4 shrink-0 text-op-text-muted"
          aria-hidden
        />
        <span className={CAPTURE_BREADCRUMB_CURRENT_CLASS}>{snap.name}</span>
      </nav>

      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{snap.name}</h1>
          <p className={ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS}>
            {snap.headerMeta}
          </p>
          {lifecycleVariant == null ? (
            <span className="text-sm font-medium text-op-text-primary">
              {lifecycleLabel}
            </span>
          ) : (
            <Badge
              variant={lifecycleVariant}
              className={cn(GUESTS_MARKETING_STATUS_BADGE_CLASS, "w-fit")}
            >
              {lifecycleLabel}
            </Badge>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-primary"
            className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
            onClick={() => pageModule.requestTabChange("setup-details")}
          >
            {copy.editLocation}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
            asChild
          >
            <Link to={createQrPath}>{copy.createQrCode}</Link>
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
            asChild
          >
            <Link to={feedbackPath}>{copy.viewFeedback}</Link>
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
            asChild
          >
            <Link to={guestsPath}>{copy.viewGuests}</Link>
          </Button>
        </div>
      </div>

      <Tabs
        value={snap.activeTabId}
        onValueChange={(value) => {
          pageModule.requestTabChange(value as LocationDetailTabId)
        }}
        className="flex min-h-0 flex-1 flex-col gap-0"
      >
        <div
          className={cn(
            ACCOUNT_WORKSPACE_FULL_BLEED_X,
            ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
            "shrink-0"
          )}
        >
          <div className={ACCOUNT_WORKSPACE_SHELL_PAD_X}>
            <TabsList
              variant="line"
              className={ACCOUNT_WORKSPACE_TAB_LIST_CLASS}
            >
              {snap.tabs.map((tab) => (
                <TabsTrigger
                  key={tab.id}
                  value={tab.id}
                  className={ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS}
                >
                  {tab.label}
                </TabsTrigger>
              ))}
            </TabsList>
          </div>
        </div>

        <div
          className={cn(
            ACCOUNT_WORKSPACE_FULL_BLEED_X,
            ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
            ACCOUNT_WORKSPACE_SHELL_PAD_X,
            ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
            ACCOUNT_WORKSPACE_TAB_BODY_CLASS
          )}
        >
          <TabsContent value="overview" className="mt-0">
            <LocationDetailOverviewTab
              snap={snap}
              createQrPath={createQrPath}
              createOfferPath={offersPath}
              createCampaignPath={campaignsPath}
            />
          </TabsContent>

          <TabsContent value="setup-details" className="mt-0">
            <LocationDetailSetupDetailsTab snap={snap} />
          </TabsContent>

          <TabsContent value="guest-loop" className="mt-0">
            <LocationDetailGuestLoopTab
              snap={snap}
              createQrPath={createQrPath}
              createOfferPath={offersPath}
              createCampaignPath={campaignsPath}
              guestsPath={guestsPath}
              feedbackPath={feedbackPath}
              redemptionsPath={redemptionsPath}
              guestProfilePathFor={guestProfilePathFor}
            />
          </TabsContent>

          <TabsContent value="team-access" className="mt-0">
            <LocationDetailTeamAccessTab
              snap={snap}
              teamPermissionsPath={teamPermissionsPath}
            />
          </TabsContent>

          <TabsContent value="location-controls" className="mt-0">
            <LocationDetailLocationControlsTab snap={snap} />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  )
}
