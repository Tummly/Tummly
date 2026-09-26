import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { MenuIcon } from "lucide-react"

import marketingArrowRight from "@/assets/svg/marketing-arrow-right.svg"
import SignInLink from "@/components/auth/SignInLink"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { MarketingNavLink } from "@/components/marketing/MarketingNavLink"
import { MarketingResourcesMenu } from "@/components/marketing/MarketingResourcesMenu"
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
  MARKETING_PRIMARY_NAV,
  MARKETING_PRICING_PATH,
  MARKETING_RESOURCES_NAV,
} from "@/constants/marketingNav"
import {
  marketingChromeBackground,
  marketingChromeContentInset,
  marketingHeaderBannerGap,
  marketingHeaderChromePadding,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"
import { clearAuthSession } from "@/pages/utils/authHelpers"
import { useAuthStore } from "@/stores/authStore"

const pilotButtonClass =
  "h-auto min-h-0 w-auto rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-5 whitespace-nowrap text-white hover:bg-[#14a74a]/90"

const logInClass =
  "rounded-sm px-[18px] py-2.5 text-sm font-medium text-[#141414] no-underline hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"

const forGroupsBannerClass =
  "inline-flex items-center gap-2 rounded-[4px] text-xs font-medium leading-5 text-[#fafafa] no-underline hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/40"

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

    if (role === "ADMIN") {
      return "/admin-dashboard"
    }

    if (role === "SUPPORT") {
      return "/support-dashboard"
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
      <div className={cn("flex w-full flex-col", marketingHeaderBannerGap)}>
        <div
          className={cn(
            "flex min-h-12 w-full flex-wrap items-center justify-center gap-x-[17px] gap-y-1 bg-[#141414] py-2",
            marketingChromeContentInset,
          )}
        >
          <p className="m-0 text-center text-sm text-[#f4f4f4]">
            Planning a multi-Location rollout?
          </p>
          <Link to={MARKETING_PRICING_PATH} className={forGroupsBannerClass}>
            See Tummly for groups
            <img
              src={marketingArrowRight}
              alt=""
              width={15}
              height={10}
              className="block size-auto h-2.5 w-[15px] shrink-0"
              aria-hidden
            />
          </Link>
        </div>

        <div className="w-full bg-[#fafafa]">
          <nav
            aria-label="Main"
            className={cn(
              "flex h-[78px] w-full items-center justify-between gap-4",
              marketingChromeContentInset,
            )}
          >
          <div className="flex min-w-0 items-center gap-6 lg:gap-[42px]">
            <Link
              to={homePath}
              className="shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"
            >
              <MarketingLogo className="max-w-[min(118px,40vw)]" />
            </Link>

            <div className="hidden items-center gap-9 lg:flex">
              {MARKETING_PRIMARY_NAV.map((item) => (
                <MarketingNavLink
                  key={item.id}
                  label={item.label}
                  href={item.href}
                  className="text-sm"
                />
              ))}
              <MarketingResourcesMenu />
            </div>
          </div>

          <div className="hidden items-center gap-3 lg:flex">
            {isSignedIn ? (
              <>
                {role === "USER" ? (
                  <Link
                    to={HELP_CENTRE_URL}
                    className={logInClass}
                  >
                    Help
                  </Link>
                ) : null}
                <Button
                  variant="secondary"
                  onClick={handleLogout}
                  className="h-[44px] min-h-0 rounded-[4px] px-[18px] text-sm"
                >
                  Log out
                </Button>
              </>
            ) : (
              <>
                <SignInLink to="/login" className={logInClass}>
                  Log in
                </SignInLink>
                <Button asChild className={pilotButtonClass}>
                  <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                    Start 30 day Pilot
                  </RequestTrialLink>
                </Button>
              </>
            )}
          </div>

          <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
            <SheetTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="lg:hidden"
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
                      <Button
                        variant="secondary"
                        className="rounded-[4px]"
                        onClick={() => {
                          closeMobile()
                          handleLogout()
                        }}
                      >
                        Log out
                      </Button>
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
                      <Button asChild className={cn(pilotButtonClass, "w-full")}>
                        <RequestTrialLink
                          planIntent={{ plan: "Pilot" }}
                          onClick={closeMobile}
                        >
                          Start 30 day Pilot
                        </RequestTrialLink>
                      </Button>
                    </>
                  )}
                </div>
              </div>
            </SheetContent>
          </Sheet>
        </nav>
        </div>
      </div>
    </header>
  )
}
