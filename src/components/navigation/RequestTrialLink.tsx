import type { ComponentProps } from "react"
import { Link } from "react-router-dom"

/** Marketing entry to self-serve Signup (replaces scroll-to Request Trial). */
export const SIGNUP_PATH = "/signup"

type RequestTrialLinkProps = Omit<ComponentProps<typeof Link>, "to">

/**
 * Kept name for call-site stability. Navigates to `/signup`.
 * Prefer updating visible labels to Signup / Pilot copy at each call site.
 */
export function RequestTrialLink(props: RequestTrialLinkProps) {
  return <Link to={SIGNUP_PATH} {...props} />
}
