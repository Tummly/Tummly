import { useState } from "react"

import { AdminShopCatalogPanel } from "@/components/dashboard/admin/AdminShopCatalogPanel"
import { AdminShopOrdersPanel } from "@/components/dashboard/admin/AdminShopOrdersPanel"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

export default function AdminDashboard() {
  const [tab, setTab] = useState("orders")

  return (
    <div className="flex w-full flex-col px-4 py-8 lg:px-8">
      <Tabs value={tab} onValueChange={setTab} className="flex flex-col gap-6">
        <TabsList variant="line" className="w-fit">
          <TabsTrigger value="orders">Shop orders</TabsTrigger>
          <TabsTrigger value="materials">Shop materials</TabsTrigger>
        </TabsList>
        <TabsContent value="orders" className="mt-0">
          <AdminShopOrdersPanel />
        </TabsContent>
        <TabsContent value="materials" className="mt-0">
          <AdminShopCatalogPanel />
        </TabsContent>
      </Tabs>
    </div>
  )
}
