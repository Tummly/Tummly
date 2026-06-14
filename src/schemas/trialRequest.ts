import { z } from "zod"

import { validationMessages } from "@/schemas/messages"
import { emailSchema, mobileSchema, optionalUrlSchema } from "@/schemas/primitives"
import type { TrialRequestPayload } from "@/types/trial"

export const trialRequestSchema = z.object({
  businessName: z
    .string()
    .trim()
    .min(1, validationMessages.trialRequest.businessName.required)
    .min(2, validationMessages.trialRequest.businessName.required)
    .max(100),
  businessCategory: z
    .string()
    .min(1, validationMessages.trialRequest.businessCategory.required),
  locations: z
    .string()
    .min(1, validationMessages.trialRequest.locations.required),
  businessLink: optionalUrlSchema,
  fullName: z
    .string()
    .trim()
    .min(1, validationMessages.trialRequest.fullName.required)
    .min(2, validationMessages.trialRequest.fullName.required)
    .max(100),
  email: emailSchema,
  mobile: mobileSchema,
  role: z.string().min(1, validationMessages.trialRequest.role.required),
  goal: z.string().min(1, validationMessages.trialRequest.goal.required),
  termsAccepted: z
    .boolean()
    .refine((value) => value === true, {
      message: validationMessages.trialRequest.terms.required,
    }),
})

export type TrialRequestFormValues = z.input<typeof trialRequestSchema>

export const trialRequestDefaultValues: TrialRequestFormValues = {
  businessName: "",
  businessCategory: "",
  locations: "",
  businessLink: "",
  fullName: "",
  email: "",
  mobile: "",
  role: "",
  goal: "",
  termsAccepted: false,
}

export function toTrialRequestPayload(
  values: TrialRequestFormValues
): TrialRequestPayload {
  const normalized = {
    ...values,
    businessName: values.businessName.trim(),
    businessLink: values.businessLink?.trim() ?? "",
    fullName: values.fullName.trim(),
    email: values.email.trim().toLowerCase(),
    mobile: values.mobile.trim(),
  }
  const parsed = trialRequestSchema.parse(normalized)

  return {
    businessName: parsed.businessName,
    businessCategory: parsed.businessCategory,
    locations: parsed.locations,
    businessLink: parsed.businessLink.trim() || undefined,
    fullName: parsed.fullName,
    email: parsed.email,
    mobile: parsed.mobile,
    role: parsed.role,
    goal: parsed.goal,
    termsAccepted: true,
  }
}
