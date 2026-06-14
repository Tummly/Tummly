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
  liveValidate?: boolean
}

function FormFloatingInput<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
>({
  control,
  name,
  liveValidate,
  ...inputProps
}: FormFloatingInputProps<TFieldValues, TName>) {
  const { trigger, formState } = useFormContext<TFieldValues>()
  const contextLiveValidate = useWizardLiveValidation(String(name))
  const shouldLiveValidate = liveValidate ?? contextLiveValidate
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
              onBlur={field.onBlur}
            />
          </FormControl>
        </FormItem>
      )}
    />
  )
}

export { FormFloatingInput }
export type { FormFloatingInputProps }
