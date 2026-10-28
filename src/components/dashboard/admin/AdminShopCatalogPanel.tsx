import { useCallback, useEffect, useState } from "react"
import axios from "axios"
import { PlusIcon, Trash2Icon, UploadIcon } from "lucide-react"
import { toast } from "sonner"

import {
  createAdminShopCatalogSku,
  createAdminShopCatalogTag,
  deleteAdminShopCatalogImage,
  deleteAdminShopCatalogSku,
  fetchAdminPlacementRecipes,
  fetchAdminShopCatalogSkus,
  fetchAdminShopCatalogTags,
  publishAdminShopCatalogSku,
  setAdminShopCatalogAvailableInShop,
  unpublishAdminShopCatalogSku,
  updateAdminShopCatalogSku,
  uploadAdminShopCatalogImage,
  type AdminPlacementRecipe,
  type AdminShopCatalogPack,
  type AdminShopCatalogSku,
  type AdminShopCatalogSpecification,
  type AdminShopCatalogTag,
} from "@/api/adminShopCatalogApi"
import { demoteAdminShopInterestBySku } from "@/api/adminShopInterestApi"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
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
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  normalisePoundsInput,
  penceToPoundsInput,
  poundsInputToPence,
} from "@/lib/admin/adminShopCatalogPricing"
import {
  APPROVAL_REFERENCE_MAX_LENGTH,
  availabilityBlockersFromError,
  availabilityConfirmCopy,
  toApprovalReference,
  withSkuAvailableInShop,
} from "@/lib/admin/adminShopCatalogAvailability"
import { OPERATOR_RIGHT_DRAWER_CONTENT_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { resolveShopCatalogImageSrc } from "@/lib/operatorShop/resolveShopCatalogImageSrc"
import { cn } from "@/lib/utils"

/** Select menus must sit above the right Drawer portal. */
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

type EditorPackRow = {
  qty: string
  /** Pack price in pounds as typed (for example "9.99"). */
  netPounds: string
}

type EditorState = {
  skuId: string
  title: string
  category: string
  description: string
  material: string
  dimensions: string
  placementRecipeId: number
  placementRecipeLabel: string
  packs: EditorPackRow[]
  /** Delivery in pounds as typed (for example "2.99"). */
  deliveryPounds: string
  supplierName: string
  internalCostNetPence: string
  aboutBody: string
  specifications: AdminShopCatalogSpecification[]
  tagIds: number[]
  mintOnShopFulfilment: boolean
  availableInShop: boolean
  isNew: boolean
  images: AdminShopCatalogSku["images"]
  status: AdminShopCatalogSku["status"] | "new"
}

function emptyEditor(recipe: AdminPlacementRecipe): EditorState {
  return {
    skuId: "",
    title: "",
    category: CATEGORY_BY_PLACEMENT_KEY[recipe.placementKey] ?? "tabletop",
    description: "",
    material: "",
    dimensions: "",
    placementRecipeId: recipe.id,
    placementRecipeLabel: recipe.label,
    packs: [{ qty: "1", netPounds: "9.99" }],
    deliveryPounds: "2.99",
    supplierName: "",
    internalCostNetPence: "",
    aboutBody: "",
    specifications: [{ label: "", value: "" }],
    tagIds: [],
    mintOnShopFulfilment: recipe.mintOnFulfilmentDefault,
    availableInShop: false,
    isNew: true,
    images: [],
    status: "new",
  }
}

function editorFromSku(sku: AdminShopCatalogSku): EditorState {
  const packs: EditorPackRow[] =
    sku.packs.length > 0
      ? sku.packs.map((pack) => ({
          qty: String(pack.qty),
          netPounds: penceToPoundsInput(pack.netPence),
        }))
      : [
          {
            qty: String(sku.minOrderQty),
            netPounds: penceToPoundsInput(sku.unitNetPence),
          },
        ]
  return {
    skuId: sku.skuId,
    title: sku.title,
    category: sku.category,
    description: sku.description,
    material: sku.material,
    dimensions: sku.dimensions,
    placementRecipeId: sku.placementRecipeId,
    placementRecipeLabel: sku.placementRecipeLabel,
    packs,
    deliveryPounds: penceToPoundsInput(sku.deliveryNetPence),
    supplierName: sku.supplierName ?? "",
    internalCostNetPence:
      sku.internalCostNetPence == null ? "" : String(sku.internalCostNetPence),
    aboutBody: sku.aboutBody,
    specifications:
      sku.specifications.length > 0
        ? sku.specifications
        : [{ label: "", value: "" }],
    tagIds: sku.tags.map((tag) => tag.id),
    mintOnShopFulfilment: sku.mintOnShopFulfilment,
    availableInShop: sku.availableInShop,
    isNew: false,
    images: sku.images,
    status: sku.status,
  }
}

function parseEditorPacks(
  rows: EditorPackRow[]
): AdminShopCatalogPack[] | null {
  const packs: AdminShopCatalogPack[] = []
  for (const row of rows) {
    const qty = Number(row.qty)
    const netPence = poundsInputToPence(row.netPounds)
    if (!Number.isFinite(qty) || qty < 1) return null
    if (netPence == null) return null
    packs.push({ qty, netPence })
  }
  if (packs.length === 0) return null
  const unique = new Set(packs.map((pack) => pack.qty))
  if (unique.size !== packs.length) return null
  return packs
}

/** A pending switch of Available in Shop, held until the operator confirms it. */
type AvailabilityRequest = {
  sku: Pick<AdminShopCatalogSku, "skuId" | "title">
  availableInShop: boolean
  approvalReference: string
  error: string | null
  blockers: string[]
  submitting: boolean
}

function apiErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const message = (error.response?.data as { message?: string } | undefined)
      ?.message
    if (message) return message
  }
  return fallback
}

