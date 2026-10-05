import { Outlet } from "react-router-dom"

import { StaffDashboardHeader } from "@/components/dashboard/staff/StaffDashboardHeader"

export default function StaffDashboardLayout() {
  return (
    <div className="flex min-h-dvh flex-col bg-background">
      <StaffDashboardHeader />
      <main className="flex min-h-0 flex-1 flex-col">
        <Outlet />
      </main>
    </div>
  )
}
