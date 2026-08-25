import {
  createElement,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { toast } from "sonner"
import { useSearchParams } from "react-router-dom"

import {
  deactivateTeamMember,
  getTeamPermissionsPage,
  reactivateTeamMember,
  removeTeamMember,
  saveTeamPermissionsMatrix,
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
        updateRole: async (membershipId, permissionRole) => {
          await updateTeamMemberRole(membershipId, permissionRole)
          toast.success("Role updated.")
        },
        updateLocationScope: async (membershipId, payload) => {
          await updateTeamMemberLocationScope(membershipId, payload)
          toast.success("Location scope updated.")
        },
        deactivate: async (membershipId) => {
          await deactivateTeamMember(membershipId)
          toast.success("Member deactivated.")
        },
        reactivate: async (membershipId) => {
          await reactivateTeamMember(membershipId)
          toast.success("Member reactivated.")
        },
        remove: async (membershipId) => {
          await removeTeamMember(membershipId)
          toast.success("Member removed.")
        },
        saveMatrix: async (cells) => {
          await saveTeamPermissionsMatrix(cells)
          toast.success("Permission matrix saved.")
        },
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
