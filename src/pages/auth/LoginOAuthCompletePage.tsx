import { useEffect } from "react"
import { isAxiosError } from "axios"
import { useNavigate } from "react-router-dom"

import { exchangeExternalAuth } from "@/api/externalAuthApi"
import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { AuthShell } from "@/components/auth/AuthShell"
import { getFetchErrorMessage, readString } from "@/lib/apiEnvelope"
import { runOAuthExchangeOnce } from "@/lib/oauthExchangeTokenGuard"
import { stashOAuthErrorMessage } from "@/lib/oauthErrorCopy"
import {
  OAUTH_OTP_HANDOFF_KEY,
  writeOAuthOtpHandoff,
  type OAuthOtpHandoff,
} from "@/lib/oauthOtpHandoff"
import {
  captureOAuthTicketToken,
  clearOAuthTicketToken,
} from "@/lib/oauthTicketToken"
import { parseOtpChallengeResponse } from "@/lib/signInOtp"
import {
  completeUserSession,
  getDeviceToken,
  isWorkspaceSetupDestination,
  parseTrustSkipLoginResponse,
  persistAuthSession,
} from "@/pages/utils/authHelpers"

function failToLogin(
  navigate: ReturnType<typeof useNavigate>,
  apiMessage?: string | null
) {
  const trimmed = apiMessage?.trim()
  if (trimmed) {
    stashOAuthErrorMessage("login", trimmed)
    navigate(
      `/login?oauthError=failed&oauthMessage=${encodeURIComponent(trimmed)}`,
      { replace: true }
    )
    return
  }

  navigate("/login?oauthError=failed", { replace: true })
}

function LoginOAuthCompletePage() {
  const navigate = useNavigate()

  useEffect(() => {
    let cancelled = false

    const token = captureOAuthTicketToken("login")
    if (!token) {
      failToLogin(navigate)
      return
    }

    const run = async () => {
      try {
        const result = await runOAuthExchangeOnce(token, async () => {
          const deviceToken = getDeviceToken()
          return exchangeExternalAuth({
            token,
            rememberDevice: true,
            ...(deviceToken ? { deviceToken } : {}),
          })
        })

        if (cancelled) {
          return
        }

        clearOAuthTicketToken("login")

        if (result?.loginType === "ADMIN") {
          if (!result.token) {
            failToLogin(navigate)
            return
          }
          persistAuthSession(result.token, "ADMIN")
          window.location.href = "/admin-dashboard"
          return
        }

        if (result?.loginType === "SUPPORT") {
          if (!result.token) {
            failToLogin(navigate)
            return
          }
          persistAuthSession(result.token, "SUPPORT")
          window.location.href = "/support-dashboard"
          return
        }

        if (result?.loginType === "USER") {
          const trustSkip = parseTrustSkipLoginResponse(result)

          if (trustSkip) {
            const destination = completeUserSession(trustSkip)
            if (isWorkspaceSetupDestination(destination)) {
              navigate("/login?step=workspace-setup", { replace: true })
              return
            }
            window.location.href = destination
            return
          }

          const challenge = parseOtpChallengeResponse(result) ?? {
            otpChannel: "email" as const,
            hasVerifiedPhone: false,
            maskedPhone: null,
          }

          const email = readString(result, "email")

          if (!email) {
            failToLogin(navigate)
            return
          }

          const handoff: OAuthOtpHandoff = {
            email,
            rememberDevice: true,
            challenge,
          }

          writeOAuthOtpHandoff(handoff)

          navigate("/login", {
            replace: true,
            state: { [OAUTH_OTP_HANDOFF_KEY]: handoff },
          })
          return
        }

        failToLogin(navigate)
      } catch (error) {
        if (cancelled) {
          return
        }

        const apiMessage = isAxiosError(error)
          ? getFetchErrorMessage(
              error.response?.data ?? {},
              ""
            )
          : error instanceof Error
            ? error.message
            : null

        failToLogin(navigate, apiMessage || null)
      }
    }

    void run()

    return () => {
      cancelled = true
    }
  }, [navigate])

  return (
    <AuthShell>
      <AuthSessionLoading />
    </AuthShell>
  )
}

export default LoginOAuthCompletePage
