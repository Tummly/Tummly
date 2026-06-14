import { useEffect, useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import axios, { isAxiosError } from "axios"
import { useFieldArray, useForm } from "react-hook-form"
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
  accountSetupMultiDefaultValues,
  accountSetupMultiSchema,
  accountSetupMultiStep1Fields,
  accountSetupMultiStep2Fields,
  emptyLocationItem,
  getAccountSetupMultiStep3FieldNames,
  toMultiLocationSetupPayload,
  type AccountSetupMultiFormValues,
} from "@/schemas/accountSetupMulti"

interface SetupAccountResponse {
  success?: boolean
  message?: string
  errors?: unknown
}

const MultiRegisterPage = () => {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get("token")

  const form = useForm<AccountSetupMultiFormValues>({
    resolver: zodResolver(accountSetupMultiSchema),
    defaultValues: {
      ...accountSetupMultiDefaultValues,
      token: token ?? "",
    },
    ...defaultFormValidationOptions,
  })

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: "locations",
  })

  const [step, setStep] = useState(1)
  const [attemptedFields, setAttemptedFields] = useState<Set<string>>(new Set())
  const [submitting, setSubmitting] = useState(false)
  const [tokenLoading, setTokenLoading] = useState(() => Boolean(token))
  const [tokenError, setTokenError] = useState(() =>
    token ? "" : "Setup token is missing."
  )

  const password = form.watch("password")
  const rootError = form.formState.errors.root?.message

  const [openSection, setOpenSection] = useState<Record<string, boolean>>({
    rollout: true,
    prompts: false,
    feedback: false,
    thankyou: false,
    offer: false,
  })

  const [feedbackItems, setFeedbackItems] = useState<Record<string, boolean>>({
    rating: true,
    issueTags: true,
    comment: true,
    firstName: true,
    contact: true,
    consent: true,
  })

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
          groupName: data.businessName || "",
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

  const toggleSection = (section: string) => {
    setOpenSection((prev) => ({
      ...prev,
      [section]: !prev[section],
    }))
  }

  const toggleFeedbackItem = (key: string) => {
    setFeedbackItems((prev) => ({
      ...prev,
      [key]: !prev[key],
    }))
  }

  const errorStyle = {
    color: "#DC2626",
    fontSize: "12px",
    marginTop: "6px",
  }

  const strength = (() => {
    let score = 0

    if (password.length >= 8) score++
    if (/[A-Z]/.test(password)) score++
    if (/[0-9]/.test(password)) score++
    if (/[^A-Za-z0-9]/.test(password)) score++

    return score
  })()

  const getBarColor = (index: number) => {
    if (strength >= index) {
      if (strength === 1) return "#EF4444"
      if (strength === 2) return "#F59E0B"
      if (strength >= 3) return "#22C55E"
    }
    return "#E5E5E5"
  }

  const handleContinueStep1 = async () => {
    const valid = await form.trigger([...accountSetupMultiStep1Fields])
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupMultiStep1Fields)
      )
      return
    }

    setStep(2)
  }

  const handleContinueStep2 = async () => {
    const valid = await form.trigger([...accountSetupMultiStep2Fields])
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupMultiStep2Fields)
      )
      return
    }

    setStep(3)
  }

  const handleContinueStep3 = async () => {
    const step3Fields = getAccountSetupMultiStep3FieldNames(fields.length)
    const valid = await form.trigger(step3Fields)
    if (!valid) {
      setAttemptedFields((current) => addAttemptedFields(current, step3Fields))
      return
    }

    setStep(4)
  }

  const onCompleteSetup = async (values: AccountSetupMultiFormValues) => {
    form.clearErrors("root")
    setSubmitting(true)

    try {
      const response = await axios.post<SetupAccountResponse>(
        `${AUTH_API_BASE_URL}/setup-account`,
        toMultiLocationSetupPayload(values)
      )

      if (response.data.success) {
        navigate("/multi-dashboard")
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
      setSubmitting(false)
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
    <div
      style={{
        minHeight: "100vh",
        background: "#F8F8F8",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        padding: "40px 20px",
        fontFamily:
          "Inter, Arial, sans-serif",
      }}
    >
      <div
        style={{
          width: "100%",
          maxWidth: "480px",
        }}
      >
      <Form {...form}>
        <WizardLiveValidationProvider attemptedFields={attemptedFields}>
        {/* STEP 1 */}

        {step === 1 && (
          <div>
            {/* TITLE */}

            <h1
              style={{
                fontSize: "34px",
                fontWeight: "700",
                color: "#202020",
                marginBottom: "14px",
                textAlign: "center",
              }}
            >
              Create your account
            </h1>

            {/* SUBTITLE */}

            <p
              style={{
                fontSize: "13px",
                lineHeight: "24px",
                color: "#4B4B4B",
                textAlign: "center",
                marginBottom: "24px",
              }}
            >
              Your multi-location setup request has
              been approved.
              <br />
              Create a password to access your
              Tummly workspace.
            </p>

            {/* EMAIL */}

            <div style={{ marginBottom: "18px" }}>
              <FormFloatingInput
                control={form.control}
                name="email"
                label="Email"
                disabled
              />
            </div>

            <div style={{ marginBottom: "18px" }}>
              <FormFloatingInput
                control={form.control}
                name="fullName"
                label="Your full name"
              />
            </div>

            <div style={{ marginBottom: "18px" }}>
              <FormFloatingInput
                control={form.control}
                name="password"
                label="Password"
                type="password"
              />

              {/* PASSWORD STRENGTH DASHES */}

              <div
                style={{
                  display: "flex",
                  gap: "8px",
                  marginTop: "12px",
                }}
              >
                {[1, 2, 3, 4].map((item) => (
                  <div
                    key={item}
                    style={{
                      flex: 1,
                      height: "4px",
                      borderRadius: "999px",
                      background: getBarColor(item),
                    }}
                  />
                ))}
              </div>

              <p
                style={{
                  marginTop: "10px",
                  fontSize: "14px",
                  color: "#444",
                }}
              >
                Use at least 8 characters,
                including a number or symbol.
              </p>
            </div>

            <div style={{ marginBottom: "24px" }}>
              <FormFloatingInput
                control={form.control}
                name="confirmPassword"
                label="Confirm password"
                type="password"
              />
            </div>

            <FormCheckboxLabel
              control={form.control}
              name="agree"
              id="agree"
              className="mb-[34px]"
              labelClassName="items-start"
            >
              <span>
                I agree to the{" "}
                <span style={{ textDecoration: "underline", fontWeight: "600" }}>
                  Terms
                </span>{" "}
                and{" "}
                <span style={{ textDecoration: "underline", fontWeight: "600" }}>
                  Privacy Notice.
                </span>
              </span>
            </FormCheckboxLabel>

            {/* STEP BAR */}

            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent:
                  "space-between",
                marginBottom: "28px",
                color: "#6A6A6A",
                fontSize: "14px",
              }}
            >
              <div>1 Account</div>

              <div
                style={{
                  flex: 1,
                  height: "1px",
                  background: "#D9D9D9",
                  margin: "0 10px",
                }}
              />

              <div>2 Group</div>

              <div
                style={{
                  flex: 1,
                  height: "1px",
                  background: "#D9D9D9",
                  margin: "0 10px",
                }}
              />

              <div>3 Locations</div>

              <div
                style={{
                  flex: 1,
                  height: "1px",
                  background: "#D9D9D9",
                  margin: "0 10px",
                }}
              />

              <div>4 Rollout</div>
            </div>

            {/* BUTTON */}

            <Button
              type="button"
              variant="secondary"
              size="auth-sm"
              onClick={handleContinueStep1}
            >
              Create account
            </Button>

            {/* HELP */}

            <p
              style={{
                textAlign: "center",
                marginTop: "18px",
                fontSize: "15px",
                color: "#4B4B4B",
                fontWeight: "500",
              }}
            >
              Need help? Contact support or visit
              the Help Centre.
            </p>

            {/* FOOTER */}

            <div
              style={{
                marginTop: "50px",
                textAlign: "center",
                fontSize: "11px",
                color: "#6B6B6B",
              }}
            >
              <div
                style={{
                  display: "flex",
                  justifyContent: "center",
                  gap: "24px",
                  flexWrap: "wrap",
                  marginBottom: "14px",
                }}
              >
                <span>© 2026 Tummly</span>
                <span>Help Centre</span>
                <span>Terms</span>
                <span>Privacy</span>
                <span>Cookie settings</span>
              </div>

              <div>
                🔒 Secure restaurant access
              </div>
            </div>
          </div>
        )}
{/* STEP 2 */}

{step === 2 && (
  <div
    style={{
      width: "100%",
      maxWidth: "480px",
      margin: "0 auto",
    }}
  >
    {/* BACK BUTTON */}

    <div
      onClick={() => setStep(1)}
      style={{
        display: "flex",
        alignItems: "center",
        gap: "10px",
        cursor: "pointer",
        marginBottom: "20px",
        color: "#1F1F1F",
        fontSize: "18px",
        fontWeight: "500",
      }}
    >
      ← Back
    </div>

    {/* TITLE */}

    <h1
      style={{
        fontSize: "32px",
        fontWeight: "700",
        lineHeight: "40px",
        textAlign: "center",
        color: "#1E1E1E",
        marginBottom: "18px",
      }}
    >
      Confirm your group details
    </h1>

    {/* DESCRIPTION */}

    <p
      style={{
        textAlign: "center",
        color: "#4F4F4F",
        fontSize: "14px",
        lineHeight: "22px",
        maxWidth: "760px",
        margin: "0 auto 24px auto",
      }}
    >
      Check the business details for the group or brand you want
      to manage in Tummly. We'll use this to create your shared
      workspace and location structure.
    </p>

    {/* GROUP / BRAND NAME */}

    <div style={{ marginBottom: "22px" }}>
      <FormFloatingInput
        control={form.control}
        name="groupName"
        label="Group / brand name"
      />
    </div>

    <div style={{ marginBottom: "22px" }}>
      <FormFloatingSelect
        control={form.control}
        name="businessCategory"
        label="Business category"
        options={[
          { value: "Restaurant", label: "Restaurant" },
          { value: "Cafe", label: "Cafe" },
          { value: "Fast Food", label: "Fast Food" },
          { value: "Bakery", label: "Bakery" },
        ]}
      />
    </div>

    <div style={{ marginBottom: "22px" }}>
      <FormFloatingSelect
        control={form.control}
        name="numLocations"
        label="Number of locations"
        options={[
          { value: "1", label: "1 Location" },
          { value: "2", label: "2 Locations" },
          { value: "3", label: "3 Locations" },
          { value: "4+", label: "4+ Locations" },
        ]}
      />
    </div>

    <div style={{ marginBottom: "22px" }}>
      <FormFloatingInput
        control={form.control}
        name="primaryPhone"
        label="Primary contact phone (optional)"
        optional
      />
    </div>

    <div style={{ marginBottom: "40px" }}>
      <FormFloatingInput
        control={form.control}
        name="businessLink"
        label="Business link (optional)"
        optional
      />
    </div>

    {/* STEPS */}

    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: "28px",
      }}
    >
      {/* STEP 1 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "15px",
          }}
        >
          1 Account
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "12px",
          }}
        />
      </div>

      {/* STEP 2 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "14px",
        }}
      >
        <span
          style={{
            color: "#444",
            fontWeight: "600",
            fontSize: "15px",
          }}
        >
          2 Group
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#DADADA",
            marginLeft: "12px",
          }}
        />
      </div>

      {/* STEP 3 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "14px",
        }}
      >
        <span
          style={{
            color: "#8A8A8A",
            fontWeight: "500",
            fontSize: "15px",
          }}
        >
          3 Locations
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#E5E5E5",
            marginLeft: "12px",
          }}
        />
      </div>

      {/* STEP 4 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "14px",
        }}
      >
        <span
          style={{
            color: "#8A8A8A",
            fontWeight: "500",
            fontSize: "15px",
          }}
        >
          4 Rollout
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#E5E5E5",
            marginLeft: "12px",
          }}
        />
      </div>
    </div>

    {/* BUTTON */}

    <Button
      type="button"
      variant="muted"
      size="form-action"
      onClick={handleContinueStep2}
    >
      Confirm group
    </Button>

    {/* HELP TEXT */}

    <p
      style={{
        textAlign: "center",
        fontSize: "13px",
        color: "#353535",
        marginBottom: "50px",
        fontWeight: "500",
      }}
    >
      Need help? Contact support or visit the Help Centre.
    </p>

    {/* FOOTER */}

    <div
      style={{
        textAlign: "center",
        color: "#777",
        fontSize: "14px",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          gap: "26px",
          flexWrap: "wrap",
          marginBottom: "12px",
        }}
      >
        <span>© 2026 Tummly</span>
        <span>Help Centre</span>
        <span>Terms</span>
        <span>Privacy</span>
        <span>Cookie settings</span>
      </div>

      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          gap: "8px",
        }}
      >
        🔒
        <span>Secure restaurant access</span>
      </div>
    </div>
  </div>
)}

