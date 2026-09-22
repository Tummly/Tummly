import { useEffect } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { Link, useNavigate } from "react-router-dom"
import { z } from "zod"

import { acceptExternalAuthTerms } from "@/api/externalAuthApi"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { SignupModalShell } from "@/components/signup/SignupModalShell"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  captureOAuthTicketToken,
  clearOAuthTicketToken,
} from "@/lib/oauthTicketToken"
import { saveSignupSessionToken } from "@/lib/signupSession"
import { validationMessages } from "@/schemas/messages"

const signupPrimaryButtonClassName =
  "h-[45px] min-h-[45px] w-full rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white hover:bg-[#129641]"

const oauthTermsSchema = z.object({
  termsAccepted: z.boolean().refine((value) => value === true, {
    message: validationMessages.trialRequest.terms.required,
  }),
})

type OAuthTermsValues = z.infer<typeof oauthTermsSchema>

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function SignupOAuthTermsPage() {
  const navigate = useNavigate()
  const token = captureOAuthTicketToken("signup")

  const form = useForm<OAuthTermsValues>({
    resolver: zodResolver(oauthTermsSchema),
    defaultValues: { termsAccepted: false },
    ...defaultFormValidationOptions,
  })

  useEffect(() => {
    if (!token) {
      navigate("/signup?oauthError=failed", { replace: true })
    }
  }, [navigate, token])

  const onSubmit = async (values: OAuthTermsValues) => {
    form.clearErrors("root")

    if (!token) {
      navigate("/signup?oauthError=failed", { replace: true })
      return
    }

    try {
      const session = await acceptExternalAuthTerms({
        token,
        termsAccepted: values.termsAccepted === true,
      })
      clearOAuthTicketToken("signup")
      saveSignupSessionToken(session.sessionToken)
      navigate("/signup/onboarding", { replace: true })
    } catch (error) {
      const message = getApiErrorMessage(
        error,
        "We could not save your Terms acceptance. Please try again."
      )
      const normalized = message.trim().toLowerCase()
      if (
        normalized.includes("email already in use") ||
        normalized.includes("already in use")
      ) {
        clearOAuthTicketToken("signup")
        navigate("/signup?oauthError=account_exists", { replace: true })
        return
      }

      form.setError("root", {
        message,
      })
    }
  }

  if (!token) {
    return null
  }

  const rootError = form.formState.errors.root?.message
  const isSubmitting = form.formState.isSubmitting

  return (
    <SignupModalShell>
      <div className="flex w-full flex-col gap-10">
        <div className="flex flex-col gap-5">
          <div className="flex flex-col gap-3 text-center text-[#141414]">
            <h1 className="m-0 font-serif text-[clamp(1.75rem,4vw,2.375rem)] font-medium leading-normal">
              Almost there
            </h1>
            <p className="m-0 text-base leading-[22px]">
              Accept the Terms and Privacy Notice to continue creating your
              Tummly account.
            </p>
          </div>

          <Form {...form}>
            <form
              onSubmit={form.handleSubmit(onSubmit)}
              noValidate
              className="flex flex-col gap-[26px]"
            >
              <FormCheckboxLabel
                control={form.control}
                name="termsAccepted"
                labelClassName="font-normal text-sm text-[#141414]"
              >
                I agree to the{" "}
                <Button
                  type="button"
                  variant="link"
                  size="link-sm"
                  asChild
                  className="font-normal text-[#141414] underline underline-offset-2"
                >
                  <Link to={LEGAL_ROUTES.terms}>Terms</Link>
                </Button>{" "}
                and acknowledge the{" "}
                <Button
                  type="button"
                  variant="link"
                  size="link-sm"
                  asChild
                  className="font-normal text-[#141414] underline underline-offset-2"
                >
                  <Link to={LEGAL_ROUTES.privacy}>Privacy Notice</Link>
                </Button>
              </FormCheckboxLabel>

              <FieldErrorSlot error={rootError} />

              <Button
                type="submit"
                disabled={isSubmitting}
                className={signupPrimaryButtonClassName}
              >
                {isSubmitting ? "Please wait..." : "Continue"}
              </Button>
            </form>
          </Form>
        </div>
      </div>
    </SignupModalShell>
  )
}

export default SignupOAuthTermsPage
