import { useState } from "react"
import { Link } from "react-router-dom"
import { useTheme } from "next-themes"
import {
  ChevronDownIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  LogOutIcon,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_URL,
} from "@/config/support"
import { OPERATOR_SHELL_TOUCH_TARGET_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

type AccountMenuPanel = "root" | "theme"

const THEME_OPTIONS = [
  { value: "system", label: "System" },
  { value: "dark", label: "Dark" },
  { value: "light", label: "Light" },
] as const

type OperatorAccountMenuProps = {
  profileDisplayName: string
  profileInitials: string
  profileSelfRoleSubtitle: string | null
  onSignOut: () => void
  onOpenNotificationPreferences?: () => void
}

export function OperatorAccountMenu({
  profileDisplayName,
  profileInitials,
  profileSelfRoleSubtitle,
  onSignOut,
  onOpenNotificationPreferences,
}: OperatorAccountMenuProps) {
  const [open, setOpen] = useState(false)
  const [panel, setPanel] = useState<AccountMenuPanel>("root")
  const notificationPreferencesEnabled = onOpenNotificationPreferences != null

  const handleOpenChange = (nextOpen: boolean) => {
    setOpen(nextOpen)
    // Reset to root only when reopening so close does not flash the root panel.
    if (nextOpen) {
      setPanel("root")
    }
  }

  return (
    <DropdownMenu open={open} onOpenChange={handleOpenChange}>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          className={cn(
            "h-auto min-h-0 gap-0 rounded px-0 py-0 md:gap-2.5 md:px-2 md:py-1",
            "text-foreground hover:bg-black/5 hover:text-foreground",
            "aria-expanded:bg-black/5 data-[state=open]:bg-black/5",
            "dark:hover:bg-white/10 dark:aria-expanded:bg-white/10 dark:data-[state=open]:bg-white/10",
            // Initials-only below md — compact hit area.
            OPERATOR_SHELL_TOUCH_TARGET_CLASS,
            "justify-center md:size-auto md:min-h-0 md:min-w-0 md:justify-start md:px-2 md:py-1"
          )}
          aria-label={`Account menu for ${profileDisplayName}`}
        >
          <Avatar
            size="sm"
            className="size-6 bg-[#ebebeb] after:border-transparent md:hidden dark:bg-[#333]"
          >
            <AvatarFallback className="bg-transparent text-[10px] font-semibold text-foreground">
              {profileInitials}
            </AvatarFallback>
          </Avatar>
          <span className="hidden min-w-0 flex-col items-start gap-0.5 text-left leading-normal md:flex">
            <span className="truncate text-sm font-medium text-foreground">
              {profileDisplayName}
            </span>
            {profileSelfRoleSubtitle ? (
              <span className="truncate text-xs font-normal text-muted-foreground">
                {profileSelfRoleSubtitle}
              </span>
            ) : null}
          </span>
          <ChevronDownIcon
            className="hidden size-3 shrink-0 opacity-80 md:block"
            aria-hidden
          />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56 p-1">
        {panel === "theme" ? (
          <ThemeSwitchPanel onBack={() => setPanel("root")} />
        ) : (
          <RootAccountPanel
            notificationPreferencesEnabled={notificationPreferencesEnabled}
            onOpenNotificationPreferences={onOpenNotificationPreferences}
            onOpenThemeSwitch={() => setPanel("theme")}
            onSignOut={onSignOut}
          />
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

function RootAccountPanel({
  notificationPreferencesEnabled,
  onOpenNotificationPreferences,
  onOpenThemeSwitch,
  onSignOut,
}: {
  notificationPreferencesEnabled: boolean
  onOpenNotificationPreferences?: () => void
  onOpenThemeSwitch: () => void
  onSignOut: () => void
}) {
  return (
    <>
      <DropdownMenuGroup>
        <DropdownMenuItem disabled>My account</DropdownMenuItem>
      </DropdownMenuGroup>
      <DropdownMenuSeparator />
      <DropdownMenuGroup>
        <DropdownMenuItem
          disabled={!notificationPreferencesEnabled}
          onSelect={() => {
            onOpenNotificationPreferences?.()
          }}
        >
          Notification preferences
        </DropdownMenuItem>
        <DropdownMenuItem
          onSelect={(event) => {
            event.preventDefault()
            onOpenThemeSwitch()
          }}
        >
          Theme Switch
          <ChevronRightIcon className="ml-auto size-4 opacity-80" aria-hidden />
        </DropdownMenuItem>
        <DropdownMenuItem asChild>
          <Link to={HELP_CENTRE_CONTACT_URL}>Send feedback</Link>
        </DropdownMenuItem>
        <DropdownMenuItem asChild>
          <Link to={HELP_CENTRE_URL}>Help & support</Link>
        </DropdownMenuItem>
      </DropdownMenuGroup>
      <DropdownMenuSeparator />
      <DropdownMenuGroup>
        <DropdownMenuItem onSelect={onSignOut}>
          <LogOutIcon aria-hidden />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuGroup>
    </>
  )
}

function ThemeSwitchPanel({ onBack }: { onBack: () => void }) {
  // Subscribe only while the theme panel is mounted (not on every account-menu open).
  const { theme, setTheme } = useTheme()

  return (
    <>
      <DropdownMenuGroup>
        <DropdownMenuItem
          onSelect={(event) => {
            event.preventDefault()
            onBack()
          }}
        >
          <ChevronLeftIcon className="size-4" aria-hidden />
          Theme Switch
        </DropdownMenuItem>
      </DropdownMenuGroup>
      <DropdownMenuSeparator />
      <DropdownMenuRadioGroup
        value={theme ?? "system"}
        onValueChange={setTheme}
      >
        {THEME_OPTIONS.map((option) => (
          <DropdownMenuRadioItem
            key={option.value}
            value={option.value}
            onSelect={(event) => {
              event.preventDefault()
            }}
            className={cn(
              "rounded-none border-r-2 border-transparent pr-3",
              "data-[state=checked]:border-primary data-[state=checked]:text-primary",
              "data-[state=checked]:focus:text-primary",
              "[&>[data-slot=dropdown-menu-checkbox-item-indicator]]:hidden"
            )}
          >
            {option.label}
          </DropdownMenuRadioItem>
        ))}
      </DropdownMenuRadioGroup>
    </>
  )
}
