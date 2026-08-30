# Billing Account is 1:1 with Restaurant, keyed by RestaurantId

One **Billing Account** equals one Restaurant. Persist it as a required 1:1 child (`BillingAccounts` / `BillingAccount`) whose primary key is `RestaurantId`. Insert the row in the same transaction as Restaurant create. Ledger writes and callers use `RestaurantId` only. Tummly **Billing status** lives on this row; Revolut customer id is an opaque unique-when-set ref. `OwnerUserId` and `BillingContactUserId` stay on `Restaurant`.

We rejected extra billing columns on `Restaurant`: workspace and Key contacts writes would share a row with plan, **Billing status**, and provider refs. We rejected a surrogate `BillingAccountId`: it would invent a second billed identity after the charting lock that one **Billing Account** is one Restaurant.

Product lock: `.scratch/credit-ledger-backend/issues/01-billing-account-persistence-on-the-restaurant.md`.
