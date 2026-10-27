import { renderToStaticMarkup } from "react-dom/server"
import { MemoryRouter } from "react-router-dom"
import { describe, expect, it } from "vitest"

import SignupVerifyPage from "@/pages/auth/SignupVerifyPage"
import { SignupVerifyStep } from "@/components/signup/SignupVerifyStep"
import { mapSignupOtpError } from "@/lib/signupOtpFeedback"

const VERIFY_TEXT =
  "If this email can be used, we sent a 6-digit code. If you already have a Tummly account, sign in instead."

function visibleText(html: string) {
  return html
    .replace(/<!--[\s\S]*?-->/g, "")
    .replace(/<[^>]+>/g, "")
    .replace(/&#x27;|&#39;|&apos;/g, "'")
}

function renderStep(feedback: ReturnType<typeof mapSignupOtpError> | null) {
  return renderToStaticMarkup(
    <MemoryRouter>
      <SignupVerifyStep
        otpCode=""
        submitting={false}
        feedback={feedback}
        resendSecondsRemaining={0}
        canResend
        onOtpChange={() => {}}
        onVerify={() => {}}
        onResend={() => {}}
        onUseDifferentEmail={() => {}}
      />
    </MemoryRouter>
  )
}

describe("SignupVerifyPage", () => {
  it("verifyPage_rendersGenericAccountText_withSignInAndForgotLinks", () => {
    const html = renderToStaticMarkup(
      <MemoryRouter initialEntries={["/signup/verify?email=owner%40example.com"]}>
        <SignupVerifyPage />
      </MemoryRouter>
    )

    expect(visibleText(html)).toContain(VERIFY_TEXT)
    expect(html).toContain('href="/login"')
    expect(html).toContain('href="/forgot-password"')
  })

  it("verifyPage_withoutEmail_rendersNothing", () => {
    const html = renderToStaticMarkup(
      <MemoryRouter initialEntries={["/signup/verify"]}>
        <SignupVerifyPage />
      </MemoryRouter>
    )

    expect(html).toBe("")
  })

  it("verifyPage_challengeLocked_showsRetryTime", () => {
    const iso = "2026-10-27T11:00:00Z"
    const html = renderStep(
      mapSignupOtpError(
        Object.assign(new Error("x"), {
          code: "challenge_locked",
          retryAfterUtc: iso,
        })
      )
    )

    const text = visibleText(html)
    expect(text).toContain("Too many attempts for this email. Try again later.")
    expect(text).toContain(
      `You can try again after ${new Date(iso).toLocaleString(undefined, {
        dateStyle: "medium",
        timeStyle: "short",
      })}.`
    )
    expect(html).toContain('href="/login"')
    expect(html).toContain('href="/forgot-password"')
    expect(html).toContain('href="/help-center/contact"')
  })

  it("verifyPage_rateLimited_showsWaitText_andRecoveryLinks", () => {
    const html = renderStep(
      mapSignupOtpError(Object.assign(new Error("x"), { code: "rate_limited" }))
    )

    expect(visibleText(html)).toContain(
      "Too many attempts. Wait a moment and try again."
    )
    expect(html).toContain('href="/login"')
    expect(html).toContain('href="/forgot-password"')
    expect(html).toContain('href="/help-center/contact"')
  })

  it("verifyPage_otpInvalid_showsNoRecoveryLinks", () => {
    const html = renderStep(
      mapSignupOtpError(Object.assign(new Error("x"), { code: "otp_invalid" }))
    )

    expect(visibleText(html)).toContain(
      "That code doesn't match. Check your email and try again."
    )
    expect(html).not.toContain('href="/help-center/contact"')
  })

  it("verifyPage_lockedState_hasOneForgotPasswordLink", () => {
    for (const code of ["challenge_locked", "rate_limited"]) {
      const html = renderStep(
        mapSignupOtpError(Object.assign(new Error("x"), { code }))
      )

      expect(html.match(/href="\/forgot-password"/g)).toHaveLength(1)
    }
  })

  it("verifyPage_normalState_keepsTheForgotPasswordLink", () => {
    const html = renderStep(null)

    expect(html.match(/href="\/forgot-password"/g)).toHaveLength(1)
  })

  it("verifyPage_linksAreAnchorsWithoutButtonType", () => {
    const html = renderStep(
      mapSignupOtpError(Object.assign(new Error("x"), { code: "rate_limited" }))
    )

    const anchors = html.match(/<a\b[^>]*>/g) ?? []
    expect(anchors.length).toBeGreaterThanOrEqual(4)
    for (const anchor of anchors) {
      expect(anchor).not.toContain('type="button"')
    }
  })
})
