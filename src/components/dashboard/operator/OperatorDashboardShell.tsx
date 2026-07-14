import { useState, type ReactNode } from "react"

import { OperatorDashboardNavbar } from "@/components/dashboard/operator/OperatorDashboardNavbar"
import { OperatorDashboardSidebar } from "@/components/dashboard/operator/OperatorDashboardSidebar"
import { OperatorNotificationsDrawer } from "@/components/dashboard/operator/OperatorNotificationsDrawer"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import {
  readSidebarCollapsed,
  writeSidebarCollapsed,
} from "@/lib/operatorHome/sidebarCollapsed"
import type {
  OperatorNotificationCategory,
  OperatorNotificationsSnapshot,
  OperatorNotificationsTab,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"
import { cn } from "@/lib/utils"
import type { OperatorShellPresentation } from "@/types/operatorHome"

type OperatorDashboardShellProps = {
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
  children?: ReactNode
}

/** Desktop main gutter — Figma ~40px from main well edge. */
const SHELL_GUTTER_X = "px-6 lg:px-10"

/** Thin muted thumb in the pane gutter — does not sit flush against cards. */
const SHELL_SCROLL_CLASS =
  "min-h-0 flex-1 overflow-y-auto overscroll-contain [scrollbar-width:thin] [scrollbar-color:rgba(92,105,122,0.35)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-[rgba(92,105,122,0.35)] hover:[&::-webkit-scrollbar-thumb]:bg-[rgba(92,105,122,0.5)]"

const SIDEBAR_EXPANDED_WIDTH = "w-[280px]"
const SIDEBAR_COLLAPSED_WIDTH = "w-[94px]"

export function OperatorDashboardShell({
  presentation,
  onSelectLocation,
  onSignOut,
  notifications,
  children,
}: OperatorDashboardShellProps) {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(readSidebarCollapsed)

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

  return (
    <div
      className="flex h-full min-h-0 flex-col overflow-hidden bg-[var(--operator-shell-wash-underlay)]"
      style={{ backgroundImage: "var(--operator-shell-wash)" }}
    >
      <OperatorDashboardNavbar
        activationPeriodBadge={presentation.activationPeriodBadge}
        locationSwitcher={presentation.locationSwitcher}
        profileDisplayName={presentation.profileDisplayName}
        profileInitials={presentation.profileInitials}
        compactLogo={sidebarCollapsed}
        notificationsUnreadCount={notifications?.snapshot.unreadCount}
        onOpenNotifications={notifications?.onOpen}
        onSelectLocation={handleSelectLocation}
        onSignOut={onSignOut}
        onOpenSidebar={() => setMobileNavOpen(true)}
      />

      {notifications ? (
        <OperatorNotificationsDrawer
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
      <div className="flex min-h-0 flex-1">
        <div
          className={cn(
            "hidden min-h-0 shrink-0 bg-transparent lg:flex lg:flex-col",
            "transition-[width] duration-200 ease-out motion-reduce:transition-none",
            sidebarCollapsed ? SIDEBAR_COLLAPSED_WIDTH : SIDEBAR_EXPANDED_WIDTH
          )}
        >
          <OperatorDashboardSidebar
            sidebarNav={presentation.sidebarNav}
            collapsed={sidebarCollapsed}
            onToggleCollapsed={handleToggleSidebarCollapsed}
          />
        </div>

        <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
          <SheetContent
            side="left"
            className="w-[min(18rem,88vw)] gap-0 bg-[var(--operator-shell-main)] p-0 sm:max-w-xs"
          >
            <SheetHeader className="sr-only">
              <SheetTitle>Operator navigation</SheetTitle>
              <SheetDescription>Open dashboard sections.</SheetDescription>
            </SheetHeader>
            <OperatorDashboardSidebar sidebarNav={presentation.sidebarNav} />
          </SheetContent>
        </Sheet>

        <main
          className={cn(
            "relative flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden",
            "rounded-tl-[var(--operator-shell-main-radius)]",
            "bg-[var(--operator-shell-main)]",
            "shadow-[var(--operator-shell-main-shadow)]"
          )}
        >
          <div className="relative z-10 flex min-h-0 flex-1 flex-col pt-8 lg:pt-10">
            <div
              className={cn(
                SHELL_GUTTER_X,
                "mb-6 flex shrink-0 flex-col gap-3 sm:mb-8 sm:flex-row sm:items-center sm:justify-between sm:gap-4"
              )}
            >
              <h1 className="text-2xl font-semibold tracking-tight text-foreground">
                {presentation.pageTitle}
              </h1>
            </div>
            <div className={SHELL_SCROLL_CLASS}>
              <div className={cn(SHELL_GUTTER_X, "pb-10 lg:pb-16")}>
                {children}
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>
  )
}
