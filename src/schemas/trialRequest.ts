import { z } from "zod"

import { ukPostcodeRegex } from "@/lib/locationUpload/locationUploadValidation"
import { validationMessages } from "@/schemas/messages"
import {
  emailSchema,
  optionalMobileSchema,
  optionalUrlSchema,
} from "@/schemas/primitives"
import type { TrialRequestPayload } from "@/types/trial"

export const trialRequestSchema = z
  .object({
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
    mainLocation: z
      .string()
      .trim()
      .min(1, validationMessages.trialRequest.mainLocation.required)
      .max(500),
    mainLocationCommitted: z.boolean(),
    mainLocationManual: z.boolean(),
    townCity: z.string().max(150),
    postcode: z.string().max(20),
    fullName: z
      .string()
      .trim()
      .min(1, validationMessages.trialRequest.fullName.required)
      .min(2, validationMessages.trialRequest.fullName.required)
      .max(100),
    email: emailSchema,
    mobile: optionalMobileSchema,
    role: z.string().min(1, validationMessages.trialRequest.role.required),
    goal: z.string().min(1, validationMessages.trialRequest.goal.required),
    termsAccepted: z
      .boolean()
      .refine((value) => value === true, {
        message: validationMessages.trialRequest.terms.required,
      }),
  })
  .superRefine((data, ctx) => {
    if (!data.mainLocationCommitted) {
      ctx.addIssue({
        code: "custom",
        message: validationMessages.trialRequest.mainLocation.commitRequired,
        path: ["mainLocation"],
      })
      return
    }

    if (!data.townCity.trim()) {
      ctx.addIssue({
        code: "custom",
        message: validationMessages.trialRequest.townCity.required,
        path: ["townCity"],
      })
    }

    const trimmedPostcode = data.postcode.trim()

    if (!trimmedPostcode) {
      ctx.addIssue({
        code: "custom",
        message: validationMessages.trialRequest.postcode.required,
        path: ["postcode"],
      })
      return
    }

    if (!ukPostcodeRegex.test(trimmedPostcode)) {
      ctx.addIssue({
        code: "custom",
        message: validationMessages.trialRequest.postcode.invalid,
        path: ["postcode"],
      })
    }
  })

export type TrialRequestFormValues = z.input<typeof trialRequestSchema>

export const trialRequestDefaultValues: TrialRequestFormValues = {
  businessName: "",
  businessCategory: "",
  locations: "",
  businessLink: "",
  mainLocation: "",
  mainLocationCommitted: false,
  mainLocationManual: false,
  townCity: "",
  postcode: "",
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
    mainLocation: values.mainLocation.trim(),
    townCity: values.townCity.trim(),
    postcode: values.postcode.trim(),
    fullName: values.fullName.trim(),
    email: values.email.trim().toLowerCase(),
  }
  const parsed = trialRequestSchema.parse(normalized)

  return {
    businessName: parsed.businessName,
    businessCategory: parsed.businessCategory,
    locations: parsed.locations,
    businessLink: parsed.businessLink.trim() || undefined,
    mainLocation: parsed.mainLocation,
    townCity: parsed.townCity.trim(),
    postcode: parsed.postcode.trim(),
    fullName: parsed.fullName,
    email: parsed.email,
    mobile: parsed.mobile || undefined,
    role: parsed.role,
    goal: parsed.goal,
    termsAccepted: true,
  }
}