export function AdminShopCatalogPanel() {
  const [skus, setSkus] = useState<AdminShopCatalogSku[]>([])
  const [tags, setTags] = useState<AdminShopCatalogTag[]>([])
  const [recipes, setRecipes] = useState<AdminPlacementRecipe[]>([])
  const [loading, setLoading] = useState(true)
  const [editor, setEditor] = useState<EditorState | null>(null)
  const [saving, setSaving] = useState(false)
  const [newTagLabel, setNewTagLabel] = useState("")
  const [availabilityRequest, setAvailabilityRequest] =
    useState<AvailabilityRequest | null>(null)

  const availableRecipes = recipes.filter((recipe) => recipe.isAvailable)

  const reload = useCallback(async () => {
    setLoading(true)
    try {
      const [nextSkus, nextTags, nextRecipes] = await Promise.all([
        fetchAdminShopCatalogSkus(),
        fetchAdminShopCatalogTags(),
        fetchAdminPlacementRecipes(),
      ])
      setSkus(nextSkus)
      setTags(nextTags)
      setRecipes(nextRecipes)
    } catch {
      toast.error("Could not load shop materials.")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void reload()
  }, [reload])

  const saveEditor = async () => {
    if (!editor) return
    const packs = parseEditorPacks(editor.packs)
    if (!packs) {
      toast.error("Enter valid unique pack quantities and prices in pounds.")
      return
    }
    const deliveryNetPence = poundsInputToPence(editor.deliveryPounds)
    if (deliveryNetPence == null) {
      toast.error("Enter a valid delivery price in pounds.")
      return
    }
    const internalCostRaw = editor.internalCostNetPence.trim()
    const internalCostNetPence =
      internalCostRaw.length === 0 ? null : Number(internalCostRaw)
    if (
      internalCostNetPence != null &&
      (!Number.isFinite(internalCostNetPence) || internalCostNetPence < 0)
    ) {
      toast.error("Enter a valid internal cost in pence.")
      return
    }

    setSaving(true)
    try {
      const body = {
        title: editor.title,
        category: editor.category,
        description: editor.description,
        material: editor.material,
        dimensions: editor.dimensions,
        packs,
        deliveryNetPence,
        supplierName: editor.supplierName.trim() || null,
        internalCostNetPence,
        aboutBody: editor.aboutBody,
        specifications: editor.specifications.filter(
          (row) => row.label.trim() || row.value.trim()
        ),
        tagIds: editor.tagIds,
        mintOnShopFulfilment: editor.mintOnShopFulfilment,
      }
      const saved = editor.isNew
        ? await createAdminShopCatalogSku({
            ...body,
            skuId: editor.skuId.trim() || undefined,
            placementRecipeId: editor.placementRecipeId,
          })
        : await updateAdminShopCatalogSku(editor.skuId, body)
      toast.success(
        editor.isNew
          ? "Draft created. Upload images below before publishing."
          : "Product saved."
      )
      // Keep the panel open: a new draft becomes editable here, with image upload enabled.
      setEditor(editorFromSku(saved))
      await reload()
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not save product."))
    } finally {
      setSaving(false)
    }
  }

  const runLifecycle = async (
    action: "publish" | "unpublish" | "delete",
    skuId: string
  ) => {
    try {
      if (action === "publish") {
        await publishAdminShopCatalogSku(skuId)
        toast.success("Product published.")
      } else if (action === "unpublish") {
        await unpublishAdminShopCatalogSku(skuId)
        toast.success("Product moved to draft.")
      } else {
        await deleteAdminShopCatalogSku(skuId)
        toast.success("Product deleted.")
        setEditor(null)
      }
      await reload()
      if (editor && editor.skuId === skuId && action !== "delete") {
        const refreshed = (await fetchAdminShopCatalogSkus()).find(
          (row) => row.skuId === skuId
        )
        if (refreshed) setEditor(editorFromSku(refreshed))
      }
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not update product status."))
    }
  }

  /** Sets availability on the list row and the open editor only, so unsaved edits stay. */
  const applyAvailability = (skuId: string, availableInShop: boolean) => {
    setSkus((prev) => withSkuAvailableInShop(prev, skuId, availableInShop))
    setEditor((prev) =>
      prev && prev.skuId === skuId ? { ...prev, availableInShop } : prev
    )
  }

  /** Opens the confirmation dialog. The switch does not change until the server accepts it. */
  const openAvailabilityRequest = (
    sku: Pick<AdminShopCatalogSku, "skuId" | "title">,
    availableInShop: boolean
  ) => {
    setAvailabilityRequest({
      sku,
      availableInShop,
      approvalReference: "",
      error: null,
      blockers: [],
      submitting: false,
    })
  }

  const confirmAvailabilityChange = async () => {
    if (!availabilityRequest) return
    const { sku, availableInShop } = availabilityRequest
    setAvailabilityRequest({ ...availabilityRequest, submitting: true, error: null, blockers: [] })
    try {
      const updated = await setAdminShopCatalogAvailableInShop(
        sku.skuId,
        availableInShop,
        toApprovalReference(availabilityRequest.approvalReference)
      )
      applyAvailability(sku.skuId, updated.availableInShop)
      setAvailabilityRequest(null)
      toast.success(
        updated.availableInShop
          ? "Product is available in Shop."
          : "Product set to Coming soon."
      )
    } catch (error) {
      setAvailabilityRequest((current) =>
        current && current.sku.skuId === sku.skuId
          ? {
              ...current,
              submitting: false,
              error: apiErrorMessage(error, "Could not update Shop availability."),
              blockers: availabilityBlockersFromError(error),
            }
          : current
      )
    }
  }

  const demoteToInterest = async (sku: AdminShopCatalogSku) => {
    if (!sku.linkedInterestProductKey) return
    try {
      await demoteAdminShopInterestBySku(sku.skuId)
      toast.success("Product demoted to Register Interest.")
      setEditor(null)
      await reload()
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not demote product."))
    }
  }

  const addTag = async () => {
    const label = newTagLabel.trim()
    if (!label) return
    try {
      const tag = await createAdminShopCatalogTag(label)
      setTags((prev) => [...prev, tag].sort((a, b) => a.label.localeCompare(b.label)))
      setNewTagLabel("")
      toast.success("Tag created.")
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not create tag."))
    }
  }

  const onUpload = async (file: File | null) => {
    if (!editor || editor.isNew || !file) {
      toast.error("Save the product draft before uploading images.")
      return
    }
    try {
      const image = await uploadAdminShopCatalogImage(editor.skuId, file)
      setEditor({ ...editor, images: [...editor.images, image] })
      await reload()
      toast.success("Image uploaded.")
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not upload image."))
    }
  }

  const onDeleteImage = async (imageId: number) => {
    if (!editor || editor.isNew) return
    try {
      await deleteAdminShopCatalogImage(editor.skuId, imageId)
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

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>Shop materials</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            Draft, publish, and tag printable QR materials for the Tummly Shop.
          </p>
        </div>
        <Button
          type="button"
          variant="op-primary"
          onClick={() => {
            if (availableRecipes.length === 0) {
              toast.error(
                "Each placement type can have one product. Edit the existing product, or delete it before adding a new one."
              )
              return
            }
            setEditor(emptyEditor(availableRecipes[0]!))
          }}
        >
          <PlusIcon className="size-4" />
          Add product
        </Button>
      </div>

      <section className={GUESTS_SECTION_CLASS}>
        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Product
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Placement
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Price
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Status
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Available
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Tags
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Actions
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                Array.from({ length: 3 }).map((_, index) => (
                  <TableRow key={index} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell
                      colSpan={7}
                      className={GUESTS_TABLE_BODY_CELL_CLASS}
                    >
                      <Skeleton className="h-8 w-full" />
                    </TableCell>
                  </TableRow>
                ))
              ) : skus.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell
                    colSpan={7}
                    className={GUESTS_TABLE_BODY_CELL_CLASS}
                  >
                    No shop materials yet.
                  </TableCell>
                </TableRow>
              ) : (
                skus.map((sku) => (
                  <TableRow key={sku.skuId} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div className="flex items-center gap-3">
                        {sku.images[0] ? (
                          <img
                            src={resolveShopCatalogImageSrc(sku.images[0].url)}
                            alt=""
                            className="size-10 rounded object-cover"
                          />
                        ) : (
                          <div className="size-10 rounded bg-muted" />
                        )}
                        <div>
                          <div className="font-medium">{sku.title}</div>
                          <div className="text-xs text-op-text-muted">
                            {sku.skuId}
                          </div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {sku.placementRecipeLabel}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      £{(sku.unitNetPence / 100).toFixed(2)}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <Badge variant="soft">{sku.status}</Badge>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {sku.status === "Published" ? (
                        <div className="flex items-center gap-2">
                          <Switch
                            checked={sku.availableInShop}
                            aria-label={`Available in Shop for ${sku.title}`}
                            onCheckedChange={(checked) =>
                              openAvailabilityRequest(sku, checked)
                            }
                          />
                          <span className="text-xs text-op-text-muted">
                            {sku.availableInShop ? "ON" : "Coming soon"}
                          </span>
                        </div>
                      ) : (
                        <span className="text-xs text-op-text-muted">—</span>
                      )}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div className="flex flex-wrap gap-1">
                        {sku.tags.map((tag) => (
                          <Badge key={tag.id} variant="soft">
                            {tag.label}
                          </Badge>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div className="flex flex-wrap gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant="op-secondary"
                          onClick={() => setEditor(editorFromSku(sku))}
                        >
                          Edit
                        </Button>
                        {sku.status === "Draft" ? (
                          <Button
                            type="button"
                            size="sm"
                            variant="op-primary"
                            onClick={() => void runLifecycle("publish", sku.skuId)}
                          >
                            Publish
                          </Button>
                        ) : (
                          <Button
                            type="button"
                            size="sm"
                            variant="op-secondary"
                            onClick={() =>
                              void runLifecycle("unpublish", sku.skuId)
                            }
                          >
                            Unpublish
                          </Button>
                        )}
                        <Button
                          type="button"
                          size="sm"
                          variant="op-secondary"
                          onClick={() => void runLifecycle("delete", sku.skuId)}
                        >
                          Delete
                        </Button>
                        {sku.linkedInterestProductKey ? (
                          <Button
                            type="button"
                            size="sm"
                            variant="op-secondary"
                            onClick={() => void demoteToInterest(sku)}
                          >
                            Demote to Interest
                          </Button>
                        ) : null}
                      </div>
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
                    {editor.isNew ? "New product" : editor.title}
                  </DrawerTitle>
                  <DrawerDescription>
                    {editor.isNew
                      ? "Creates as Draft. Upload images before publish."
                      : `${editor.skuId} · ${editor.status}`}
                  </DrawerDescription>
                </div>
                <DrawerClose asChild>
                  <Button type="button" variant="ghost" size="sm">
                    Close
                  </Button>
                </DrawerClose>
              </div>

              <div className="flex flex-col gap-4">
                {editor.isNew ? (
                  <FloatingLabelInput
                    label="SKU id (optional — auto from title)"
                    value={editor.skuId}
                    onChange={(event) =>
                      setEditor({ ...editor, skuId: event.target.value })
                    }
                  />
                ) : null}
                <FloatingLabelInput
                  label="Title"
                  value={editor.title}
                  onChange={(event) =>
                    setEditor({ ...editor, title: event.target.value })
                  }
                />
                <div className="grid grid-cols-2 gap-3">
                  <FloatingLabelSelect
                    label="Category"
                    value={editor.category}
                    options={[...CATEGORY_OPTIONS]}
                    contentClassName={DRAWER_SELECT_CONTENT_CLASS}
                    onValueChange={(value) =>
                      setEditor({ ...editor, category: value })
                    }
                  />
                  <FloatingLabelSelect
                    label="Placement"
                    value={String(editor.placementRecipeId)}
                    options={
                      editor.isNew
                        ? availableRecipes.map((recipe) => ({
                            value: String(recipe.id),
                            label: recipe.label,
                          }))
                        : [
                            {
                              value: String(editor.placementRecipeId),
                              label: editor.placementRecipeLabel,
                            },
                          ]
                    }
                    disabled={!editor.isNew}
                    contentClassName={DRAWER_SELECT_CONTENT_CLASS}
                    onValueChange={(value) => {
                      const recipe = recipes.find(
                        (row) => String(row.id) === value
                      )
                      if (!recipe) return
                      setEditor({
                        ...editor,
                        placementRecipeId: recipe.id,
                        placementRecipeLabel: recipe.label,
                        category:
                          CATEGORY_BY_PLACEMENT_KEY[recipe.placementKey] ??
                          editor.category,
                        mintOnShopFulfilment: recipe.mintOnFulfilmentDefault,
                      })
                    }}
                  />
                </div>
                <FloatingLabelTextarea
                  label="Short description"
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
                  <div className="flex flex-col gap-1">
                    <FloatingLabelInput
                      label="Dimensions (e.g. 105mm x 148mm)"
                      value={editor.dimensions}
                      onChange={(event) =>
                        setEditor({
                          ...editor,
                          dimensions: event.target.value,
                        })
                      }
                    />
                    <p className="text-xs leading-body text-op-text-primary">
                      Use finished size, e.g. 105mm x 148mm (A6) or 150mm x
                      150mm.
                    </p>
                  </div>
                </div>
                <div className="flex flex-col gap-2">
                  <Label>Pack tiers (qty + pack price in £)</Label>
                  {editor.packs.map((row, index) => (
                    <div key={index} className="flex gap-2">
                      <FloatingLabelInput
                        label="Pack qty"
                        inputMode="numeric"
                        className="min-w-0 flex-1"
                        value={row.qty}
                        onChange={(event) => {
                          const next = [...editor.packs]
                          next[index] = { ...row, qty: event.target.value }
                          setEditor({ ...editor, packs: next })
                        }}
                      />
                      <FloatingLabelInput
                        label="Pack price (£)"
                        inputMode="decimal"
                        className="min-w-0 flex-1"
                        value={row.netPounds}
                        onChange={(event) => {
                          const next = [...editor.packs]
                          next[index] = {
                            ...row,
                            netPounds: event.target.value,
                          }
                          setEditor({ ...editor, packs: next })
                        }}
                        onBlur={() => {
                          const current = editor.packs[index]
                          if (!current) return
                          const next = [...editor.packs]
                          next[index] = {
                            ...current,
                            netPounds: normalisePoundsInput(current.netPounds),
                          }
                          setEditor({ ...editor, packs: next })
                        }}
                      />
                      <Button
                        type="button"
                        size="icon"
                        variant="ghost"
                        className="mt-1 shrink-0"
                        onClick={() =>
                          setEditor({
                            ...editor,
                            packs: editor.packs.filter((_, i) => i !== index),
                          })
                        }
                      >
                        <Trash2Icon className="size-4" />
                      </Button>
                    </div>
                  ))}
                  <Button
                    type="button"
                    variant="op-secondary"
                    size="sm"
                    className="w-fit"
                    onClick={() =>
                      setEditor({
                        ...editor,
                        packs: [...editor.packs, { qty: "", netPounds: "" }],
                      })
                    }
                  >
                    Add pack tier
                  </Button>
                  <p className="text-xs leading-body text-op-text-primary">
                    Catalog “From £” uses the lowest pack price. Min order qty
                    syncs to the smallest pack qty.
                  </p>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <FloatingLabelInput
                    label="Delivery (£)"
                    inputMode="decimal"
                    value={editor.deliveryPounds}
                    onChange={(event) =>
                      setEditor({
                        ...editor,
                        deliveryPounds: event.target.value,
                      })
                    }
                    onBlur={() =>
                      setEditor({
                        ...editor,
                        deliveryPounds: normalisePoundsInput(
                          editor.deliveryPounds
                        ),
                      })
                    }
                  />
                  <FloatingLabelInput
                    label="Supplier (admin only)"
                    value={editor.supplierName}
                    onChange={(event) =>
                      setEditor({
                        ...editor,
                        supplierName: event.target.value,
                      })
                    }
                  />
                </div>
                <FloatingLabelInput
                  label="Internal cost (pence, admin only)"
                  inputMode="numeric"
                  value={editor.internalCostNetPence}
                  onChange={(event) =>
                    setEditor({
                      ...editor,
                      internalCostNetPence: event.target.value,
                    })
                  }
                />
                <FloatingLabelTextarea
                  label="About (product page)"
                  value={editor.aboutBody}
                  onChange={(event) =>
                    setEditor({ ...editor, aboutBody: event.target.value })
                  }
                />

                <div className="flex flex-col gap-2">
                  <Label>Product specifications</Label>
                  {editor.specifications.map((row, index) => (
                    <div key={index} className="flex gap-2">
                      <FloatingLabelInput
                        label="Label"
                        className="min-w-0 flex-1"
                        value={row.label}
                        onChange={(event) => {
                          const next = [...editor.specifications]
                          next[index] = {
                            ...row,
                            label: event.target.value,
                          }
                          setEditor({ ...editor, specifications: next })
                        }}
                      />
                      <FloatingLabelInput
                        label="Value"
                        className="min-w-0 flex-1"
                        value={row.value}
                        onChange={(event) => {
                          const next = [...editor.specifications]
                          next[index] = {
                            ...row,
                            value: event.target.value,
                          }
                          setEditor({ ...editor, specifications: next })
                        }}
                      />
                      <Button
                        type="button"
                        size="icon"
                        variant="ghost"
                        className="mt-1 shrink-0"
                        onClick={() =>
                          setEditor({
                            ...editor,
                            specifications: editor.specifications.filter(
                              (_, i) => i !== index
                            ),
                          })
                        }
                      >
                        <Trash2Icon className="size-4" />
                      </Button>
                    </div>
                  ))}
                  <Button
                    type="button"
                    variant="op-secondary"
                    size="sm"
                    className="w-fit"
                    onClick={() =>
                      setEditor({
                        ...editor,
                        specifications: [
                          ...editor.specifications,
                          { label: "", value: "" },
                        ],
                      })
                    }
                  >
                    Add specification
                  </Button>
                </div>

                <div className="flex flex-col gap-2">
                  <Label>Tags</Label>
                  <div className="flex flex-col gap-2">
                    {tags.map((tag) => (
                      <CheckboxLabel
                        key={tag.id}
                        checked={editor.tagIds.includes(tag.id)}
                        onCheckedChange={(on) => {
                          setEditor({
                            ...editor,
                            tagIds: on
                              ? [...editor.tagIds, tag.id]
                              : editor.tagIds.filter((id) => id !== tag.id),
                          })
                        }}
                      >
                        {tag.label}
                      </CheckboxLabel>
                    ))}
                  </div>
                  <div className="flex gap-2">
                    <FloatingLabelInput
                      label="New tag label"
                      className="min-w-0 flex-1"
                      value={newTagLabel}
                      onChange={(event) => setNewTagLabel(event.target.value)}
                    />
                    <Button
                      type="button"
                      variant="op-secondary"
                      className="mt-1 shrink-0"
                      onClick={() => void addTag()}
                    >
                      Add tag
                    </Button>
                  </div>
                </div>

                <CheckboxLabel
                  checked={editor.mintOnShopFulfilment}
                  onCheckedChange={(on) =>
                    setEditor({
                      ...editor,
                      mintOnShopFulfilment: on,
                    })
                  }
                >
                  Add QR Tracking to Locations on Shop Fulfilments
                </CheckboxLabel>

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
                      editor.isNew && "pointer-events-none opacity-50"
                    )}
                  >
                    <UploadIcon className="size-4" />
                    Upload image
                    <input
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      className="hidden"
                      disabled={editor.isNew}
                      onChange={(event) => {
                        void onUpload(event.target.files?.[0] ?? null)
                        event.target.value = ""
                      }}
                    />
                  </label>
                </div>

                {!editor.isNew && editor.status === "Published" ? (
                  <div className="flex items-center justify-between gap-3 rounded-md border px-3 py-2">
                    <div className="flex flex-col gap-0.5">
                      <Label htmlFor="available-in-shop">
                        Available in Shop
                      </Label>
                      <span className="text-xs text-op-text-muted">
                        OFF keeps the product listed as Coming soon.
                      </span>
                    </div>
                    <Switch
                      id="available-in-shop"
                      checked={editor.availableInShop}
                      onCheckedChange={(checked) =>
                        openAvailabilityRequest(
                          { skuId: editor.skuId, title: editor.title },
                          checked
                        )
                      }
                    />
                  </div>
                ) : null}

                <div className="mt-4 flex flex-wrap gap-2 border-t pt-4">
                  <Button
                    type="button"
                    variant="op-primary"
                    disabled={saving}
                    onClick={() => void saveEditor()}
                  >
                    {editor.isNew ? "Create draft" : "Save changes"}
                  </Button>
                  {!editor.isNew && editor.status === "Draft" ? (
                    <Button
                      type="button"
                      variant="op-secondary"
                      onClick={() => void runLifecycle("publish", editor.skuId)}
                    >
                      Publish
                    </Button>
                  ) : null}
                  {!editor.isNew && editor.status === "Published" ? (
                    <Button
                      type="button"
                      variant="op-secondary"
                      onClick={() =>
                        void runLifecycle("unpublish", editor.skuId)
                      }
                    >
                      Unpublish
                    </Button>
                  ) : null}
                </div>
              </div>
            </div>
          ) : null}
        </DrawerContent>
      </Drawer>

      <Dialog
        open={availabilityRequest !== null}
        onOpenChange={(open) => {
          if (!open && !availabilityRequest?.submitting) setAvailabilityRequest(null)
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {availabilityRequest
                ? availabilityConfirmCopy(availabilityRequest.availableInShop).title
                : ""}
            </DialogTitle>
            <DialogDescription>
              {availabilityRequest
                ? availabilityConfirmCopy(availabilityRequest.availableInShop).description
                : ""}
            </DialogDescription>
          </DialogHeader>
          {availabilityRequest ? (
            <div className="flex flex-col gap-3">
              <p className="m-0 text-sm text-op-text-primary">
                Product: {availabilityRequest.sku.title}
              </p>
              <p className="m-0 text-sm text-op-text-primary">
                New state:{" "}
                {availabilityRequest.availableInShop ? "ON" : "Coming soon"}
              </p>
              <div className="flex flex-col gap-2">
                <Label htmlFor="shop-availability-approval">
                  Approval reference (optional)
                </Label>
                <Input
                  id="shop-availability-approval"
                  maxLength={APPROVAL_REFERENCE_MAX_LENGTH}
                  value={availabilityRequest.approvalReference}
                  disabled={availabilityRequest.submitting}
                  onChange={(event) =>
                    setAvailabilityRequest((current) =>
                      current
                        ? { ...current, approvalReference: event.target.value }
                        : current
                    )
                  }
                />
              </div>
              {availabilityRequest.error ? (
                <div role="alert" className="flex flex-col gap-1 text-sm text-destructive">
                  <p className="m-0">{availabilityRequest.error}</p>
                  {availabilityRequest.blockers.length > 0 ? (
                    <ul className="m-0 list-disc pl-5">
                      {availabilityRequest.blockers.map((blocker) => (
                        <li key={blocker}>{blocker}</li>
                      ))}
                    </ul>
                  ) : null}
                </div>
              ) : null}
            </div>
          ) : null}
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setAvailabilityRequest(null)}
              disabled={availabilityRequest?.submitting}
            >
              Cancel
            </Button>
            <Button
              onClick={() => void confirmAvailabilityChange()}
              disabled={!availabilityRequest || availabilityRequest.submitting}
            >
              {availabilityRequest?.submitting
                ? "Saving…"
                : availabilityConfirmCopy(availabilityRequest?.availableInShop ?? true)
                    .actionLabel}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
