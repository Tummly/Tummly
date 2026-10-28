import { useCallback, useEffect, useState } from "react"
import axios from "axios"
import { Trash2Icon, UploadIcon } from "lucide-react"
import { toast } from "sonner"

import {
  demoteAdminShopInterestProduct,
  fetchAdminShopInterest,
  fetchAdminShopInterestProducts,
  promoteAdminShopInterestProduct,
  updateAdminShopInterestProduct,
  uploadAdminShopInterestImage,
  deleteAdminShopInterestImage,
  type AdminShopInterestProduct,
  type AdminShopInterestProductCount,
  type AdminShopInterestRegistration,
  type AdminShopInterestScope,
} from "@/api/adminShopInterestApi"
import {
  fetchAdminPlacementRecipes,
  fetchAdminShopCatalogTags,
  type AdminPlacementRecipe,
  type AdminShopCatalogTag,
} from "@/api/adminShopCatalogApi"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { FloatingLabelTextarea } from "@/components/ui/floating-label-textarea"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { resolveShopCatalogImageSrc } from "@/lib/operatorShop/resolveShopCatalogImageSrc"
import { cn } from "@/lib/utils"

const DRAWER_SELECT_CONTENT_CLASS = "z-[150]"

const CATEGORY_OPTIONS = [
  { value: "tabletop", label: "Tabletop" },
  { value: "window", label: "Window" },
  { value: "payment", label: "Payment" },
  { value: "takeaway", label: "Takeaway" },
  { value: "delivery", label: "Delivery" },
] as const

const CATEGORY_BY_PLACEMENT_KEY: Record<string, string> = {
  TableTent: "tabletop",
  WindowSticker: "window",
  OfferCard: "payment",
}

type EditorState = {
  productKey: string
  title: string
  description: string
  material: string
  dimensions: string
  isActive: boolean
  linkedSkuId: string | null
  images: AdminShopInterestProduct["images"]
}

type PromoteState = {
  productKey: string
  title: string
  placementRecipeId: number
  category: string
  packs: { qty: string; netPence: string }[]
  deliveryNetPence: string
  mintOnShopFulfilment: boolean
  tagIds: number[]
}

const LOAD_ERROR_TEXT =
  "Could not load shop interest. Use Refresh to try again."

const SCOPE_OPTIONS: { value: AdminShopInterestScope; label: string }[] = [
  { value: "active", label: "Active only" },
  { value: "all", label: "All, including withdrawn" },
]

function formatUtc(iso: string) {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString("en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  })
}

function apiErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const message = (error.response?.data as { message?: string } | undefined)
      ?.message
    if (message) return message
  }
  return fallback
}

function editorFromProduct(product: AdminShopInterestProduct): EditorState {
  return {
    productKey: product.productKey,
    title: product.title,
    description: product.description,
    material: product.material,
    dimensions: product.dimensions,
    isActive: product.isActive,
    linkedSkuId: product.linkedSkuId,
    images: product.images,
  }
}

