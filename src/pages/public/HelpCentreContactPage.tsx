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
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingSelect } from "@/components/form/FormFloatingSelect"
import { FormFloatingTextarea } from "@/components/form/FormFloatingTextarea"
import { HelpCentreFormPanel } from "@/components/help-centre/HelpCentreFormPanel"
import { Button } from "@/components/ui/button"
import { Form } from "@/components/ui/form"
import {
  HELP_CENTRE_CONTACT_SUCCESS_URL,
  HELP_CENTRE_MY_QUERIES_URL,
} from "@/config/support"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { HELP_CENTRE_QUERY_TOPICS } from "@/content/helpCentre/queryTopics"
import { getFetchErrorMessage } from "@/lib/apiEnvelope"
import {
  helpCentreContactFormSchema,
  type HelpCentreContactFormValues,
} from "@/schemas/helpCentreContact"
import { useAuthStore } from "@/stores/authStore"
const topicOptions = HELP_CENTRE_QUERY_TOPICS.map((topic) => ({
  value: topic.slug,
  label: topic.label,
}))

export default function HelpCentreContactPage() {
  const navigate = useNavigate()
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const isOperator = Boolean(token && role === "USER")

  const form = useForm<HelpCentreContactFormValues>({
    resolver: zodResolver(helpCentreContactFormSchema),
    mode: "onChange",
    defaultValues: {      topic: "",
      businessName: "",
      submitterName: "",
      submitterEmail: "",
      phone: "",
      restaurantLocationId: "",
      message: "",
    },
  })

  const [locations, setLocations] = useState<
    Array<{ id: number; label: string }>
  >([])
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {    if (!isOperator) {
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
          topic: form.getValues("topic"),
          businessName: prefill.businessName,
          submitterName: prefill.submitterName,
          submitterEmail: prefill.submitterEmail,
          phone: form.getValues("phone"),
          restaurantLocationId: "",
          message: form.getValues("message"),
        })
        setLocations(prefill.locations)
      } catch {
        // Prefill is best-effort.
      }
    })()

    return () => {
      active = false
    }
  }, [form, isOperator])

  const onSubmit = form.handleSubmit(async (values) => {
    setSubmitError(null)

    try {
      await createHelpCentreQuery({
        topic: values.topic,
        businessName: values.businessName.trim(),
        submitterName: values.submitterName.trim(),
        submitterEmail: values.submitterEmail.trim(),
        phone: values.phone.trim() || undefined,
        restaurantLocationId: values.restaurantLocationId
          ? Number(values.restaurantLocationId)
          : undefined,
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

  const locationOptions = locations.map((location) => ({
    value: String(location.id),
    label: location.label,
  }))

  return (
    <>
      <HelpCentreFormPanel>
        <div className="flex w-full flex-col gap-[50px]">
          <header className="flex flex-col items-center gap-3.5 text-center text-[#232323]">
            {isOperator && (
              <Link
                to={HELP_CENTRE_MY_QUERIES_URL}
                className="text-sm font-medium text-[#14a74a] underline-offset-4 hover:underline"
              >
                View my queries
              </Link>
            )}
            <h1 className="m-0 text-[36px] font-bold tracking-[-0.72px]">              Contact us
            </h1>
            <p className="m-0 max-w-[400px] text-lg leading-6 tracking-[-0.36px]">
              Tell us what you need help with and we&apos;ll route your request
              to the right team.
            </p>
          </header>

          <Form {...form}>
            <form onSubmit={onSubmit} className="flex w-full flex-col gap-6">
              <div className="flex flex-col gap-6">
                <FormFloatingSelect
                  control={form.control}
                  name="topic"
                  label="I need help with"
                  options={topicOptions}
                />

                <FormFloatingInput
                  control={form.control}
                  name="businessName"
                  label="Restaurant/business name"
                />

                <FormFloatingInput
                  control={form.control}
                  name="submitterName"
                  label="Your name"
                />

                <div className="grid gap-4 sm:grid-cols-2">
                  <FormFloatingInput
                    control={form.control}
                    name="submitterEmail"
                    label="Email"
                    type="email"
                  />
                  <FormFloatingInput
                    control={form.control}
                    name="phone"
                    label="Phone"
                    optional
                  />
                </div>

                {isOperator && locationOptions.length > 0 && (
                  <FormFloatingSelect
                    control={form.control}
                    name="restaurantLocationId"
                    label="Location"
                    options={locationOptions}
                  />
                )}

                <FormFloatingTextarea
                  control={form.control}
                  name="message"
                  label="Message"
                  className="min-h-[191px]"
                />

                <hr className="m-0 border-0 border-t border-[#e5e5e5]" />
              </div>

              <div className="flex flex-col gap-[50px]">
                <p className="m-0 text-sm leading-normal text-[#141414]">
                  By submitting this request, you confirm that the information
                  provided may be used to manage and respond to your support
                  case in line with our{" "}
                  <Link
                    to={LEGAL_ROUTES.privacy}
                    className="text-[#141414] underline underline-offset-2"
                  >                    Privacy Policy
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
                  disabled={!form.formState.isValid || form.formState.isSubmitting}                  className="h-[50px] w-full rounded-[54px] border border-transparent px-[21px] py-[11px] text-base font-medium disabled:bg-[#e0e0e0] disabled:text-[#7d7d7d] disabled:opacity-100 enabled:bg-[#14a74a] enabled:text-white enabled:hover:bg-[#129641]"
                >
                  {form.formState.isSubmitting ? "Submitting..." : "Submit"}
                </Button>
              </div>
            </form>
          </Form>
        </div>
      </HelpCentreFormPanel>
      <Footer />
    </>
  )
}
