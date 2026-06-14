import { useEffect, useState } from "react"
import type { ReactNode } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import axios, { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { useNavigate, useSearchParams } from "react-router-dom"

import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingSelect } from "@/components/form/FormFloatingSelect"
import { WizardLiveValidationProvider } from "@/components/form/WizardLiveValidationContext"
import { API_BASE_URL, AUTH_API_BASE_URL } from "@/config/api"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form, FormField } from "@/components/ui/form"
import { addAttemptedFields, defaultFormValidationOptions } from "@/lib/form"
import {
  accountSetupSingleDefaultValues,
  accountSetupSingleSchema,
  accountSetupSingleStep1Fields,
  accountSetupSingleStep2Fields,
  toSingleLocationSetupPayload,
  type AccountSetupSingleFormValues,
} from "@/schemas/accountSetupSingle"

interface PasswordStrengthProps {
  password: string
}

interface ProgressBarProps {
  activeStep: number
}

interface AccordionProps {
  title: string
  children: ReactNode
  open: boolean
  setOpen: () => void
}

interface SetupAccountResponse {
  success?: boolean
  message?: string
  errors?: unknown
}

const PasswordStrength = ({ password }: PasswordStrengthProps) => {
  let strength = 0

  if (password.length >= 8) strength++
  if (/[A-Z]/.test(password)) strength++
  if (/[0-9!@#$%^&*]/.test(password)) strength++
  if (password.length >= 12) strength++

  const getColor = (index: number) => {
    if (index >= strength) {
      return "bg-[#E5E7EB]"
    }

    if (strength === 1) {
      return "bg-red-500"
    }

    if (strength === 2) {
      return "bg-yellow-500"
    }

    return "bg-[#22C55E]"
  }

  return (
    <div className="mb-2 mt-5 flex gap-3">
      {[1, 2, 3, 4].map((item, index) => (
        <div
          key={item}
          className={`h-[5px] flex-1 rounded-full transition-all duration-300 ${getColor(
            index
          )}`}
        />
      ))}
    </div>
  )
}

const ProgressBar = ({ activeStep }: ProgressBarProps) => {
  return (
    <div className="pt-7">
      <div className="mb-3 flex items-center justify-between text-[12px]">
        <span
          className={`font-medium ${
            activeStep >= 1 ? "text-black" : "text-[#9CA3AF]"
          }`}
        >
          1 Account
        </span>

        <span
          className={`font-medium ${
            activeStep >= 2 ? "text-black" : "text-[#9CA3AF]"
          }`}
        >
          2 Restaurant
        </span>

        <span
          className={`font-medium ${
            activeStep >= 3 ? "text-black" : "text-[#9CA3AF]"
          }`}
        >
          3 Guest Loop
        </span>
      </div>

      <div className="flex items-center gap-2">
        <div
          className={`h-[3px] flex-1 rounded-full ${
            activeStep >= 1 ? "bg-black" : "bg-[#E5E7EB]"
          }`}
        />
        <div
          className={`h-[3px] flex-1 rounded-full ${
            activeStep >= 2 ? "bg-black" : "bg-[#E5E7EB]"
          }`}
        />
        <div
          className={`h-[3px] flex-1 rounded-full ${
            activeStep >= 3 ? "bg-black" : "bg-[#E5E7EB]"
          }`}
        />
      </div>
    </div>
  )
}

const Accordion = ({
  title,
  children,
  open,
  setOpen,
}: AccordionProps) => {
  return (
    <div className="border-b border-[#E5E7EB] pb-5">
      <Button variant="section-toggle" onClick={setOpen}>
        <h3 className="text-[20px] font-semibold leading-none text-[#111827]">
          {title}
        </h3>

        <svg
          className={`h-5 w-5 text-[#6B7280] transition-all duration-300 ${
            open ? "rotate-180" : ""
          }`}
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
        >
          <path d="M6 9l6 6 6-6" />
        </svg>
      </Button>

      {open ? <div className="pt-6">{children}</div> : null}
    </div>
  )
}

const businessCategoryOptions = [
  { value: "Restaurant", label: "Restaurant" },
  { value: "Cafe", label: "Cafe" },
  { value: "Fast Food", label: "Fast Food" },
]

const touchpointOptions = [
  "Counter card",
  "Table card",
  "Receipt prompt",
  "Packaging sticker",
  "Delivery insert",
  "Window sticker",
  "Digital Smart Guest Link",
]

const feedbackOptions = [
  "Food quality",
  "Service quality",
  "Staff behaviour",
  "Cleanliness",
]

function RegisterSinglePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get("token")

  const form = useForm<AccountSetupSingleFormValues>({
    resolver: zodResolver(accountSetupSingleSchema),
    defaultValues: {
      ...accountSetupSingleDefaultValues,
      token: token ?? "",
    },
    ...defaultFormValidationOptions,
  })

  const [step, setStep] = useState(1)
  const [attemptedFields, setAttemptedFields] = useState<Set<string>>(new Set())
  const [loading, setLoading] = useState(false)
  const [tokenLoading, setTokenLoading] = useState(() => Boolean(token))
  const [tokenError, setTokenError] = useState(() =>
    token ? "" : "Setup token is missing."
  )

  const password = form.watch("password")
  const touchpoints = form.watch("touchpoints")
  const feedbackTags = form.watch("feedbackTags")
  const rootError = form.formState.errors.root?.message

  useEffect(() => {
    if (!token) {
      return
    }

    let active = true

    void (async () => {
      try {
        const response = await axios.get(
          `${API_BASE_URL}/Trial/validate-setup-token?token=${token}`
        )

        if (!active) {
          return
        }

        const data = response.data.data

        form.reset({
          ...form.getValues(),
          token,
          email: data.email || "",
          fullName: data.fullName || "",
          restaurantName: data.businessName || "",
        })

        setTokenError("")
      } catch (error: unknown) {
        if (!active) {
          return
        }

        if (isAxiosError<{ message?: string }>(error)) {
          setTokenError(
            error.response?.data?.message || "Invalid setup token"
          )
        } else {
          setTokenError("Invalid setup token")
        }
      } finally {
        if (active) {
          setTokenLoading(false)
        }
      }
    })()

    return () => {
      active = false
    }
  }, [form, token])

  const toggleArrayValue = (
    field: "touchpoints" | "feedbackTags",
    value: string
  ) => {
    const current = form.getValues(field)
    const next = current.includes(value)
      ? current.filter((item) => item !== value)
      : [...current, value]

    form.setValue(field, next, { shouldDirty: true })
  }

  const handleContinueStep1 = async () => {
    const valid = await form.trigger([...accountSetupSingleStep1Fields])
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupSingleStep1Fields)
      )
      return
    }

    setStep(2)
  }

  const handleContinueStep2 = async () => {
    const valid = await form.trigger([...accountSetupSingleStep2Fields])
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupSingleStep2Fields)
      )
      return
    }

    setStep(3)
  }

  const onCompleteSetup = async (values: AccountSetupSingleFormValues) => {
    form.clearErrors("root")
    setLoading(true)

    try {
      const response = await axios.post<SetupAccountResponse>(
        `${AUTH_API_BASE_URL}/setup-account`,
        toSingleLocationSetupPayload(values)
      )

      if (response.data.success) {
        navigate("/single-dashboard")
        return
      }

      form.setError("root", {
        message: response.data.message || "Account setup failed.",
      })
    } catch (error: unknown) {
      if (isAxiosError<SetupAccountResponse>(error)) {
        const apiMessage = error.response?.data?.message
        const apiErrors = error.response?.data?.errors

        form.setError("root", {
          message:
            apiMessage ||
            (apiErrors
              ? JSON.stringify(apiErrors, null, 2)
              : "Something went wrong"),
        })
      } else {
        form.setError("root", {
          message: "Something went wrong",
        })
      }
    } finally {
      setLoading(false)
    }
  }

  if (tokenLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center text-[20px] font-semibold">
        Validating setup token...
      </div>
    )
  }

  if (tokenError) {
    return (
      <div className="flex min-h-screen items-center justify-center px-6">
        <div className="max-w-[500px] text-center">
          <h1 className="mb-4 text-[34px] font-bold text-red-500">
            Invalid Setup Link
          </h1>

          <p className="text-[#6B7280]">{tokenError}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen flex-col bg-[#FAFAFA]">
      <Form {...form}>
        <WizardLiveValidationProvider attemptedFields={attemptedFields}>
        {step === 1 ? (
          <main className="flex flex-1 items-center justify-center px-4 py-10 sm:px-6 sm:py-14 md:px-8 md:py-20">
            <div className="w-full max-w-[820px] rounded-[24px] border border-[#E5E7EB] bg-white p-6 shadow-lg sm:p-8 md:p-12">
              <div className="mb-10 text-center md:mb-14">
                <h1 className="mb-3 text-[34px] font-bold leading-[1.1] tracking-[-2px] text-[#111827] sm:text-[42px] md:text-[56px]">
                  Create your account
                </h1>

                <p className="text-[15px] text-[#6B7280] sm:text-[16px]">
                  Complete your account setup to continue
                </p>
              </div>

              <div className="flex flex-col gap-8 md:gap-10">
                <FormFloatingInput
                  control={form.control}
                  name="email"
                  label="Email address"
                  type="email"
                  disabled
                />

                <FormFloatingInput
                  control={form.control}
                  name="fullName"
                  label="Full name"
                />

                <FormFloatingInput
                  control={form.control}
                  name="password"
                  label="Password"
                  type="password"
                />

                <PasswordStrength password={password} />

                <FormFloatingInput
                  control={form.control}
                  name="confirmPassword"
                  label="Confirm Password"
                  type="password"
                />

                <FormCheckboxLabel
                  control={form.control}
                  name="agree"
                  id="agree"
                  labelClassName="items-center"
                >
                  I agree to Terms
                </FormCheckboxLabel>

                <ProgressBar activeStep={1} />

                <Button
                  type="button"
                  variant="secondary"
                  size="form-continue"
                  onClick={handleContinueStep1}
                >
                  Continue
                </Button>
              </div>
            </div>
          </main>
        ) : null}

        {step === 2 ? (
          <main className="flex flex-1 items-center justify-center px-6 py-16 md:py-20">
            <div className="w-full max-w-[700px] space-y-5">
              <FormFloatingInput
                control={form.control}
                name="restaurantName"
                label="Restaurant Name"
              />

              <FormFloatingInput
                control={form.control}
                name="locationName"
                label="Location Name"
              />

              <FormFloatingInput
                control={form.control}
                name="address"
                label="Address"
              />

              <FormFloatingInput
                control={form.control}
                name="postcode"
                label="Postcode"
                optional
              />

              <FormFloatingInput
                control={form.control}
                name="phone"
                label="Public Phone Number"
                type="tel"
              />

              <FormFloatingInput
                control={form.control}
                name="businessLink"
                label="Business Link"
                optional
              />

              <FormFloatingSelect
                control={form.control}
                name="businessCategory"
                label="Select Category"
                options={businessCategoryOptions}
              />

              <ProgressBar activeStep={2} />

              <div className="flex gap-4">
                <Button
                  type="button"
                  variant="outline"
                  size="form-row"
                  onClick={() => setStep(1)}
                >
                  Back
                </Button>

                <Button
                  type="button"
                  variant="secondary"
                  size="form-row"
                  onClick={handleContinueStep2}
                >
                  Continue
                </Button>
              </div>
            </div>
          </main>
        ) : null}

        {step === 3 ? (
          <main className="flex flex-1 items-center justify-center px-6 py-16 md:py-20">
            <div className="w-full max-w-[700px] space-y-8">
              <Accordion title="Touchpoints" open={true} setOpen={() => {}}>
                <div className="space-y-4">
                  {touchpointOptions.map((item) => (
                    <label key={item} className="flex items-center gap-3">
                      <input
                        type="checkbox"
                        checked={touchpoints.includes(item)}
                        onChange={() => toggleArrayValue("touchpoints", item)}
                      />
                      <span>{item}</span>
                    </label>
                  ))}
                </div>
              </Accordion>

              <Accordion title="Feedback Tags" open={true} setOpen={() => {}}>
                <div className="space-y-4">
                  {feedbackOptions.map((item) => (
                    <label key={item} className="flex items-center gap-3">
                      <input
                        type="checkbox"
                        checked={feedbackTags.includes(item)}
                        onChange={() => toggleArrayValue("feedbackTags", item)}
                      />
                      <span>{item}</span>
                    </label>
                  ))}
                </div>
              </Accordion>

              <FormField
                control={form.control}
                name="thankYouMessage"
                render={({ field, fieldState }) => (
                  <div className="flex flex-col gap-1.5">
                    <textarea
                      {...field}
                      placeholder="Thank you message"
                      className="h-[140px] w-full rounded-[18px] border p-5"
                      aria-invalid={fieldState.error ? true : undefined}
                    />
                    {fieldState.error?.message ? (
                      <p
                        className="text-[12px] text-red-500"
                        role="alert"
                      >
                        {fieldState.error.message}
                      </p>
                    ) : null}
                  </div>
                )}
              />

              <FormFloatingInput
                control={form.control}
                name="offerHeadline"
                label="Offer headline"
                optional
              />

              <FormFloatingInput
                control={form.control}
                name="offerDetails"
                label="Offer details"
                optional
              />

              <FormFloatingInput
                control={form.control}
                name="offerExpiry"
                label="Offer expiry"
                optional
              />

              <FormFloatingInput
                control={form.control}
                name="offerRedemption"
                label="Offer redemption"
                optional
              />

              <FormFloatingInput
                control={form.control}
                name="offerUsageLimit"
                label="Offer usage limit"
                optional
              />

              <FieldErrorSlot
                error={rootError}
                reserveClassName="min-h-0"
              />

              <ProgressBar activeStep={3} />

              <div className="flex gap-4">
                <Button
                  type="button"
                  variant="outline"
                  size="form-row"
                  onClick={() => setStep(2)}
                >
                  Back
                </Button>

                <Button
                  type="button"
                  variant="secondary"
                  size="form-row"
                  disabled={loading}
                  onClick={form.handleSubmit(onCompleteSetup)}
                >
                  {loading ? "Creating..." : "Complete Setup"}
                </Button>
              </div>
            </div>
          </main>
        ) : null}
        </WizardLiveValidationProvider>
      </Form>
    </div>
  )
}

export default RegisterSinglePage