{/* STEP 3 */}

{step === 3 && (
  <div
    style={{
      width: "100%",
      maxWidth: "460px",
      margin: "0 auto",
    }}
  >
    {/* BACK */}

    <div
      onClick={() => setStep(2)}
      style={{
        display: "flex",
        alignItems: "center",
        gap: "10px",
        cursor: "pointer",
        marginBottom: "20px",
        color: "#1F1F1F",
        fontSize: "18px",
        fontWeight: "500",
      }}
    >
      ← Back
    </div>

    {/* TITLE */}

    <h1
      style={{
        fontSize: "28px",
        fontWeight: "700",
        lineHeight: "34px",
        textAlign: "center",
        color: "#1E1E1E",
        marginBottom: "16px",
      }}
    >
      Add your locations
    </h1>

    {/* DESCRIPTION */}

    <p
      style={{
        textAlign: "center",
        color: "#4F4F4F",
        fontSize: "14px",
        lineHeight: "22px",
        marginBottom: "42px",
      }}
    >
      Add the locations you want to include in your first
      Tummly rollout.
      <br />
      You can add more locations later from your workspace.
    </p>


   {/* LOCATIONS LIST */}
{fields.map((field, index) => {
  const includeInRollout = form.watch(`locations.${index}.includeInRollout`)

  return (
  <div key={field.id} style={{ marginBottom: "20px" }}>

         {/* LOCATION HEADER */}
    <div
      style={{
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        marginBottom: "12px",
      }}
    >
      <span
        style={{
          fontSize: "18px",
          fontWeight: "700",
        }}
      >
       
      </span>

      {fields.length > 1 && (
        <Button
          type="button"
          variant="link-destructive"
          onClick={() => remove(index)}
        >
          Delete
        </Button>
      )}
    </div>

    {/* LOCATION NAME */}
    <FormFloatingInput
      control={form.control}
      name={`locations.${index}.locationName`}
      label="Location name"
      className="mb-2.5"
    />
    <FormFloatingInput
      control={form.control}
      name={`locations.${index}.address`}
      label="Address"
      className="mb-2.5"
    />
    <FormFloatingInput
      control={form.control}
      name={`locations.${index}.postcode`}
      label="Postcode"
      className="mb-2.5"
    />
    <FormFloatingInput
      control={form.control}
      name={`locations.${index}.locationPhone`}
      label="Location phone (optional)"
      optional
      className="mb-2.5"
    />
    <FormFloatingInput
      control={form.control}
      name={`locations.${index}.localContact`}
      label="Local contact (optional)"
      optional
    />
    <FormField
      control={form.control}
      name={`locations.${index}.includeInRollout`}
      render={({ field: rolloutField }) => (
    <div
  onClick={() => rolloutField.onChange(!rolloutField.value)}
  style={{
    display: "flex",
    alignItems: "center",
    gap: "12px",
    marginTop: "12px",
    cursor: "pointer",
  }}
>
  <span>Include in first rollout?</span>

  <div
    style={{
      width: "42px",
      height: "24px",
      borderRadius: "20px",
      background: includeInRollout
        ? "#22C55E"
        : "#D8D8D8",
      position: "relative",
      cursor: "pointer",
    }}
  >
    <div
      style={{
        width: "18px",
        height: "18px",
        borderRadius: "50%",
        background: "#fff",
        position: "absolute",
        top: "3px",
        left: includeInRollout
          ? "21px"
          : "3px",
      }}
    />
  </div>
</div>
      )}
    />
  </div>
  )
})}

    {/* DIVIDER */}

    <div
      style={{
        width: "100%",
        height: "1px",
        background: "#E6E6E6",
        marginBottom: "24px",
      }}
    />

    {/* ADD LOCATION */}

   <div
  onClick={() => append(emptyLocationItem)}
  style={{
    display: "flex",
    alignItems: "center",
    gap: "10px",
    marginBottom: "18px",
    cursor: "pointer",
    fontWeight: "600",
    color: "#1F1F1F",
    fontSize: "16px",
  }}
>
  ⊕ Add location
</div>

    {/* UPLOAD */}

    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: "10px",
        marginBottom: "38px",
        cursor: "pointer",
        fontWeight: "600",
        color: "#1F1F1F",
        fontSize: "16px",
      }}
    >
      ↥ Upload locations instead
    </div>

    {/* STEP BAR */}

    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: "28px",
      }}
    >
      {/* STEP 1 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "14px",
          }}
        >
          1 Account
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 2 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "12px",
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "14px",
          }}
        >
          2 Group
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 3 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "12px",
        }}
      >
        <span
          style={{
            color: "#1F1F1F",
            fontWeight: "700",
            fontSize: "14px",
          }}
        >
          3 Locations
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#D9D9D9",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 4 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "12px",
        }}
      >
        <span
          style={{
            color: "#9B9B9B",
            fontWeight: "500",
            fontSize: "14px",
          }}
        >
          4 Rollout
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#E5E5E5",
            marginLeft: "10px",
          }}
        />
      </div>
    </div>

    {/* BUTTON */}

    <Button
      type="button"
      variant="muted"
      size="form-action-lg"
      onClick={handleContinueStep3}
    >
      Continue to rollout
    </Button>

    {/* HELP */}

    <p
      style={{
        textAlign: "center",
        fontSize: "13px",
        color: "#353535",
        marginBottom: "50px",
        fontWeight: "500",
      }}
    >
      Need help? Contact support or visit the Help Centre.
    </p>

    {/* FOOTER */}

    <div
      style={{
        textAlign: "center",
        color: "#777",
        fontSize: "14px",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          gap: "24px",
          flexWrap: "wrap",
          marginBottom: "12px",
        }}
      >
        <span>© 2026 Tummly</span>
        <span>Help Centre</span>
        <span>Terms</span>
        <span>Privacy</span>
        <span>Cookie settings</span>
      </div>

      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          gap: "8px",
        }}
      >
        🔒
        <span>Secure restaurant access</span>
      </div>
    </div>
  </div>
)}


