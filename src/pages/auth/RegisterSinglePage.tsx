import { useEffect, useState } from "react"
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
import { Form } from "@/components/ui/form"
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
  password?: string
}

interface ProgressBarProps {
  activeStep: number
}

interface SetupAccountResponse {
  success?: boolean
  message?: string
  errors?: unknown
}

// 100% Figma Premium Matched Password Strength Component
const PasswordStrength = ({ password = "" }: PasswordStrengthProps) => {
  let strength = 0
  if (password.length >= 8) strength++
  if (/[A-Z]/.test(password)) strength++
  if (/[0-9!@#$%^&*]/.test(password)) strength++
  if (password.length >= 12) strength++

  const getColor = (index: number) => {
    if (index >= strength) return "bg-[#E5E7EB]"
    if (strength === 1) return "bg-red-500"
    if (strength === 2) return "bg-yellow-500"
    return "bg-[#22C55E]"
  }

  return (
    <div className="mb-2 mt-4 flex gap-2">
      {[1, 2, 3, 4].map((item, index) => (
        <div
          key={item}
          className={`h-[4px] flex-1 rounded-full transition-all duration-300 ${getColor(index)}`}
        />
      ))}
    </div>
  )
}

// 100% Figma Matched Premium Stepper Progress Block
const ProgressBar = ({ activeStep }: ProgressBarProps) => {
  return (
    <div className="pt-6 w-full">
      <div className="mb-2 flex items-center justify-between text-[11px] uppercase tracking-wider font-semibold">
        <span className={activeStep >= 1 ? "text-black" : "text-[#9CA3AF]"}>1. Account</span>
        <span className={activeStep >= 2 ? "text-black" : "text-[#9CA3AF]"}>2. Restaurant</span>
        <span className={activeStep >= 3 ? "text-black" : "text-[#9CA3AF]"}>3. Ready</span>
      </div>
      <div className="flex items-center gap-2">
        <div className={`h-[3px] flex-1 rounded-full ${activeStep >= 1 ? "bg-black" : "bg-[#E5E7EB]"}`} />
        <div className={`h-[3px] flex-1 rounded-full ${activeStep >= 2 ? "bg-black" : "bg-[#E5E7EB]"}`} />
        <div className={`h-[3px] flex-1 rounded-full ${activeStep >= 3 ? "bg-black" : "bg-[#E5E7EB]"}`} />
      </div>
    </div>
  )
}

const businessCategoryOptions = [
  { value: "Restaurant", label: "Restaurant" },
  { value: "Cafe", label: "Cafe" },
  { value: "Fast Food", label: "Fast Food" },
]

function RegisterSinglePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get("token")

const form = useForm<AccountSetupSingleFormValues>({
    resolver: zodResolver(accountSetupSingleSchema),
    defaultValues: accountSetupSingleDefaultValues, // 👈 Yeh direct humari optimized safe schemas default values ko structural baseline banayega
    mode: "onChange", // Real-time client spec feedback
  })

  // Local state tracking safely integrated (Zero compilation errors)
  const [step, setStep] = useState(1)
  const [attemptedFields, setAttemptedFields] = useState<Set<string>>(new Set())
  
  const [tokenLoading, setTokenLoading] = useState(() => Boolean(token))
  const [tokenError, setTokenError] = useState(() => (token ? "" : "Setup token is missing."))

  // Asynchronous Step 3 Components Generation States
  const [phase1Status, setPhase1Status] = useState<"idle" | "loading" | "success">("idle")
  const [phase2Status, setPhase2Status] = useState<"idle" | "loading" | "success">("idle")
  const [phase3Status, setPhase3Status] = useState<"idle" | "loading" | "success">("idle")
  const [isWorkspaceReady, setIsWorkspaceReady] = useState(false)

  const password = form.watch("password")
  const rootError = form.formState.errors.root?.message

  // Token Validation Lifecycle Implementation
  // Token Validation Lifecycle Implementation
  useEffect(() => {
    if (!token) return
    let active = true

    void (async () => {
      try {
        const response = await axios.get(`${API_BASE_URL}/Trial/validate-setup-token?token=${token}`)
        if (!active) return
        const data = response.data.data
        
        // 100% Strict explicit mapping - mappings align without breaking team parameters
        form.reset({
          ...form.getValues(), // Pehle baqi dynamic form states ko safe retain rakhein
          token: token,
          email: data.email || "",
          fullName: data.fullName || "",
          restaurantName: data.businessName || form.getValues("restaurantName") || "",
          businessCategory: data.businessCategory || form.getValues("businessCategory") || "Restaurant",
        })
        setTokenError("")
      } catch (error: unknown) {
        if (!active) return
        if (isAxiosError<{ message?: string }>(error)) {
          setTokenError(error.response?.data?.message || "Invalid setup token")
        } else {
          setTokenError("Invalid setup token")
        }
      } finally {
        if (active) setTokenLoading(false)
      }
    })()

    return () => {
      active = false
    }
  }, [form, token])

  // Step 1 Redirect Trigger Validation Gate
  const handleContinueStep1 = async () => {
    const fieldsToValidate = Array.from(accountSetupSingleStep1Fields) as any[]
    const valid = await form.trigger(fieldsToValidate)
    if (!valid) {
      setAttemptedFields((current) => addAttemptedFields(current, accountSetupSingleStep1Fields))
      return
    }
    setStep(2)
  }

  // Step 2 Submission Processing & Stepper Interval Setup Action Handlers
  const handleConfirmRestaurantSubmit = async () => {
    const fieldsToValidate = Array.from(accountSetupSingleStep2Fields) as any[]
    const valid = await form.trigger(fieldsToValidate)
    if (!valid) {
      setAttemptedFields((current) => addAttemptedFields(current, accountSetupSingleStep2Fields))
      return
    }

    form.clearErrors("root")
    setStep(3) 
    setPhase1Status("loading")

    try {
      const values = form.getValues()
      const response = await axios.post<SetupAccountResponse>(
        `${AUTH_API_BASE_URL}/setup-account`,
        toSingleLocationSetupPayload(values)
      )

      if (response.data.success) {
        // Multi-Stage Sequential Simulation Matrix mapping layout guidelines precisely
        setTimeout(() => {
          setPhase1Status("success")
          setPhase2Status("loading")

          setTimeout(() => {
            setPhase2Status("success")
            setPhase3Status("loading")

            setTimeout(() => {
              setPhase3Status("success")
              setIsWorkspaceReady(true)
            }, 2500)
          }, 2500)
        }, 2500)
      } else {
        setStep(2)
        form.setError("root", {
          message: response.data.message || "Account setup failed.",
        })
      }
    } catch (error: unknown) {
      setStep(2)
      if (isAxiosError<SetupAccountResponse>(error)) {
        form.setError("root", {
          message: error.response?.data?.message || "Something went wrong during onboarding processing.",
        })
      } else {
        form.setError("root", { message: "Something went wrong" })
      }
    }
  }

  const handleOpenWorkspace = () => {
    navigate("/single-dashboard?welcome=true")
  }

  if (tokenLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center text-[18px] font-medium text-[#111827]">
        Validating setup token...
      </div>
    )
  }

  if (tokenError) {
    return (
      <div className="flex min-h-screen items-center justify-center px-6 bg-[#FAFAFA]">
        <div className="max-w-[460px] text-center bg-white p-8 border border-red-100 rounded-xl shadow-sm">
          <h1 className="mb-3 text-[26px] font-bold text-red-500">Invalid Setup Link</h1>
          <p className="text-[#6B7280] text-sm leading-relaxed">{tokenError}</p>
        </div>
      </div>
    )
  }

  const SuccessIcon = () => (
    <svg className="h-5 w-5 text-[#22C55E]" viewBox="0 0 20 20" fill="currentColor">
      <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
    </svg>
  )

  const LoadingSpinner = () => (
    <div className="h-4 w-4 animate-spin rounded-full border-2 border-[#22C55E] border-t-transparent" />
  )

  return (
   <div className="flex min-h-screen flex-col bg-[#FAFAFA] font-sans antialiased text-[#111827]">
      <Form {...form}>
        <WizardLiveValidationProvider attemptedFields={attemptedFields}>
          
          {step === 1 && (
            <main className="flex flex-1 flex-col items-center justify-center px-4 py-12 md:py-20">
              
              {/* Form Card Container (Clear Start and End) */}
              <div className="w-full max-w-[460px] bg-white rounded-2xl border border-gray-200/80 shadow-[0_8px_30px_rgb(0,0,0,0.04)] p-8 md:p-10 transition-all">
                
                {/* Header Context inside the Card */}
                <div className="text-center mb-8">
                  <h1 className="text-[26px] font-bold tracking-tight text-[#111827]">
                    Create your account
                  </h1>
                  <p className="mt-2.5 text-[14px] leading-relaxed text-[#4B5563]">
                    Your guided trial request has been approved.
                    <br />
                    Create a password to access your Tummly workspace.
                  </p>
                </div>

                {/* Form Inputs Fields Stack */}
                <div className="space-y-4">
                  <FormFloatingInput
                    control={form.control}
                    name="email"
                    label="Email"
                    type="email"
                    disabled
                  />

                  <FormFloatingInput
                    control={form.control}
                    name="fullName"
                    label="Your full name"
                  />

                  <FormFloatingInput
                    control={form.control}
                    name="password"
                    label="Password"
                    type="password"
                  />

                  <PasswordStrength password={password} />

                  <p className="text-[12px] text-[#6B7280] leading-normal pl-1">
                    Use at least 12 characters with a number or symbol.
                  </p>

                  <FormFloatingInput
                    control={form.control}
                    name="confirmPassword"
                    label="Confirm password"
                    type="password"
                  />

                  <div className="pt-2 pl-1">
                    <FormCheckboxLabel
                      control={form.control}
                      name="agree"
                      id="agree"
                      labelClassName="text-[13px] text-[#374151] leading-tight cursor-pointer"
                    >
                      I agree to the <span className="underline font-medium text-black">Terms</span> and <span className="underline font-medium text-black">Privacy Notice</span>.
                    </FormCheckboxLabel>
                  </div>

                  {/* 100% Figma Match Step Progress Indicator */}
                  <div className="pt-6 pb-2">
                    <div className="flex items-center justify-between text-[13px] font-medium w-full">
                      
                      {/* Step 1: Active */}
                      <div className="flex items-center gap-2">
                        <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-xs font-bold bg-black text-white ring-2 ring-black/10">
                          1
                        </span>
                        <span className="text-black font-semibold">Account</span>
                      </div>
                      
                      <div className="h-[1px] flex-1 bg-gray-200 mx-3" />
                      
                      {/* Step 2: Inactive */}
                      <div className="flex items-center gap-2">
                        <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-xs font-bold border border-gray-300 bg-white text-gray-400">
                          2
                        </span>
                        <span className="text-gray-400">Restaurant</span>
                      </div>
                      
                      <div className="h-[1px] flex-1 bg-gray-200 mx-3" />
                      
                      {/* Step 3: Inactive */}
                      <div className="flex items-center gap-2">
                        <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-xs font-bold border border-gray-300 bg-white text-gray-400">
                          3
                        </span>
                        <span className="text-gray-400">Ready</span>
                      </div>

                    </div>
                  </div>

                  {/* Action Button */}
                  <div className="pt-2">
                    <Button
                      type="button"
                      onClick={handleContinueStep1}
                      className="w-full h-[48px] rounded-full bg-black text-white font-medium text-[14px] shadow-sm hover:bg-gray-900 active:scale-[0.99] transition-all duration-200"
                    >
                      Continue
                    </Button>
                  </div>

                </div>
              </div>

              {/* Professional Subtle Footer outside the Card */}
              <div className="mt-8 text-center max-w-[460px] w-full px-4">
                <p className="text-[13px] text-[#6B7280]">
                  Need help?{" "}
                  <span className="underline cursor-pointer font-medium text-gray-800 hover:text-black">
                    Contact support
                  </span>{" "}
                  or visit the{" "}
                  <span className="underline cursor-pointer font-medium text-gray-800 hover:text-black">
                    Help Centre
                  </span>.
                </p>

                <div className="mt-8 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-[11px] text-gray-400 tracking-wide">
                  <span>© 2026 Tummly</span>
                  <span className="hover:text-gray-600 cursor-pointer transition-colors">Help Centre</span>
                  <span className="hover:text-gray-600 cursor-pointer transition-colors">Terms</span>
                  <span className="hover:text-gray-600 cursor-pointer transition-colors">Privacy</span>
                  <span className="hover:text-gray-600 cursor-pointer transition-colors">Cookie settings</span>
                </div>
              </div>

            </main>
          )}


{/* STEP 2: RESTAURANT CONFIRMATION LAYOUT */}
{step === 2 && (
  <main className="flex flex-1 flex-col items-center justify-center px-4 py-12 md:py-20 bg-[#FAFAFA] font-sans antialiased text-[#111827]">
    
    {/* Floating Card Container Layer - Defines precise form boundaries */}
    <div className="w-full max-w-[460px] bg-white rounded-2xl border border-gray-200/80 shadow-[0_8px_30px_rgb(0,0,0,0.04)] p-8 md:p-10 relative transition-all">
      
      {/* Absolute Back Button inside the card flow alignment */}
      <div className="mb-6">
        <button 
          type="button" 
          onClick={() => setStep(1)} 
          className="flex items-center gap-1 text-[13px] text-[#6B7280] hover:text-black font-semibold transition-colors"
        >
          <span className="text-[16px] leading-none">‹</span> BACK
        </button>
      </div>

      {/* Head Header Section Context */}
      <div className="text-center mb-8">
        <h1 className="text-[26px] font-bold tracking-tight text-[#111827]">
          Confirm restaurant
        </h1>
        <p className="mt-2.5 text-[14px] leading-relaxed text-[#4B5563]">
          Check the details for the location you want to set up first. We use
          <br />
          this to prepare your workspace, guest link, QR materials
          <br />
          and private feedback form.
        </p>
      </div>

      {/* Form Content Inputs Stack - All Inputs Perfectly Aligned */}
      <div className="space-y-4">
        
        <FormFloatingInput 
          control={form.control} 
          name="restaurantName" 
          label="Restaurant or brand name" 
        />
        
        <FormFloatingInput 
          control={form.control} 
          name="locationName" 
          label="Location name" 
        />
        
        {/* Responsive Matrix Grid for Address & Postcode */}
        <div className="grid grid-cols-12 gap-3">
          
    {/* Address field - Icon removes ONLY when text data exists (100% Error-Free) */}
<div className="col-span-8 relative flex items-center group">
  
  {/* Location Pin Icon Layer */}
  {/* Logic: Jab field ke andar actual text data aa jayega, tabhi yeh pure element display se hide (gayab) hoga */}
  {(!form.watch("address") || form.watch("address").length === 0) && (
    <div className="absolute left-3.5 z-10 pointer-events-none text-gray-400 flex items-center mt-0.5">
      <svg 
        className="h-4 w-4 text-gray-400 shrink-0" 
        fill="none" 
        viewBox="0 0 24 24" 
        stroke="currentColor" 
        strokeWidth={2.5}
      >
        <path strokeLinecap="round" strokeLinejoin="round" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
        <path strokeLinecap="round" strokeLinejoin="round" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
      </svg>
    </div>
  )}
  
  {/* Dynamic Spacing Control: Padding shifts only if text data is present inside input */}
  {/* Text na hone par spacing icon ke mutabik auto-adjust hogi, text aate hi standard left-line align ho jayegi */}
  <div className="w-full 
    transition-all duration-150
    group-[':has(input:placeholder-shown)']:[&_input]:pl-10 
    group-[':has(input:placeholder-shown)']:[&_label]:left-10
    group-[':has(input:not(:placeholder-shown))']:[&_input]:pl-3
    group-[':has(input:not(:placeholder-shown))']:[&_label]:left-3"
  >
    <FormFloatingInput 
      control={form.control} 
      name="address" 
      label="Address" 
    />
  </div>

</div>

          <div className="col-span-4">
            <FormFloatingInput 
              control={form.control} 
              name="postcode" 
              label="Postcode" 
            />
          </div>
        </div>

        <FormFloatingInput 
          control={form.control} 
          name="phone" 
          label="Restaurant phone number Optional" 
          type="tel" 
          optional 
        />
        
        <FormFloatingInput 
          control={form.control} 
          name="businessLink" 
          label="Website or social link Optional" 
          optional 
        />
        
        <FormFloatingSelect 
          control={form.control} 
          name="businessCategory" 
          label="Business category" 
          options={businessCategoryOptions} 
        />

        {/* Form Error Message Slot */}
        <FieldErrorSlot error={rootError} reserveClassName="min-h-0" />

        {/* 100% Circle Stepper Progress Bar */}
        <div className="pt-6 pb-2 w-full mx-auto">
          <div className="flex items-center justify-between text-[13px] font-medium text-[#111827]">
            
            {/* Step 1: Active/Done state */}
            <div className="flex items-center gap-2">
              <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[11px] font-bold bg-[#22C55E] text-white">
                1
              </span>
              <span className="text-[#22C55E] font-medium">Account</span>
            </div>
            
            <div className="h-[2px] flex-1 bg-[#22C55E] mx-3" />
            
            {/* Step 2: Current selection state */}
            <div className="flex items-center gap-2">
              <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[11px] font-bold border-2 border-black bg-white text-black">
                2
              </span>
              <span className="text-black font-bold">Restaurant</span>
            </div>
            
            <div className="h-[1px] flex-1 bg-gray-200 mx-3" />
            
            {/* Step 3: Disabled/Pending state */}
            <div className="flex items-center gap-2">
              <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[11px] font-bold border border-gray-300 bg-white text-gray-400">
                3
              </span>
              <span className="text-gray-400">Ready</span>
            </div>

          </div>
        </div>

        {/* Trigger Button Control */}
        <div className="pt-2">
          <Button
            type="button"
            onClick={handleConfirmRestaurantSubmit}
            className="w-full h-[48px] rounded-full bg-black text-white font-medium text-[14px] shadow-sm hover:bg-gray-900 active:scale-[0.99] transition-all duration-200"
          >
            Confirm restaurant
          </Button>
        </div>

      </div>
    </div>

    {/* Standalone Global Footer Link Stack Outside Card Container */}
    <div className="mt-8 text-center max-w-[460px] w-full px-4">
      <p className="text-[13px] text-[#6B7280]">
        Need help?{" "}
        <span className="underline cursor-pointer font-medium text-gray-800 hover:text-black">
          Contact support
        </span>{" "}
        or visit the{" "}
        <span className="underline cursor-pointer font-medium text-gray-800 hover:text-black">
          Help Centre
        </span>.
      </p>

      <div className="mt-8 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-[11px] text-gray-400 tracking-wide">
        <span>© 2026 Tummly</span>
        <span className="hover:text-gray-600 cursor-pointer transition-colors">Help Centre</span>
        <span className="hover:text-gray-600 cursor-pointer transition-colors">Terms</span>
        <span className="hover:text-gray-600 cursor-pointer transition-colors">Privacy</span>
        <span className="hover:text-gray-600 cursor-pointer transition-colors">Cookie settings</span>
      </div>
    </div>

  </main>
)}

        {/* STEP 3: PROCESS MAP MONITOR - FIGMA & PDF ALIGNED COMPLIANT */}
{step === 3 && (
  <main className="flex flex-1 flex-col items-center justify-center px-4 py-12 md:py-20 bg-[#FAFAFA] font-sans antialiased text-[#111827]">
    
    {/* Clean Floating Card Container - Matching Exact Bounds and Shadows of Step 2 */}
    <div className="w-full max-w-[460px] bg-white rounded-2xl border border-gray-200/80 shadow-[0_8px_30px_rgb(0,0,0,0.04)] p-8 md:p-10 relative transition-all">
      
      {/* Header Context Section */}
      <div className="mb-8 text-center">
        <h1 className="text-[24px] font-bold tracking-tight text-[#111827] leading-tight">
          {isWorkspaceReady ? "Your system is configuration-ready!" : "Setting up your first Guest Loop"}
        </h1>
        <p className="mt-2.5 text-[14px] leading-relaxed text-[#6B7280]">
          We are automatically generating components for your live setup environment. Do not close or refresh this view window.
        </p>
      </div>

      {/* Progress States Container Card Area */}
      <div className="space-y-5 rounded-2xl bg-[#F9FAFB] p-6 border border-gray-100 mb-6">
        
        {/* PHASE 1: SMART GUEST LINK */}
        <div className={`flex flex-col gap-1.5 transition-opacity duration-300 ${phase1Status === "idle" ? "opacity-30" : "opacity-100"}`}>
          <div className="flex items-center justify-between">
            <span className={`text-[14px] font-semibold tracking-tight ${phase1Status === "success" ? "text-[#22C55E]" : "text-[#111827]"}`}>
              {phase1Status === "success" ? "✓ Smart Guest Link ready" : "Creating your Smart Guest Link"}
            </span>
            {phase1Status === "loading" && <LoadingSpinner />}
            {phase1Status === "success" && <SuccessIcon />}
          </div>
          <p className="text-[12px] leading-normal text-[#6B7280]">Generating tracking endpoints and dedicated physical access points maps inside database storage layers.</p>
          <div className="w-full bg-[#E5E7EB] h-[3px] rounded-full mt-1 overflow-hidden">
            <div className={`h-full bg-[#22C55E] transition-all duration-[2500ms] ${phase1Status === "success" || phase1Status === "loading" ? "w-full" : "w-0"}`} />
          </div>
        </div>

        {/* PHASE 2: PRIVATE FEEDBACK FORM */}
        <div className={`flex flex-col gap-1.5 transition-opacity duration-300 ${phase2Status === "idle" ? "opacity-30" : "opacity-100"}`}>
          <div className="flex items-center justify-between">
            <span className={`text-[14px] font-semibold tracking-tight ${phase2Status === "success" ? "text-[#22C55E]" : "text-[#111827]"}`}>
              {phase2Status === "success" ? "✓ Guest form ready" : "Preparing your private feedback form"}
            </span>
            {phase2Status === "loading" && <LoadingSpinner />}
            {phase2Status === "success" && <SuccessIcon />}
          </div>
          <p className="text-[12px] leading-normal text-[#6B7280]">Injecting standard question template schema matrices and legal data-compliance opt-in consent headers into location tables.</p>
          <div className="w-full bg-[#E5E7EB] h-[3px] rounded-full mt-1 overflow-hidden">
            <div className={`h-full bg-[#22C55E] transition-all duration-[2500ms] ${phase2Status === "success" ? "w-full" : phase2Status === "loading" ? "w-3/4" : "w-0"}`} />
          </div>
        </div>

        {/* PHASE 3: STARTER QR MATERIALS */}
        <div className={`flex flex-col gap-1.5 transition-opacity duration-300 ${phase3Status === "idle" ? "opacity-30" : "opacity-100"}`}>
          <div className="flex items-center justify-between">
            <span className={`text-[14px] font-semibold tracking-tight ${phase3Status === "success" ? "text-[#22C55E]" : "text-[#111827]"}`}>
              {phase3Status === "success" ? "✓ Starter QR materials ready" : "Preparing your starter QR materials"}
            </span>
            {phase3Status === "loading" && <LoadingSpinner />}
            {phase3Status === "success" && <SuccessIcon />}
          </div>
          <p className="text-[12px] leading-normal text-[#6B7280]">Combining branding frameworks with unique vector graphics matrices to build a comprehensive 12-page PDF table-tent bundle flyer file.</p>
          <div className="w-full bg-[#E5E7EB] h-[3px] rounded-full mt-1 overflow-hidden">
            <div className={`h-full bg-[#22C55E] transition-all duration-[2500ms] ${phase3Status === "success" ? "w-full" : phase3Status === "loading" ? "w-3/4" : "w-0"}`} />
          </div>
        </div>

      </div>

      {/* Synchronized Stepper Progress Visual Footer */}
      <div className="mb-6">
        <ProgressBar activeStep={3} />
      </div>

      {/* Action Controller Pin Element */}
      <Button
        type="button"
        disabled={!isWorkspaceReady}
        className={`w-full h-[48px] rounded-full font-medium text-[14px] transition-all duration-200 ${
          isWorkspaceReady 
            ? "bg-black text-white hover:bg-gray-900 active:scale-[0.99] shadow-sm cursor-pointer" 
            : "bg-[#E5E7EB] text-[#9CA3AF] cursor-not-allowed"
        }`}
        onClick={handleOpenWorkspace}
      >
        Open workspace
      </Button>
    </div>

    {/* Optional Branding Signature (Matches Footer Layout out of Main Card Boundary) */}
    <div className="mt-8 text-center text-[11px] text-gray-400 tracking-wide">
      <span>© 2026 Tummly</span>
    </div>

  </main>
)}
        </WizardLiveValidationProvider>
      </Form>
    </div>
  )
}

export default RegisterSinglePage          
