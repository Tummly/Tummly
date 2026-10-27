import { readShopApiErrorCode } from "@/lib/operatorShop/readShopApiErrorCode"

/** True when a Shop write was refused because the location has no card offer attached. */
export function shouldOpenOfferCardSetupForError(error: unknown): boolean {
  return readShopApiErrorCode(error) === "offer_card_offer_required"
}