{/* STEP 4 */}

{step === 4 && (
  <div
    style={{
      width: "100%",
      maxWidth: "620px",
      margin: "0 auto",
      position: "relative",
    }}
  >
    {/* BACK BUTTON */}

    <div
      onClick={() => setStep(3)}
      style={{
        display: "flex",
        alignItems: "center",
        gap: "10px",
        cursor: "pointer",
        marginBottom: "22px",
        color: "#1F1F1F",
        fontSize: "15px",
        fontWeight: "500",
      }}
    >
      <div
        style={{
          width: "26px",
          height: "26px",
          borderRadius: "50%",
          background: "#F2F2F2",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: "12px",
        }}
      >
        ←
      </div>

      Back
    </div>

    {/* TITLE */}

    <h1
      style={{
        fontSize: "46px",
        fontWeight: "700",
        textAlign: "center",
        color: "#1E1E1E",
        marginBottom: "16px",
        lineHeight: "54px",
      }}
    >
      Prepare your first rollout
    </h1>

    {/* SUBTITLE */}

    <p
      style={{
        textAlign: "center",
        color: "#5E5E5E",
        fontSize: "16px",
        lineHeight: "28px",
        maxWidth: "760px",
        margin: "0 auto 42px auto",
      }}
    >
      Choose the guest touchpoints and starter settings
      you want to use across your first locations. You
      can customise each location later.
    </p>

  {/* ========================= */}
{/* ROLLOUT APPROACH */}
{/* ========================= */}
<div
  style={{
    borderBottom: "1px solid #E5E5E5",
    paddingBottom: "18px",
    marginBottom: "18px",
  }}
>
  <div
    onClick={() => toggleSection("rollout")}
    style={{
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      cursor: "pointer",
    }}
  >
    <span
      style={{
        fontSize: "24px",
        fontWeight: "600",
      }}
    >
      Rollout approach
    </span>

    <span>
      {openSection.rollout ? "▲" : "▼"}
    </span>
  </div>

  {openSection.rollout && (
    <div style={{ marginTop: "16px" }}>
      <FormFloatingSelect
        control={form.control}
        name="rolloutApproach"
        label="How do you want to start?"
        options={[
          { value: "qr", label: "QR code feedback" },
          { value: "tablet", label: "Tablet feedback station" },
          { value: "sms", label: "SMS review flow" },
          { value: "email", label: "Email follow-up" },
        ]}
      />
    </div>
  )}
</div>


{/* ========================= */}
{/* GUEST PROMPTS */}
{/* ========================= */}

<div
  style={{
    borderBottom: "1px solid #E5E5E5",
    paddingBottom: "20px",
    marginBottom: "20px",
  }}
>
  <div
    onClick={() => toggleSection("prompts")}
    style={{
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      cursor: "pointer",
    }}
  >
    <span
      style={{
        fontSize: "24px",
        fontWeight: "600",
        color: "#1F1F1F",
      }}
    >
      Where will guests see your prompts?
    </span>

    <span
      style={{
        fontSize: "18px",
        color: "#555",
      }}
    >
      {openSection.prompts ? "▲" : "▼"}
    </span>
  </div>

  {openSection.prompts && (
    <div style={{ marginTop: "16px" }}>
      <FormFloatingSelect
        control={form.control}
        name="guestPrompt"
        label="Choose option"
        options={[
          { value: "table", label: "On Tables" },
          { value: "receipt", label: "On Receipt" },
          { value: "counter", label: "At Counter" },
        ]}
      />
    </div>
  )}
</div>
<div
  style={{
    borderBottom: "1px solid #E5E5E5",
    paddingBottom: "26px",
    marginBottom: "22px",
  }}
>
  {/* HEADER */}

  <div
    onClick={() => toggleSection("feedback")}
    style={{
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: "18px",
      cursor: "pointer",
    }}
  >
    <span
      style={{
        fontSize: "24px",
        fontWeight: "600",
        color: "#1F1F1F",
      }}
    >
      Feedback form
    </span>

    <span
      style={{
        fontSize: "18px",
        color: "#555",
      }}
    >
      {openSection.feedback ? "▲" : "▼"}
    </span>
  </div>

  {/* CONTENT */}

  {openSection.feedback && (
    <>
      <p
        style={{
          color: "#666",
          fontSize: "14px",
          lineHeight: "24px",
          marginBottom: "14px",
        }}
      >
        Guests can rate their experience, choose issue
        tags, leave an optional comment and choose whether
        to join your guest list.
      </p>

      <div
        style={{
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          background: "#EAFBEF",
          color: "#22C55E",
          padding: "8px 14px",
          borderRadius: "999px",
          fontSize: "13px",
          fontWeight: "600",
          marginBottom: "20px",
        }}
      >
        Use recommended starter form
      </div>

      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: "10px",
          marginBottom: "18px",
        }}
      >
        <span
          style={{
            fontSize: "14px",
            color: "#555",
            fontWeight: "500",
          }}
        >
          Review starter form settings
        </span>

        <span
          style={{
            fontSize: "12px",
            color: "#555",
          }}
        >
          ▲
        </span>
      </div>

   {[
  { label: "Rating", key: "rating" },
  { label: "Issue tags", key: "issueTags" },
  { label: "Optional comment", key: "comment" },
  { label: "First name", key: "firstName" },
  { label: "Email or phone", key: "contact" },
  { label: "Consent checkboxes", key: "consent" },
].map((item, index) => (
  <div
    key={index}
    style={{
      display: "flex",
      alignItems: "center",
      gap: "12px",
      marginBottom: "14px",
      cursor: "pointer",
    }}
    onClick={() => toggleFeedbackItem(item.key)}
  >
    <div
      style={{
        width: "18px",
        height: "18px",
        border: "1px solid #22C55E",
        borderRadius: "2px",
        background: feedbackItems[item.key]
          ? "#22C55E"
          : "#fff",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        color: "#fff",
        fontSize: "11px",
        fontWeight: "700",
      }}
    >
      {feedbackItems[item.key] ? "✓" : ""}
    </div>

    <span
      style={{
        fontSize: "14px",
        color: "#333",
      }}
    >
      {item.label}
    </span>
  </div>
))}
    </>
  )}
