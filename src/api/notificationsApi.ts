import axiosInstance from "./axiosInstance"
import type {
  OperatorNotification,
  OperatorNotificationPreferences,
  NotificationVisibleFilter,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"
import { normalizeNotificationPreferences } from "@/lib/operatorNotifications/createOperatorNotificationsModule"

export type NotificationsListResponse = {
  success: boolean
  unreadCount: number
  items: OperatorNotification[]
}

/** Wire shape from NotificationsController preferences endpoints. */
type NotificationPreferencesApiDto = {
  productUpdates: boolean
  accountNotices: boolean
  weeklyBriefReminders: boolean
  tipsAndPlaybooks: boolean
  campaignAndReportUpdates: boolean
}

export type NotificationsPreferencesResponse = {
  success: boolean
  preferences: NotificationPreferencesApiDto
}

function fromApiPreferences(
  dto: NotificationPreferencesApiDto
): OperatorNotificationPreferences {
  return normalizeNotificationPreferences({
    "product-updates": dto.productUpdates,
    "account-notices": dto.accountNotices,
    "weekly-brief-reminders": dto.weeklyBriefReminders,
    "tips-and-playbooks": dto.tipsAndPlaybooks,
    "campaign-and-report-updates": dto.campaignAndReportUpdates,
  })
}

function toApiPreferences(
  preferences: OperatorNotificationPreferences
): NotificationPreferencesApiDto {
  return {
    productUpdates: preferences["product-updates"],
    accountNotices: preferences["account-notices"],
    weeklyBriefReminders: preferences["weekly-brief-reminders"],
    tipsAndPlaybooks: preferences["tips-and-playbooks"],
    campaignAndReportUpdates: preferences["campaign-and-report-updates"],
  }
}

export const listNotifications = async (): Promise<NotificationsListResponse> => {
  const response = await axiosInstance.get<NotificationsListResponse>(
    "/notifications"
  )
  return response.data
}

export const markNotificationRead = async (id: number): Promise<void> => {
  await axiosInstance.post(`/notifications/${id}/read`)
}

export const markInboxRead = async (): Promise<void> => {
  await axiosInstance.post("/notifications/read-all")
}

export const markVisibleNotificationsRead = async (
  filter: NotificationVisibleFilter
): Promise<void> => {
  await axiosInstance.post("/notifications/read-visible", filter)
}

export const getNotificationPreferences =
  async (): Promise<OperatorNotificationPreferences> => {
    const response = await axiosInstance.get<NotificationsPreferencesResponse>(
      "/notifications/preferences"
    )
    return fromApiPreferences(response.data.preferences)
  }

export const setNotificationPreferences = async (
  preferences: OperatorNotificationPreferences
): Promise<OperatorNotificationPreferences> => {
  const response = await axiosInstance.put<NotificationsPreferencesResponse>(
    "/notifications/preferences",
    toApiPreferences(preferences)
  )
  return fromApiPreferences(response.data.preferences)
}

export type EnsureSeedsResponse = {
  success: boolean
  reToast: OperatorNotification[]
}

export const ensureNotificationSeeds = async (): Promise<{
  reToast: OperatorNotification[]
}> => {
  const response = await axiosInstance.post<EnsureSeedsResponse>(
    "/notifications/ensure-seeds"
  )
  return { reToast: response.data.reToast ?? [] }
}
