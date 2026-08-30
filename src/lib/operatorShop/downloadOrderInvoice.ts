import { triggerBrowserDownload } from "@/lib/operatorHome/homeActions"
import type { DetailedShopOrder } from "@/lib/operatorShop/shopOrdersFilterSheetSchema"

export function generateInvoiceHtml(order: DetailedShopOrder): string {
  const invoiceNumber = `INV-${order.orderNumber.replace("#", "")}`
  const totalNumeric = order.totalNumeric ?? 82.8
  const subtotal = (totalNumeric / 1.2).toFixed(2)
  const tax = (totalNumeric - Number(subtotal)).toFixed(2)
  const email = `${order.placedBy.toLowerCase().replace(/\s+/g, ".")}@padella.co.uk`
  const materialTitle = order.materials.split("·")[0]?.trim() || "Tummly Table Tents"
  const materialDesc = order.materials.split("·")[1]?.trim() || "A5 folded card, pack of 20"

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Invoice ${invoiceNumber} - ${order.orderNumber}</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
  <style>
    * {
      box-sizing: border-box;
      margin: 0;
      padding: 0;
    }
    body {
      background-color: #0d0d0d;
      color: #ffffff;
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      display: flex;
      justify-content: center;
      padding: 40px 20px;
      -webkit-font-smoothing: antialiased;
    }
    .invoice-card {
      width: 595px;
      max-width: 100%;
      background-color: #141414;
      border: 1px solid #262626;
      border-radius: 4px;
      padding: 40px;
      display: flex;
      flex-direction: column;
      gap: 32px;
      box-shadow: 0 20px 40px rgba(0,0,0,0.5);
    }
    .header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .logo-text {
      font-size: 24px;
      font-weight: 800;
      letter-spacing: -0.5px;
      color: #ffffff;
    }
    .logo-dot {
      color: #10B981;
    }
    .title-section {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .title {
      font-size: 30px;
      font-weight: 700;
      color: #ffffff;
      letter-spacing: -0.5px;
    }
    .metadata-list {
      display: flex;
      flex-direction: column;
      gap: 4px;
      color: #78716c;
      font-size: 12px;
      font-weight: 400;
    }
    .divider {
      height: 1px;
      background-color: #262626;
      width: 100%;
    }
    .addresses-grid {
      display: flex;
      justify-content: flex-start;
      gap: 24px;
    }
    .address-col {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .address-header {
      font-size: 12px;
      font-weight: 600;
      color: #ffffff;
      margin-bottom: 2px;
    }
    .address-line {
      font-size: 10px;
      color: #78716c;
      line-height: 1.4;
    }
    .total-banner {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .total-banner-amount {
      font-size: 24px;
      font-weight: 700;
      color: #ffffff;
    }
    .total-banner-status {
      font-size: 12px;
      color: #78716c;
    }
    .table-container {
      display: flex;
      flex-direction: column;
      width: 100%;
    }
    .table-header {
      background-color: #262626;
      padding: 10px 12px;
      display: flex;
      font-size: 10px;
      font-weight: 600;
      color: #78716c;
      border-radius: 2px;
    }
    .table-row {
      padding: 12px 12px;
      display: flex;
      flex-direction: column;
      gap: 4px;
      border-bottom: 1px solid rgba(229, 231, 235, 0.1);
    }
    .table-row-cols {
      display: flex;
      align-items: center;
      font-size: 10px;
    }
    .col-desc { flex: 1; font-weight: 600; color: #ffffff; }
    .col-desc-sub { font-size: 10px; color: #78716c; }
    .col-qty { width: 40px; text-align: center; color: #78716c; }
    .col-unit { width: 80px; text-align: right; color: #78716c; }
    .col-tax { width: 40px; text-align: center; color: #78716c; }
    .col-amount { width: 80px; text-align: right; font-weight: 600; color: #ffffff; }
    
    .totals-summary {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding-top: 4px;
    }
    .summary-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 10px;
    }
    .summary-label { color: #78716c; }
    .summary-value { color: #ffffff; }
    .summary-due { font-weight: 700; color: #ffffff; }

    .footer-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 10px;
      color: #78716c;
      padding-top: 12px;
      border-top: 1px solid #262626;
    }

    @media print {
      body {
        background-color: #ffffff;
        color: #000000;
        padding: 0;
      }
      .invoice-card {
        width: 100%;
        border: none;
        box-shadow: none;
        background-color: #ffffff;
        color: #000000;
        padding: 20px;
      }
      .logo-text, .title, .address-header, .total-banner-amount, .col-desc, .col-amount, .summary-value, .summary-due {
        color: #000000 !important;
      }
      .metadata-list, .address-line, .total-banner-status, .table-header, .col-desc-sub, .col-qty, .col-unit, .col-tax, .summary-label, .footer-row {
        color: #555555 !important;
      }
      .table-header {
        background-color: #f3f4f6 !important;
      }
      .divider, .footer-row {
        border-color: #e5e7eb !important;
        background-color: #e5e7eb !important;
      }
    }
  </style>
</head>
<body>
  <div class="invoice-card">
    <div class="header-row">
      <div class="logo-text">tummly<span class="logo-dot">.</span></div>
    </div>

    <div class="title-section">
      <h1 class="title">Invoice</h1>
      <div class="metadata-list">
        <div>Invoice number: ${invoiceNumber}</div>
        <div>Date of issue: ${order.orderDate}</div>
        <div>Due date: ${order.orderDate}</div>
        <div>Currency: GBP</div>
      </div>
    </div>

    <div class="divider"></div>

    <div class="addresses-grid">
      <div class="address-col">
        <div class="address-header">From</div>
        <div class="address-line">Tummly Ltd</div>
        <div class="address-line">Registered address line 1</div>
        <div class="address-line">Town or city</div>
        <div class="address-line">Postcode</div>
        <div class="address-line">United Kingdom</div>
        <div class="address-line">billing@tummly.co.uk</div>
      </div>

      <div class="address-col">
        <div class="address-header">Bill to</div>
        <div class="address-line">${order.locationName}</div>
        <div class="address-line">6 Southwark Street</div>
        <div class="address-line">London</div>
        <div class="address-line">SE1 1TQ</div>
        <div class="address-line">United Kingdom</div>
        <div class="address-line">${email}</div>
      </div>

      <div class="address-col">
        <div class="address-header">Deliver to</div>
        <div class="address-line">${order.placedBy}</div>
        <div class="address-line">${order.locationName}</div>
        <div class="address-line">6 Southwark Street</div>
        <div class="address-line">London</div>
        <div class="address-line">SE1 1TQ</div>
        <div class="address-line">United Kingdom</div>
      </div>
    </div>

    <div class="total-banner">
      <div class="total-banner-amount">${order.total} due ${order.orderDate}</div>
      <div class="total-banner-status">Paid via Visa ending in 4242</div>
    </div>

    <div class="divider"></div>

    <div class="table-container">
      <div class="table-header">
        <div class="col-desc">Description</div>
        <div class="col-qty">Qty</div>
        <div class="col-unit">Unit price</div>
        <div class="col-tax">Tax</div>
        <div class="col-amount">Amount</div>
      </div>

      <div class="table-row">
        <div class="table-row-cols">
          <div class="col-desc">${materialTitle}</div>
          <div class="col-qty">1</div>
          <div class="col-unit">£${subtotal}</div>
          <div class="col-tax">20%</div>
          <div class="col-amount">£${subtotal}</div>
        </div>
        <div class="col-desc-sub">${materialDesc}</div>
      </div>

      <div class="table-row">
        <div class="table-row-cols">
          <div class="col-desc">Standard delivery</div>
          <div class="col-qty">1</div>
          <div class="col-unit">£0.00</div>
          <div class="col-tax">20%</div>
          <div class="col-amount">£0.00</div>
        </div>
      </div>
    </div>

    <div class="totals-summary">
      <div class="summary-row">
        <span class="summary-label">Subtotal</span>
        <span class="summary-value">£${subtotal}</span>
      </div>
      <div class="summary-row">
        <span class="summary-label">Total excluding tax</span>
        <span class="summary-value">£${subtotal}</span>
      </div>
      <div class="summary-row">
        <span class="summary-label">Tax (20% on £${subtotal})</span>
        <span class="summary-value">£${tax}</span>
      </div>
      <div class="summary-row">
        <span class="summary-label">Total</span>
        <span class="summary-value">${order.total}</span>
      </div>
      <div class="summary-row">
        <span class="summary-label">Amount due</span>
        <span class="summary-due">${order.total}</span>
      </div>
    </div>

    <div class="footer-row">
      <span>Tax = VAT</span>
      <span>Page 1 of 1</span>
    </div>
  </div>
</body>
</html>`
}

export function downloadOrderInvoice(order: DetailedShopOrder): void {
  const invoiceHtml = generateInvoiceHtml(order)
  const invoiceNumber = `INV-${order.orderNumber.replace("#", "")}`
  const blob = new Blob([invoiceHtml], { type: "text/html;charset=utf-8" })
  triggerBrowserDownload(blob, `${invoiceNumber}.html`)
}
