import type {
  ShopOrderDetailWire,
  ShopOrderListItemWire,
  ShopOrdersListWire,
} from "@/api/shopOrdersApi"
import {
  formatShopDisplayDate,
  formatShopGbpFromPence,
} from "@/lib/operatorShop/formatShopMoney"
import type {
  DetailedShopOrder,
  ShopOrdersMaterialTypeId,
} from "@/lib/operatorShop/shopOrdersFilterSheetSchema"

export function mapShopOrderListItemToRow(
  item: ShopOrderListItemWire
): DetailedShopOrder {
  return {
    id: item.id,
    orderNumber: item.orderNumber,
    orderDate: item.orderDate,
    isoDate: item.updatedAtUtc.slice(0, 10),
    locationId: item.locationId,
    locationName: item.locationName,
    materials: item.materialsSummary,
    materialTypes: item.materialTypes as ShopOrdersMaterialTypeId[],
    placedBy: item.placedBy,
    total: item.totalFormatted,
    totalNumeric: item.totalGrossPence / 100,
    paymentStatus: item.paymentStatus as DetailedShopOrder["paymentStatus"],
    fulfilmentStatus: item.fulfilmentStatus as DetailedShopOrder["fulfilmentStatus"],
    updatedDate: formatShopDisplayDate(item.updatedAtUtc),
  }
}

export function mapShopOrdersListResponse(response: ShopOrdersListWire) {
  return {
    orders: response.items.map(mapShopOrderListItemToRow),
    totalCount: response.totalCount,
    page: response.page,
    pageSize: response.pageSize,
    aggregates: response.aggregates,
  }
}

export function mapShopOrderDetailToRow(
  detail: ShopOrderDetailWire
): DetailedShopOrder {
  return {
    id: detail.id,
    orderNumber: detail.orderNumber,
    orderDate: detail.orderDate,
    isoDate: detail.updatedAtUtc.slice(0, 10),
    locationId: detail.locationId,
    locationName: detail.locationName,
    materials:
      detail.lines.length > 0
        ? detail.lines.map((line) => line.title).join(" · ")
        : "Materials",
    materialTypes: detail.lines.map(
      (line) => line.skuId as ShopOrdersMaterialTypeId
    ),
    placedBy: detail.placedBy,
    total: formatShopGbpFromPence(detail.grossPence),
    totalNumeric: detail.grossPence / 100,
    paymentStatus: detail.paymentStatusLabel as DetailedShopOrder["paymentStatus"],
    fulfilmentStatus:
      detail.fulfilmentStatusLabel as DetailedShopOrder["fulfilmentStatus"],
    updatedDate: formatShopDisplayDate(detail.updatedAtUtc),
    canCancel: detail.canCancel,
    cancelBlockReason: detail.cancelBlockReason ?? null,
    items: detail.lines.map(
      (line) => `${line.quantity}x ${line.title} (${line.materialType})`
    ),
    detail,
  }
}
