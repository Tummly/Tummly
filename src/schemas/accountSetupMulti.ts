import { z } from "zod"

import { ukPostcodeRegex } from "@/lib/locationUpload/locationUploadValidation"
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
    .min(1, validationMessages.accountSetup.postcode.required)
    .regex(ukPostcodeRegex, validationMessages.accountSetup.postcode.invalid),
  addressOverridden: z.boolean().optional(),
  locationPhone: z.string(),
  localContact: z.string(),
})

export type LocationFormItem = z.infer<typeof locationItemSchema>

export const emptyLocationItem: LocationFormItem = {
  locationName: "",
  address: "",
  postcode: "",
  addressOverridden: false,
  locationPhone: "",
  localContact: "",
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
  "numLocations",
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

const passwordMatchRefine = {
  message: validationMessages.password.mismatch,
  path: ["confirmPassword"],
}

const accountSetupMultiBaseSchema = z.object({
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
  numLocations: z
    .string()
    .min(1, validationMessages.accountSetup.numLocations.required),
  primaryPhone: z.string(),
  businessLink: optionalUrlSchema,
  locations: z
    .array(locationItemSchema)
    .min(1, validationMessages.accountSetup.locations.required),
})

export const accountSetupMultiStep1Schema = accountSetupMultiBaseSchema
  .pick({
    email: true,
    fullName: true,
    password: true,
    confirmPassword: true,
    agree: true,
  })
  .refine(
    (data) => data.password === data.confirmPassword,
    passwordMatchRefine
  )

export const accountSetupMultiStep2Schema = accountSetupMultiBaseSchema.pick({
  groupName: true,
  businessCategory: true,
  numLocations: true,
})

export const accountSetupMultiStep3Schema = z.object({
  locations: z
    .array(
      locationItemSchema.pick({
        locationName: true,
        address: true,
        postcode: true,
      })
    )
    .min(1, validationMessages.accountSetup.locations.required),
})

export const accountSetupMultiSchema = accountSetupMultiBaseSchema.refine(
  (data) => data.password === data.confirmPassword,
  passwordMatchRefine
)

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
}

export function toMultiLocationSetupPayload(
  values: AccountSetupMultiFormValues
): CompleteSetupPayload {
  const parsed = accountSetupMultiSchema.parse(values)

  return {
    token: parsed.token,
    fullName: parsed.fullName,
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
      ...(location.addressOverridden ? { addressOverridden: true } : {}),
    })),
  }
}
