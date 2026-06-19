import { useEffect, useMemo } from "react"
import {
  get,
  useWatch,
  type FieldPath,
  type FieldValues,
  type UseFormReturn,
} from "react-hook-form"
import type { z } from "zod"

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

type UseGuestLoopStepOptions<TFieldValues extends FieldValues> = {
  selectStepValues?: (values: TFieldValues) => unknown
  /** Fields that validate on blur only — skip auto-trigger while typing. */
  shouldSkipValidationFeedback?: (fieldPath: string) => boolean
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

    const fieldsToTrigger = new Set<string>()

    for (const issue of result.error.issues) {
      const fieldPath = issuePathToFieldPath(issue.path)

      if (shouldSkipValidationFeedback?.(fieldPath)) {
        continue
      }

      const fieldValue = get(values, fieldPath)

      if (fieldHasContent(fieldValue)) {
        fieldsToTrigger.add(fieldPath)
      }
    }

    for (const fieldPath of fieldsToTrigger) {
      void form.trigger(fieldPath as FieldPath<TFieldValues>)
    }
  }, [
    watchedValues,
    isStepComplete,
    form,
    fields,
    stepSchema,
    selectStepValues,
    shouldSkipValidationFeedback,
  ])
}
