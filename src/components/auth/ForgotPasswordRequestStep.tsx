import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"
import { Link } from "react-router-dom"

import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import type { SignInEmailValues } from "@/schemas/signIn"

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

type ForgotPasswordRequestStepProps = {
  form: UseFormReturn<SignInEmailValues>
  onSubmit: (values: SignInEmailValues) => Promise<void>
}

export function ForgotPasswordRequestStep({
  form,
  onSubmit,
}: ForgotPasswordRequestStepProps) {
  const rootError = form.formState.errors.root?.message
  const isSubmitting = form.formState.isSubmitting

  useEffect(() => {
    const subscription = form.watch((_value, { name, type }) => {
      if (type !== "change") {
        return
      }

      if (name === "email") {
        form.clearErrors("root")
      }
    })

    return () => subscription.unsubscribe()
  }, [form])

  return (
    <div
      className={`flex w-full max-w-[490px] shrink-0 flex-col rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] ${cardShadow}`}
    >
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          noValidate
          className="flex flex-col gap-6 sm:gap-7 lg:gap-[34px]"
        >
          <header className="flex flex-col gap-4 text-[#232323]">
            <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
              Reset your password
            </h1>
            <p className="m-0 text-sm leading-normal">
              Enter the email address linked to your Tummly account. If an
              account exists, we&apos;ll send instructions to reset your
              password.
            </p>
          </header>

          <div className="flex flex-col gap-1.5">
            <FormFloatingInput
              control={form.control}
              name="email"
              type="email"
              label="Email"
              autoComplete="email"
              required
            />

            <FieldErrorSlot error={rootError} />
          </div>

          <Button
            type="submit"
            disabled={isSubmitting}
            className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
          >
            {isSubmitting ? "Please wait..." : "Send reset link"}
          </Button>

          <Button
            type="button"
            variant="link"
            size="link-sm"
            asChild
            className="self-start font-medium text-primary underline underline-offset-2"
          >
            <Link to="/login">Back to sign in</Link>
          </Button>
        </form>
      </Form>
    </div>
  )
}
