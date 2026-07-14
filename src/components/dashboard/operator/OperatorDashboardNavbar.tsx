import { Link } from "react-router-dom"
import { useTheme } from "next-themes"
import {
  BellIcon,
  ChevronDownIcon,
  CircleHelpIcon,
  CrownIcon,
  MenuIcon,
  SearchIcon,
  SparklesIcon,
} from "lucide-react"
import type { ReactNode } from "react"

import logoMark from "@/assets/svg/logo-mark.svg"
import logo from "@/assets/svg/logo.svg"
import { OperatorLocationSwitcher } from "@/components/dashboard/operator/OperatorLocationSwitcher"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type {
  ActivationPeriodBadgeCopy,
  ActivationPeriodBadgeTone,
} from "@/lib/operatorHome/activationPeriod"
import { cn } from "@/lib/utils"
import type { OperatorShellPresentation } from "@/types/operatorHome"

const ACTIVATION_PERIOD_BADGE_TONE_CLASS: Record<
  ActivationPeriodBadgeTone,
  string
> = {
  default: "bg-black/5 text-foreground dark:bg-white/10 dark:text-[#f4f4f4]",
  warning: "bg-[#f3eae4] text-foreground dark:bg-[#f3eae4]/25 dark:text-[#f4f4f4]",
  urgent: "bg-[#f9dfdf] text-foreground dark:bg-[#f9dfdf]/25 dark:text-[#f4f4f4]",
}

type OperatorDashboardNavbarProps = {
  activationPeriodBadge: ActivationPeriodBadgeCopy | null
  locationSwitcher: OperatorShellPresentation["locationSwitcher"]
  profileDisplayName: string
  profileInitials: string
  /** Compact “t” mark when the desktop sidebar is collapsed. */
  compactLogo?: boolean
  notificationsUnreadCount?: number
  onOpenNotifications?: () => void
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
  activationPeriodBadge,
  locationSwitcher,
  profileDisplayName,
  profileInitials,
  compactLogo = false,
  notificationsUnreadCount = 0,
  onOpenNotifications,
  onSelectLocation,
  onSignOut,
  onOpenSidebar,
}: OperatorDashboardNavbarProps) {
  const { theme, setTheme } = useTheme()
  const notificationsEnabled = onOpenNotifications != null
  const showUnreadBadge = notificationsUnreadCount > 0

  return (
    <header className="z-40 h-20 w-full shrink-0 bg-transparent">
      <nav
        aria-label="Operator dashboard"
        className="relative flex h-full items-center justify-between gap-3 px-4 sm:gap-4 sm:px-6 lg:px-[38px]"
      >
        <div className="flex min-w-0 flex-1 items-center gap-3 lg:gap-10">
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

          <div className="shrink-0">
            <OperatorLocationSwitcher
              locationSwitcher={locationSwitcher}
              onSelectLocation={onSelectLocation}
            />
          </div>
        </div>

        {activationPeriodBadge ? (
          <div
            className={cn(
              "pointer-events-none absolute top-1/2 left-1/2 hidden -translate-x-1/2 -translate-y-1/2",
              "lg:flex"
            )}
          >
            <div
              className={cn(
                "inline-flex items-center gap-2 rounded-lg px-3 py-2.5 text-xs",
                ACTIVATION_PERIOD_BADGE_TONE_CLASS[activationPeriodBadge.tone]
              )}
              aria-label={`${activationPeriodBadge.remaining} in your free trial`}
            >
              <CrownIcon className="size-4 shrink-0" aria-hidden />
              <span className="whitespace-nowrap">
                {activationPeriodBadge.remaining} in your free trial.{" "}
                <span className="font-medium text-primary">Choose a plan</span>
              </span>
            </div>
          </div>
        ) : null}

        <div className="flex shrink-0 items-center gap-1 sm:gap-2">
          <DisabledChromeButton label="Search" className="size-9 p-0">
            <SearchIcon />
          </DisabledChromeButton>

          <DisabledChromeButton
            label="AI assistant"
            className={cn(
              "hidden h-auto min-h-0 gap-2 rounded-lg border border-primary/40 px-3.5 py-2",
              "text-sm font-medium text-foreground md:inline-flex"
            )}
          >
            <SparklesIcon className="text-primary" />
            AI assistant
          </DisabledChromeButton>

          <DisabledChromeButton label="Help" className="size-9 p-0">
            <CircleHelpIcon />
          </DisabledChromeButton>

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

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                className={cn(
                  "h-9 min-h-0 gap-2 rounded px-2 py-0",
                  "text-foreground hover:bg-black/5 hover:text-foreground",
                  "aria-expanded:bg-black/5 data-[state=open]:bg-black/5",
                  "dark:hover:bg-white/10 dark:aria-expanded:bg-white/10 dark:data-[state=open]:bg-white/10"
                )}
                aria-label={`Account menu for ${profileDisplayName}`}
              >
                <Avatar
                  size="sm"
                  className="size-7 rounded-[10px] after:rounded-[10px] after:border-0"
                >
                  <AvatarFallback className="rounded-[10px] bg-muted text-xs font-semibold text-foreground">
                    {profileInitials}
                  </AvatarFallback>
                </Avatar>
                <span className="hidden text-sm font-medium sm:inline">
                  Account
                </span>
                <ChevronDownIcon className="size-3.5 opacity-80" aria-hidden />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <DropdownMenuGroup>
                <DropdownMenuLabel className="font-normal">
                  <span className="text-sm font-medium text-foreground">
                    {profileDisplayName}
                  </span>
                </DropdownMenuLabel>
              </DropdownMenuGroup>
              <DropdownMenuSeparator />
              <DropdownMenuGroup>
                <DropdownMenuLabel>Theme</DropdownMenuLabel>
                <DropdownMenuRadioGroup
                  value={theme ?? "system"}
                  onValueChange={setTheme}
                >
                  <DropdownMenuRadioItem value="system">
                    System
                  </DropdownMenuRadioItem>
                  <DropdownMenuRadioItem value="light">
                    Light
                  </DropdownMenuRadioItem>
                  <DropdownMenuRadioItem value="dark">
                    Dark
                  </DropdownMenuRadioItem>
                </DropdownMenuRadioGroup>
              </DropdownMenuGroup>
              <DropdownMenuSeparator />
              <DropdownMenuGroup>
                <DropdownMenuItem onClick={onSignOut}>Sign out</DropdownMenuItem>
              </DropdownMenuGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </nav>
    </header>
  )
}
