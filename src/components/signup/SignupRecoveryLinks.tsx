import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"

const linkClassName = "font-medium text-[#141414] underline underline-offset-2"

/** Sign in, Forgot password and Contact us. Shown under locked and rate-limited errors. */
export function SignupRecoveryLinks() {
  return (
    <nav
      aria-label="Account help"
      className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm"
    >
      <Button variant="link" size="link-sm" asChild className={linkClassName}>
        <Link to="/login">Sign in</Link>
      </Button>
      <Button variant="link" size="link-sm" asChild className={linkClassName}>
        <Link to="/forgot-password">Forgot password</Link>
      </Button>
      <Button variant="link" size="link-sm" asChild className={linkClassName}>
        <Link to="/help-center/contact">Contact us</Link>
      </Button>
    </nav>
  )
}
