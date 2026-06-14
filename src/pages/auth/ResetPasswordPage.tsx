import { useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import axios, { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { Link, useNavigate, useSearchParams } from "react-router-dom"

import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { AUTH_API_BASE_URL } from "@/config/api"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  resetPasswordDefaultValues,
  resetPasswordFormSchema,
  toResetPasswordPayload,
  type ResetPasswordFormValues,
} from "@/schemas/resetPassword"

interface ResetPasswordResponse {
  message: string
}

const missingTokenMessage = "Invalid or missing token"

function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get("token")

  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordFormSchema),
    defaultValues: resetPasswordDefaultValues,
    ...defaultFormValidationOptions,
  })

  const [submitting, setSubmitting] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const rootError = form.formState.errors.root?.message

  const onSubmit = async (values: ResetPasswordFormValues) => {
    if (!token) {
      form.setError("root", { message: missingTokenMessage })
      return
    }

    form.clearErrors("root")
    setSubmitting(true)

    try {
      const response = await axios.post<ResetPasswordResponse>(
        `${AUTH_API_BASE_URL}/reset-password`,
        toResetPasswordPayload(values, token)
      )

      setSuccessMessage(response.data.message)
      window.setTimeout(() => {
        navigate("/login")
      }, 1500)
    } catch (error: unknown) {
      const message = isAxiosError<ResetPasswordResponse>(error)
        ? error.response?.data?.message || "Something went wrong"
        : "Something went wrong"

      form.setError("root", { message })
    } finally {
      setSubmitting(false)
    }
  }

  if (!token) {
    return (
      <div className="flex min-h-screen w-full items-center justify-center bg-[#f5f5f5]">
        <div className="w-[420px] rounded-xl bg-white p-10 shadow-[0_10px_30px_rgba(0,0,0,0.08)]">
          <h2 className="m-0 mb-2.5 text-xl font-semibold">Reset Password</h2>
          <p className="m-0 mb-6 text-sm text-destructive" role="alert">
            {missingTokenMessage}
          </p>
          <Button asChild size="block">
            <Link to="/login">Back to Sign in</Link>
          </Button>
        </div>
      </div>
    )
  }

  if (successMessage) {
    return (
      <div className="flex min-h-screen w-full items-center justify-center bg-[#f5f5f5]">
        <div className="w-[420px] rounded-xl bg-white p-10 shadow-[0_10px_30px_rgba(0,0,0,0.08)]">
          <h2 className="m-0 mb-2.5 text-xl font-semibold">Reset Password</h2>
          <p className="m-0 mb-6 text-sm font-medium text-[#14a247]" role="status">
            {successMessage}
          </p>
          <p className="m-0 text-sm text-[#666]">
            Redirecting you to Sign in...
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen w-full items-center justify-center bg-[#f5f5f5]">
      <div className="w-[420px] rounded-xl bg-white p-10 shadow-[0_10px_30px_rgba(0,0,0,0.08)]">
        <h2 className="m-0 mb-2.5 text-xl font-semibold">Reset Password</h2>

        <p className="m-0 mb-6 text-sm text-[#666]">
          Enter your new password below.
        </p>

        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmit)}
            noValidate
            className="flex flex-col"
          >
            <FormFloatingInput
              control={form.control}
              name="newPassword"
              type="password"
              label="New Password"
              className="mb-[15px]"
              required
            />

            <FormFloatingInput
              control={form.control}
              name="confirmPassword"
              type="password"
              label="Confirm Password"
              className="mb-5"
              required
            />

            <FieldErrorSlot error={rootError} reserveClassName="min-h-0 mb-4" />

            <Button type="submit" disabled={submitting} size="block">
              {submitting ? "Please wait..." : "Update Password"}
            </Button>
          </form>
        </Form>
      </div>
    </div>
  )
}

export default ResetPasswordPage
