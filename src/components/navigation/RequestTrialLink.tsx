import type { ComponentProps } from "react"
import { Link, useLocation, useNavigate } from "react-router-dom"

import {
  REQUEST_TRIAL_HASH,
  scrollToRequestTrial,
} from "@/lib/scrollToRequestTrial"

export function useScrollToRequestTrial() {
  const { pathname } = useLocation()
  const navigate = useNavigate()

  return (event?: { preventDefault: () => void }) => {
    event?.preventDefault()

    if (pathname === "/") {
      if (window.location.hash !== REQUEST_TRIAL_HASH) {
        window.history.pushState(null, "", REQUEST_TRIAL_HASH)
      }

      scrollToRequestTrial()
      return
    }

    void navigate({ pathname: "/", hash: REQUEST_TRIAL_HASH })
  }
}

type RequestTrialLinkProps = Omit<
  ComponentProps<typeof Link>,
  "to" | "onClick"
> & {
  onClick?: ComponentProps<typeof Link>["onClick"]
}

export function RequestTrialLink({
  onClick,
  ...props
}: RequestTrialLinkProps) {
  const scrollToTrial = useScrollToRequestTrial()

  return (
    <Link
      to={{ pathname: "/", hash: REQUEST_TRIAL_HASH }}
      onClick={(event) => {
        onClick?.(event)

        if (!event.defaultPrevented) {
          scrollToTrial(event)
        }
      }}
      {...props}
    />
  )
}
