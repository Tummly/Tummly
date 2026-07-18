import { Link } from "react-router-dom"
import { BellIcon, CircleHelpIcon, MenuIcon, SearchIcon } from "lucide-react"
import type { ReactNode } from "react"

import logoMark from "@/assets/svg/logo-mark.svg"
import logo from "@/assets/svg/logo.svg"
import aiAssistantIcon from "@/assets/svg/ui-icons/ai-assistant.png"
import { OperatorAccountMenu } from "@/components/dashboard/operator/OperatorAccountMenu"
import { OperatorLocationSwitcher } from "@/components/dashboard/operator/OperatorLocationSwitcher"
import HelpCentreHubLink from "@/components/navigation/HelpCentreHubLink"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { HELP_CENTRE_URL } from "@/config/support"
import { cn } from "@/lib/utils"
import type { OperatorShellPresentation } from "@/types/operatorHome"

/** Figma header search / AI / location surface (`#ebebeb` light, `#212121` dark). */
const UTILITY_SURFACE_CLASS =
  "rounded-[2px] bg-[#ebebeb] dark:bg-[#212121]"

/** Shared control height for search + AI (Figma 40px). */
const UTILITY_CONTROL_HEIGHT_CLASS = "h-10 min-h-10"

type OperatorDashboardNavbarProps = {
  locationSwitcher: OperatorShellPresentation["locationSwitcher"]
  profileDisplayName: string
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

function DisabledChromeButton({
  label,
  children,
  className,
}: {
  label: string
  children: ReactNode
  className?: string
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      disabled
      aria-disabled="true"
      aria-label={`${label} (unavailable)`}
      title={`${label} is unavailable`}
      className={cn("shrink-0 text-foreground opacity-50", className)}
    >
      {children}
    </Button>
  )
}

export function OperatorDashboardNavbar({
  locationSwitcher,
  profileDisplayName,
  profileSelfRoleSubtitle,
  compactLogo = false,
  notificationsUnreadCount = 0,
  onOpenNotifications,
  onOpenNotificationPreferences,
  onSelectLocation,
  onSignOut,
  onOpenSidebar,
}: OperatorDashboardNavbarProps) {
  const notificationsEnabled = onOpenNotifications != null
  const showUnreadBadge = notificationsUnreadCount > 0

  return (
    <header className="z-40 h-[60px] w-full shrink-0 bg-[var(--operator-shell-chrome)]">
      <nav
        aria-label="Operator dashboard"
        className="relative flex h-full items-center gap-6 py-2.5 pl-4 pr-6 sm:gap-10 sm:pl-[17px] sm:pr-12 lg:gap-[83px] lg:pr-[70px]"
      >
        <div className="flex shrink-0 items-center gap-3">
          {onOpenSidebar ? (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className="shrink-0 text-foreground lg:hidden"
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
            {compactLogo ? (
              <img
                src={logoMark}
                alt=""
                width={21}
                height={31}
                className="hidden h-[31px] w-[21px] object-contain brightness-0 lg:block dark:brightness-100"
              />
            ) : null}
            <img
              src={logo}
              alt=""
              width={124}
              height={30}
              className={cn(
                "block h-6 w-auto max-w-[7.75rem] object-contain brightness-0 sm:h-7 dark:brightness-100",
                compactLogo && "lg:hidden"
              )}
            />
          </Link>
        </div>

        <div className="flex min-w-0 flex-1 items-center gap-3">
          <div className="shrink-0">
            <OperatorLocationSwitcher
              locationSwitcher={locationSwitcher}
              onSelectLocation={onSelectLocation}
            />
          </div>

          <DisabledChromeButton
            label="Search"
            className="size-9 p-0 md:hidden"
          >
            <SearchIcon />
          </DisabledChromeButton>

          <div
            role="search"
            aria-disabled="true"
            aria-label="Search (unavailable)"
            title="Search is unavailable"
            className={cn(
              "hidden min-w-0 flex-1 items-center gap-3 px-3.5",
              UTILITY_CONTROL_HEIGHT_CLASS,
              "text-sm text-[#707070]",
              "md:flex",
              UTILITY_SURFACE_CLASS
            )}
          >
            <SearchIcon className="size-4 shrink-0" aria-hidden />
            <span className="truncate">
              Search guests, feedback, offers and campaigns…
            </span>
          </div>

          <div className="flex shrink-0 items-center gap-0.5">
            <DisabledChromeButton
              label="AI assistant"
              className={cn(
                "hidden gap-2 rounded-[2px] px-4 opacity-100",
                UTILITY_CONTROL_HEIGHT_CLASS,
                "text-sm font-medium text-foreground md:inline-flex",
                UTILITY_SURFACE_CLASS
              )}
            >
              <span className="relative size-[18px] shrink-0 overflow-hidden">
                <img
                  src={aiAssistantIcon}
                  alt=""
                  width={18}
                  height={18}
                  className="size-full object-cover"
                  aria-hidden
                />
                <span
                  aria-hidden
                  className="absolute inset-0 bg-gradient-to-r from-[#14a946] to-[#135acc] mix-blend-hue"
                />
              </span>
              AI assistant
            </DisabledChromeButton>

            <HelpCentreHubLink
              to={HELP_CENTRE_URL}
              className={cn(
                "inline-flex h-10 shrink-0 items-center gap-1.5 rounded-[2px] px-3.5",
                "text-sm font-medium text-foreground no-underline",
                "hover:bg-black/5 hover:text-foreground",
                "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                "dark:hover:bg-white/10"
              )}
              aria-label="Help Centre"
            >
              <CircleHelpIcon className="size-5 shrink-0" aria-hidden />
              <span>Help</span>
            </HelpCentreHubLink>

            <Separator
              orientation="vertical"
              className="mx-1 hidden h-7 self-center sm:block"
            />

            {notificationsEnabled ? (
              <Button
                type="button"
                variant="ghost"
                className={cn(
                  // Match Account inset: h-9 with size-7 content → 4px all around.
                  "relative size-9 min-h-0 shrink-0 rounded p-0",
                  "text-foreground hover:bg-black/5 hover:text-foreground",
                  "dark:hover:bg-white/10"
                )}
                aria-label={
                  showUnreadBadge
                    ? `Notifications, ${notificationsUnreadCount} unread`
                    : "Notifications"
                }
                onClick={onOpenNotifications}
              >
                <span className="relative flex size-7 items-center justify-center">
                  <BellIcon className="size-4" />
                  {showUnreadBadge ? (
                    <span
                      aria-hidden
                      className="absolute top-0.5 right-0.5 size-1.5 rounded-full bg-primary"
                    />
                  ) : null}
                </span>
              </Button>
            ) : (
              <DisabledChromeButton
                label="Notifications"
                className="relative size-9 min-h-0 rounded p-0"
              >
                <span className="relative flex size-7 items-center justify-center">
                  <BellIcon className="size-4" />
                  <span
                    aria-hidden
                    className="absolute top-0.5 right-0.5 size-1.5 rounded-full bg-primary"
                  />
                </span>
              </DisabledChromeButton>
            )}

            <OperatorAccountMenu
              profileDisplayName={profileDisplayName}
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