</div>

    {/* ========================= */}
    {/* THANK YOU MESSAGE */}
    {/* ========================= */}

    <div
      style={{
        borderBottom: "1px solid #E5E5E5",
        paddingBottom: "24px",
        marginBottom: "22px",
      }}
    >
      {/* HEADER */}

    <div
  onClick={() => toggleSection("thankyou")}
  style={{
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: "14px",
    cursor: "pointer",
  }}
>
  <span
    style={{
      fontSize: "24px",
      fontWeight: "600",
      color: "#1F1F1F",
    }}
  >
    Thank-you message
  </span>

  <span
    style={{
      fontSize: "18px",
      color: "#555",
    }}
  >
    {openSection.thankyou ? "▲" : "▼"}
  </span>
</div>

{openSection.thankyou && (
  <>
    <p
      style={{
        color: "#666",
        fontSize: "14px",
        marginBottom: "14px",
      }}
    >
      You can customise this by location later.
    </p>

    <FormField
      control={form.control}
      name="thankYouMessage"
      render={({ field, fieldState }) => (
        <>
          <textarea
            {...field}
            placeholder="Thank you message"
            style={{
              width: "100%",
              minHeight: "120px",
              border: "1px solid #D8D8D8",
              borderRadius: "4px",
              padding: "16px",
              fontSize: "14px",
              lineHeight: "24px",
              resize: "none",
              outline: "none",
              background: "#fff",
              color: "#333",
              boxSizing: "border-box",
            }}
            aria-invalid={fieldState.error ? true : undefined}
          />
          {fieldState.error?.message ? (
            <p style={errorStyle} role="alert">
              {fieldState.error.message}
            </p>
          ) : null}
        </>
      )}
    />
  </>
)}
    </div>

    {/* ========================= */}
    {/* STARTER OFFER */}
    {/* ========================= */}

    <div
      style={{
        borderBottom: "1px solid #E5E5E5",
        paddingBottom: "28px",
        marginBottom: "30px",
      }}
    >
      {/* HEADER */}

  <div
  onClick={() => toggleSection("offer")}
  style={{
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: "14px",
    cursor: "pointer",
  }}
