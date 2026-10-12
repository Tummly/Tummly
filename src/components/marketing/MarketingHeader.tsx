import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { MenuIcon } from "lucide-react"

import SignInLink from "@/components/auth/SignInLink"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { MarketingNavLink } from "@/components/marketing/MarketingNavLink"
import { MarketingResourcesMenu } from "@/components/marketing/MarketingResourcesMenu"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import { Button } from "@/components/ui/button"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet"
import { HELP_CENTRE_URL } from "@/config/support"
import {
  MARKETING_DEMO_PATH,
  MARKETING_PRIMARY_NAV,
  MARKETING_RESOURCES_NAV,
} from "@/constants/marketingNav"
import {
  marketingChromeBackground,
  marketingHeaderChromePadding,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"
import { clearAuthSession } from "@/pages/utils/authHelpers"
import { useAuthStore } from "@/stores/authStore"

	const logInClass =
  "rounded-sm px-[18px] py-2.5 text-sm font-medium leading-5 text-[#141414] no-underline hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"

/** Figma marketing Header (`5700:25213`): nav | centred logo | CTAs. */
export default function MarketingHeader() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const navigate = useNavigate()
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const accountType = useAuthStore((state) => state.accountType)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  const isSignedIn =
    hasHydrated
    && Boolean(token)
    && (role === "ADMIN" || role === "USER" || role === "SUPPORT")

  const homePath = (() => {
    if (!isSignedIn) {
      return "/"
    }

    if (role === "ADMIN" || role === "SUPPORT") {
      return "/admin-dashboard"
    }

    if (accountType === "Single") {
      return "/single-dashboard"
    }

    return "/multi-dashboard"
  })()

  const handleLogout = () => {
    clearAuthSession()
    navigate("/login", { replace: true })
  }

  const closeMobile = () => setMobileOpen(false)

  return (
    <header
      className={cn(
        // Below portal overlays (dropdown/sheet z-50) so Resources menu stacks above.
        "sticky top-0 z-40 w-full",
        marketingChromeBackground,
        marketingHeaderChromePadding,
      )}
    >
      <nav
        aria-label="Main"
        className="grid w-full grid-cols-[1fr_auto_1fr] items-center gap-4"
      >
        {/* Full nav needs ~xl; below that use the sheet so mid widths do not collide. */}
        <div className="hidden min-w-0 items-center gap-6 xl:flex xl:gap-9">
          {MARKETING_PRIMARY_NAV.map((item) => (
            <MarketingNavLink
              key={item.id}
              label={item.label}
              href={item.href}
              className="shrink-0 whitespace-nowrap text-sm"
            />
          ))}
          <MarketingResourcesMenu />
        </div>

        <div className="flex xl:hidden" />

        <Link
          to={homePath}
          className="justify-self-center rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"
        >
          <MarketingLogo className="max-w-[min(118px,40vw)]" />
        </Link>

        <div className="flex items-center justify-end justify-self-end">
          <div className="hidden items-center gap-3 xl:flex">
            {isSignedIn ? (
              <>
                {role === "USER" ? (
                  <Link to={HELP_CENTRE_URL} className={logInClass}>
                    Help
                  </Link>
                ) : null}
                <MarketingButton
                  variant="secondary"
                  showArrow={false}
                  onClick={handleLogout}
                >
                  Log out
                </MarketingButton>
              </>
            ) : (
              <>
                <SignInLink to="/login" className={cn(logInClass, "shrink-0")}>
                  Log in
                </SignInLink>
                <MarketingButton asChild variant="secondary">
                  <Link to={MARKETING_DEMO_PATH}>Try demo</Link>
                </MarketingButton>
                <MarketingButton asChild variant="primary">
                  <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                    Start 30-day Pilot
                  </RequestTrialLink>
                </MarketingButton>
              </>
            )}
          </div>

          <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
            <SheetTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="xl:hidden"
                aria-label="Open menu"
              >
                <MenuIcon className="size-5" />
              </Button>
            </SheetTrigger>
            <SheetContent side="right" className="w-full max-w-sm gap-0 p-0">
              <SheetHeader className="border-b border-[#e7e7e7] px-5 py-4">
                <SheetTitle className="text-left text-base font-medium text-[#141414]">
                  Menu
                </SheetTitle>
              </SheetHeader>
              <div className="flex flex-col gap-6 overflow-y-auto px-5 py-6">
                <div className="flex flex-col gap-4">
                  {MARKETING_PRIMARY_NAV.map((item) => (
                    <MarketingNavLink
                      key={item.id}
                      label={item.label}
                      href={item.href}
                      className="text-base"
                      onNavigate={closeMobile}
                    />
                  ))}
                </div>

                <div className="flex flex-col gap-3 border-t border-[#e7e7e7] pt-5">
                  <p className="m-0 text-sm font-medium text-[#141414]">
                    Resources
                  </p>
                  {MARKETING_RESOURCES_NAV.map((item) => (
                    <MarketingNavLink
                      key={item.id}
                      label={item.label}
                      href={item.href}
                      className="text-sm"
                      onNavigate={closeMobile}
                    />
                  ))}
                </div>

                <div className="mt-2 flex flex-col gap-3 border-t border-[#e7e7e7] pt-5">
                  {isSignedIn ? (
                    <>
                      {role === "USER" ? (
                        <Link
                          to={HELP_CENTRE_URL}
                          className="text-sm font-medium text-[#141414]"
                          onClick={closeMobile}
                        >
                          Help
                        </Link>
                      ) : null}
                      <MarketingButton
                        variant="secondary"
                        showArrow={false}
                        className="w-full"
                        onClick={() => {
                          closeMobile()
                          handleLogout()
                        }}
                      >
                        Log out
                      </MarketingButton>
                    </>
                  ) : (
                    <>
                      <SignInLink
                        to="/login"
                        className="text-sm font-medium text-[#141414]"
                        onClick={closeMobile}
                      >
                        Log in
                      </SignInLink>
                      <MarketingButton
                        asChild
                        variant="secondary"
                        className="w-full"
                      >
                        <Link to={MARKETING_DEMO_PATH} onClick={closeMobile}>
                          Try demo
                        </Link>
                      </MarketingButton>
                      <MarketingButton
                        asChild
                        variant="primary"
                        className="w-full"
                      >
                        <RequestTrialLink
                          planIntent={{ plan: "Pilot" }}
                          onClick={closeMobile}
                        >
                          Start 30-day Pilot
                        </RequestTrialLink>
                      </MarketingButton>
                    </>
                  )}
                </div>
              </div>
            </SheetContent>
          </Sheet>
        </div>
      </nav>
    </header>
  )
}
