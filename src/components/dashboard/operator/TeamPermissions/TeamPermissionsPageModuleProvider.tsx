import {
  createElement,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { toast } from "sonner"
import { useSearchParams } from "react-router-dom"

import {
  getTeamPermissionsPage,
  getTeamAccessActivity,
  reactivateTeamMember,
  removeTeamMember,
  resendTeamInvitation,
  revokeTeamInvitation,
  saveTeamPermissionsMatrix,
  sendTeamInvitation,
  updateTeamMemberLocationScope,
  updateTeamMemberRole,
} from "@/api/teamPermissionsApi"
import { teamPermissionsPageModuleContext } from "@/components/dashboard/operator/TeamPermissions/utils/teamPermissionsPageModuleContext"
import {
  createOperatorTeamPermissionsPageModule,
  resolveTeamPermissionsTabId,
} from "@/lib/operatorTeamPermissions/createOperatorTeamPermissionsPageModule"

export function TeamPermissionsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [searchParams] = useSearchParams()
  const initialTabId = searchParams.get("tab")
  const [pageModule] = useState(() =>
    createOperatorTeamPermissionsPageModule(
      {
        getPage: getTeamPermissionsPage,
        updateMemberProfile: async (membershipId, payload) => {
          try {
            // Location scope must land before role when both change — role
            // validation reads the member's current scope (Area/Location Manager
            // require a named list).
            if (payload.locationScope != null) {
              await updateTeamMemberLocationScope(
                membershipId,
                payload.locationScope
              )
            }
            if (payload.permissionRole != null) {
              await updateTeamMemberRole(membershipId, payload.permissionRole)
            }
            toast.success("Member updated.")
          } catch (error) {
            toast.error(
              error instanceof Error
                ? error.message
                : "Could not update member."
            )
            throw error
          }
        },
        reactivate: async (membershipId) => {
          await reactivateTeamMember(membershipId)
          toast.success("Member reactivated.")
        },
        remove: async (membershipId) => {
          await removeTeamMember(membershipId)
          toast.success("Member suspended.")
        },
        saveMatrix: async (cells) => {
          await saveTeamPermissionsMatrix(cells)
          toast.success("Permission matrix saved.")
        },
        sendInvite: async (payload) => {
          await sendTeamInvitation(payload)
          toast.success("Invitation sent.")
        },
        resendInvite: async (invitationId) => {
          try {
            await resendTeamInvitation(invitationId)
            toast.success("Invitation resent.")
          } catch (error) {
            toast.error(
              error instanceof Error
                ? error.message
                : "Could not resend invitation."
            )
            throw error
          }
        },
        revokeInvite: async (invitationId) => {
          try {
            await revokeTeamInvitation(invitationId)
            toast.success("Invitation revoked.")
          } catch (error) {
            toast.error(
              error instanceof Error
                ? error.message
                : "Could not revoke invitation."
            )
            throw error
          }
        },
        getAccessActivity: getTeamAccessActivity,
      },
      { initialTabId }
    )
  )

  useEffect(() => {
    void pageModule.load()
  }, [pageModule])

  useEffect(() => {
    pageModule.setActiveTabFromUrl(searchParams.get("tab"))
  }, [pageModule, searchParams])

  return createElement(
    teamPermissionsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

export { resolveTeamPermissionsTabId }
