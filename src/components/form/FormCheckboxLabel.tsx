import type { Control, FieldPath, FieldValues } from "react-hook-form"
import { useFormContext } from "react-hook-form"

import { useWizardLiveValidation } from "@/components/form/WizardLiveValidationContext"
import { CheckboxLabel, type CheckboxLabelProps } from "@/components/ui/checkbox-label"
import { FormField } from "@/components/ui/form"

type FormCheckboxLabelProps<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
> = Omit<CheckboxLabelProps, "checked" | "onCheckedChange" | "error"> & {
  control: Control<TFieldValues>
  name: TName
  liveValidate?: boolean
}

function FormCheckboxLabel<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
>({
  control,
  name,
  liveValidate,
  ...checkboxProps
}: FormCheckboxLabelProps<TFieldValues, TName>) {
  const { trigger } = useFormContext<TFieldValues>()
  const contextLiveValidate = useWizardLiveValidation(String(name))
  const shouldLiveValidate = liveValidate ?? contextLiveValidate

  return (
    <FormField
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <CheckboxLabel
          {...checkboxProps}
          checked={field.value === true}
          onCheckedChange={(value) => {
            field.onChange(value)
            if (shouldLiveValidate) {
              void trigger(name)
            }
          }}
          error={fieldState.error?.message}
        />
      )}
    />
  )
}

export { FormCheckboxLabel }
export type { FormCheckboxLabelProps }
