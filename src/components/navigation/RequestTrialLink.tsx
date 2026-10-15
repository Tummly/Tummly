import type { ComponentProps } from "react"
import { Link } from "react-router-dom"

import { MARKETING_PILOT_PATH } from "@/constants/marketingNav"
import {
  buildSignupPath,
  SIGNUP_PATH,
  type SignupPlanIntent,
} from "@/lib/signupPlanIntent"

export { SIGNUP_PATH }

type RequestTrialLinkProps = Omit<ComponentProps<typeof Link>, "to"> & {
  /** When set, navigates to `/signup?plan=&cadence=` (Pilot → marketing Pilot page). */
  planIntent?: SignupPlanIntent | null
}

/**
 * Kept name for call-site stability.
 * Pilot intent opens the marketing Pilot page; other plans open `/signup` with intent.
 */
export function RequestTrialLink({
  planIntent,
  ...props
}: RequestTrialLinkProps) {
  const to =
    planIntent?.plan === "Pilot"
      ? MARKETING_PILOT_PATH
      : buildSignupPath(planIntent)

  return <Link to={to} {...props} />
}
