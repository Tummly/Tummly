import { useEffect, useState } from "react"
import { useLocation, useNavigate, useSearchParams } from "react-router-dom"

import {
  getSetupAccountPath,
  type SetupAccountType,
  type SetupTokenPrefill,
} from "@/lib/setupToken"

interface SetupLocationState {
  prefill?: SetupTokenPrefill
}

interface UseInvitePrefillResult {
  token: string
  tokenLoading: boolean
  tokenError: string
  prefill: SetupTokenPrefill | null
}

export function useInvitePrefill(
  expectedAccountType: SetupAccountType,
): UseInvitePrefillResult {
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()
  const token = searchParams.get("token")?.trim() ?? ""

  const statePrefill = (location.state as SetupLocationState | null)?.prefill
    ?? null

  const [tokenLoading, setTokenLoading] = useState(() =>
    Boolean(token) && !statePrefill,
  )
  const [tokenError, setTokenError] = useState(() =>
    token ? "" : "Setup token is missing from this link.",
  )
  const [prefill, setPrefill] = useState<SetupTokenPrefill | null>(statePrefill)

  useEffect(() => {
    if (!token) {
      setTokenLoading(false)
      setTokenError("Setup token is missing from this link.")
      setPrefill(null)
      return
    }

    if (statePrefill) {
      if (statePrefill.accountType !== expectedAccountType) {
        navigate(getSetupAccountPath(statePrefill.accountType, token), {
          replace: true,
          state: { prefill: statePrefill },
        })
        return
      }

      setPrefill(statePrefill)
      setTokenLoading(false)
      setTokenError("")
      return
    }

    setTokenLoading(true)
    navigate(`/setup-account?token=${encodeURIComponent(token)}`, {
      replace: true,
    })
  }, [expectedAccountType, navigate, statePrefill, token])

  return {
    token,
    tokenLoading,
    tokenError,
    prefill,
  }
}
