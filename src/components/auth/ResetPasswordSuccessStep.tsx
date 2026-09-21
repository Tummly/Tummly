import { Link } from "react-router-dom"

import { AuthFormHeader } from "@/components/auth/AuthFormHeader"
import { Button } from "@/components/ui/button"

export function ResetPasswordSuccessStep() {
  return (
    <div className="flex w-full flex-col gap-10">
      <AuthFormHeader
        title="Password updated"
        description="Your Tummly account password has been changed."
      />
      <Button
        variant="link"
        size="link-sm"
        asChild
        className="self-start font-medium text-primary underline underline-offset-2"
      >
        <Link to="/login">Go to login</Link>
      </Button>
    </div>
  )
}
