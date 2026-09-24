import { z } from "zod"

import { emailSchema } from "@/schemas/primitives"

const optionalEmailSchema = z.union([
  z.literal(""),
  emailSchema,
])

export const helpCentreContactFormSchema = z.object({
  topic: z.string().min(1, "Select a topic."),
  submitterName: z.string().trim().min(1, "Full name is required."),
  submitterEmail: emailSchema,
  businessName: z.string(),
  locationCount: z.string(),
  alreadyUsingTummly: z.string(),
  alternateEmail: optionalEmailSchema,
  message: z
    .string()
    .trim()
    .min(1, "Message is required.")
    .max(5000, "Use 5,000 characters or fewer."),
})

export type HelpCentreContactFormValues = z.infer<
  typeof helpCentreContactFormSchema
>

/** @deprecated Use helpCentreContactFormSchema */
export const helpCentreGuestContactFormSchema = helpCentreContactFormSchema

/** @deprecated Use helpCentreContactFormSchema */
export const helpCentreOperatorContactFormSchema = helpCentreContactFormSchema

/** @deprecated Use HelpCentreContactFormValues */
export type HelpCentreGuestContactFormValues = HelpCentreContactFormValues

/** @deprecated Use HelpCentreContactFormValues */
export type HelpCentreOperatorContactFormValues = HelpCentreContactFormValues
