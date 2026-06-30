import { Link } from "react-router-dom"

import logo from "@/assets/svg/logo.svg"
import SignInLink from "@/components/auth/SignInLink"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import { Button } from "@/components/ui/button"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { marketingSectionInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const footerNavLinkClass =
  "text-xs font-medium text-[#e7e7e7] no-underline hover:text-white hover:underline"

const footerInlineLinkClass =
  "text-xs font-medium text-[#a4a4a4] no-underline hover:text-[#e7e7e7] hover:underline"

const footerButtonClass =
  "h-[35px] min-h-[35px] px-[17px] text-sm lg:h-[38px] lg:min-h-[38px] lg:px-[17px] lg:text-base lg:leading-5"

export default function Footer() {
  return (
    <footer className="w-full bg-[#141414]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-[42px] py-10 lg:gap-10.5",
          marketingSectionInset
        )}
      >
        <div className="flex flex-col items-start gap-[26px] lg:flex-row lg:items-center lg:justify-between lg:gap-8">
          <Link
            to="/"
            className="shrink-0 rounded-sm focus-visible:ring-2 focus-visible:ring-white/40 focus-visible:outline-none"
          >
            <img
              src={logo}
              alt="Tummly"
              width={138}
              height={34}
              className="block h-[34px] w-auto max-w-[min(138px,42vw)] object-contain lg:h-8.5"
            />
          </Link>

          <div className="flex flex-row items-center gap-3 lg:gap-4">
            <Button asChild className={footerButtonClass}>
              <RequestTrialLink>Request trial</RequestTrialLink>
            </Button>

            <Button variant="secondary" asChild className={footerButtonClass}>
              <SignInLink to="/login">Sign in</SignInLink>
            </Button>
          </div>
        </div>

        <div className="flex flex-col gap-[26px] lg:flex-row lg:items-end lg:justify-between lg:gap-8">
          <div className="flex max-w-149.5 flex-col gap-3 text-xs font-medium text-[#a4a4a4]">
            <p className="m-0 leading-normal">
              © 2026{" "}
              <Button
                variant="link"
                size="link-sm"
                asChild
                className={footerInlineLinkClass}
              >
                <a
                  href="https://tummly.com/"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  Tummly.com
                </a>
              </Button>{" "}
              Limited. All rights reserved.
            </p>

            <p className="m-0 leading-5">
              Tummly is operated by{" "}
              <Button
                variant="link"
                size="link-sm"
                asChild
                className={footerInlineLinkClass}
              >
                <a
                  href="https://tummly.com/"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  TUMMLY.COM
                </a>
              </Button>{" "}
              LIMITED, company number 16236040. Registered office: 71–75 Shelton
              Street, Covent Garden, London, WC2H 9JQ. Registered in England and
              Wales.
            </p>
          </div>

          <nav
            aria-label="Footer"
            className="flex w-full flex-nowrap items-center gap-x-[15px] lg:w-auto lg:gap-x-5"
          >
            <Button
              variant="link"
              size="link-sm"
              asChild
              className={footerNavLinkClass}
            >
              <a href="#">Help Centre</a>
            </Button>
            <Button
              variant="link"
              size="link-sm"
              asChild
              className={footerNavLinkClass}
            >
              <a href="#">Contact</a>
            </Button>
            <Button
              variant="link"
              size="link-sm"
              asChild
              className={footerNavLinkClass}
            >
              <Link to={LEGAL_ROUTES.terms}>Terms</Link>
            </Button>
            <Button
              variant="link"
              size="link-sm"
              asChild
              className={footerNavLinkClass}
            >
              <Link to={LEGAL_ROUTES.privacy}>Privacy</Link>
            </Button>
            <Button
              variant="link"
              size="link-sm"
              asChild
              className={footerNavLinkClass}
            >
              <Link to={LEGAL_ROUTES.cookieSettings}>Cookie settings</Link>
            </Button>
          </nav>
        </div>
      </div>
    </footer>
  )
}
