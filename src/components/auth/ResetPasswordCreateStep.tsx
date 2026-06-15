import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"
import { useWatch } from "react-hook-form"

import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { PasswordStrengthMeter } from "@/components/auth/PasswordStrengthMeter"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import type { ResetPasswordFormValues } from "@/schemas/resetPassword"

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

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

      if (form.getValues("confirmPassword")) {
        void form.trigger("confirmPassword")
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
              Create a new password
            </h1>
            <p className="m-0 text-sm leading-normal">
              Choose a secure password for your Tummly account.
            </p>
          </header>

          <div className="flex flex-col gap-5">
            <div className="flex flex-col gap-3">
              <FormFloatingInput
                control={form.control}
                name="newPassword"
                type="password"
                label="Password"
                autoComplete="new-password"
                required
              />

              <PasswordStrengthMeter password={newPassword ?? ""} />
            </div>

            <FormFloatingInput
              control={form.control}
              name="confirmPassword"
              type="password"
              label="Confirm your password"
              autoComplete="new-password"
              required
              liveValidate
            />
          </div>

          <p className="m-0 text-sm leading-5 text-[#232323]">
            Use at least 12 characters with a mix of letters, numbers and
            symbols.
          </p>

          <FieldErrorSlot error={rootError} />

          <Button
            type="submit"
            disabled={isSubmitting}
            className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
          >
            {isSubmitting ? "Please wait..." : "Update password"}
          </Button>
        </form>
      </Form>
    </div>
  )
}