>
  <span
    style={{
      fontSize: "24px",
      fontWeight: "600",
      color: "#1F1F1F",
    }}
  >
    Starter offer — optional
  </span>

  <span
    style={{
      fontSize: "18px",
      color: "#555",
    }}
  >
    {openSection.offer ? "▲" : "▼"}
  </span>
</div>

{openSection.offer && (
  <>
    <p
      style={{
        color: "#666",
        fontSize: "14px",
        lineHeight: "22px",
        marginBottom: "18px",
      }}
    >
      Add a simple offer for guests who opt in, or
      skip this step and create one later from your
      workspace.
    </p>

    <FormFloatingSelect
      control={form.control}
      name="offerType"
      label="Offer type"
      className="mb-3.5"
      options={[
        { value: "discount", label: "Discount code" },
        { value: "freeItem", label: "Free item" },
        { value: "voucher", label: "Voucher" },
      ]}
    />

    <FormFloatingInput
      control={form.control}
      name="offerTitle"
      label="Thank you offer"
      className="mb-3.5"
    />

    <FormFloatingInput
      control={form.control}
      name="offerMessage"
      label="Example: Free side with your next order"
      className="mb-3.5"
    />

    <FormFloatingSelect
      control={form.control}
      name="expiry"
      label="Expiry"
      className="mb-3.5"
      options={[
        { value: "7days", label: "7 Days" },
        { value: "14days", label: "14 Days" },
        { value: "30days", label: "30 Days" },
      ]}
    />

    <FormFloatingSelect
      control={form.control}
      name="redemptionMethod"
      label="Redemption method"
      className="mb-3.5"
      options={[
        { value: "showStaff", label: "Show to staff" },
        { value: "coupon", label: "Coupon code" },
        { value: "scanQr", label: "Scan QR code" },
      ]}
    />

    <FormFloatingSelect
      control={form.control}
      name="usageLimit"
      label="Usage limit"
      className="mb-3.5"
      options={[
        { value: "one", label: "One time use" },
        { value: "multi", label: "Multiple use" },
      ]}
    />

    <FormField
      control={form.control}
      name="guestPreview"
      render={({ field }) => (
        <textarea
          {...field}
          placeholder="Guest thank-you preview"
          style={{
            width: "100%",
            minHeight: "100px",
            border: "1px solid #D8D8D8",
            borderRadius: "4px",
            padding: "14px",
            fontSize: "14px",
            lineHeight: "22px",
            resize: "none",
            outline: "none",
            background: "#F8F8F8",
            color: "#555",
            boxSizing: "border-box",
          }}
        />
      )}
    />
  </>
)}
    </div>

    {/* ========================= */}
    {/* STEP BAR */}
    {/* ========================= */}

    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: "34px",
      }}
    >
      {/* STEP 1 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "13px",
          }}
        >
          1 Account
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 2 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "10px",
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "13px",
          }}
        >
          2 Group
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 3 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "10px",
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "13px",
          }}
        >
          3 Locations
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 4 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "10px",
        }}
      >
        <span
          style={{
            color: "#1F1F1F",
            fontWeight: "700",
            fontSize: "13px",
          }}
        >
          4 Rollout
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#1F1F1F",
            marginLeft: "10px",
          }}
        />
      </div>
    </div>

    <FieldErrorSlot error={rootError} reserveClassName="min-h-0 mb-4" />

    {/* BUTTONS */}

    <div
      style={{
        display: "flex",
        gap: "16px",
        marginBottom: "34px",
      }}
    >
      {/* SKIP */}

      <Button
        type="button"
        variant="secondary"
        size="toolbar"
        onClick={() => navigate("/multi-dashboard")}
      >
        Skip for now
      </Button>

      {/* COMPLETE */}

<Button
  type="button"
  variant="muted"
  size="toolbar-flex"
  disabled={submitting}
  onClick={form.handleSubmit(onCompleteSetup)}
>
  {submitting ? "Creating..." : "Complete setup and open workspace"}
</Button>
    </div>

    {/* HELP */}

    <p
      style={{
        textAlign: "center",
        fontSize: "14px",
        color: "#444",
        marginBottom: "100px",
        fontWeight: "500",
      }}
    >
      Need help? Contact support or visit the Help
      Centre.
    </p>

    {/* FOOTER */}

    <div
      style={{
        textAlign: "center",
        color: "#777",
        fontSize: "12px",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          gap: "20px",
          flexWrap: "wrap",
          marginBottom: "10px",
        }}
      >
        <span>© 2026 Tummly</span>
        <span>Help Centre</span>
        <span>Terms</span>
        <span>Privacy</span>
        <span>Cookie settings</span>
      </div>

      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          gap: "8px",
        }}
      >
        🔒
        <span>Secure restaurant access</span>
      </div>
    </div>
  </div>
  
)}
        </WizardLiveValidationProvider>
      </Form>
  </div>
  </div>
  );
};

export default MultiRegisterPage;