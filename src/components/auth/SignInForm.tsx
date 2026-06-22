import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"
import { Link } from "react-router-dom"

import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import type { SignInCredentialsValues } from "@/schemas/signIn"

interface SignInFormProps {
  form: UseFormReturn<SignInCredentialsValues>
  onSubmit: (values: SignInCredentialsValues) => Promise<void>
}

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

function SignInFooterLink({
  label,
  linkLabel,
  href,
  to,
}: {
  label: string
  linkLabel: string
  href?: string
  to?: string
}) {
  return (
    <p className="m-0 flex flex-wrap items-center gap-2.5 text-sm font-medium tracking-[0.4px] text-[#232323]">
      <span>{label}</span>
      {to ? (
        <Button
          variant="link"
          size="link-sm"
          asChild
          className="font-medium text-primary underline underline-offset-2"
        >
          <Link to={to}>{linkLabel}</Link>
        </Button>
      ) : (
        <Button
          variant="link"
          size="link-sm"
          asChild
          className="font-medium text-primary underline underline-offset-2"
        >
          <a href={href ?? "#"}>{linkLabel}</a>
        </Button>
      )}
    </p>
  )
}

export function SignInForm({ form, onSubmit }: SignInFormProps) {
  const rootError = form.formState.errors.root?.message
  const isSubmitting = form.formState.isSubmitting

  useEffect(() => {
    const subscription = form.watch((_value, { name, type }) => {
      if (type !== "change") {
        return
      }

      if (name === "email" || name === "password") {
        form.clearErrors("root")
      }
    })

    return () => subscription.unsubscribe()
  }, [form])

  return (
    <div
      className={`flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] sm:gap-7 lg:gap-8 ${cardShadow}`}
    >
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          noValidate
          className="flex flex-col gap-6 lg:gap-7"
        >
          <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px] text-[#232323]">
            Sign in
          </h1>

          <div className="flex flex-col gap-5">
            <FormFloatingInput
              control={form.control}
              name="email"
              type="email"
              label="Email"
              autoComplete="email"
              required
            />

            <div className="flex flex-col gap-3.5">
              <FormFloatingInput
                control={form.control}
                name="password"
                type="password"
                label="Password"
                autoComplete="current-password"
                required
              />

              <p className="m-0 flex flex-wrap items-center gap-2.5 text-xs font-medium tracking-[0.4px] text-[#232323]">
                <span>Forgot password?</span>
                <Button
                  type="button"
                  variant="link"
                  size="link-sm"
                  asChild
                  className="font-medium text-primary underline underline-offset-2"
                >
                  <Link to="/forgot-password">Reset password</Link>
                </Button>
              </p>
            </div>
          </div>

          <FormCheckboxLabel
            control={form.control}
            name="rememberDevice"
            labelClassName="font-medium"

          >
            Remember this device for 30 days
          </FormCheckboxLabel>

          <FieldErrorSlot error={rootError} />

          <Button
            type="submit"
            disabled={isSubmitting}
            className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
          >
            {isSubmitting ? "Please wait..." : "Sign in"}
          </Button>

          <p className="m-0 text-sm leading-normal text-[#232323]">
            We may send a verification code by email or SMS to help keep your
            account secure. Message and data rates may apply.
          </p>
        </form>
      </Form>

      <div className="flex flex-col gap-[18px]">
        <SignInFooterLink
          label="New to Tummly?"
          linkLabel="Request guided trial"
          to="/"
        />
        <SignInFooterLink
          label="Need help?"
          linkLabel="Visit Help Centre"
          href="#"
        />
      </div>
    </div>
  )
}
