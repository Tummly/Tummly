import { Link } from "react-router-dom"

import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { cn } from "@/lib/utils"

type MarketingSignupLegalNoteProps = {
  className?: string
  /** `read` = "Read our Terms… Privacy Notice…"; `agree` = "By signing up, I agree…". */
  variant?: "agree" | "read"
}

/**
 * Legal line under marketing sign-up forms. `read` ("Read our Terms of Service
 * and Privacy Notice…") is Figma Home hero `6808:36207` and Sign up `6788:42946`;
 * other pages keep the `agree` line ("By signing up, I agree…").
 */
export function MarketingSignupLegalNote({
  className,
  variant = "read",
}: MarketingSignupLegalNoteProps) {
  if (variant === "agree") {
    return (
      <p className={cn("m-0 text-xs", className)}>
        By signing up, I agree to Tummly&apos;s{" "}
        <Link to={LEGAL_ROUTES.terms} className="underline underline-offset-2">
          Terms of Service
        </Link>{" "}
        and{" "}
        <Link
          to={LEGAL_ROUTES.privacy}
          className="underline underline-offset-2"
        >
          Privacy Policy
        </Link>
        .
      </p>
    )
  }

  return (
    <p className={cn("m-0 text-xs", className)}>
      Read our{" "}
      <Link to={LEGAL_ROUTES.terms} className="underline underline-offset-2">
        Terms of Service
      </Link>{" "}
      and{" "}
      <Link to={LEGAL_ROUTES.privacy} className="underline underline-offset-2">
        Privacy Notice
      </Link>
      . You’ll review and accept the required terms during account setup.
    </p>
  )
}
