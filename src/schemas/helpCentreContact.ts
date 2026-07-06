import { z } from "zod"

import { emailSchema } from "@/schemas/primitives"

export const helpCentreGuestContactFormSchema = z.object({
  topic: z.string().min(1, "Select a topic."),
  businessName: z.string().trim().min(1, "Business name is required."),
  submitterName: z.string().trim().min(1, "Your name is required."),
  submitterEmail: emailSchema,
  phone: z.string(),
  restaurantLocationId: z.string(),
  message: z.string().trim().min(1, "Message is required."),
})

export const helpCentreOperatorContactFormSchema = z.object({
  topic: z.string().min(1, "Select a topic."),
  businessName: z.string().trim().min(1, "Business name is required."),
  submitterEmail: emailSchema,
  restaurantLocationId: z.string(),
  submitterName: z.string().trim().min(1, "Your name is required."),
  message: z.string().trim().min(1, "Message is required."),
})

export type HelpCentreGuestContactFormValues = z.infer<
  typeof helpCentreGuestContactFormSchema
>

export type HelpCentreOperatorContactFormValues = z.infer<
  typeof helpCentreOperatorContactFormSchema
>

/** @deprecated Use helpCentreGuestContactFormSchema or helpCentreOperatorContactFormSchema */
export const helpCentreContactFormSchema = helpCentreGuestContactFormSchema

/** @deprecated Use HelpCentreGuestContactFormValues */
export type HelpCentreContactFormValues = HelpCentreGuestContactFormValues