export function AdminShopInterestPanel() {
  const [items, setItems] = useState<AdminShopInterestRegistration[]>([])
  const [counts, setCounts] = useState<AdminShopInterestProductCount[]>([])
  const [products, setProducts] = useState<AdminShopInterestProduct[]>([])
  const [recipes, setRecipes] = useState<AdminPlacementRecipe[]>([])
  const [tags, setTags] = useState<AdminShopCatalogTag[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(false)
  const [editor, setEditor] = useState<EditorState | null>(null)
  const [promote, setPromote] = useState<PromoteState | null>(null)
  const [saving, setSaving] = useState(false)
  const [scope, setScope] = useState<AdminShopInterestScope>("active")

  const availableRecipes = recipes.filter((recipe) => recipe.isAvailable)

  const reload = useCallback(async () => {
    setLoading(true)
    try {
      const [interest, nextProducts, nextRecipes, nextTags] = await Promise.all(
        [
          fetchAdminShopInterest(scope),
          fetchAdminShopInterestProducts(),
          fetchAdminPlacementRecipes(),
          fetchAdminShopCatalogTags(),
        ]
      )
      setItems(interest.items)
      setCounts(interest.countsByProduct)
      setProducts(nextProducts)
      setRecipes(nextRecipes)
      setTags(nextTags)
      setLoadError(false)
    } catch {
      // Clear stale rows so the tables show the error, not an empty list.
      setItems([])
      setCounts([])
      setProducts([])
      setLoadError(true)
      toast.error("Could not load shop interest.")
    } finally {
      setLoading(false)
    }
  }, [scope])

  useEffect(() => {
    void reload()
  }, [reload])

  const saveEditor = async () => {
    if (!editor) return
    setSaving(true)
    try {
      const saved = await updateAdminShopInterestProduct(editor.productKey, {
        title: editor.title,
        description: editor.description,
        material: editor.material,
        dimensions: editor.dimensions,
      })
      toast.success("Interest product saved.")
      setEditor(editorFromProduct(saved))
      await reload()
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not save interest product."))
    } finally {
      setSaving(false)
    }
  }

  const onUpload = async (file: File | null) => {
    if (!editor || !file) return
    try {
      const image = await uploadAdminShopInterestImage(editor.productKey, file)
      setEditor({ ...editor, images: [...editor.images, image] })
      await reload()
      toast.success("Image uploaded.")
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not upload image."))
    }
  }

  const onDeleteImage = async (imageId: number) => {
    if (!editor) return
    try {
      await deleteAdminShopInterestImage(editor.productKey, imageId)
      setEditor({
        ...editor,
        images: editor.images.filter((image) => image.id !== imageId),
      })
      await reload()
      toast.success("Image removed.")
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not remove image."))
    }
  }

  const openPromote = (product: AdminShopInterestProduct) => {
    if (product.images.length === 0) {
      toast.error("Upload at least one image before promote.")
      return
    }
    if (!product.dimensions.trim()) {
      toast.error("Enter dimensions before promote.")
      return
    }
    // Re-promote after demote reuses the Draft SKU recipe (may be marked taken).
    const recipe =
      availableRecipes[0] ??
      recipes.find((row) => row.slug === product.productKey) ??
      recipes[0]
    if (!recipe) {
      toast.error(
        "No placement recipe available. Demote or delete a shop product first."
      )
      return
    }
    setPromote({
      productKey: product.productKey,
      title: product.title,
      placementRecipeId: recipe.id,
      category:
        CATEGORY_BY_PLACEMENT_KEY[recipe.placementKey] ?? "tabletop",
      packs: [{ qty: "1", netPence: "999" }],
      deliveryNetPence: "299",
      mintOnShopFulfilment: recipe.mintOnFulfilmentDefault,
      tagIds: [],
    })
  }

  const runPromote = async () => {
    if (!promote) return
    const packs = promote.packs
      .map((row) => ({
        qty: Number(row.qty),
        netPence: Number(row.netPence),
      }))
      .filter(
        (row) =>
          Number.isFinite(row.qty) &&
          row.qty >= 1 &&
          Number.isFinite(row.netPence) &&
          row.netPence >= 0
      )
    if (packs.length === 0) {
      toast.error("Enter valid pack quantities and prices in pence.")
      return
    }
    const deliveryNetPence = Number(promote.deliveryNetPence)
    if (!Number.isFinite(deliveryNetPence) || deliveryNetPence < 0) {
      toast.error("Enter a valid delivery price in pence.")
      return
    }

    setSaving(true)
    try {
      await promoteAdminShopInterestProduct(promote.productKey, {
        placementRecipeId: promote.placementRecipeId,
        category: promote.category,
        packs,
        deliveryNetPence,
        mintOnShopFulfilment: promote.mintOnShopFulfilment,
        tagIds: promote.tagIds,
      })
      toast.success("Product promoted to Shop.")
      setPromote(null)
      setEditor(null)
      await reload()
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not promote product."))
    } finally {
      setSaving(false)
    }
  }

  const runDemote = async (productKey: string) => {
    try {
      await demoteAdminShopInterestProduct(productKey)
      toast.success("Product demoted to Register Interest.")
      setEditor(null)
      await reload()
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not demote product."))
    }
  }

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>Shop interest</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            Manage Register Interest products, then promote them to buyable Shop
            materials. Registration history stays after promote.
          </p>
        </div>
        <Button
          type="button"
          variant="op-secondary"
          disabled={loading}
          onClick={() => void reload()}
        >
          Refresh
        </Button>
      </div>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="mb-4">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Interest products</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            Edit copy, dimensions, and images. Promote when shop-ready.
          </p>
        </div>
        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Product
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Status
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Active registrations
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Actions
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell colSpan={4} className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <Skeleton className="h-8 w-full" />
                  </TableCell>
                </TableRow>
              ) : products.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell colSpan={4} className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    {loadError ? LOAD_ERROR_TEXT : "No interest products yet."}
                  </TableCell>
                </TableRow>
              ) : (
                products.map((product) => (
                  <TableRow
                    key={product.productKey}
                    className={GUESTS_TABLE_BODY_ROW_CLASS}
                  >
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div className="flex items-center gap-3">
                        {product.images[0] ? (
                          <img
                            src={resolveShopCatalogImageSrc(
                              product.images[0].url
                            )}
                            alt=""
                            className="size-10 rounded object-cover"
                          />
                        ) : (
                          <div className="size-10 rounded bg-muted" />
                        )}
                        <div>
                          <div className="font-medium">{product.title}</div>
                          <div className="text-xs text-op-text-muted">
                            {product.productKey}
                          </div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {product.isActive ? (
                        <Badge variant="soft">Register interest</Badge>
                      ) : (
                        <Badge variant="soft">
                          In Shop
                          {product.linkedSkuId
                            ? ` · ${product.linkedSkuId}`
                            : ""}
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {product.registrationCount}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div className="flex flex-wrap gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant="op-secondary"
                          onClick={() => setEditor(editorFromProduct(product))}
                        >
                          Edit
                        </Button>
                        {product.isActive ? (
                          <Button
                            type="button"
                            size="sm"
                            variant="op-primary"
                            onClick={() => openPromote(product)}
                          >
                            Promote
                          </Button>
                        ) : (
                          <Button
                            type="button"
                            size="sm"
                            variant="op-secondary"
                            onClick={() => void runDemote(product.productKey)}
                          >
                            Demote
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="mb-4">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Counts by product</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            Active registrations per product. Withdrawn interest is not counted.
          </p>
        </div>
        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Product
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Active registrations
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell colSpan={2} className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <Skeleton className="h-8 w-full" />
                  </TableCell>
                </TableRow>
              ) : counts.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell colSpan={2} className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    {loadError
                      ? LOAD_ERROR_TEXT
                      : "No interest registrations yet."}
                  </TableCell>
                </TableRow>
              ) : (
                counts.map((row) => (
                  <TableRow
                    key={row.productKey}
                    className={GUESTS_TABLE_BODY_ROW_CLASS}
                  >
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div className="font-medium">{row.title}</div>
                      <div className="text-xs text-op-text-muted">
                        {row.productKey}
                      </div>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {row.count}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="mb-4">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Interest list</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            Workspace, Location, and registrant. Emails are masked. Newest first.
          </p>
        </div>
        <div className={cn(GUESTS_TOOLBAR_ROW_CLASS, "mb-4")}>
          <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
            <Select
              value={scope}
              onValueChange={(value) =>
                setScope(value as AdminShopInterestScope)
              }
            >
              <SelectTrigger
                aria-label="Show registrations"
                className={cn(
                  OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS,
                  "w-full min-w-40 data-[size=default]:h-auto sm:w-64"
                )}
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent className={OPERATOR_SHELL_MENU_PANEL_CLASS}>
                {SCOPE_OPTIONS.map((option) => (
                  <SelectItem
                    key={option.value}
                    value={option.value}
                    className={OPERATOR_SHELL_MENU_ITEM_CLASS}
                  >
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Product
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  State
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Workspace
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Location
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Email (masked)
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Name
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Registered (UTC)
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Withdrawn (UTC)
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                Array.from({ length: 3 }).map((_, index) => (
                  <TableRow key={index} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell
                      colSpan={8}
                      className={GUESTS_TABLE_BODY_CELL_CLASS}
                    >
                      <Skeleton className="h-8 w-full" />
                    </TableCell>
                  </TableRow>
                ))
              ) : items.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell
                    colSpan={8}
                    className={GUESTS_TABLE_BODY_CELL_CLASS}
                  >
                    {loadError
                      ? LOAD_ERROR_TEXT
                      : scope === "active"
                        ? "No active interest registrations."
                        : "No interest records yet."}
                  </TableCell>
                </TableRow>
              ) : (
                items.map((row) => (
                  <TableRow key={row.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div className="font-medium">{row.productTitle}</div>
                      <div className="text-xs text-op-text-muted">
                        {row.productKey}
                      </div>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <Badge variant="soft">{row.state}</Badge>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {row.restaurantName}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {row.locationName ?? "—"}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {row.userEmailMasked}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {row.userFullName}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {formatUtc(row.createdAtUtc)}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {row.withdrawnAtUtc ? formatUtc(row.withdrawnAtUtc) : "—"}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </section>

      <Drawer
        open={editor != null}
        onOpenChange={(open) => {
          if (!open) setEditor(null)
        }}
        direction="right"
      >
        <DrawerContent className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}>
          {editor ? (
            <div className="flex min-h-0 flex-1 flex-col overflow-y-auto px-[22px] py-8">
              <div className="mb-6 flex items-start justify-between gap-4">
                <div>
                  <DrawerTitle className="text-2xl font-bold">
                    {editor.title}
                  </DrawerTitle>
                  <DrawerDescription>
                    {editor.productKey}
                    {editor.isActive
                      ? " · Register interest"
                      : " · Promoted to Shop"}
                  </DrawerDescription>
                </div>
                <DrawerClose asChild>
                  <Button type="button" variant="ghost" size="sm">
                    Close
                  </Button>
                </DrawerClose>
              </div>

              <div className="flex flex-col gap-4">
                <FloatingLabelInput
                  label="Title"
                  value={editor.title}
                  onChange={(event) =>
                    setEditor({ ...editor, title: event.target.value })
                  }
                />
                <FloatingLabelTextarea
                  label="Description"
                  value={editor.description}
                  onChange={(event) =>
                    setEditor({ ...editor, description: event.target.value })
                  }
                />
                <div className="grid grid-cols-2 gap-3">
                  <FloatingLabelInput
                    label="Material"
                    value={editor.material}
                    onChange={(event) =>
                      setEditor({ ...editor, material: event.target.value })
                    }
                  />
                  <FloatingLabelInput
                    label="Dimensions (e.g. 105mm x 148mm)"
                    value={editor.dimensions}
                    onChange={(event) =>
                      setEditor({ ...editor, dimensions: event.target.value })
                    }
                  />
                </div>

                <div className="flex flex-col gap-2">
                  <Label>Images</Label>
                  <div className="flex flex-wrap gap-3">
                    {editor.images.map((image) => (
                      <div key={image.id} className="relative">
                        <img
                          src={resolveShopCatalogImageSrc(image.url)}
                          alt=""
                          className="size-20 rounded border object-cover"
                        />
                        <Button
                          type="button"
                          size="icon"
                          variant="ghost"
                          className="absolute -right-2 -top-2 size-7"
                          onClick={() => void onDeleteImage(image.id)}
                        >
                          <Trash2Icon className="size-3.5" />
                        </Button>
                      </div>
                    ))}
                  </div>
                  <label
                    className={cn(
                      "inline-flex w-fit cursor-pointer items-center gap-2 rounded-md border px-3 py-2 text-sm",
                      !editor.isActive && "pointer-events-none opacity-50"
                    )}
                  >
                    <UploadIcon className="size-4" />
                    Upload image
                    <input
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      className="hidden"
                      disabled={!editor.isActive}
                      onChange={(event) => {
                        void onUpload(event.target.files?.[0] ?? null)
                        event.target.value = ""
                      }}
                    />
                  </label>
                </div>

                <div className="mt-4 flex flex-wrap gap-2 border-t pt-4">
                  <Button
                    type="button"
                    variant="op-primary"
                    disabled={saving || !editor.isActive}
                    onClick={() => void saveEditor()}
                  >
                    Save changes
                  </Button>
                  {editor.isActive ? (
                    <Button
                      type="button"
                      variant="op-secondary"
                      onClick={() => {
                        const product = products.find(
                          (row) => row.productKey === editor.productKey
                        )
                        if (product) openPromote(product)
                      }}
                    >
                      Promote to Shop
                    </Button>
                  ) : (
                    <Button
                      type="button"
                      variant="op-secondary"
                      onClick={() => void runDemote(editor.productKey)}
                    >
                      Demote to Interest
                    </Button>
                  )}
                </div>
              </div>
            </div>
          ) : null}
        </DrawerContent>
      </Drawer>

      <Drawer
        open={promote != null}
        onOpenChange={(open) => {
          if (!open) setPromote(null)
        }}
        direction="right"
      >
        <DrawerContent className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}>
          {promote ? (
            <div className="flex min-h-0 flex-1 flex-col overflow-y-auto px-[22px] py-8">
              <div className="mb-6 flex items-start justify-between gap-4">
                <div>
                  <DrawerTitle className="text-2xl font-bold">
                    Promote to Shop
                  </DrawerTitle>
                  <DrawerDescription>
                    {promote.title} · becomes buyable when promote succeeds
                  </DrawerDescription>
                </div>
                <DrawerClose asChild>
                  <Button type="button" variant="ghost" size="sm">
                    Close
                  </Button>
                </DrawerClose>
              </div>

              <div className="flex flex-col gap-4">
                <FloatingLabelSelect
                  label="Placement"
                  value={String(promote.placementRecipeId)}
                  options={(availableRecipes.length > 0
                    ? availableRecipes
                    : recipes.filter(
                        (row) => row.id === promote.placementRecipeId
                      )
                  ).map((recipe) => ({
                    value: String(recipe.id),
                    label: recipe.label,
                  }))}
                  contentClassName={DRAWER_SELECT_CONTENT_CLASS}
                  onValueChange={(value) => {
                    const recipe = recipes.find(
                      (row) => String(row.id) === value
                    )
                    if (!recipe) return
                    setPromote({
                      ...promote,
                      placementRecipeId: recipe.id,
                      category:
                        CATEGORY_BY_PLACEMENT_KEY[recipe.placementKey] ??
                        promote.category,
                      mintOnShopFulfilment: recipe.mintOnFulfilmentDefault,
                    })
                  }}
                />
                <FloatingLabelSelect
                  label="Category"
                  value={promote.category}
                  options={[...CATEGORY_OPTIONS]}
                  contentClassName={DRAWER_SELECT_CONTENT_CLASS}
                  onValueChange={(value) =>
                    setPromote({ ...promote, category: value })
                  }
                />
                <div className="flex flex-col gap-2">
                  <Label>Pack tiers (qty + pack price in pence)</Label>
                  {promote.packs.map((row, index) => (
                    <div key={index} className="flex gap-2">
                      <FloatingLabelInput
                        label="Pack qty"
                        inputMode="numeric"
                        className="min-w-0 flex-1"
                        value={row.qty}
                        onChange={(event) => {
                          const next = [...promote.packs]
                          next[index] = { ...row, qty: event.target.value }
                          setPromote({ ...promote, packs: next })
                        }}
                      />
                      <FloatingLabelInput
                        label="Pack price (pence)"
                        inputMode="numeric"
                        className="min-w-0 flex-1"
                        value={row.netPence}
                        onChange={(event) => {
                          const next = [...promote.packs]
                          next[index] = {
                            ...row,
                            netPence: event.target.value,
                          }
                          setPromote({ ...promote, packs: next })
                        }}
                      />
                    </div>
                  ))}
                </div>
                <FloatingLabelInput
                  label="Delivery (pence)"
                  inputMode="numeric"
                  value={promote.deliveryNetPence}
                  onChange={(event) =>
                    setPromote({
                      ...promote,
                      deliveryNetPence: event.target.value,
                    })
                  }
                />
                <div className="flex flex-col gap-2">
                  <Label>Tags</Label>
                  {tags.map((tag) => (
                    <CheckboxLabel
                      key={tag.id}
                      checked={promote.tagIds.includes(tag.id)}
                      onCheckedChange={(on) => {
                        setPromote({
                          ...promote,
                          tagIds: on
                            ? [...promote.tagIds, tag.id]
                            : promote.tagIds.filter((id) => id !== tag.id),
                        })
                      }}
                    >
                      {tag.label}
                    </CheckboxLabel>
                  ))}
                </div>
                <CheckboxLabel
                  checked={promote.mintOnShopFulfilment}
                  onCheckedChange={(on) =>
                    setPromote({
                      ...promote,
                      mintOnShopFulfilment: on,
                    })
                  }
                >
                  Add QR Tracking to Locations on Shop Fulfilments
                </CheckboxLabel>
                <Button
                  type="button"
                  variant="op-primary"
                  disabled={saving}
                  onClick={() => void runPromote()}
                >
                  Promote and publish
                </Button>
              </div>
            </div>
          ) : null}
        </DrawerContent>
      </Drawer>
    </div>
  )
}
