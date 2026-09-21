import { ArrowLeft } from "lucide-react"
import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"

type AuthBackToLoginProps = {
  to?: string
}

export function AuthBackToLogin({ to = "/login" }: AuthBackToLoginProps) {
  return (
    <Button
      type="button"
      variant="link"
      size="link-sm"
      asChild
      className="h-auto gap-3 self-start p-0 text-sm font-medium text-[#555] no-underline hover:no-underline"
    >
      <Link to={to}>
        <ArrowLeft className="size-3.5 shrink-0" aria-hidden />
        Back to login
      </Link>
    </Button>
  )
}
