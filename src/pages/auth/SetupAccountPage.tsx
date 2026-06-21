import { useEffect } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { validateInviteToken } from "@/api/trialApi"
import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import {
  getSetupAccountPath,
  getSetupTokenErrorMessage,
  parseValidateSetupTokenResponse,
  type SetupAccountType,
} from "@/lib/setupToken"

function SetupAccountPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get("token")?.trim() ?? ""

  useEffect(() => {
    if (!token) {
      return
    }

    let active = true

    const validateInvite = async () => {
      try {
        const response = await validateInviteToken(token)

        if (!active) {
          return
        }

        const parsed = parseValidateSetupTokenResponse(response)

        if (!parsed) {
          navigate("/login?setup=invalid", { replace: true })
          return
        }

        const accountType: SetupAccountType = parsed.accountType

        navigate(getSetupAccountPath(accountType, token), {
          replace: true,
          state: { prefill: parsed },
        })
      } catch (error: unknown) {
        if (!active) {
          return
        }

        if (
          error &&
          typeof error === "object" &&
          "response" in error &&
          error.response &&
          typeof error.response === "object" &&
          "status" in error.response &&
          error.response.status === 409
        ) {
          navigate("/login?setup=used", { replace: true })
          return
        }

        void getSetupTokenErrorMessage(error)
        navigate("/login?setup=invalid", { replace: true })
      }
    }

    void validateInvite()

    return () => {
      active = false
    }
  }, [token, navigate])

  if (!token) {
    return (
      <SetupAccountStatus
        tone="error"
        title="Invalid setup link"
        message="Setup token is missing from this link."
      />
    )
  }

  return (
    <SetupAccountStatus title="Validating your Setup Link" />
  )
}

export default SetupAccountPage
