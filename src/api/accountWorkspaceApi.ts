import axiosInstance from "@/api/axiosInstance"
import type { AccountWorkspaceDetails } from "@/lib/operatorAccountWorkspace/createOperatorAccountWorkspacePageModule"

type AccountWorkspaceApiStatus = {
  workspaceStatus: string
  planStatus: string
  billingStatus: string
  accountCreatedAt: string
  activeLocations: number
  teamMembers: number
  guestProfiles: number
  guestFormStatus: string
  lastAccountUpdateAt: string
}

type AccountWorkspaceApiDetails = {
  success: boolean
  workspaceName: string
  accountStructure: string
  businessCategory: string | null
  businessCategoryLabel: string | null
  mainOperatingCountry: string
  brandLogoOperatorUrl: string | null
  brandLogoPublicUrl: string | null
  lastSavedAt: string | null
  status: AccountWorkspaceApiStatus
}

function mapDetails(
  data: AccountWorkspaceApiDetails
): AccountWorkspaceDetails {
  return {
    workspaceName: data.workspaceName,
    accountStructure: data.accountStructure,
    businessCategory: data.businessCategory,
    businessCategoryLabel: data.businessCategoryLabel,
    mainOperatingCountry: data.mainOperatingCountry,
    brandLogoOperatorUrl: data.brandLogoOperatorUrl,
    brandLogoPublicUrl: data.brandLogoPublicUrl,
    lastSavedAt: data.lastSavedAt,
    status: data.status,
  }
}

export async function getAccountWorkspaceDetails(): Promise<AccountWorkspaceDetails> {
  const response = await axiosInstance.get<AccountWorkspaceApiDetails>(
    "/account-workspace"
  )
  return mapDetails(response.data)
}

export async function updateAccountWorkspaceDetails(params: {
  name: string
  logo: File | null
}): Promise<AccountWorkspaceDetails> {
  const form = new FormData()
  form.append("name", params.name)
  if (params.logo != null) {
    form.append("logo", params.logo)
  }

  const response = await axiosInstance.put<AccountWorkspaceApiDetails>(
    "/account-workspace/account-details",
    form
  )
  return mapDetails(response.data)
}
