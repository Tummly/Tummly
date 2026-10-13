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
  unpublishAdminShopCatalogSku,
  updateAdminShopCatalogSku,
  uploadAdminShopCatalogImage,
  type AdminPlacementRecipe,
  type AdminShopCatalogSku,
  type AdminShopCatalogSpecification,
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

type EditorState = {
  skuId: string
  title: string
  category: string
  description: string
  material: string
  dimensions: string
  placementRecipeId: number
  placementRecipeLabel: string
  unitNetPence: string
  minOrderQty: string
  aboutBody: string
  specifications: AdminShopCatalogSpecification[]
  tagIds: number[]
  mintOnShopFulfilment: boolean
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
    unitNetPence: "1800",
    minOrderQty: "1",
    aboutBody: "",
    specifications: [{ label: "", value: "" }],
    tagIds: [],
    mintOnShopFulfilment: recipe.mintOnFulfilmentDefault,
    isNew: true,
    images: [],
    status: "new",
  }
}

function editorFromSku(sku: AdminShopCatalogSku): EditorState {
  return {
    skuId: sku.skuId,
    title: sku.title,
    category: sku.category,
    description: sku.description,
    material: sku.material,
    dimensions: sku.dimensions,
    placementRecipeId: sku.placementRecipeId,
    placementRecipeLabel: sku.placementRecipeLabel,
    unitNetPence: String(sku.unitNetPence),
    minOrderQty: String(sku.minOrderQty),
    aboutBody: sku.aboutBody,
    specifications:
      sku.specifications.length > 0
        ? sku.specifications
        : [{ label: "", value: "" }],
    tagIds: sku.tags.map((tag) => tag.id),
    mintOnShopFulfilment: sku.mintOnShopFulfilment,
    isNew: false,
    images: sku.images,
    status: sku.status,
  }
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
    const unitNetPence = Number(editor.unitNetPence)
    const minOrderQty = Number(editor.minOrderQty)
    if (!Number.isFinite(unitNetPence) || unitNetPence < 0) {
      toast.error("Enter a valid unit price in pence.")
      return
    }
    if (!Number.isFinite(minOrderQty) || minOrderQty < 1) {
      toast.error("Minimum order quantity must be at least 1.")
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
        unitNetPence,
        minOrderQty,
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
      toast.success(editor.isNew ? "Draft product created." : "Product saved.")
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
      <div className="flex flex-wrap items-start justify-between gap-4">
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
                "All placement recipes already have a product. Delete one before adding another."
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
                      colSpan={6}
                      className={GUESTS_TABLE_BODY_CELL_CLASS}
                    >
                      <Skeleton className="h-8 w-full" />
                    </TableCell>
                  </TableRow>
                ))
              ) : skus.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell
                    colSpan={6}
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
                          <div className="text-xs text-muted-foreground">
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
                    <p className="text-xs text-muted-foreground">
                      Use finished size, e.g. 105mm x 148mm (A6) or 150mm x
                      150mm.
                    </p>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <FloatingLabelInput
                    label="Unit price (pence)"
                    inputMode="numeric"
                    value={editor.unitNetPence}
                    onChange={(event) =>
                      setEditor({
                        ...editor,
                        unitNetPence: event.target.value,
                      })
                    }
                  />
                  <FloatingLabelInput
                    label="Min order qty"
                    inputMode="numeric"
                    value={editor.minOrderQty}
                    onChange={(event) =>
                      setEditor({
                        ...editor,
                        minOrderQty: event.target.value,
                      })
                    }
                  />
                </div>
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
    </div>
  )
}
