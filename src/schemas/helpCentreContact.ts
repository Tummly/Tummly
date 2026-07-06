import { z } from "zod"

import { emailSchema } from "@/schemas/primitives"

export const helpCentreContactFormSchema = z.object({
  topic: z.string().min(1, "Select a topic."),
  businessName: z.string().trim().min(1, "Business name is required."),
  submitterName: z.string().trim().min(1, "Your name is required."),
  submitterEmail: emailSchema,
  phone: z.string(),
  restaurantLocationId: z.string(),
  message: z.string().trim().min(1, "Message is required."),
})

export type HelpCentreContactFormValues = z.infer<
  typeof helpCentreContactFormSchema
>
