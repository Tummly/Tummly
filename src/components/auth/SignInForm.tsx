import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"
import { Link } from "react-router-dom"

import { AuthSocialContinueButtons } from "@/components/auth/AuthSocialContinueButtons"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import { HELP_CENTRE_URL } from "@/config/support"
import { prefetchHelpCentreHero } from "@/lib/prefetchHelpCentreHero"
import type { SignInCredentialsValues } from "@/schemas/signIn"

interface SignInFormProps {
  form: UseFormReturn<SignInCredentialsValues>
  onSubmit: (values: SignInCredentialsValues) => Promise<void>
}

function SignInFooterLink({
  label,
  linkLabel,
  href,
  to,
  onLinkHover,
}: {
  label: string
  linkLabel: string
  href?: string
  to?: string
  onLinkHover?: () => void
}) {
  const prefetch = () => onLinkHover?.()

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
          <Link
            to={to}
            onMouseEnter={prefetch}
            onFocus={prefetch}
            onTouchStart={prefetch}
          >
            {linkLabel}
          </Link>
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
    <div className="flex w-full flex-col gap-10">
      <Link
        to="/"
        className="inline-flex w-fit shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"
      >
        <MarketingLogo
          onLight
          width={145}
          height={37}
          className="h-[37px] w-auto"
        />
      </Link>

      <div className="flex flex-col gap-10">
        <div className="flex flex-col gap-3 text-[#141414]">
          <h1 className="m-0 font-serif text-[clamp(1.75rem,4vw,2.25rem)] font-medium leading-normal">
            Welcome back
          </h1>
          <p className="m-0 text-base leading-[22px]">
            Log in to your Tummly account.
          </p>
        </div>

        <div className="flex flex-col gap-5">
          <AuthSocialContinueButtons />

          <div className="flex w-full items-center gap-5">
            <div className="h-px min-w-0 flex-1 bg-[#d2d2d2]" />
            <span className="shrink-0 text-sm text-[#888]">Or</span>
            <div className="h-px min-w-0 flex-1 bg-[#d2d2d2]" />
          </div>

          <Form {...form}>
            <form
              onSubmit={form.handleSubmit(onSubmit)}
              noValidate
              className="flex flex-col gap-9"
            >
              <div className="flex flex-col gap-3.5">
                <FormFloatingInput
                  control={form.control}
                  name="email"
                  type="email"
                  label="Work email"
                  autoComplete="email"
                  required
                />

                <div className="flex flex-col gap-3">
                  <FormFloatingInput
                    control={form.control}
                    name="password"
                    type="password"
                    label="Password"
                    autoComplete="current-password"
                    required
                  />

                  <p className="m-0 flex flex-wrap items-center gap-2.5 text-xs tracking-[0.4px] text-[#232323]">
                    <span>Forgot password?</span>
                    <Button
                      type="button"
                      variant="link"
                      size="link-sm"
                      asChild
                      className="text-xs font-normal text-primary underline underline-offset-2"
                    >
                      <Link to="/forgot-password">Reset password</Link>
                    </Button>
                  </p>
                </div>
              </div>

              <FormCheckboxLabel
                control={form.control}
                name="rememberDevice"
                labelClassName="font-medium text-sm text-[#141414]"
              >
                Keep me signed in
              </FormCheckboxLabel>

              <FieldErrorSlot error={rootError} />

              <Button
                type="submit"
                disabled={isSubmitting}
                className="h-[45px] min-h-[45px] w-full rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white hover:bg-[#129641]"
              >
                {isSubmitting ? "Please wait..." : "Log in"}
              </Button>
            </form>
          </Form>
        </div>

        <div className="h-px w-full bg-[#d2d2d2]" />

        <div className="flex flex-col gap-[18px]">
          <SignInFooterLink
            label="New to Tummly?"
            linkLabel="Start 30-day Pilot"
            to="/signup"
          />
          <SignInFooterLink
            label="Need help?"
            linkLabel="Visit Help Centre"
            to={HELP_CENTRE_URL}
            onLinkHover={prefetchHelpCentreHero}
          />
        </div>
      </div>
    </div>
  )
}
