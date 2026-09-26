import { zodResolver } from "@hookform/resolvers/zod"
import { isAxiosError } from "axios"
import { useEffect, useState } from "react"
import { useForm } from "react-hook-form"
import { Link, useNavigate } from "react-router-dom"

import {
  createHelpCentreQuery,
  getHelpCentreContactPrefill,
} from "@/api/helpCentreApi"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingSelect } from "@/components/form/FormFloatingSelect"
import { FormFloatingTextarea } from "@/components/form/FormFloatingTextarea"
import { ContactPageShell } from "@/components/help-centre/ContactPageShell"
import { Button } from "@/components/ui/button"
import { Form } from "@/components/ui/form"
import { HELP_CENTRE_CONTACT_SUCCESS_URL } from "@/config/support"
import {
  HELP_CENTRE_ALREADY_USING_OPTIONS,
  HELP_CENTRE_LOCATION_COUNT_OPTIONS,
  HELP_CENTRE_QUERY_TOPICS,
} from "@/content/helpCentre/queryTopics"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { getFetchErrorMessage } from "@/lib/apiEnvelope"
import {
  buildHelpCentreContactMessage,
  getContactTopicFieldFlags,
  resolveHelpCentreContactBusinessName,
} from "@/lib/helpCentreContactForm"
import type { HelpCentreContactSuccessState } from "@/lib/helpCentreContactSuccess"
import { FEEDBACK_DIALOG_SELECT_ITEM_CLASS } from "@/lib/operatorFeedback/feedbackPresentation"
import { OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import {
  helpCentreContactFormSchema,
  type HelpCentreContactFormValues,
} from "@/schemas/helpCentreContact"
import { useAuthStore } from "@/stores/authStore"

/** Opaque panel — Operator fill tokens are only set under `html.op`. */
const contactSelectMenuClass = `${OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS} z-50 min-w-40 gap-0 bg-white p-0 px-0 py-1 text-[#171717]`

const topicOptions = HELP_CENTRE_QUERY_TOPICS.map((topic) => ({
  value: topic.slug,
  label: topic.label,
}))

const locationCountOptions = HELP_CENTRE_LOCATION_COUNT_OPTIONS.map(
  (option) => ({
    value: option.value,
    label: option.label,
  })
)

const alreadyUsingOptions = HELP_CENTRE_ALREADY_USING_OPTIONS.map((option) => ({
  value: option.value,
  label: option.label,
}))

const submitButtonClass =
  "h-auto min-h-0 w-full gap-1.5 rounded-[4px] bg-[#141414] px-[18px] py-[14px] text-sm font-medium leading-5 text-white shadow-none hover:bg-[#141414]/90 disabled:bg-[#e0e0e0] disabled:text-[#7d7d7d] disabled:opacity-100"

export default function HelpCentreContactPage() {
  const navigate = useNavigate()
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const isOperator = Boolean(token && role === "USER")

  const form = useForm<HelpCentreContactFormValues>({
    resolver: zodResolver(helpCentreContactFormSchema),
    mode: "onChange",
    defaultValues: {
      topic: "",
      submitterName: "",
      submitterEmail: "",
      businessName: "",
      locationCount: "",
      alreadyUsingTummly: "",
      alternateEmail: "",
      message: "",
    },
  })

  const [submitError, setSubmitError] = useState<string | null>(null)
  const topic = form.watch("topic")
  const fieldFlags = getContactTopicFieldFlags(topic)

  useEffect(() => {
    if (!isOperator) {
      return
    }

    let active = true

    void (async () => {
      try {
        const prefill = await getHelpCentreContactPrefill()
        if (!active) {
          return
        }

        form.reset({
          ...form.getValues(),
          submitterName: prefill.submitterName || form.getValues("submitterName"),
          submitterEmail:
            prefill.submitterEmail || form.getValues("submitterEmail"),
          businessName: prefill.businessName || form.getValues("businessName"),
        })
      } catch {
        // Prefill is best-effort.
      }
    })()

    return () => {
      active = false
    }
  }, [isOperator, form])

  const onSubmit = form.handleSubmit(async (values) => {
    setSubmitError(null)

    const message = buildHelpCentreContactMessage(values)
    if (message.length > 5000) {
      setSubmitError(
        "Message is too long after adding the extra details. Shorten your message."
      )
      return
    }

    try {
      const result = await createHelpCentreQuery({
        topic: values.topic,
        businessName: resolveHelpCentreContactBusinessName(values),
        submitterName: values.submitterName.trim(),
        submitterEmail: values.submitterEmail.trim(),
        message,
      })

      const successState: HelpCentreContactSuccessState = {
        queryId: result.id,
        email: values.submitterEmail.trim(),
      }

      navigate(HELP_CENTRE_CONTACT_SUCCESS_URL, { state: successState })
    } catch (error) {
      setSubmitError(
        isAxiosError(error)
          ? getFetchErrorMessage(error.response?.data, "Unable to submit request.")
          : "Unable to submit request."
      )
    }
  })

  return (
    <ContactPageShell
      panel={
        <>
          <header className="flex flex-col gap-[18px]">
            <h2 className="m-0 font-jakarta text-[30px] font-medium leading-normal text-black lg:text-[54px]">
              How can we help?
            </h2>
            <p className="m-0 max-w-[590px] text-base leading-[22px] text-[#141414] lg:text-lg lg:leading-6">
              Tell us how we can help. Do not include passwords, payment card
              details or identity documents.
            </p>
          </header>

          <Form {...form}>
            <form onSubmit={onSubmit} className="flex w-full flex-col gap-6">
              <FormFloatingSelect
                control={form.control}
                name="topic"
                label="Choose a topic"
                options={topicOptions}
                disableFocusRing
                contentClassName={contactSelectMenuClass}
                itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
              />

              {fieldFlags.showQrOrderHint && (
                <p className="m-0 text-base leading-[22px] text-[#141414]">
                  For an existing order, include your order reference if you
                  have it.
                </p>
              )}

              {fieldFlags.showAlreadyUsing && (
                <FormFloatingSelect
                  control={form.control}
                  name="alreadyUsingTummly"
                  label="Are you already using Tummly?"
                  options={alreadyUsingOptions}
                  disableFocusRing
                  contentClassName={contactSelectMenuClass}
                  itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                />
              )}

              <FormFloatingInput
                control={form.control}
                name="submitterName"
                label="Full name *"
              />

              <div className="flex flex-col gap-3">
                <FormFloatingInput
                  control={form.control}
                  name="submitterEmail"
                  label="Email address *"
                  type="email"
                />
                <p className="m-0 text-sm leading-5 text-black">
                  We&apos;ll reply to this email address.
                </p>
              </div>

              {fieldFlags.showAlternateEmail && (
                <FormFloatingInput
                  control={form.control}
                  name="alternateEmail"
                  label={fieldFlags.alternateEmailLabel}
                  type="email"
                  optional
                />
              )}

              {fieldFlags.showBusinessName && (
                <FormFloatingInput
                  control={form.control}
                  name="businessName"
                  label={fieldFlags.businessNameLabel}
                  optional
                />
              )}

              {fieldFlags.showLocationCount && (
                <FormFloatingSelect
                  control={form.control}
                  name="locationCount"
                  label="How many locations do you operate? (optional)."
                  options={locationCountOptions}
                  disableFocusRing
                  contentClassName={contactSelectMenuClass}
                  itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                />
              )}

              <div className="flex flex-col gap-3.5">
                <FormFloatingTextarea
                  control={form.control}
                  name="message"
                  label="Message *"
                  className="min-h-[176px]"
                />
                <p className="m-0 text-xs leading-4 text-black">
                  Use 5,000 characters or fewer.
                </p>
              </div>

              <p className="m-0 max-w-[480px] text-xs font-medium leading-[18px] text-[#7c7c7c]">
                We&apos;ll use your details to respond to your enquiry. Read our{" "}
                <Link
                  to={LEGAL_ROUTES.privacy}
                  className="underline underline-offset-2"
                >
                  Privacy Notice
                </Link>{" "}
                to learn how we handle your personal data.
              </p>

              {submitError && (
                <p className="m-0 text-sm text-destructive" role="alert">
                  {submitError}
                </p>
              )}

              <Button
                type="submit"
                disabled={!form.formState.isValid || form.formState.isSubmitting}
                className={submitButtonClass}
              >
                {form.formState.isSubmitting ? "Sending..." : "Send enquiry"}
              </Button>
            </form>
          </Form>
        </>
      }
    />
  )
}
