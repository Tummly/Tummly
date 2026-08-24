import axiosInstance from "@/api/axiosInstance"
import {
  defaultAccountWorkspaceCountry,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import type {
  AccountWorkspaceBusinessDetails,
  AccountWorkspaceDetails,
  AccountWorkspaceKeyContacts,
  TeamMemberPickerItem,
  UpdateBusinessDetailsPayload,
  UpdateKeyContactsPayload,
} from "@/lib/operatorAccountWorkspace/createOperatorAccountWorkspacePageModule"

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

type AccountWorkspaceApiBusinessDetails = {
  legalStructure: string | null
  legalBusinessName: string | null
  tradingName: string | null
  companyNumber: string | null
  vatNumber: string | null
  countryOfRegistration: string | null
  addressLine1: string | null
  addressLine2: string | null
  townCity: string | null
  county: string | null
  postcode: string | null
  country: string | null
}

type AccountWorkspaceApiPickerItem = {
  userId: number
  fullName: string
  email: string
}

type AccountWorkspaceApiKeyContacts = {
  accountOwner: AccountWorkspaceApiPickerItem
  billingContactUserId: number
  privacyContactUserId: number
  supportContactUserId: number
  eligibleMembers: AccountWorkspaceApiPickerItem[]
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
  businessDetails?: AccountWorkspaceApiBusinessDetails | null
  keyContacts?: AccountWorkspaceApiKeyContacts | null
}

function mapBusinessDetails(
  data: AccountWorkspaceApiBusinessDetails | null | undefined
): AccountWorkspaceBusinessDetails {
  return {
    legalStructure: data?.legalStructure ?? "",
    legalBusinessName: data?.legalBusinessName ?? "",
    tradingName: data?.tradingName ?? "",
    companyNumber: data?.companyNumber ?? "",
    vatNumber: data?.vatNumber ?? "",
    countryOfRegistration: defaultAccountWorkspaceCountry(
      data?.countryOfRegistration
    ),
    addressLine1: data?.addressLine1 ?? "",
    addressLine2: data?.addressLine2 ?? "",
    townCity: data?.townCity ?? "",
    county: data?.county ?? "",
    postcode: data?.postcode ?? "",
    country: defaultAccountWorkspaceCountry(data?.country),
  }
}

function mapPickerItem(
  data: AccountWorkspaceApiPickerItem | null | undefined
): TeamMemberPickerItem {
  return {
    userId: data?.userId ?? 0,
    fullName: data?.fullName ?? "",
    email: data?.email ?? "",
  }
}

function mapKeyContacts(
  data: AccountWorkspaceApiKeyContacts | null | undefined
): AccountWorkspaceKeyContacts {
  const accountOwner = mapPickerItem(data?.accountOwner)
  const ownerId = accountOwner.userId
  return {
    accountOwner,
    billingContactUserId: data?.billingContactUserId || ownerId,
    privacyContactUserId: data?.privacyContactUserId || ownerId,
    supportContactUserId: data?.supportContactUserId || ownerId,
    eligibleMembers: (data?.eligibleMembers ?? []).map(mapPickerItem),
  }
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
    businessDetails: mapBusinessDetails(data.businessDetails),
    keyContacts: mapKeyContacts(data.keyContacts),
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

export async function updateAccountWorkspaceBusinessDetails(
  payload: UpdateBusinessDetailsPayload
): Promise<AccountWorkspaceDetails> {
  const response = await axiosInstance.put<AccountWorkspaceApiDetails>(
    "/account-workspace/business-details",
    payload
  )
  return mapDetails(response.data)
}

export async function updateAccountWorkspaceKeyContacts(
  payload: UpdateKeyContactsPayload
): Promise<AccountWorkspaceDetails> {
  const response = await axiosInstance.put<AccountWorkspaceApiDetails>(
    "/account-workspace/key-contacts",
    payload
  )
  return mapDetails(response.data)
}
