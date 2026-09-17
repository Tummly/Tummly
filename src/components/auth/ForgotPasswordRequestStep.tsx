import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"

import { AuthBackToLogin } from "@/components/auth/AuthBackToLogin"
import { AuthFormHeader } from "@/components/auth/AuthFormHeader"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import type { SignInEmailValues } from "@/schemas/signIn"

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
    <div className="flex w-full flex-col gap-10">
      <AuthFormHeader
        title="Reset your password"
        description="Enter the email address for your Tummly account. If an account matches, we'll send instructions to reset your password."
      />

      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          noValidate
          className="flex flex-col gap-9"
        >
          <FormFloatingInput
            control={form.control}
            name="email"
            type="email"
            label="Work email"
            autoComplete="email"
            required
          />

          <FieldErrorSlot error={rootError} />

          <Button
            type="submit"
            disabled={isSubmitting}
            className="h-[45px] min-h-[45px] w-full rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white hover:bg-[#129641]"
          >
            {isSubmitting ? "Please wait..." : "Send reset link"}
          </Button>

          <div className="h-px w-full bg-[#d2d2d2]" />

          <AuthBackToLogin />
        </form>
      </Form>
    </div>
  )
}
