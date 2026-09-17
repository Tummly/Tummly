import type { ComponentProps, ReactNode } from "react"
import { Link, useLocation, useNavigate } from "react-router-dom"

import SignInLink from "@/components/auth/SignInLink"
import { CookieSettingsTrigger } from "@/components/common/CookieSettingsDialog"
import type { MarketingNavHref } from "@/constants/marketingNav"
import { cn } from "@/lib/utils"

type MarketingNavLinkProps = {
  label: string
  href: MarketingNavHref
  className?: string
  onNavigate?: () => void
}

function HashLink({
  hash,
  className,
  children,
  onNavigate,
  ...props
}: {
  hash: string
  className?: string
  children: ReactNode
  onNavigate?: () => void
} & Omit<ComponentProps<"a">, "href">) {
  const { pathname } = useLocation()
  const navigate = useNavigate()

  return (
    <a
      href={pathname === "/" ? hash : `/${hash}`}
      className={className}
      onClick={(event) => {
        event.preventDefault()
        onNavigate?.()

        if (pathname === "/") {
          if (window.location.hash !== hash) {
            window.history.pushState(null, "", hash)
          }
          const id = hash.replace(/^#/, "")
          document.getElementById(id)?.scrollIntoView({ behavior: "smooth" })
          return
        }

        void navigate({ pathname: "/", hash })
      }}
      {...props}
    >
      {children}
    </a>
  )
}

export function MarketingNavLink({
  label,
  href,
  className,
  onNavigate,
}: MarketingNavLinkProps) {
  const linkClass = cn(
    "rounded-sm text-left text-sm font-normal text-[#141414] no-underline transition-colors hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30",
    className,
  )

  if (href.kind === "placeholder") {
    return (
      <span
        className={cn(linkClass, "cursor-default hover:no-underline")}
        aria-disabled="true"
      >
        {label}
      </span>
    )
  }

  if (href.kind === "cookie-settings") {
    return (
      <CookieSettingsTrigger className={linkClass}>{label}</CookieSettingsTrigger>
    )
  }

  if (href.kind === "hash") {
    return (
      <HashLink
        hash={href.hash}
        className={linkClass}
        onNavigate={onNavigate}
      >
        {label}
      </HashLink>
    )
  }

  if (href.kind === "external") {
    return (
      <a
        href={href.href}
        className={linkClass}
        target="_blank"
        rel="noopener noreferrer"
        onClick={() => onNavigate?.()}
      >
        {label}
      </a>
    )
  }

  if (href.to === "/login") {
    return (
      <SignInLink
        to="/login"
        className={linkClass}
        onClick={() => onNavigate?.()}
      >
        {label}
      </SignInLink>
    )
  }

  return (
    <Link to={href.to} className={linkClass} onClick={() => onNavigate?.()}>
      {label}
    </Link>
  )
}
