import { Link } from "react-router-dom"

import logo from "@/assets/svg/logo.svg"
import { Button } from "@/components/ui/button"

const footerNavLinkClass =
  "text-xs font-medium text-[#e7e7e7] no-underline hover:text-white hover:underline"

const footerInlineLinkClass =
  "text-xs font-medium text-[#a4a4a4] no-underline hover:text-[#e7e7e7] hover:underline"

export default function Footer() {
  return (
    <footer className="w-full bg-[#141414]">
      <div className="mx-auto flex w-full flex-col gap-10 px-4 py-10 sm:px-6 md:px-10 lg:gap-10.5 lg:px-16 xl:px-45">
        <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
          <Link
            to="/"
            className="shrink-0 rounded-sm focus-visible:ring-2 focus-visible:ring-white/40 focus-visible:outline-none"
          >
            <img
              src={logo}
              alt="Tummly"
              width={138}
              height={34}
              className="block h-8 w-auto max-w-[min(138px,42vw)] object-contain sm:h-8.5"
            />
          </Link>

          <div className="flex w-full flex-col gap-3 sm:w-auto sm:flex-row sm:items-center">
            <Button size="responsive" asChild>
              <Link to="/request-trial">Request trial</Link>
            </Button>

            <Button variant="secondary" size="responsive" asChild>
              <Link to="/login">Sign in</Link>
            </Button>
          </div>
        </div>

        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between lg:gap-8">
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
            className="flex flex-wrap items-center gap-x-5 gap-y-2 lg:justify-end"
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
              <a href="#">Terms</a>
            </Button>
            <Button
              variant="link"
              size="link-sm"
              asChild
              className={footerNavLinkClass}
            >
              <a href="#">Privacy</a>
            </Button>
            <Button
              variant="link"
              size="link-sm"
              asChild
              className={footerNavLinkClass}
            >
              <a href="#">Cookie settings</a>
            </Button>
          </nav>
        </div>
      </div>
    </footer>
  )
}
