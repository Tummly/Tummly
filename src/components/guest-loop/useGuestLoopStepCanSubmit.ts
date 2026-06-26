import { useEffect, useMemo, useRef } from "react"
import {
  get,
  useWatch,
  type FieldPath,
  type FieldValues,
  type UseFormReturn,
} from "react-hook-form"
import type { z } from "zod"

export type ValidateWizardStepOptions<TFieldValues extends FieldValues> = {
  selectStepValues?: (values: TFieldValues) => unknown
}

function pickStepValues<TFieldValues extends FieldValues>(
  values: TFieldValues,
  fields: readonly FieldPath<TFieldValues>[]
) {
  return Object.fromEntries(
    fields.map((field) => [field, get(values, field)])
  )
}

function issuePathToFieldPath(path: PropertyKey[]) {
  return path.map(String).join(".")
}

function fieldHasContent(value: unknown) {
  return typeof value === "string" && value.trim().length > 0
}

type UseGuestLoopStepOptions<TFieldValues extends FieldValues> =
  ValidateWizardStepOptions<TFieldValues> & {
    /** Fields that validate on blur only — skip auto-trigger while typing. */
    shouldSkipValidationFeedback?: (fieldPath: string) => boolean
  }

/**
 * Validates only the current wizard step schema and maps Zod issues onto RHF
 * fields. Use this instead of `form.trigger` for step navigation — the form
 * resolver runs the full schema and blocks advance when later steps are empty.
 */
export function validateWizardStep<TFieldValues extends FieldValues>(
  form: UseFormReturn<TFieldValues>,
  fields: readonly FieldPath<TFieldValues>[],
  stepSchema: z.ZodType,
  options?: ValidateWizardStepOptions<TFieldValues>
): boolean {
  const values = form.getValues()
  const stepValues = options?.selectStepValues
    ? options.selectStepValues(values)
    : pickStepValues(values, fields)
  const result = stepSchema.safeParse(stepValues)

  for (const field of fields) {
    form.clearErrors(field)
  }

  if (result.success) {
    return true
  }

  for (const issue of result.error.issues) {
    const fieldPath = issuePathToFieldPath(
      issue.path
    ) as FieldPath<TFieldValues>

    form.setError(fieldPath, {
      type: "custom",
      message: issue.message,
    })
  }

  return false
}

export function useGuestLoopStepCanSubmit<TFieldValues extends FieldValues>(
  form: UseFormReturn<TFieldValues>,
  fields: readonly FieldPath<TFieldValues>[],
  stepSchema: z.ZodType,
  options?: UseGuestLoopStepOptions<TFieldValues>
) {
  const { selectStepValues } = options ?? {}
  const watchedValues = useWatch({ control: form.control }) as
    | TFieldValues
    | undefined

  return useMemo(() => {
    const values = watchedValues ?? form.getValues()
    const stepValues = selectStepValues
      ? selectStepValues(values)
      : pickStepValues(values, fields)

    return stepSchema.safeParse(stepValues).success
  }, [watchedValues, fields, form, selectStepValues, stepSchema])
}

/**
 * Surfaces step-schema failures on fields that already have input, so the CTA
 * gate and inline errors stay in sync when liveValidate has not fired yet.
 */
export function useGuestLoopStepValidationFeedback<
  TFieldValues extends FieldValues,
>(
  form: UseFormReturn<TFieldValues>,
  fields: readonly FieldPath<TFieldValues>[],
  stepSchema: z.ZodType,
  isStepComplete: boolean,
  options?: UseGuestLoopStepOptions<TFieldValues>
) {
  const { selectStepValues, shouldSkipValidationFeedback } = options ?? {}
  const shouldSkipValidationFeedbackRef = useRef(shouldSkipValidationFeedback)
  shouldSkipValidationFeedbackRef.current = shouldSkipValidationFeedback

  const watchedValues = useWatch({ control: form.control }) as
    | TFieldValues
    | undefined

  useEffect(() => {
    if (isStepComplete) {
      return
    }

    const values = watchedValues ?? form.getValues()
    const stepValues = selectStepValues
      ? selectStepValues(values)
      : pickStepValues(values, fields)
    const result = stepSchema.safeParse(stepValues)

    if (result.success) {
      return
    }

    for (const issue of result.error.issues) {
      const fieldPath = issuePathToFieldPath(issue.path)

      if (shouldSkipValidationFeedbackRef.current?.(fieldPath)) {
        continue
      }

      const fieldValue = get(values, fieldPath)

      if (!fieldHasContent(fieldValue)) {
        continue
      }

      form.setError(fieldPath as FieldPath<TFieldValues>, {
        type: "custom",
        message: issue.message,
      })
    }
  }, [
    watchedValues,
    isStepComplete,
    form,
    fields,
    stepSchema,
    selectStepValues,
  ])
}
