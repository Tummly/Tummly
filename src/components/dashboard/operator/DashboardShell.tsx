import { useState, type ReactNode } from "react"
import { Link, useLocation } from "react-router-dom"

import { AiAssistantDrawer } from "@/components/dashboard/operator/AiAssistantDrawer"
import { DashboardNavbar } from "@/components/dashboard/operator/DashboardNavbar"
import { DashboardSidebar } from "@/components/dashboard/operator/DashboardSidebar"
import { MobileNavSheetHeader } from "@/components/dashboard/operator/MobileNavSheetHeader"
import { NotificationsDrawer } from "@/components/dashboard/operator/NotificationsDrawer"
import {
  Alert,
  AlertAction,
  AlertDescription,
  AlertTitle,
} from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import type { GuestMicAudioLevelSource } from "@/lib/guestFeedback/guestMicAudioLevel"
import type {
  OperatorAiAssistantAction,
  OperatorAiAssistantDraftLocation,
  OperatorAiAssistantHelpfulFill,
  OperatorAiAssistantSnapshot,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import { assistantSideNavExpandLock } from "@/lib/operatorHome/assistantSideNavExpandLock"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  OPERATOR_MOBILE_NAV_SHEET_CLASS,
  OPERATOR_SHELL_GUTTER_X,
  OPERATOR_SHELL_GUTTER_Y,
} from "@/lib/operatorHome/shellResponsivePresentation"
import {
  readSidebarCollapsed,
  writeSidebarCollapsed,
} from "@/lib/operatorHome/sidebarCollapsed"
import {
  readSidebarSettingsExpanded,
  writeSidebarSettingsExpanded,
} from "@/lib/operatorHome/sidebarSettingsExpanded"
import type {
  OperatorNotificationCategory,
  OperatorNotificationsSnapshot,
  OperatorNotificationsTab,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"
import { cn } from "@/lib/utils"
import type { OperatorShellPresentation } from "@/types/operatorHome"

type DashboardShellProps = {
  presentation: OperatorShellPresentation
  onSelectLocation: (locationId: number) => void
  onSignOut: () => void
  notifications?: {
    snapshot: OperatorNotificationsSnapshot
    onOpen: () => void
    onOpenChange: (open: boolean) => void
    onSetTab: (tab: OperatorNotificationsTab) => void
    onMarkOneRead: (id: number) => void
    onMarkVisibleRead: () => void
    onActivateCta: (id: number) => void
    onOpenSettings: () => void
    onCloseSettings: () => void
    onSetPreference: (
      category: OperatorNotificationCategory,
      enabled: boolean
    ) => void
  }
  aiAssistant?: {
    snapshot: OperatorAiAssistantSnapshot
    onOpen: () => void
    onOpenChange: (open: boolean) => void
    onStartNewChat: () => void
    onOpenRecent: () => void
    onOpenArchive: () => void
    onBackToConversation: () => void
    onSearchQueryChange: (query: string) => void
    onOpenConversation: (conversationId: string) => void
    onArchiveConversation: (conversationId: string) => void
    onUnarchiveConversation: (conversationId: string) => void
    onRequestDelete: (conversationId: string) => void
    onCancelDelete: () => void
    onConfirmDelete: () => void
    onRetryList: () => void
    onRetryBody: () => void
    onExpand: () => void
    onLeaveExpand: () => void
    onRouteDestination: () => void
    onOpenChangeScope: () => void
    onChangeScopeOpenChange: (open: boolean) => void
    onChangeScopeDraftLocation: (locationId: OperatorAiAssistantDraftLocation) => void
    onChangeScopeDraftReportingPeriod: (range: HomePerformanceDateRange) => void
    onApplyChangeScope: () => void
    onSetComposerDraft: (text: string) => void
    onFillComposerFromChip: (label: string) => void
    onSend: () => void
    onStartMic: () => void
    onConfirmMic: () => void
    onCancelMic: () => void
    onDismissMicError: () => void
    micAudioLevelSource: GuestMicAudioLevelSource
    onRetry: () => void
    onToggleHelpful: (
      messageId: string,
      fill: OperatorAiAssistantHelpfulFill
    ) => void
    onActivateAction: (action: OperatorAiAssistantAction) => void
    onDismissFromEscape: () => void
    onViewUsage: () => void
    onAddCredits: () => void
    onFollowRestorationHelper: () => void
  }
  children?: ReactNode
}

/** Thin muted thumb in the pane gutter — does not sit flush against cards. */
const SHELL_SCROLL_CLASS =
  "min-h-0 flex-1 overflow-y-auto overscroll-contain [scrollbar-width:thin] [scrollbar-color:rgba(92,105,122,0.35)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-[rgba(92,105,122,0.35)] hover:[&::-webkit-scrollbar-thumb]:bg-[rgba(92,105,122,0.5)]"

/** Figma Side-nav expanded / collapsed widths. */
const SIDEBAR_EXPANDED_WIDTH = "w-[260px]"
const SIDEBAR_COLLAPSED_WIDTH = "w-[52px]"

export function DashboardShell({
  presentation,
  onSelectLocation,
  onSignOut,
  notifications,
  aiAssistant,
  children,
}: DashboardShellProps) {
  const location = useLocation()
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(readSidebarCollapsed)
  const [settingsExpanded, setSettingsExpanded] = useState(
    readSidebarSettingsExpanded
  )
  const sideNavExpandLock = assistantSideNavExpandLock({
    priorCollapsed: sidebarCollapsed,
    assistantExpanded:
      aiAssistant?.snapshot.drawerOpen === true
      && aiAssistant.snapshot.widthMode === "expanded",
  })
  const effectiveSidebarCollapsed = sideNavExpandLock.effectiveCollapsed

  const handleSelectLocation = (locationId: number) => {
    onSelectLocation(locationId)
    setMobileNavOpen(false)
  }

  const handleToggleSidebarCollapsed = () => {
    setSidebarCollapsed((prev) => {
      const next = !prev
      writeSidebarCollapsed(next)
      return next
    })
  }

  const handleToggleSettingsExpanded = () => {
    setSettingsExpanded((prev) => {
      const next = !prev
      writeSidebarSettingsExpanded(next)
      return next
    })
  }

  const handleExpandSidebarAndOpenSettings = () => {
    setSidebarCollapsed(false)
    writeSidebarCollapsed(false)
    setSettingsExpanded(true)
    writeSidebarSettingsExpanded(true)
  }

  const handleOpenAiAssistant = () => {
    setMobileNavOpen(false)
    aiAssistant?.onOpen()
  }

  return (
    <div className="flex h-full min-h-0 flex-col overflow-hidden bg-op-header-background">
      <DashboardNavbar
        locationSwitcher={presentation.locationSwitcher}
        profileDisplayName={presentation.profileDisplayName}
        profileInitials={presentation.profileInitials}
        profileSelfRoleSubtitle={presentation.profileSelfRoleSubtitle}
        compactLogo={effectiveSidebarCollapsed}
        notificationsUnreadCount={notifications?.snapshot.unreadCount}
        onOpenNotifications={notifications?.onOpen}
        onOpenNotificationPreferences={
          notifications
            ? () => {
                notifications.onOpen()
                notifications.onOpenSettings()
              }
            : undefined
        }
        onOpenAiAssistant={
          aiAssistant ? handleOpenAiAssistant : undefined
        }
        onRouteDestination={aiAssistant?.onRouteDestination}
        onSelectLocation={handleSelectLocation}
        onSignOut={onSignOut}
        onOpenSidebar={() => setMobileNavOpen(true)}
      />

      {notifications ? (
        <NotificationsDrawer
          snapshot={notifications.snapshot}
          onOpenChange={notifications.onOpenChange}
          onSetTab={notifications.onSetTab}
          onMarkOneRead={notifications.onMarkOneRead}
          onMarkVisibleRead={notifications.onMarkVisibleRead}
          onActivateCta={notifications.onActivateCta}
          onOpenSettings={notifications.onOpenSettings}
          onCloseSettings={notifications.onCloseSettings}
          onSetPreference={notifications.onSetPreference}
        />
      ) : null}

      {aiAssistant ? (
        <AiAssistantDrawer
          snapshot={aiAssistant.snapshot}
          sidebarCollapsed={effectiveSidebarCollapsed}
          onOpenChange={aiAssistant.onOpenChange}
          onStartNewChat={aiAssistant.onStartNewChat}
          onOpenRecent={aiAssistant.onOpenRecent}
          onOpenArchive={aiAssistant.onOpenArchive}
          onBackToConversation={aiAssistant.onBackToConversation}
          onSearchQueryChange={aiAssistant.onSearchQueryChange}
          onOpenConversation={aiAssistant.onOpenConversation}
          onArchiveConversation={aiAssistant.onArchiveConversation}
          onUnarchiveConversation={aiAssistant.onUnarchiveConversation}
          onRequestDelete={aiAssistant.onRequestDelete}
          onCancelDelete={aiAssistant.onCancelDelete}
          onConfirmDelete={aiAssistant.onConfirmDelete}
          onRetryList={aiAssistant.onRetryList}
          onRetryBody={aiAssistant.onRetryBody}
          onExpand={aiAssistant.onExpand}
          onLeaveExpand={aiAssistant.onLeaveExpand}
          onOpenChangeScope={aiAssistant.onOpenChangeScope}
          onChangeScopeOpenChange={aiAssistant.onChangeScopeOpenChange}
          onChangeScopeDraftLocation={aiAssistant.onChangeScopeDraftLocation}
          onChangeScopeDraftReportingPeriod={
            aiAssistant.onChangeScopeDraftReportingPeriod
          }
          onApplyChangeScope={aiAssistant.onApplyChangeScope}
          onSetComposerDraft={aiAssistant.onSetComposerDraft}
          onFillComposerFromChip={aiAssistant.onFillComposerFromChip}
          onSend={aiAssistant.onSend}
          onStartMic={aiAssistant.onStartMic}
          onConfirmMic={aiAssistant.onConfirmMic}
          onCancelMic={aiAssistant.onCancelMic}
          onDismissMicError={aiAssistant.onDismissMicError}
          micAudioLevelSource={aiAssistant.micAudioLevelSource}
          onRetry={aiAssistant.onRetry}
          onToggleHelpful={aiAssistant.onToggleHelpful}
          onActivateAction={aiAssistant.onActivateAction}
          onDismissFromEscape={aiAssistant.onDismissFromEscape}
          onViewUsage={aiAssistant.onViewUsage}
          onAddCredits={aiAssistant.onAddCredits}
          onFollowRestorationHelper={aiAssistant.onFollowRestorationHelper}
        />
      ) : null}
      <div className="flex min-h-0 flex-1">
        <div
          className={cn(
            "hidden min-h-0 shrink-0 lg:flex lg:flex-col",
            "transition-[width] duration-200 ease-out motion-reduce:transition-none",
            effectiveSidebarCollapsed
              ? SIDEBAR_COLLAPSED_WIDTH
              : SIDEBAR_EXPANDED_WIDTH
          )}
        >
          <DashboardSidebar
            sidebarNav={presentation.sidebarNav}
            collapsed={effectiveSidebarCollapsed}
            onToggleCollapsed={
              sideNavExpandLock.toggleLocked
                ? undefined
                : handleToggleSidebarCollapsed
            }
            settingsExpanded={settingsExpanded}
            onToggleSettingsExpanded={handleToggleSettingsExpanded}
            onExpandSidebarAndOpenSettings={
              sideNavExpandLock.toggleLocked
                ? undefined
                : handleExpandSidebarAndOpenSettings
            }
            onNavigate={() => {
              setMobileNavOpen(false)
              aiAssistant?.onRouteDestination()
            }}
          />
        </div>

        <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
          <SheetContent
            side="left"
            showCloseButton={false}
            className={cn(
              "flex flex-col gap-0 bg-op-header-background p-0",
              OPERATOR_MOBILE_NAV_SHEET_CLASS
            )}
          >
            <SheetHeader className="sr-only">
              <SheetTitle>Operator navigation</SheetTitle>
              <SheetDescription>Open dashboard sections.</SheetDescription>
            </SheetHeader>
            <MobileNavSheetHeader />
            <div className="min-h-0 flex-1 overflow-y-auto">
              <DashboardSidebar
                sidebarNav={presentation.sidebarNav}
                settingsExpanded={settingsExpanded}
                onToggleSettingsExpanded={handleToggleSettingsExpanded}
                onNavigate={() => {
                  setMobileNavOpen(false)
                  aiAssistant?.onRouteDestination()
                }}
              />
            </div>
          </SheetContent>
        </Sheet>

        <main
          className={cn(
            "flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden",
            "rounded-tl-[var(--operator-shell-main-radius)]",
            "bg-op-app-background-default"
          )}
        >
          {/*
            Scroll pane must not be a flex column with a flex-1 padded child —
            that combo drops padding-bottom from the scroll height (cards flush
            to the pane edge). Block + min-h-full keeps gutters in overflow.
          */}
          <div className={SHELL_SCROLL_CLASS}>
            {/* Stepped pane gutters — Figma 70px at lg; see shellResponsivePresentation. */}
            <div
              className={cn(
                OPERATOR_SHELL_GUTTER_X,
                OPERATOR_SHELL_GUTTER_Y,
                "box-border flex min-h-full flex-col gap-4"
              )}
            >
              {presentation.lockAlert != null ? (
                <Alert className="shrink-0">
                  <AlertTitle>{presentation.lockAlert.title}</AlertTitle>
                  <AlertDescription>
                    <p>{presentation.lockAlert.body}</p>
                  </AlertDescription>
                  {presentation.lockAlert.buttonLabel != null
                    && presentation.lockAlert.buttonHref != null ? (
                    <AlertAction>
                      <Button asChild variant="op-primary" className="h-8 px-3 text-sm">
                        <Link
                          to={presentation.lockAlert.buttonHref}
                          onClick={(event) => {
                            const href = presentation.lockAlert?.buttonHref
                            if (href == null) {
                              return
                            }
                            const target = new URL(href, window.location.origin)
                            if (target.pathname !== location.pathname) {
                              return
                            }
                            const hashId = target.hash.replace(/^#/, "")
                            if (hashId === "") {
                              return
                            }
                            // Already on landing surface — scroll instead of a no-op nav.
                            event.preventDefault()
                            document
                              .getElementById(hashId)
                              ?.scrollIntoView({ behavior: "smooth", block: "start" })
                          }}
                        >
                          {presentation.lockAlert.buttonLabel}
                        </Link>
                      </Button>
                    </AlertAction>
                  ) : null}
                </Alert>
              ) : null}
              {children}
            </div>
          </div>
        </main>
      </div>
    </div>
  )
}
