import { useEffect, useRef, useSyncExternalStore } from "react"
import { useNavigate } from "react-router-dom"
import { toast } from "sonner"

import {
  ensureNotificationSeeds,
  getNotificationPreferences,
  listNotifications,
  markInboxRead,
  markNotificationRead,
  markVisibleNotificationsRead,
  setNotificationPreferences,
} from "@/api/notificationsApi"
import { connectNotificationsHub } from "@/lib/operatorNotifications/connectNotificationsHub"
import {
  createOperatorNotificationsModule,
  type OperatorNotificationCategory,
  type OperatorNotificationsModule,
  type OperatorNotificationsSnapshot,
  type OperatorNotificationsTab,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"

export type OperatorNotificationsApi = {
  snapshot: OperatorNotificationsSnapshot
  connect: OperatorNotificationsModule["connect"]
  disconnect: OperatorNotificationsModule["disconnect"]
  openDrawer: OperatorNotificationsModule["openDrawer"]
  closeDrawer: OperatorNotificationsModule["closeDrawer"]
  setTab: (tab: OperatorNotificationsTab) => void
  markOneRead: (id: number) => void
  markVisibleRead: () => void
  activateCta: (id: number) => void
  openSettings: () => Promise<void>
  closeSettings: () => void
  setPreference: (
    category: OperatorNotificationCategory,
    enabled: boolean
  ) => void
}

export function useOperatorNotificationsModule(): OperatorNotificationsApi {
  const navigate = useNavigate()
  const navigateRef = useRef(navigate)
  navigateRef.current = navigate

  const moduleRef = useRef<OperatorNotificationsModule | null>(null)

  if (moduleRef.current == null) {
    moduleRef.current = createOperatorNotificationsModule({
      listNotifications: async () => {
        const response = await listNotifications()
        return {
          unreadCount: response.unreadCount,
          items: response.items,
        }
      },
      markOneRead: markNotificationRead,
      markInboxRead: async () => {
        await markInboxRead()
      },
      markVisibleRead: markVisibleNotificationsRead,
      getPreferences: getNotificationPreferences,
      setPreferences: setNotificationPreferences,
      navigate: (href) => {
        navigateRef.current(href)
      },
      connectRealtime: connectNotificationsHub,
      ensureSeeds: ensureNotificationSeeds,
      showToast: (notification) => {
        toast(notification.title, {
          description: notification.body,
        })
      },
    })
  }

  const notifications = moduleRef.current
  const snapshot = useSyncExternalStore(
    notifications.subscribe,
    notifications.getSnapshot,
    notifications.getSnapshot
  )

  const connectRef = useRef(notifications.connect)
  const disconnectRef = useRef(notifications.disconnect)
  connectRef.current = notifications.connect
  disconnectRef.current = notifications.disconnect

  useEffect(() => {
    void connectRef.current()
    return () => {
      void disconnectRef.current()
    }
  }, [])

  return {
    snapshot,
    connect: notifications.connect,
    disconnect: notifications.disconnect,
    openDrawer: notifications.openDrawer,
    closeDrawer: notifications.closeDrawer,
    setTab: notifications.setTab,
    markOneRead: notifications.markOneRead,
    markVisibleRead: notifications.markVisibleRead,
    activateCta: notifications.activateCta,
    openSettings: notifications.openSettings,
    closeSettings: notifications.closeSettings,
    setPreference: notifications.setPreference,
  }
}
