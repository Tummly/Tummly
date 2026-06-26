import { useState } from "react"
import type { Control, FieldPath, FieldValues } from "react-hook-form"
import { useFormContext } from "react-hook-form"

import { useWizardLiveValidation } from "@/components/form/WizardLiveValidationContext"
import {
  FloatingLabelInput,
  type FloatingLabelInputProps,
} from "@/components/ui/floating-label-input"
import { FormControl, FormField, FormItem } from "@/components/ui/form"
import { getCrossFieldPeers } from "@/lib/form"

type FormFloatingInputProps<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
> = Omit<
  FloatingLabelInputProps,
  "value" | "defaultValue" | "onChange" | "onBlur" | "name" | "error"
> & {
  control: Control<TFieldValues>
  name: TName
  /** Validate on every change (and blur). */
  liveValidate?: boolean
  /** Validate on first blur, then live on each change after that. */
  blurThenLiveValidate?: boolean
  /** Validate only when the field loses focus. */
  validateOnBlur?: boolean
}

function FormFloatingInput<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
>({
  control,
  name,
  liveValidate,
  blurThenLiveValidate,
  validateOnBlur,
  ...inputProps
}: FormFloatingInputProps<TFieldValues, TName>) {
  const { trigger, clearErrors, formState, getValues, getFieldState } =
    useFormContext<TFieldValues>()
  const contextLiveValidate = useWizardLiveValidation(String(name))
  const [liveAfterBlur, setLiveAfterBlur] = useState(false)
  const shouldLiveValidate =
    liveValidate === true ||
    contextLiveValidate ||
    (blurThenLiveValidate === true && liveAfterBlur)
  const shouldValidateOnBlur =
    shouldLiveValidate || validateOnBlur === true || blurThenLiveValidate === true
  const crossFieldPeers = getCrossFieldPeers(String(name))

  const revalidateCrossFieldPeers = () => {
    for (const peer of crossFieldPeers) {
      const peerPath = peer as FieldPath<TFieldValues>
      const peerValue = getValues(peerPath)
      const peerTouched = getFieldState(peerPath, formState).isTouched

      if (!peerTouched) {
        continue
      }

      if (typeof peerValue === "string" && peerValue.trim().length > 0) {
        void trigger(peerPath)
      } else {
        clearErrors(peerPath)
      }
    }
  }

  return (
    <FormField
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <FormItem className="w-full gap-0">
          <FormControl>
            <FloatingLabelInput
              {...inputProps}
              {...field}
              value={field.value ?? ""}
              error={fieldState.error?.message}
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

                if (blurThenLiveValidate) {
                  setLiveAfterBlur(true)
                }

                if (
                  validateOnBlur &&
                  !liveValidate &&
                  !blurThenLiveValidate &&
                  !hasValue
                ) {
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

export { FormFloatingInput }
export type { FormFloatingInputProps }
