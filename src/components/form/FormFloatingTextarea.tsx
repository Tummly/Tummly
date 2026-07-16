import type { Control, FieldPath, FieldValues } from "react-hook-form"
import { useFormContext } from "react-hook-form"

import { useWizardLiveValidation } from "@/components/form/WizardLiveValidationContext"
import {
  FloatingLabelTextarea,
  type FloatingLabelTextareaProps,
} from "@/components/ui/floating-label-textarea"
import { FormControl, FormField, FormItem } from "@/components/ui/form"
import { getCrossFieldPeers } from "@/lib/form"

type FormFloatingTextareaProps<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
> = Omit<
  FloatingLabelTextareaProps,
  "value" | "defaultValue" | "onChange" | "onBlur" | "name" | "error"
> & {
  control: Control<TFieldValues>
  name: TName
  liveValidate?: boolean
  validateOnBlur?: boolean
  /** Prefer over fieldState.error when mic/STT shows inline recovery. */
  errorOverride?: string
}

function FormFloatingTextarea<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
>({
  control,
  name,
  liveValidate,
  validateOnBlur,
  errorOverride,
  ...inputProps
}: FormFloatingTextareaProps<TFieldValues, TName>) {
  const { trigger, clearErrors, formState } = useFormContext<TFieldValues>()
  const contextLiveValidate = useWizardLiveValidation(String(name))
  const shouldLiveValidate = liveValidate ?? contextLiveValidate
  const shouldValidateOnBlur = shouldLiveValidate || validateOnBlur === true
  const crossFieldPeers = getCrossFieldPeers(String(name))

  const revalidateCrossFieldPeers = () => {
    for (const peer of crossFieldPeers) {
      void trigger(peer as FieldPath<TFieldValues>)
    }
  }

  return (
    <FormField
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <FormItem className="w-full gap-0">
          <FormControl>
            <FloatingLabelTextarea
              {...inputProps}
              {...field}
              value={field.value ?? ""}
              error={errorOverride ?? fieldState.error?.message}
              onChange={(event) => {
                field.onChange(event)
                if (shouldLiveValidate) {
                  void trigger(name)
                  revalidateCrossFieldPeers()
                } else if (formState.isSubmitted && crossFieldPeers.length > 0) {
                  revalidateCrossFieldPeers()
                }
              }}
              onBlur={(event) => {
                field.onBlur(event)

                const value = String(event.currentTarget.value ?? field.value ?? "")
                const hasValue = value.trim().length > 0

                if (validateOnBlur && !hasValue) {
                  clearErrors(name)
                  return
                }

                if (shouldValidateOnBlur) {
                  void trigger(name)
                  revalidateCrossFieldPeers()
                }
              }}
            />
          </FormControl>
        </FormItem>
      )}
    />
  )
}

export { FormFloatingTextarea }
export type { FormFloatingTextareaProps }
