import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"
import { Link } from "react-router-dom"

import { AuthSocialContinueButtons } from "@/components/auth/AuthSocialContinueButtons"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import type { SignupStartValues } from "@/schemas/signup"

const signupPrimaryButtonClassName =
  "h-[45px] min-h-[45px] w-full rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white hover:bg-[#129641]"

type SignupFormProps = {
  form: UseFormReturn<SignupStartValues>
  onSubmit: (values: SignupStartValues) => Promise<void>
}

export function SignupForm({ form, onSubmit }: SignupFormProps) {
  const rootError = form.formState.errors.root?.message
  const isSubmitting = form.formState.isSubmitting

  useEffect(() => {
    const subscription = form.watch((_value, { name, type }) => {
      if (type !== "change") {
        return
      }

      if (name === "email" || name === "termsAccepted") {
        form.clearErrors("root")
      }
    })

    return () => subscription.unsubscribe()
  }, [form])

  return (
    <div className="flex w-full flex-col gap-10">
      <div className="flex flex-col gap-5">
        <div className="flex flex-col gap-3 text-center text-[#141414]">
          <h1 className="m-0 font-serif text-[clamp(1.75rem,4vw,2.375rem)] font-medium leading-normal">
            Create your Tummly account
          </h1>
          <p className="m-0 text-base leading-[22px]">
            Get started with Tummly and set up your restaurant.
          </p>
        </div>

        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmit)}
            noValidate
            className="flex flex-col gap-[26px]"
          >
            <FormFloatingInput
              control={form.control}
              name="email"
              type="email"
              label="Work email"
              autoComplete="email"
              required
            />

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
              {isSubmitting ? "Please wait..." : "Create account"}
            </Button>
          </form>
        </Form>

        <div className="flex w-full items-center gap-5">
          <div className="h-px min-w-0 flex-1 bg-[#d2d2d2]" />
          <span className="shrink-0 text-sm text-[#888]">Or</span>
          <div className="h-px min-w-0 flex-1 bg-[#d2d2d2]" />
        </div>

        <AuthSocialContinueButtons layout="row" returnPath="/signup" />
      </div>

      <p className="m-0 flex flex-wrap items-center justify-center gap-2.5 text-sm font-medium tracking-[0.4px] text-[#232323]">
        <span>Already have a Tummly account?</span>
        <Button
          variant="link"
          size="link-sm"
          asChild
          className="font-medium text-primary underline underline-offset-2"
        >
          <Link to="/login">Sign in</Link>
        </Button>
      </p>
    </div>
  )
}
