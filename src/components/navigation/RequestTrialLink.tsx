import type { ComponentProps } from "react"
import { Link } from "react-router-dom"

import {
  buildSignupPath,
  SIGNUP_PATH,
  type SignupPlanIntent,
} from "@/lib/signupPlanIntent"

export { SIGNUP_PATH }

type RequestTrialLinkProps = Omit<ComponentProps<typeof Link>, "to"> & {
  /** When set, navigates to `/signup?plan=&cadence=`. */
  planIntent?: SignupPlanIntent | null
}

/**
 * Kept name for call-site stability. Navigates to `/signup` (optionally with plan intent).
 */
export function RequestTrialLink({
  planIntent,
  ...props
}: RequestTrialLinkProps) {
  return <Link to={buildSignupPath(planIntent)} {...props} />
}
