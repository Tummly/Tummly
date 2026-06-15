import { useEffect, useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { validateSetupToken } from "@/api/trialApi"
import {
  getSetupAccountPath,
  getSetupTokenErrorMessage,
  parseValidateSetupTokenResponse,
  type SetupAccountType,
  type SetupTokenPrefill,
} from "@/lib/setupToken"

interface UseSetupTokenValidationResult {
  token: string
  tokenLoading: boolean
  tokenError: string
  prefill: SetupTokenPrefill | null
}

export function useSetupTokenValidation(
  expectedAccountType: SetupAccountType
): UseSetupTokenValidationResult {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get("token")?.trim() ?? ""

  const [tokenLoading, setTokenLoading] = useState(() => Boolean(token))
  const [tokenError, setTokenError] = useState(() =>
    token ? "" : "Setup token is missing from this link."
  )
  const [prefill, setPrefill] = useState<SetupTokenPrefill | null>(null)

  useEffect(() => {
    if (!token) {
      setTokenLoading(false)
      setTokenError("Setup token is missing from this link.")
      setPrefill(null)
      return
    }

    let active = true
    setTokenLoading(true)
    setTokenError("")
    setPrefill(null)

    void (async () => {
      try {
        const response = await validateSetupToken(token)

        if (!active) {
          return
        }

        const parsed = parseValidateSetupTokenResponse(response)

        if (!parsed) {
          setTokenError(
            "Unable to read this setup invitation. Please request a new link."
          )
          return
        }

        if (parsed.accountType !== expectedAccountType) {
          navigate(getSetupAccountPath(parsed.accountType, token), {
            replace: true,
          })
          return
        }

        setPrefill(parsed)
      } catch (error: unknown) {
        if (!active) {
          return
        }

        setTokenError(getSetupTokenErrorMessage(error))
      } finally {
        if (active) {
          setTokenLoading(false)
        }
      }
    })()

    return () => {
      active = false
    }
  }, [expectedAccountType, navigate, token])

  return {
    token,
    tokenLoading,
    tokenError,
    prefill,
  }
}
