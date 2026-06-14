import { z } from "zod"

import { validationMessages } from "@/schemas/messages"
import {
  emailSchema,
  optionalUrlSchema,
  passwordSchema,
} from "@/schemas/primitives"
import type { CompleteSetupPayload } from "@/types/trial"

export const locationItemSchema = z.object({
  locationName: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.locationName.required),
  address: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.address.required),
  postcode: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.postcode.required),
  locationPhone: z.string(),
  localContact: z.string(),
  includeInRollout: z.boolean(),
})

export type LocationFormItem = z.infer<typeof locationItemSchema>

export const emptyLocationItem: LocationFormItem = {
  locationName: "",
  address: "",
  postcode: "",
  locationPhone: "",
  localContact: "",
  includeInRollout: true,
}

export const accountSetupMultiStep1Fields = [
  "email",
  "fullName",
  "password",
  "confirmPassword",
  "agree",
] as const

export const accountSetupMultiStep2Fields = [
  "groupName",
  "businessCategory",
] as const

export const accountSetupMultiStep4Fields = [
  "rolloutApproach",
] as const

export function getAccountSetupMultiStep3FieldNames(locationCount: number) {
  return Array.from({ length: locationCount }, (_, index) => [
    `locations.${index}.locationName`,
    `locations.${index}.address`,
    `locations.${index}.postcode`,
  ]).flat() as [
    `locations.${number}.locationName` | `locations.${number}.address` | `locations.${number}.postcode`,
    ...(`locations.${number}.locationName` | `locations.${number}.address` | `locations.${number}.postcode`)[],
  ]
}

export const accountSetupMultiSchema = z
  .object({
    token: z.string().min(1),
    email: emailSchema,
    fullName: z
      .string()
      .trim()
      .min(1, validationMessages.accountSetup.fullName.required)
      .min(2, validationMessages.accountSetup.fullName.required)
      .max(100),
    password: passwordSchema,
    confirmPassword: z.string().min(1, validationMessages.password.required),
    agree: z.boolean().refine((value) => value === true, {
      message: validationMessages.accountSetup.terms.required,
    }),
    groupName: z
      .string()
      .trim()
      .min(1, validationMessages.accountSetup.groupName.required),
    businessCategory: z
      .string()
      .min(1, validationMessages.accountSetup.businessCategory.required),
    numLocations: z.string(),
    primaryPhone: z.string(),
    businessLink: optionalUrlSchema,
    locations: z
      .array(locationItemSchema)
      .min(1, validationMessages.accountSetup.locations.required),
    rolloutApproach: z
      .string()
      .min(1, validationMessages.accountSetup.rolloutApproach.required),
    guestPrompt: z.string(),
    thankYouMessage: z
      .string()
      .trim()
      .min(1, validationMessages.accountSetup.thankYouMessage.required),
    offerType: z.string(),
    offerTitle: z.string(),
    offerMessage: z.string(),
    expiry: z.string(),
    redemptionMethod: z.string(),
    usageLimit: z.string(),
    guestPreview: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: validationMessages.password.mismatch,
    path: ["confirmPassword"],
  })
  .superRefine((data, ctx) => {
    if (!data.offerType.trim()) {
      return
    }

    if (!data.offerTitle.trim()) {
      ctx.addIssue({
        code: "custom",
        path: ["offerTitle"],
        message: validationMessages.accountSetup.offerTitle.required,
      })
    }

    if (!data.expiry) {
      ctx.addIssue({
        code: "custom",
        path: ["expiry"],
        message: validationMessages.accountSetup.offerExpiry.required,
      })
    }

    if (!data.redemptionMethod) {
      ctx.addIssue({
        code: "custom",
        path: ["redemptionMethod"],
        message: validationMessages.accountSetup.redemptionMethod.required,
      })
    }

    if (!data.usageLimit) {
      ctx.addIssue({
        code: "custom",
        path: ["usageLimit"],
        message: validationMessages.accountSetup.usageLimit.required,
      })
    }
  })

export type AccountSetupMultiFormValues = z.input<
  typeof accountSetupMultiSchema
>

export const accountSetupMultiDefaultValues: AccountSetupMultiFormValues = {
  token: "",
  email: "",
  fullName: "",
  password: "",
  confirmPassword: "",
  agree: false,
  groupName: "",
  businessCategory: "",
  numLocations: "",
  primaryPhone: "",
  businessLink: "",
  locations: [emptyLocationItem],
  rolloutApproach: "",
  guestPrompt: "",
  thankYouMessage:
    "Thanks for your feedback. We appreciate you taking a moment to help us improve.",
  offerType: "",
  offerTitle: "",
  offerMessage: "",
  expiry: "",
  redemptionMethod: "",
  usageLimit: "",
  guestPreview: "",
}

export function toMultiLocationSetupPayload(
  values: AccountSetupMultiFormValues
): CompleteSetupPayload {
  const parsed = accountSetupMultiSchema.parse(values)

  return {
    token: parsed.token,
    password: parsed.password,
    confirmPassword: parsed.confirmPassword,
    groupName: parsed.groupName,
    businessCategory: parsed.businessCategory,
    primaryPhone: parsed.primaryPhone.trim() || undefined,
    businessLink: parsed.businessLink.trim() || undefined,
    locations: parsed.locations.map((location) => ({
      locationName: location.locationName,
      address: location.address,
      postcode: location.postcode.trim() || undefined,
      locationPhone: location.locationPhone.trim() || undefined,
      localContact: location.localContact.trim() || undefined,
      includeInRollout: location.includeInRollout,
    })),
    rolloutApproach: parsed.rolloutApproach,
    guestPrompt: parsed.guestPrompt.trim() || undefined,
    thankYouMessage: parsed.thankYouMessage,
    offerType: parsed.offerType.trim() || undefined,
    offerTitle: parsed.offerTitle.trim() || undefined,
    offerMessage: parsed.offerMessage.trim() || undefined,
    offerExpiry: parsed.expiry.trim() || undefined,
    redemptionMethod: parsed.redemptionMethod.trim() || undefined,
    usageLimit: parsed.usageLimit.trim() || undefined,
  }
}
