import { useEffect } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import { AUTH_API_BASE_URL } from "../../config/api"
import {
  getSetupAccountPath,
  parseValidateSetupTokenResponse,
  type SetupAccountType,
} from "@/lib/setupToken"

interface ValidateInviteResponse {
  success: boolean
  message?: string
  accountType?: string
}

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
        const response = await fetch(`${AUTH_API_BASE_URL}/validate-invite`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ token }),
        })

        const data = (await response.json()) as ValidateInviteResponse

        if (!active) {
          return
        }

        if (!data.success) {
          navigate("/login?setup=invalid", { replace: true })
          return
        }

        const parsed = parseValidateSetupTokenResponse({
          data: {
            accountType: data.accountType,
          },
        })

        const accountType: SetupAccountType =
          parsed?.accountType ??
          (data.accountType?.toLowerCase() === "multi" ? "Multi" : "Single")

        navigate(getSetupAccountPath(accountType, token), { replace: true })
      } catch {
        if (!active) {
          return
        }

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
