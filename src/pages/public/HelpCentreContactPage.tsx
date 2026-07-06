import { useEffect, useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { zodResolver } from "@hookform/resolvers/zod"
import { isAxiosError } from "axios"
import { useForm } from "react-hook-form"

import {
  createHelpCentreQuery,
  getHelpCentreContactPrefill,
} from "@/api/helpCentreApi"
import Footer from "@/components/home/Footer"
import { HelpCentreAttachmentUpload } from "@/components/help-centre/HelpCentreAttachmentUpload"
import { HelpCentreFormPanel } from "@/components/help-centre/HelpCentreFormPanel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingSelect } from "@/components/form/FormFloatingSelect"
import { FormFloatingTextarea } from "@/components/form/FormFloatingTextarea"
import { Button } from "@/components/ui/button"
import { Form } from "@/components/ui/form"
import {
  HELP_CENTRE_CONTACT_SUCCESS_URL,
} from "@/config/support"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { HELP_CENTRE_QUERY_TOPICS } from "@/content/helpCentre/queryTopics"
import { getFetchErrorMessage } from "@/lib/apiEnvelope"
import { validateHelpCentreAttachments } from "@/lib/helpCentreAttachments"
import {
  helpCentreGuestContactFormSchema,
  helpCentreOperatorContactFormSchema,
  type HelpCentreGuestContactFormValues,
  type HelpCentreOperatorContactFormValues,
} from "@/schemas/helpCentreContact"
import { useAuthStore } from "@/stores/authStore"

const topicOptions = HELP_CENTRE_QUERY_TOPICS.map((topic) => ({
  value: topic.slug,
  label: topic.label,
}))

const emptyLocationOption = { value: "", label: " " }

export default function HelpCentreContactPage() {
  const navigate = useNavigate()
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const isOperator = Boolean(token && role === "USER")

  const guestForm = useForm<HelpCentreGuestContactFormValues>({
    resolver: zodResolver(helpCentreGuestContactFormSchema),
    mode: "onChange",
    defaultValues: {
      topic: "",
      businessName: "",
      submitterName: "",
      submitterEmail: "",
      phone: "",
      restaurantLocationId: "",
      message: "",
    },
  })

  const operatorForm = useForm<HelpCentreOperatorContactFormValues>({
    resolver: zodResolver(helpCentreOperatorContactFormSchema),
    mode: "onChange",
    defaultValues: {
      topic: "",
      businessName: "",
      submitterName: "",
      submitterEmail: "",
      restaurantLocationId: "",
      message: "",
    },
  })

  const [locations, setLocations] = useState<
    Array<{ id: number; label: string }>
  >([])
  const [attachments, setAttachments] = useState<File[]>([])
  const [submitError, setSubmitError] = useState<string | null>(null)

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

        const singleLocationId =
          prefill.locations.length === 1
            ? String(prefill.locations[0].id)
            : ""

        operatorForm.reset({
          topic: operatorForm.getValues("topic"),
          businessName: prefill.businessName,
          submitterName: prefill.submitterName,
          submitterEmail: prefill.submitterEmail,
          restaurantLocationId: singleLocationId,
          message: operatorForm.getValues("message"),
        })
        setLocations(prefill.locations)
      } catch {
        // Prefill is best-effort.
      }
    })()

    return () => {
      active = false
    }
  }, [isOperator, operatorForm])

  const locationOptions = [
    emptyLocationOption,
    ...locations.map((location) => ({
      value: String(location.id),
      label: location.label,
    })),
  ]

  const submitGuest = guestForm.handleSubmit(async (values) => {
    setSubmitError(null)

    try {
      await createHelpCentreQuery({
        topic: values.topic,
        businessName: values.businessName.trim(),
        submitterName: values.submitterName.trim(),
        submitterEmail: values.submitterEmail.trim(),
        phone: values.phone.trim() || undefined,
        message: values.message.trim(),
      })

      navigate(HELP_CENTRE_CONTACT_SUCCESS_URL)
    } catch (error) {
      setSubmitError(
        isAxiosError(error)
          ? getFetchErrorMessage(error.response?.data, "Unable to submit request.")
          : "Unable to submit request."
      )
    }
  })

  const submitOperator = operatorForm.handleSubmit(async (values) => {
    setSubmitError(null)

    const attachmentError = validateHelpCentreAttachments(attachments)
    if (attachmentError) {
      setSubmitError(attachmentError)
      return
    }

    try {
      await createHelpCentreQuery({
        topic: values.topic,
        businessName: values.businessName.trim(),
        submitterName: values.submitterName.trim(),
        submitterEmail: values.submitterEmail.trim(),
        restaurantLocationId: values.restaurantLocationId
          ? Number(values.restaurantLocationId)
          : undefined,
        message: values.message.trim(),
        attachments,
      })

      navigate(HELP_CENTRE_CONTACT_SUCCESS_URL)
    } catch (error) {
      setSubmitError(
        isAxiosError(error)
          ? getFetchErrorMessage(error.response?.data, "Unable to submit request.")
          : "Unable to submit request."
      )
    }
  })

  const guestValid = guestForm.formState.isValid
  const operatorValid =
    operatorForm.formState.isValid
    && validateHelpCentreAttachments(attachments) === null

  return (
    <div className="flex w-full flex-1 flex-col bg-white">
      <HelpCentreFormPanel className="flex flex-1 flex-col">
        <div className="flex w-full flex-col gap-[50px]">
          <header className="flex flex-col items-center gap-3.5 text-center text-[#232323]">
            <h1 className="m-0 text-[36px] font-bold tracking-[-0.72px]">
              Contact us
            </h1>
            <p className="m-0 max-w-[400px] text-lg leading-6 tracking-[-0.36px]">
              Tell us what you need help with and we&apos;ll route your request
              to the right team.
            </p>
          </header>

          {isOperator ? (
            <Form {...operatorForm}>
              <form
                onSubmit={submitOperator}
                className="flex w-full flex-col gap-6"
              >
                <div className="flex flex-col gap-6">
                  <FormFloatingInput
                    control={operatorForm.control}
                    name="businessName"
                    label="Business name"
                  />

                  <FormFloatingInput
                    control={operatorForm.control}
                    name="submitterEmail"
                    label="Email"
                    type="email"
                  />

                  {locations.length > 1 && (
                    <FormFloatingSelect
                      control={operatorForm.control}
                      name="restaurantLocationId"
                      label="Location"
                      options={locationOptions}
                    />
                  )}

                  <FormFloatingSelect
                    control={operatorForm.control}
                    name="topic"
                    label="I need help with"
                    options={topicOptions}
                  />

                  <FormFloatingInput
                    control={operatorForm.control}
                    name="submitterName"
                    label="Your name"
                  />

                  <FormFloatingTextarea
                    control={operatorForm.control}
                    name="message"
                    label="Message"
                    className="min-h-[191px]"
                  />

                  <HelpCentreAttachmentUpload
                    files={attachments}
                    onChange={setAttachments}
                  />

                  <hr className="m-0 border-0 border-t border-[#e5e5e5]" />
                </div>

                <ContactFormFooter
                  submitError={submitError}
                  isSubmitting={operatorForm.formState.isSubmitting}
                  isValid={operatorValid}
                />
              </form>
            </Form>
          ) : (
            <Form {...guestForm}>
              <form onSubmit={submitGuest} className="flex w-full flex-col gap-6">
                <div className="flex flex-col gap-6">
                  <FormFloatingSelect
                    control={guestForm.control}
                    name="topic"
                    label="I need help with"
                    options={topicOptions}
                  />

                  <FormFloatingInput
                    control={guestForm.control}
                    name="businessName"
                    label="Restaurant/business name"
                  />

                  <FormFloatingInput
                    control={guestForm.control}
                    name="submitterName"
                    label="Your name"
                  />

                  <div className="grid gap-4 sm:grid-cols-2">
                    <FormFloatingInput
                      control={guestForm.control}
                      name="submitterEmail"
                      label="Email"
                      type="email"
                    />
                    <FormFloatingInput
                      control={guestForm.control}
                      name="phone"
                      label="Phone"
                      optional
                    />
                  </div>

                  <FormFloatingTextarea
                    control={guestForm.control}
                    name="message"
                    label="Message"
                    className="min-h-[191px]"
                  />

                  <hr className="m-0 border-0 border-t border-[#e5e5e5]" />
                </div>

                <ContactFormFooter
                  submitError={submitError}
                  isSubmitting={guestForm.formState.isSubmitting}
                  isValid={guestValid}
                />
              </form>
            </Form>
          )}
        </div>
      </HelpCentreFormPanel>
      <div className="mt-auto shrink-0">
        <Footer />
      </div>
    </div>
  )
}

function ContactFormFooter({
  submitError,
  isSubmitting,
  isValid,
}: {
  submitError: string | null
  isSubmitting: boolean
  isValid: boolean
}) {
  return (
    <div className="flex flex-col gap-[50px]">
      <p className="m-0 text-sm leading-normal text-[#141414]">
        By submitting this request, you confirm that the information provided may
        be used to manage and respond to your support case in line with our{" "}
        <Link
          to={LEGAL_ROUTES.privacy}
          className="text-[#141414] underline underline-offset-2"
        >
          Privacy Policy
        </Link>
        .
      </p>

      {submitError && (
        <p className="m-0 text-sm text-destructive" role="alert">
          {submitError}
        </p>
      )}

      <Button
        type="submit"
        disabled={!isValid || isSubmitting}
        className="h-[50px] w-full rounded-[54px] border border-transparent px-[21px] py-[11px] text-base font-medium disabled:bg-[#e0e0e0] disabled:text-[#7d7d7d] disabled:opacity-100 enabled:bg-[#14a74a] enabled:text-white enabled:hover:bg-[#129641]"
      >
        {isSubmitting ? "Submitting..." : "Submit"}
      </Button>
    </div>
  )
}
