import { Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

type ShopToolbarProps = {
  searchQuery: string
  onSearchQueryChange: (query: string) => void
  onCreateQrAsset: () => void
  onViewOrders: () => void
}

export function ShopToolbar({
  searchQuery,
  onSearchQueryChange,
  onCreateQrAsset,
  onViewOrders,
}: ShopToolbarProps) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="relative flex-1">
        <Search className="absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          value={searchQuery}
          onChange={(e) => onSearchQueryChange(e.target.value)}
          placeholder="Search materials"
          className="h-10 w-full rounded-md border-op-border-default bg-op-surface-secondary pl-9.5 pr-4 text-sm placeholder:text-muted-foreground focus-visible:ring-1 focus-visible:ring-op-action-primary"
        />
      </div>

      <div className="flex items-center gap-2.5 shrink-0">
        <Button
          type="button"
          variant="op-primary"
          className="h-10 rounded-md px-4 text-sm font-medium"
          onClick={onCreateQrAsset}
        >
          Create QR asset
        </Button>
        <Button
          type="button"
          variant="op-secondary"
          className="h-10 rounded-md px-4 text-sm font-medium"
          onClick={onViewOrders}
        >
          View orders
        </Button>
      </div>
    </div>
  )
}
