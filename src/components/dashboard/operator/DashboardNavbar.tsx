import { Link } from "react-router-dom"
import { BellIcon, MenuIcon } from "lucide-react"

import logoMark from "@/assets/svg/logo-mark.svg"
import logo from "@/assets/svg/logo.svg"
import { AccountMenu } from "@/components/dashboard/operator/AccountMenu"
import { LocationSwitcher } from "@/components/dashboard/operator/LocationSwitcher"
import {
  OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
  OPERATOR_UTILITY_SURFACE_CLASS,
  OperatorShellDisabledChromeButton,
  OperatorShellDisabledSearchField,
  OperatorShellHelpLink,
} from "@/components/dashboard/operator/ShellUtilityChrome"
import { AiAssistantIcon } from "@/components/ui/ai-assistant-icon"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { OPERATOR_SHELL_TOUCH_TARGET_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"
import type { OperatorShellPresentation } from "@/types/operatorHome"

type DashboardNavbarProps = {
  locationSwitcher: OperatorShellPresentation["locationSwitcher"]
  profileDisplayName: string
  profileInitials: string
  profileSelfRoleSubtitle: string | null
  /** Compact “t” mark when the desktop sidebar is collapsed. */
  compactLogo?: boolean
  notificationsUnreadCount?: number
  onOpenNotifications?: () => void
  onOpenNotificationPreferences?: () => void
  onSelectLocation: (locationId: number) => void
  onSignOut: () => void
  onOpenSidebar?: () => void
}

export function DashboardNavbar({
  locationSwitcher,
  profileDisplayName,
  profileInitials,
  profileSelfRoleSubtitle,
  compactLogo = false,
  notificationsUnreadCount = 0,
  onOpenNotifications,
  onOpenNotificationPreferences,
  onSelectLocation,
  onSignOut,
  onOpenSidebar,
}: DashboardNavbarProps) {
  const notificationsEnabled = onOpenNotifications != null
  const showUnreadBadge = notificationsUnreadCount > 0

  return (
    <header className="z-40 h-[60px] w-full shrink-0 overflow-x-hidden bg-[var(--operator-shell-chrome)]">
      <nav
        aria-label="Operator dashboard"
        className="relative flex h-full min-w-0 items-center gap-1.5 overflow-x-hidden py-2 pl-2 pr-2 sm:gap-3 sm:pl-[17px] sm:pr-6 md:gap-4 md:pr-8 lg:gap-[83px] lg:py-[10px] lg:pr-[70px]"
      >
        <div className="flex shrink-0 items-center gap-1.5 sm:gap-2">
          {onOpenSidebar ? (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className={cn(
                "shrink-0 text-foreground lg:hidden",
                OPERATOR_SHELL_TOUCH_TARGET_CLASS
              )}
              aria-label="Open navigation"
              onClick={onOpenSidebar}
            >
              <MenuIcon />
            </Button>
          ) : null}

          <Link
            to="."
            aria-label="tummly"
            className="shrink-0 rounded-sm focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
          >
            <img
              src={logoMark}
              alt=""
              width={21}
              height={31}
              className={cn(
                "h-[31px] w-[21px] object-contain brightness-0 dark:brightness-100",
                compactLogo
                  ? "block sm:hidden lg:block"
                  : "block sm:hidden"
              )}
            />
            <img
              src={logo}
              alt=""
              width={124}
              height={30}
              className={cn(
                "h-6 w-auto max-w-[7.75rem] object-contain brightness-0 sm:h-7 dark:brightness-100",
                compactLogo
                  ? "hidden sm:block lg:hidden"
                  : "hidden sm:block"
              )}
            />
          </Link>
        </div>

        <div className="flex min-w-0 flex-1 items-center justify-end gap-1.5 sm:gap-2 lg:justify-start lg:gap-3">
          <LocationSwitcher
            locationSwitcher={locationSwitcher}
            onSelectLocation={onSelectLocation}
          />

          <OperatorShellDisabledSearchField className="hidden min-w-0 flex-1 lg:flex" />

          <div className="flex shrink-0 items-center gap-0.5 lg:ml-auto">
            <OperatorShellDisabledChromeButton
              label="AI assistant"
              className={cn(
                "hidden gap-1.5 rounded-[2px] pl-2 pr-3 opacity-100 md:inline-flex",
                OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
                "text-xs font-medium text-foreground lg:h-10 lg:min-h-10 lg:gap-2 lg:px-4 lg:text-sm",
                OPERATOR_UTILITY_SURFACE_CLASS
              )}
            >
              <AiAssistantIcon />
              AI assistant
            </OperatorShellDisabledChromeButton>

            <OperatorShellHelpLink className="hidden lg:inline-flex" />

            <Separator
              orientation="vertical"
              className="hidden h-7 w-px shrink-0 self-center data-vertical:h-7 data-vertical:self-center lg:block"
            />

            {notificationsEnabled ? (
              <Button
                type="button"
                variant="ghost"
                className={cn(
                  "relative shrink-0 rounded p-0",
                  "text-foreground hover:bg-black/5 hover:text-foreground",
                  "dark:text-[#707070] dark:hover:bg-white/10 dark:hover:text-[#707070]",
                  OPERATOR_SHELL_TOUCH_TARGET_CLASS,
                  "lg:size-auto lg:h-10 lg:min-h-10 lg:min-w-0 lg:px-3.5"
                )}
                aria-label={
                  showUnreadBadge
                    ? `Notifications, ${notificationsUnreadCount} unread`
                    : "Notifications"
                }
                onClick={onOpenNotifications}
              >
                <span className="relative flex size-5 items-center justify-center">
                  <BellIcon className="size-4" />
                  {showUnreadBadge ? (
                    <span
                      aria-hidden
                      className="absolute top-0 right-0 size-1.5 rounded-full bg-primary"
                    />
                  ) : null}
                </span>
              </Button>
            ) : (
              <OperatorShellDisabledChromeButton
                label="Notifications"
                className={cn(
                  "relative rounded p-0 dark:text-[#707070]",
                  OPERATOR_SHELL_TOUCH_TARGET_CLASS,
                  "lg:size-auto lg:h-10 lg:min-h-10 lg:min-w-0 lg:px-3.5"
                )}
              >
                <span className="relative flex size-5 items-center justify-center">
                  <BellIcon className="size-4" />
                  <span
                    aria-hidden
                    className="absolute top-0 right-0 size-1.5 rounded-full bg-primary"
                  />
                </span>
              </OperatorShellDisabledChromeButton>
            )}

            <AccountMenu
              profileDisplayName={profileDisplayName}
              profileInitials={profileInitials}
              profileSelfRoleSubtitle={profileSelfRoleSubtitle}
              onSignOut={onSignOut}
              onOpenNotificationPreferences={onOpenNotificationPreferences}
            />
          </div>
        </div>
      </nav>
    </header>
  )
}
