import axiosInstance from "@/api/axiosInstance"

export async function generateActivationCode(inviteToken: string) {
  const response = await axiosInstance.post(
    "/auth/generate-activation-code",
    { token: inviteToken },
    { skipAuthRedirect: true }
  )

  return response.data
}

export async function activateAccount(activationCode: string) {
  const response = await axiosInstance.post("/auth/activate", {
    activationCode,
  })

  return response.data
}
