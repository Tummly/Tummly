import { Link, NavLink, useNavigate } from "react-router-dom"
import { useTheme } from "next-themes"
import { MoonIcon, SunIcon } from "lucide-react"

import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  STAFF_DASHBOARD_ADMIN_URL,
  STAFF_DASHBOARD_SUPPORT_URL,
  STAFF_DASHBOARD_URL,
} from "@/config/staffDashboard"
import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"
import { clearAuthSession } from "@/pages/utils/authHelpers"
import { useAuthStore } from "@/stores/authStore"

const THEME_OPTIONS = [
  { value: "system", label: "System" },
  { value: "light", label: "Light" },
  { value: "dark", label: "Dark" },
] as const

const sectionLinkClass = ({ isActive }: { isActive: boolean }) =>
  cn(
    "inline-flex h-8 items-center rounded-md px-3 text-sm font-medium transition-colors",
    isActive
      ? "bg-background text-foreground shadow-sm"
      : "text-muted-foreground hover:text-foreground"
  )

export function StaffDashboardHeader() {
  const navigate = useNavigate()
  const { theme, setTheme, resolvedTheme } = useTheme()
  const role = useAuthStore((state) => state.role)
  const canOpenAdmin = role === "ADMIN"

  const handleLogout = () => {
    clearAuthSession()
    navigate("/login", { replace: true })
  }

  return (
    <header className="sticky top-0 z-40 w-full border-b border-border bg-background">
      <div className="flex h-14 w-full items-center justify-between gap-4 px-4 lg:px-8">
        <div className="flex min-w-0 items-center gap-4 sm:gap-6">
          <Link
            to={STAFF_DASHBOARD_URL}
            className="shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <MarketingLogo
              onLight={resolvedTheme !== "dark"}
              className="max-w-[min(118px,36vw)]"
            />
          </Link>

          {canOpenAdmin ? (
            <nav
              aria-label="Staff sections"
              className="inline-flex items-center rounded-lg bg-muted p-0.75"
            >
              <NavLink
                to={STAFF_DASHBOARD_ADMIN_URL}
                className={sectionLinkClass}
              >
                Admin
              </NavLink>
              <NavLink
                to={STAFF_DASHBOARD_SUPPORT_URL}
                className={sectionLinkClass}
              >
                Support
              </NavLink>
            </nav>
          ) : (
            <p className="text-sm font-medium text-muted-foreground">Support</p>
          )}
        </div>

        <div className="flex shrink-0 items-center gap-2">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label="Theme"
              >
                {resolvedTheme === "dark" ? (
                  <MoonIcon className="size-4" aria-hidden />
                ) : (
                  <SunIcon className="size-4" aria-hidden />
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              className={cn("w-36", OPERATOR_SHELL_MENU_PANEL_CLASS)}
            >
              <DropdownMenuRadioGroup
                value={theme ?? "system"}
                onValueChange={setTheme}
              >
                {THEME_OPTIONS.map((option) => (
                  <DropdownMenuRadioItem
                    key={option.value}
                    value={option.value}
                    className={OPERATOR_SHELL_MENU_ITEM_CLASS}
                  >
                    {option.label}
                  </DropdownMenuRadioItem>
                ))}
              </DropdownMenuRadioGroup>
            </DropdownMenuContent>
          </DropdownMenu>

          <Button type="button" variant="op-secondary" onClick={handleLogout}>
            Log out
          </Button>
        </div>
      </div>
    </header>
  )
}
