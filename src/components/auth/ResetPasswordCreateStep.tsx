import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"
import { useWatch } from "react-hook-form"

import { AuthFormHeader } from "@/components/auth/AuthFormHeader"
import { PasswordStrengthMeter } from "@/components/auth/PasswordStrengthMeter"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import { PASSWORD_REQUIREMENTS_HINT } from "@/constants/passwordCopy"
import type { ResetPasswordFormValues } from "@/schemas/resetPassword"

type ResetPasswordCreateStepProps = {
  form: UseFormReturn<ResetPasswordFormValues>
  onSubmit: (values: ResetPasswordFormValues) => Promise<void>
}

export function ResetPasswordCreateStep({
  form,
  onSubmit,
}: ResetPasswordCreateStepProps) {
  const rootError = form.formState.errors.root?.message
  const isSubmitting = form.formState.isSubmitting
  const newPassword = useWatch({
    control: form.control,
    name: "newPassword",
    defaultValue: "",
  })

  useEffect(() => {
    const subscription = form.watch((_value, { name, type }) => {
      if (type !== "change" || name !== "newPassword") {
        return
      }

      if (
        form.getValues("confirmPassword") &&
        form.getFieldState("confirmPassword").isTouched
      ) {
        void form.trigger("confirmPassword")
      }
    })

    return () => subscription.unsubscribe()
  }, [form])

  return (
    <div className="flex w-full flex-col gap-10">
      <AuthFormHeader
        title="Create a new password"
        description="Create a new password for your Tummly account."
      />

      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          noValidate
          className="flex flex-col gap-9"
        >
          <div className="flex flex-col gap-5">
            <div className="flex flex-col gap-3">
              <FormFloatingInput
                control={form.control}
                name="newPassword"
                type="password"
                label="New password"
                autoComplete="new-password"
                required
                blurThenLiveValidate
              />

              <PasswordStrengthMeter password={newPassword ?? ""} hideWhenEmpty />
            </div>

            <FormFloatingInput
              control={form.control}
              name="confirmPassword"
              type="password"
              label="Confirm new password"
              autoComplete="new-password"
              required
              blurThenLiveValidate
            />
          </div>

          <p className="m-0 text-sm leading-5 text-[#232323]">
            {PASSWORD_REQUIREMENTS_HINT}
          </p>

          <FieldErrorSlot error={rootError} />

          <Button
            type="submit"
            disabled={isSubmitting}
            className="h-[45px] min-h-[45px] w-full rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white hover:bg-[#129641]"
          >
            {isSubmitting ? "Please wait..." : "Reset password"}
          </Button>
        </form>
      </Form>
    </div>
  )
}
