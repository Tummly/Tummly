import axiosInstance from "./axiosInstance"

const loginContextConfig = {
  skipAuthRedirect: true,
} as const

export async function fetchCurrentUser(): Promise<unknown> {
  const response = await axiosInstance.get("/auth/me", loginContextConfig)
  return response.data
}

export async function fetchWorkspaces(): Promise<unknown> {
  const response = await axiosInstance.get(
    "/auth/workspaces",
    loginContextConfig
  )
  return response.data
}

export async function selectWorkspace(
  locationId: number
): Promise<unknown> {
  const response = await axiosInstance.post(
    "/auth/select-workspace",
    { locationId },
    loginContextConfig
  )
  return response.data
}
