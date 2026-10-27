import { renderToStaticMarkup } from "react-dom/server"
import { useForm, type UseFormProps } from "react-hook-form"
import { MemoryRouter } from "react-router-dom"
import { describe, expect, it } from "vitest"

import {
  SIGNUP_RECOVERY_ERROR_TYPE,
  SignupForm,
} from "@/components/signup/SignupForm"
import {
  signupStartDefaultValues,
  type SignupStartValues,
} from "@/schemas/signup"

function FormWithRootError({ type }: { type?: string }) {
  const form = useForm<SignupStartValues>({
    defaultValues: signupStartDefaultValues,
    // The root error is set through the `errors` prop so static rendering can show it.
    errors: {
      root: { type: type ?? "server", message: "Too many attempts." },
    } as unknown as UseFormProps<SignupStartValues>["errors"],
  })
  return <SignupForm form={form} onSubmit={async () => {}} />
}

function render(type?: string) {
  return renderToStaticMarkup(
    <MemoryRouter>
      <FormWithRootError type={type} />
    </MemoryRouter>
  )
}

describe("SignupForm", () => {
  it("signupForm_recoveryError_showsSignInForgotAndContactLinks", () => {
    const html = render(SIGNUP_RECOVERY_ERROR_TYPE)

    expect(html).toContain("Too many attempts.")
    expect(html).toContain('aria-label="Account help"')
    expect(html).toContain('href="/forgot-password"')
    expect(html).toContain('href="/help-center/contact"')
  })

  it("signupForm_plainError_showsNoRecoveryLinks", () => {
    const html = render()

    expect(html).toContain("Too many attempts.")
    expect(html).not.toContain('aria-label="Account help"')
  })
})
