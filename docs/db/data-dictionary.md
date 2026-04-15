# DeliverTable – Data Dictionary

> **Source of truth**: [`er-diagram.md`](./er-diagram.md)
>
> This document describes every entity, attribute, data type, constraint, and relationship defined in the DeliverTable ER model.
> It is intended for backend developers, database administrators, and anyone who needs a precise understanding of the data layer.

---

## Conventions

| Convention | Detail |
|---|---|
| **Primary keys** | Opaque, non-guessable identifiers (UUID / ULID). Integer PKs acceptable for purely internal tables. |
| **Timestamps** | All `datetime` fields are stored as **UTC, timezone-aware** values (e.g. PostgreSQL `timestamptz`). Local conversion happens at the application / UI layer only. |
| **Soft deletion** | Tables that support soft deletion use `is_active` flags or a `deleted_at` timestamp instead of hard deletes. |
| **Naming** | `snake_case` for all columns and tables. |
| **Enumerations** | Status and type fields are modeled as constrained types / enums at both the database and application levels. |
| **Currency amounts** | Stored as `float` in the ER model; implementations should prefer `DECIMAL` / `NUMERIC` for financial precision. |

---

## Table of Contents

1. [USER](#1-user)
2. [CUSTOMER_PROFILE](#2-customer_profile)
3. [RESTAURANT_OWNER_PROFILE](#3-restaurant_owner_profile)
4. [RESTAURANT](#4-restaurant)
5. [RESTAURANT_TABLE](#5-restaurant_table)
6. [RESTAURANT_TRANSACTION](#6-restaurant_transaction)
7. [MENU_ITEM](#7-menu_item)
8. [PROMOTION](#8-promotion)
9. [PROMOTION_DISH](#9-promotion_dish)
10. [LOYALTY_PROGRAM](#10-loyalty_program)
11. [LOYALTY_ACCOUNT](#11-loyalty_account)
12. [LOYALTY_TRANSACTION](#12-loyalty_transaction)
13. [DISCOUNT_CODE](#13-discount_code)
14. [DISCOUNT_CODE_REDEMPTION](#14-discount_code_redemption)
15. [CUSTOMER_FAVOURITE_RESTAURANT](#15-customer_favourite_restaurant)
16. [CUSTOMER_HIDDEN_RESTAURANT](#16-customer_hidden_restaurant)
17. [CART](#17-cart)
18. [CART_ITEM](#18-cart_item)
19. [ORDER](#19-order)
20. [ORDER_ITEM](#20-order_item)
21. [ORDER_DISCOUNT](#21-order_discount)
22. [ORDER_RULE](#22-order_rule)
23. [ORDER_BLOCKED_SLOT](#23-order_blocked_slot)
24. [PAYMENT](#24-payment)
25. [REFUND](#25-refund)
26. [PROCESSED_STRIPE_EVENT](#26-processed_stripe_event)
26b. [DISPUTE](#26b-dispute)
27. [INVOICE](#27-invoice)
28. [INVOICE_LINE](#28-invoice_line)
29. [INVOICE_COUNTER](#29-invoice_counter)
30. [EVENT](#30-event)
31. [EVENT_MENU_ITEM](#31-event_menu_item)
32. [EVENT_BOOKING_POLICY](#32-event_booking_policy)
33. [RESTAURANT_RATING](#33-restaurant_rating)
34. [CUSTOMER_RATING](#34-customer_rating)
35. [NOTIFICATION](#35-notification)
36. [MODERATION_ACTION](#36-moderation_action)

---

## Enumerations

- [OrderStatus](#orderstatus)
- [PaymentStatus](#paymentstatus)
- [LoyaltyRedemptionStatus](#loyaltyredemptionstatus)
- [DiscountRedemptionStatus](#discountredemptionstatus)
- [VatRate](#vatrate)
- [InvoiceKind](#invoicekind)
- [InvoiceIssuerType](#invoiceissuertype)
- [InvoiceStatus](#invoicestatus)
- [DisputeState](#disputestate)
- [TransactionType](#transactiontype)
- [NotificationType](#notificationtype)

---

## 1. USER

Central account table for all platform participants. Role-based extensions (see `CUSTOMER_PROFILE`, `RESTAURANT_OWNER_PROFILE`) provide role-specific attributes via 1-to-1 composition.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique user identifier (UUID/ULID). |
| `email` | `string` | **NOT NULL, UNIQUE** | User's email address; used for authentication. |
| `password_hash` | `string` | **NOT NULL** | Hashed password. Raw passwords are never stored. |
| `role` | `string` (enum) | **NOT NULL** | User role. Allowed values: `CUSTOMER`, `RESTAURANT_OWNER`, `ADMIN`. |
| `status` | `string` (enum) | **NOT NULL** | Account status. Allowed values: `ACTIVE`, `SUSPENDED`, `BANNED`. |
| `first_name` | `string` | **NOT NULL** | User's first name. |
| `last_name` | `string` | **NOT NULL** | User's last name. |
| `stripe_customer_id` | `string` | NULLABLE | Stripe `Customer` object ID (`cus_...`). Set when the user first initiates a Stripe payment. Used to attach saved payment methods and retrieve Stripe-side customer data. |
| `created_at` | `datetime` | **NOT NULL** | Timestamp of account creation (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Timestamp of last update (UTC). |

**Relationships:**

- `1 → 0..1` **CUSTOMER_PROFILE** — extended profile for customers.
- `1 → 0..1` **RESTAURANT_OWNER_PROFILE** — extended profile for restaurant owners.
- `1 → 0..*` **RESTAURANT** — a restaurant owner can own multiple restaurants.
- `1 → 0..*` **CART** — a customer can have carts at multiple restaurants (one per restaurant).
- `1 → 0..*` **ORDER** — a customer can place multiple orders (immediate and scheduled).
- `1 → 0..*` **LOYALTY_ACCOUNT** — a customer can have accounts across multiple loyalty programs.
- `1 → 0..*` **CUSTOMER_FAVOURITE_RESTAURANT** — customer favourites.
- `1 → 0..*` **CUSTOMER_HIDDEN_RESTAURANT** — customer hidden restaurants.
- `1 → 0..*` **NOTIFICATION** — a user receives notifications.
- `1 → 0..*` **MODERATION_ACTION** — an admin performs moderation actions.

---

## 2. CUSTOMER_PROFILE

Extension table for users with the `CUSTOMER` role. Holds dietary preferences, allergy information, and marketing consent.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `user_id` | `integer` | **PK, FK → USER.id** | References the parent user account. |
| `allergy_notes` | `string` | NULLABLE | Free-text and/or structured JSON describing the customer's allergies. |
| `dietary_preferences` | `string` | NULLABLE | Dietary preferences (e.g. vegetarian, vegan, halal). |
| `marketing_opt_in` | `boolean` | **NOT NULL, DEFAULT false** | Whether the customer has opted in to marketing communications. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `1 → 1` **USER** — each customer profile belongs to exactly one user.

---

## 3. RESTAURANT_OWNER_PROFILE

Extension table for users with the `RESTAURANT_OWNER` role. Stores business contact and legal information.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `user_id` | `integer` | **PK, FK → USER.id** | References the parent user account. |
| `company_name` | `string` | NULLABLE | Legal or trading company name. |
| `vat_number` | `string` | NULLABLE | VAT / tax registration number. |
| `contact_phone` | `string` | NULLABLE | Business contact phone number. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `1 → 1` **USER** — each restaurant owner profile belongs to exactly one user.

---

## 4. RESTAURANT

Represents a dining establishment managed by a restaurant owner.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique restaurant identifier (UUID/ULID). |
| `owner_user_id` | `string` | **FK → USER.id, NOT NULL** | The restaurant owner who manages this restaurant. |
| `name` | `string` | **NOT NULL** | Display name of the restaurant. |
| `restaurant_type` | `string` | NULLABLE | Category / cuisine type (e.g. `ITALIAN`, `STEAKHOUSE`, `VEGAN`). |
| `description` | `string` | NULLABLE | Free-text description visible to customers. |
| `address_line1` | `string` | **NOT NULL** | Primary address line. |
| `address_line2` | `string` | NULLABLE | Secondary address line (suite, floor, etc.). |
| `city` | `string` | **NOT NULL** | City. |
| `postal_code` | `string` | **NOT NULL** | Postal / ZIP code. |
| `country` | `string` | **NOT NULL** | Country (ISO 3166-1 alpha-2 recommended). |
| `latitude` | `float` | NULLABLE | Geographic latitude for map display / proximity search. |
| `longitude` | `float` | NULLABLE | Geographic longitude for map display / proximity search. |
| `siret` | `string` | NULLABLE, MAX 14 | 14-digit SIRET number identifying the legal business entity. |
| `legal_name` | `string` | NULLABLE | Official registered company name (may differ from display `name`). |
| `legal_address` | `string` | NULLABLE | Full registered address used on invoices. |
| `legal_form` | `string` | NULLABLE | Legal form of the entity (e.g. `SARL`, `SAS`, `EI`, `Auto-entrepreneur`). |
| `is_vat_registered` | `boolean` | **NOT NULL, DEFAULT false** | Whether the restaurant is registered for VAT and must include VAT on invoices. |
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Soft-delete / visibility flag. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **USER** — each restaurant is owned by one user (restaurant owner).
- `1 → 0..*` **INVOICE** — a restaurant can be the issuer of invoices (when `issuer_type = RESTAURANT`).
- `1 → 0..*` **INVOICE** — a restaurant can be the recipient of invoices (e.g. platform billing).
- `1 → 0..*` **RESTAURANT_TABLE** — a restaurant has zero or more tables.
- `1 → 0..*` **MENU_ITEM** — a restaurant offers menu items.
- `1 → 0..*` **PROMOTION** — a restaurant can configure promotions.
- `1 → 0..*` **LOYALTY_PROGRAM** — a restaurant can define loyalty programs.
- `1 → 0..*` **DISCOUNT_CODE** — a restaurant can issue discount codes.
- `1 → 0..*` **ORDER_RULE** — a restaurant configures order rules.
- `1 → 0..*` **ORDER_BLOCKED_SLOT** — a restaurant can block order slots.
- `1 → 0..*` **CART** — customers can have carts targeting this restaurant.
- `1 → 0..*` **ORDER** — a restaurant receives orders.
- `1 → 0..*` **RESTAURANT_TRANSACTION** — a restaurant has financial transactions.
- `1 → 0..*` **CUSTOMER_FAVOURITE_RESTAURANT** — customers can favourite the restaurant.
- `1 → 0..*` **CUSTOMER_HIDDEN_RESTAURANT** — customers can hide the restaurant.

---

## 5. RESTAURANT_TABLE

A physical or logical table within a restaurant, used for seating allocation.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique table identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant this table belongs to. |
| `label` | `string` | **NOT NULL, MAX 50** | Human-readable label (e.g. `T1`, `Terrace-A`). |
| `capacity` | `int` | **NOT NULL** | Maximum number of guests the table can seat. |
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Whether the table is currently available. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each table belongs to one restaurant.
- `1 → 0..*` **ORDER** — a table can be assigned to multiple dine-in orders (over time).

---

## 6. RESTAURANT_TRANSACTION

Records financial transactions for a restaurant, including credits from delivered orders and withdrawals.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique transaction identifier. |
| `restaurant_id` | `integer` | **FK → RESTAURANT.id, NOT NULL** | The restaurant this transaction belongs to. |
| `order_id` | `integer` | **FK → ORDER.id, NULLABLE** | The order that generated this transaction. `NULL` for manual adjustments or withdrawals. |
| `type` | `string` (enum) | **NOT NULL** | Transaction type. Allowed values: `CREDIT`, `WITHDRAWAL`, `DISPUTE_REVERSAL` (100), `DISPUTE_RESTORED` (101). See [`TransactionType`](#transactiontype). |
| `gross_amount` | `decimal(9,2)` | **NOT NULL** | Gross amount before commission. |
| `commission_amount` | `decimal(9,2)` | **NOT NULL** | Platform commission deducted. |
| `net_amount` | `decimal(9,2)` | **NOT NULL** | Net amount credited or debited (`gross_amount - commission_amount`). |
| `balance_after` | `decimal(9,2)` | **NOT NULL** | Restaurant account balance after this transaction. |
| `created_at` | `datetime` | **NOT NULL** | Transaction timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each transaction belongs to one restaurant.
- `N → 0..1` **ORDER** — a transaction may reference an order.

---

## 7. MENU_ITEM

A dish or beverage offered by a restaurant. Used in orders (via `ORDER_ITEM`) and event menus (via `EVENT_MENU_ITEM`).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique menu item identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant that offers this item. |
| `name` | `string` | **NOT NULL** | Display name of the menu item. |
| `description` | `string` | NULLABLE | Description, ingredients, preparation notes. |
| `type_of_dish` | `string` (enum) | **NOT NULL** | Course category for menu structure and filtering. Allowed values: `STARTER`, `MAIN`, `DESSERT`, `APERITIF`, `BEVERAGE`. |
| `base_price` | `float` | **NOT NULL** | Standard price. Use `DECIMAL` in the physical schema for precision. |
| `vat_rate` | `string` (enum) | **NOT NULL, DEFAULT REDUCED5_5** | VAT rate applicable to this dish. Allowed values: `ZERO`, `SPECIAL2_1`, `REDUCED5_5`, `INTERMEDIATE10`, `NORMAL20`. See [`VatRate`](#vatrate). |
| `is_vegetarian` | `boolean` | **NOT NULL, DEFAULT false** | Whether the item is vegetarian. |
| `is_vegan` | `boolean` | **NOT NULL, DEFAULT false** | Whether the item is vegan. |
| `is_gluten_free` | `boolean` | **NOT NULL, DEFAULT false** | Whether the item is gluten-free. |
| `is_allergen_hazard` | `boolean` | **NOT NULL, DEFAULT false** | If `true`, the item should be hidden from customers with matching allergen profiles. |
| `is_dish_of_the_day` | `boolean` | **NOT NULL, DEFAULT false** | Highlights the item as the daily special. |
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Soft-delete / visibility flag. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each menu item belongs to one restaurant.
- `1 → 0..*` **CART_ITEM** — a menu item can be added to multiple carts.
- `1 → 0..*` **ORDER_ITEM** — a menu item can appear in multiple orders (including pre-orders for scheduled dine-in).
- `1 → 0..*` **EVENT_MENU_ITEM** — a menu item can appear in event-specific menus.
- `1 → 0..*` **PROMOTION_DISH** — a menu item can be linked to item-based promotions.

---

## 8. PROMOTION

A time-bound promotional offer configured by a restaurant (e.g. 20% off, happy hour).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique promotion identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant running this promotion. |
| `name` | `string` | **NOT NULL** | Promotion title. |
| `description` | `string` | NULLABLE | Detailed description of the offer. |
| `discount_type` | `string` (enum) | **NOT NULL** | Type of discount. Allowed values: `PERCENTAGE`, `FIXED_AMOUNT`. |
| `discount_value` | `float` | **NOT NULL** | Numeric value of the discount (percentage or fixed amount). |
| `starts_at` | `datetime` | **NOT NULL** | Start date/time of the promotion (UTC). |
| `ends_at` | `datetime` | **NOT NULL** | End date/time of the promotion (UTC). |
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Whether the promotion is currently enabled. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each promotion belongs to one restaurant.
- `1 → 0..*` **PROMOTION_DISH** — an item-based promotion can target specific dishes.

---

## 9. PROMOTION_DISH

Links an item-based promotion to a specific dish, enabling promotions that target individual menu items.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique identifier. |
| `promotion_id` | `integer` | **FK → PROMOTION.id, NOT NULL** | The promotion this link belongs to. |
| `dish_id` | `integer` | **FK → MENU_ITEM.id, NOT NULL** | The dish targeted by the promotion. |

**Relationships:**

- `N → 1` **PROMOTION** — each row belongs to one promotion.
- `N → 1` **MENU_ITEM** — each row references one menu item.

---

## 10. LOYALTY_PROGRAM

Defines a loyalty rewards program for a restaurant (e.g. points per order, tiered rewards).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique loyalty program identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant that created this program. |
| `name` | `string` | **NOT NULL** | Program display name. |
| `description` | `string` | NULLABLE | Customer-facing description of the program. |
| `rules_schema` | `string` (JSON) | NULLABLE | JSON schema defining earning and redeeming rules. |
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Whether the program is accepting new participants. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each loyalty program belongs to one restaurant.
- `1 → 0..*` **LOYALTY_ACCOUNT** — a program can have multiple enrolled customer accounts.

---

## 11. LOYALTY_ACCOUNT

Tracks a single customer's enrolment and points balance in a loyalty program.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique loyalty account identifier (UUID/ULID). |
| `loyalty_program_id` | `string` | **FK → LOYALTY_PROGRAM.id, NOT NULL** | The program this account belongs to. |
| `customer_user_id` | `string` | **FK → USER.id, NOT NULL** | The enrolled customer. |
| `points_balance` | `int` | **NOT NULL, DEFAULT 0** | Current points balance. |
| `created_at` | `datetime` | **NOT NULL** | Enrolment timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **LOYALTY_PROGRAM** — each account belongs to one program.
- `N → 1` **USER** — each account belongs to one customer.
- `1 → 0..*` **LOYALTY_TRANSACTION** — an account can have multiple point transactions.

---

## 12. LOYALTY_TRANSACTION

Ledger entry recording points earned, redeemed, or manually adjusted on a loyalty account. Entries are created with `PENDING` status and committed or reversed depending on payment outcome.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique transaction identifier (UUID/ULID). |
| `loyalty_account_id` | `string` | **FK → LOYALTY_ACCOUNT.id, NOT NULL** | The loyalty account affected. |
| `type` | `string` (enum) | **NOT NULL** | Transaction type. Allowed values: `EARN`, `REDEEM`, `ADJUST`. |
| `status` | `string` (enum) | **NOT NULL, DEFAULT PENDING** | Lifecycle state. Allowed values: `PENDING`, `COMMITTED`, `REVERSED`. See [`LoyaltyRedemptionStatus`](#loyaltyredemptionstatus). |
| `points` | `int` | **NOT NULL** | Number of points (positive for earn/adjust, negative for redeem). |
| `source` | `string` | NULLABLE | Origin reference (e.g. an order ID, or "admin adjustment"). |
| `created_at` | `datetime` | **NOT NULL** | Transaction timestamp (UTC). |

**Relationships:**

- `N → 1` **LOYALTY_ACCOUNT** — each transaction belongs to one account.

---

## 13. DISCOUNT_CODE

A reusable or limited-use discount code issued by a restaurant.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique discount code identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant that issued this code. |
| `code` | `string` | **NOT NULL, UNIQUE per restaurant** | The alphanumeric code customers enter at checkout. |
| `description` | `string` | NULLABLE | Internal or customer-facing description of the discount. |
| `discount_type` | `string` (enum) | **NOT NULL** | Type of discount. Allowed values: `PERCENTAGE`, `FIXED_AMOUNT`. |
| `discount_value` | `float` | **NOT NULL** | Numeric value of the discount. |
| `valid_from` | `datetime` | **NOT NULL** | Start of validity period (UTC). |
| `valid_until` | `datetime` | **NOT NULL** | End of validity period (UTC). |
| `max_redemptions` | `int` | NULLABLE | Maximum total number of times this code can be used. `NULL` = unlimited. |
| `per_user_limit` | `int` | NULLABLE | Maximum number of times a single user can use this code. `NULL` = unlimited. |
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Whether the code is currently enabled. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each discount code belongs to one restaurant.
- `1 → 0..*` **ORDER_DISCOUNT** — a code can be applied to multiple orders (tracked via ORDER_DISCOUNT).
- `1 → 0..*` **DISCOUNT_CODE_REDEMPTION** — tracks each use of the code by a customer.

---

## 14. DISCOUNT_CODE_REDEMPTION

Tracks each use of a discount code by a customer on an order. Redemptions are created with `PENDING` status and committed or reversed depending on payment outcome.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique redemption identifier. |
| `discount_code_id` | `integer` | **FK → DISCOUNT_CODE.id, NOT NULL** | The discount code that was redeemed. |
| `customer_user_id` | `integer` | **FK → USER.id, NOT NULL** | The customer who redeemed the code. |
| `order_id` | `integer` | **FK → ORDER.id, NOT NULL** | The order the code was applied to. |
| `status` | `string` (enum) | **NOT NULL, DEFAULT PENDING** | Lifecycle state. Allowed values: `PENDING`, `COMMITTED`, `REVERSED`. See [`DiscountRedemptionStatus`](#discountredemptionstatus). |
| `created_at` | `datetime` | **NOT NULL** | Redemption timestamp (UTC). |

**Relationships:**

- `N → 1` **DISCOUNT_CODE** — each redemption references one discount code.
- `N → 1` **USER** — each redemption references one customer.
- `N → 1` **ORDER** — each redemption references one order.

---

## 15. CUSTOMER_FAVOURITE_RESTAURANT

Join table recording which restaurants a customer has marked as favourites.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `customer_user_id` | `string` | **FK → USER.id, PK (composite)** | The customer who favourited the restaurant. |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, PK (composite)** | The favourited restaurant. |
| `created_at` | `datetime` | **NOT NULL** | Timestamp when the favourite was created (UTC). |

**Relationships:**

- `N → 1` **USER** — each row references one customer.
- `N → 1` **RESTAURANT** — each row references one restaurant.

---

## 16. CUSTOMER_HIDDEN_RESTAURANT

Join table recording which restaurants a customer has chosen to hide from their feed.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `customer_user_id` | `string` | **FK → USER.id, PK (composite)** | The customer who hid the restaurant. |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, PK (composite)** | The hidden restaurant. |
| `created_at` | `datetime` | **NOT NULL** | Timestamp when the restaurant was hidden (UTC). |

**Relationships:**

- `N → 1` **USER** — each row references one customer.
- `N → 1` **RESTAURANT** — each row references one restaurant.

---

## 17. CART

Persistent shopping cart, one per customer per restaurant. Allows customers to build an order across sessions before checking out.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique cart identifier. |
| `customer_user_id` | `integer` | **FK → USER.id, NOT NULL** | The customer who owns this cart. |
| `restaurant_id` | `integer` | **FK → RESTAURANT.id, NOT NULL** | The restaurant this cart targets. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Constraints:**

- `UNIQUE (customer_user_id, restaurant_id)` — one cart per customer per restaurant.

**Relationships:**

- `N → 1` **USER** — each cart belongs to one customer.
- `N → 1` **RESTAURANT** — each cart targets one restaurant.
- `1 → 0..*` **CART_ITEM** — a cart contains zero or more items.

---

## 18. CART_ITEM

A line item in a cart, linking a cart to a menu item with quantity, price snapshot, and optional special instructions.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique cart item identifier. |
| `cart_id` | `integer` | **FK → CART.id, NOT NULL** | The parent cart. |
| `dish_id` | `integer` | **FK → MENU_ITEM.id, NOT NULL** | The menu item added to the cart. |
| `quantity` | `int` | **NOT NULL, DEFAULT 1** | Number of units. Must be ≥ 1 and ≤ 99. |
| `unit_price` | `decimal(7,2)` | **NOT NULL** | Price per unit at the time of adding (snapshot). |
| `special_instructions` | `string` | NULLABLE, MAX 500 | Customer notes (allergies, preferences, etc.). |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Constraints:**

- `UNIQUE (cart_id, dish_id)` — a dish appears at most once per cart (quantity is incremented on re-add).
- `ON DELETE RESTRICT` on `dish_id` — prevents deleting a dish while it is in a cart.

**Relationships:**

- `N → 1` **CART** — each item belongs to one cart. Cascade delete: removing a cart removes all its items.
- `N → 1` **MENU_ITEM** — each item references one menu item.

---

## 19. ORDER

A confirmed customer order. Supports two order types: delivery (food brought to an address) and dine-in (table at the restaurant). Created from a cart at checkout; the cart is deleted upon successful order creation. Orders can be immediate or scheduled (via `scheduled_at`) and optionally linked to an event.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique order identifier. |
| `customer_user_id` | `integer` | **FK → USER.id, NOT NULL** | The customer who placed the order. |
| `restaurant_id` | `integer` | **FK → RESTAURANT.id, NOT NULL** | The restaurant fulfilling the order. |
| `order_type` | `string` (enum) | **NOT NULL** | Fulfilment mode. Allowed values: `DELIVERY`, `DINE_IN`. |
| `status` | `string` (enum) | **NOT NULL** | Order lifecycle state. Allowed values: `AWAITING_PAYMENT`, `PENDING`, `CONFIRMED`, `PREPARING`, `READY`, `DELIVERING`, `DELIVERED`, `CANCELLED`. `AWAITING_PAYMENT` is the initial state when a Stripe PaymentIntent is created but not yet confirmed. See [`OrderStatus`](#orderstatus). |
| `payment_status` | `string` (enum) | **NOT NULL** | Payment lifecycle state. Allowed values: `PENDING`, `AUTHORIZED`, `COMPLETED`, `FAILED`, `REFUNDED`, `PARTIALLY_REFUNDED`. `AUTHORIZED` means the card has been authorized but not yet captured. See [`PaymentStatus`](#paymentstatus). |
| `total_amount` | `decimal(9,2)` | **NOT NULL** | Sum of all order item subtotals. |
| `guest_count` | `int` | **NOT NULL, DEFAULT 1** | Number of people dining (1–50). For delivery: how many will eat. For dine-in: how many seats to prepare. |
| `delivery_address` | `string` | NULLABLE, MAX 500 | Required for `DELIVERY` orders. Empty for `DINE_IN`. |
| `notes` | `string` | NULLABLE, MAX 500 | Free-text notes (delivery instructions, seating preferences, etc.). |
| `source` | `string` (enum) | **NOT NULL** | Channel the order was placed from. Allowed values: `CUSTOMER_APP`, `RESTAURANT_PORTAL`, `ADMIN`. |
| `scheduled_at` | `datetime` | NULLABLE | Date and time of the reservation or scheduled delivery (UTC). `NULL` for immediate orders. |
| `restaurant_table_id` | `integer` | **FK → RESTAURANT_TABLE.id, NULLABLE** | Assigned table for dine-in orders. `NULL` for delivery or unassigned. |
| `is_event_booking` | `boolean` | **NOT NULL, DEFAULT false** | Whether this order is associated with an event. |
| `event_id` | `integer` | **FK → EVENT.id, NULLABLE** | Link to an event for event bookings. `NULL` for standard orders. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Indexes:**

- `IX_Orders_CustomerId` — accelerates customer order history queries.
- `IX_Orders_RestaurantId` — accelerates restaurant order dashboard queries.
- `IX_Orders_Status` — accelerates filtering by order status.

**Relationships:**

- `N → 1` **USER** — each order belongs to one customer. `ON DELETE RESTRICT`.
- `N → 1` **RESTAURANT** — each order targets one restaurant. `ON DELETE RESTRICT`.
- `N → 0..1` **RESTAURANT_TABLE** — a dine-in order may be assigned to a table.
- `N → 0..1` **EVENT** — an event-linked order references the event.
- `1 → 0..*` **ORDER_ITEM** — an order contains one or more line items.
- `1 → 0..*` **ORDER_DISCOUNT** — an order can have discounts applied from various sources.
- `1 → 0..*` **PAYMENT** — an order can have one or more payment attempts.
- `1 → 0..*` **RESTAURANT_RATING** — an order can yield a restaurant rating.
- `1 → 0..*` **CUSTOMER_RATING** — an order can yield a customer rating.
- `1 → 0..*` **RESTAURANT_TRANSACTION** — a delivered order generates a credit transaction.
- `1 → 0..*` **INVOICE** — a completed order can trigger one or more invoices (original + credit notes).

---

## 20. ORDER_ITEM

A line item in an order. Snapshots the dish name and price at order time so the order remains accurate even if the menu changes later.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique order item identifier. |
| `order_id` | `integer` | **FK → ORDER.id, NOT NULL** | The parent order. |
| `dish_id` | `integer` | **FK → MENU_ITEM.id, NOT NULL** | The menu item that was ordered. |
| `dish_name` | `string` | **NOT NULL, MAX 255** | Snapshot of the dish name at order time. |
| `quantity` | `int` | **NOT NULL, DEFAULT 1** | Number of units ordered. Must be ≥ 1. |
| `unit_price` | `decimal(7,2)` | **NOT NULL** | Price per unit at order time (snapshot). |
| `special_instructions` | `string` | NULLABLE, MAX 500 | Customer notes for this specific item. |

**Relationships:**

- `N → 1` **ORDER** — each item belongs to one order. Cascade delete: removing an order removes all its items.
- `N → 1` **MENU_ITEM** — each item references one menu item. `ON DELETE RESTRICT`.

---

## 21. ORDER_DISCOUNT

Tracks discounts applied to an order from various sources (promotions, discount codes, loyalty points).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique identifier. |
| `order_id` | `integer` | **FK → ORDER.id, NOT NULL** | The order this discount was applied to. |
| `source` | `string` (enum) | **NOT NULL** | Source of the discount. Allowed values: `PROMOTION`, `DISCOUNT_CODE`, `LOYALTY_POINTS`. |
| `source_id` | `integer` | NULLABLE | FK to the source entity (promotion, discount code, or loyalty account). |
| `description` | `string` | **NOT NULL, MAX 200** | Human-readable description of the discount. |
| `amount` | `decimal(9,2)` | **NOT NULL** | Monetary amount of the discount. |

**Relationships:**

- `N → 1` **ORDER** — each discount belongs to one order.

---

## 22. ORDER_RULE

Configurable rules that govern how orders work for a given restaurant (lead time, advance window, slot duration, availability, pre-order, delivery, minimum confirmation amount).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique order rule identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant these rules apply to. |
| `min_confirm_amount` | `float` | NULLABLE | Minimum prepayment amount required to confirm an order. |
| `min_lead_time_hours` | `int` | NULLABLE | No order allowed before this many hours in advance (e.g. 24 = order at least 24h ahead). |
| `max_advance_days` | `int` | NULLABLE | No order allowed beyond this many days in the future (e.g. 30 = up to 30 days ahead). |
| `slot_duration_minutes` | `int` | NULLABLE | Duration of one bookable slot in minutes (e.g. 120 for 2-hour slots). |
| `availability_ranges` | `string` (JSON) | NULLABLE | Time ranges when the restaurant accepts orders, per day (e.g. opening hours per weekday). |
| `allow_preorder` | `boolean` | **NOT NULL, DEFAULT false** | Whether customers can pre-order menu items with their order. |
| `allow_delivery` | `boolean` | **NOT NULL, DEFAULT false** | Whether delivery is supported for orders at this restaurant. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each order rule set belongs to one restaurant.

---

## 23. ORDER_BLOCKED_SLOT

Represents a time range during which a restaurant (or a specific table) is unavailable for orders (e.g. maintenance, private events, holidays).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique blocked slot identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant the slot belongs to. |
| `restaurant_table_id` | `string` | **FK → RESTAURANT_TABLE.id, NULLABLE** | If set, only this table is blocked. `NULL` = entire restaurant is blocked. |
| `starts_at` | `datetime` | **NOT NULL** | Start of blocked period (UTC). |
| `ends_at` | `datetime` | **NOT NULL** | End of blocked period (UTC). |
| `reason` | `string` | NULLABLE | Explanation for the block (internal use). |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each blocked slot belongs to one restaurant.
- `N → 0..1` **RESTAURANT_TABLE** — optionally scoped to a single table.

---

## 24. PAYMENT

Records a payment transaction against an order. Designed to be Stripe-ready: persists Stripe identifiers while never storing raw card data.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique payment identifier (UUID/ULID). |
| `order_id` | `string` | **FK → ORDER.id, NOT NULL** | The order this payment is for. |
| `provider` | `string` | **NOT NULL** | Payment provider name. Current value: `STRIPE`. |
| `stripe_payment_intent_id` | `string` | NULLABLE | Stripe `PaymentIntent` ID (e.g. `pi_...`). |
| `stripe_charge_id` | `string` | NULLABLE | Stripe `Charge` ID, populated depending on Stripe integration mode. |
| `amount` | `float` | **NOT NULL** | Payment amount. Use `DECIMAL` in physical schema. |
| `currency` | `string` | **NOT NULL** | ISO 4217 currency code (e.g. `EUR`, `USD`). |
| `status` | `string` (enum) | **NOT NULL** | Payment lifecycle state. Allowed values: `REQUIRES_PAYMENT_METHOD`, `REQUIRES_CONFIRMATION`, `AUTHORIZED`, `SUCCEEDED`, `CANCELED`, `REFUNDED`. `AUTHORIZED` indicates the PaymentIntent has been confirmed and funds are on hold pending capture. |
| `authorized_at` | `datetime` | NULLABLE | Timestamp when the payment was authorized (UTC). |
| `captured_at` | `datetime` | NULLABLE | Timestamp when the payment was captured (UTC). |
| `canceled_at` | `datetime` | NULLABLE | Timestamp when the payment was canceled (UTC). |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **ORDER** — each payment belongs to one order.
- `1 → 0..*` **REFUND** — a payment can have zero or more refunds (supporting partial refunds).

---

## 25. REFUND

Records a Stripe refund issued against a payment. Supports both full and partial refunds; multiple `REFUND` rows against the same `PAYMENT` result in `PaymentStatus.PARTIALLY_REFUNDED` until the total refunded equals the original amount.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique refund identifier (UUID/ULID). |
| `payment_id` | `string` | **FK → PAYMENT.id, NOT NULL** | The payment this refund is applied to. |
| `stripe_refund_id` | `string` | NULLABLE | Stripe `Refund` object ID (`re_...`). Populated when Stripe processes the refund. |
| `amount` | `decimal(9,2)` | **NOT NULL** | Refunded amount. Must be > 0 and ≤ remaining refundable amount on the payment. |
| `currency` | `string` | **NOT NULL** | ISO 4217 currency code (e.g. `EUR`, `USD`). Must match the parent payment's currency. |
| `reason` | `string` (enum) | NULLABLE | Reason for the refund. Allowed values: `REQUESTED_BY_CUSTOMER`, `DUPLICATE`, `FRAUDULENT`, `OTHER`. |
| `status` | `string` (enum) | **NOT NULL** | Refund lifecycle state. Allowed values: `PENDING`, `SUCCEEDED`, `FAILED`, `CANCELED`. |
| `created_by_user_id` | `string` | **FK → USER.id, NULLABLE** | The admin or system actor who initiated the refund. `NULL` for automated refunds. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **PAYMENT** — each refund belongs to one payment.
- `N → 0..1` **USER** — optionally references the admin who initiated the refund.

---

## 26. PROCESSED_STRIPE_EVENT

Idempotency log for Stripe webhook events. Before processing any incoming event, the system checks this table; if the `id` already exists, the event is skipped. This prevents duplicate side-effects caused by Stripe retrying delivery.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Stripe event ID (`evt_...`). Used as the natural primary key to guarantee uniqueness and enable fast lookup. |
| `event_type` | `string` | **NOT NULL** | Stripe event type string (e.g. `payment_intent.succeeded`, `charge.refunded`). |
| `processed_at` | `datetime` | **NOT NULL** | Timestamp when the event was successfully processed (UTC). |

**Notes:**

- No foreign keys: this table is intentionally standalone and must remain lightweight for fast idempotency checks.
- Rows are insert-only; there is no update or delete path.

---

## 26b. DISPUTE

Tracks Stripe disputes (chargebacks) end-to-end. Rows are upserted by `stripe_dispute_id` from `charge.dispute.*` webhook events. When a dispute opens, the platform immediately debits the restaurant balance via a `RESTAURANT_TRANSACTION` of type `DISPUTE_REVERSAL`; a won dispute restores the balance via `DISPUTE_RESTORED`, a lost dispute leaves the reversal in place. Evidence submission remains out-of-band in the Stripe dashboard.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique dispute identifier. |
| `stripe_dispute_id` | `string(200)` | **NOT NULL, UNIQUE** | Stripe Dispute object ID (`dp_...`). Natural idempotency key for webhook handling. |
| `payment_id` | `integer` | **FK → PAYMENT.id, NOT NULL** | The payment being disputed. `ON DELETE RESTRICT`. |
| `order_id` | `integer` | **FK → ORDER.id, NOT NULL** | Denormalized from `PAYMENT` for direct lookup by order. `ON DELETE RESTRICT`. |
| `restaurant_id` | `integer` | **FK → RESTAURANT.id, NOT NULL** | Denormalized from `ORDER` for fast restaurant-scoped queries. `ON DELETE RESTRICT`. |
| `amount` | `decimal(9,2)` | **NOT NULL** | Disputed amount (may be partial vs. the underlying payment). |
| `currency` | `string(3)` | **NOT NULL, DEFAULT `EUR`** | ISO 4217 currency code. |
| `reason_code` | `string(60)` | **NOT NULL** | Raw Stripe reason string (`fraudulent`, `product_not_received`, etc.). |
| `state` | `string` (enum) | **NOT NULL, DEFAULT `OPEN`** | Dispute lifecycle state. Allowed values: `OPEN`, `WON`, `LOST`. See [`DisputeState`](#disputestate). |
| `due_by` | `datetime` | NULLABLE | Stripe evidence submission deadline (UTC). |
| `opened_at` | `datetime` | **NOT NULL** | Stripe `created` timestamp (UTC). |
| `closed_at` | `datetime` | NULLABLE | Timestamp when the dispute reached `WON` or `LOST` (UTC). |
| `stripe_payload` | `string(8000)` | NOT NULL | Last-known JSON snapshot of the Stripe dispute object (debug / audit). |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Indexes:**

- `UNIQUE (stripe_dispute_id)` — idempotency key for webhook upsert.
- `(payment_id)`, `(order_id)`, `(restaurant_id)` — direct-lookup indexes.
- `(restaurant_id, state)` — composite index powering the open-dispute refund guard and restaurant-scoped filters.

**Relationships:**

- `N → 1` **PAYMENT** — each dispute targets exactly one payment.
- `N → 1` **ORDER** — denormalized; matches `payment.order_id`.
- `N → 1` **RESTAURANT** — denormalized; matches `order.restaurant_id`.
- Dispute lifecycle events emit `RESTAURANT_TRANSACTION` rows with type `DISPUTE_REVERSAL` (on open) and `DISPUTE_RESTORED` (on win); no direct FK is stored on `RESTAURANT_TRANSACTION`.

---

## 27. INVOICE

A legal billing document issued after a payment event. An invoice may be issued by the platform (charging the restaurant a commission) or by the restaurant (billing the customer). Credit notes are represented as `INVOICE` rows with `kind = CREDIT_NOTE` that reference the original invoice.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique invoice identifier (UUID/ULID). |
| `invoice_number` | `string` | **NOT NULL, UNIQUE** | Human-readable sequential reference (e.g. `FAC-2025-00042`). Generated via `INVOICE_COUNTER`. |
| `kind` | `string` (enum) | **NOT NULL** | Document type. Allowed values: `ORDER_INVOICE`, `CREDIT_NOTE`. See [`InvoiceKind`](#invoicekind). |
| `issuer_type` | `string` (enum) | **NOT NULL** | Whether the platform or a restaurant is the legal issuer. Allowed values: `PLATFORM`, `RESTAURANT`. See [`InvoiceIssuerType`](#invoiceissuertype). |
| `issuer_restaurant_id` | `string` | **FK → RESTAURANT.id, NULLABLE** | Set when `issuer_type = RESTAURANT`. References the issuing restaurant. |
| `recipient_user_id` | `string` | **FK → USER.id, NULLABLE** | Set when the invoice recipient is a customer. Mutually exclusive with `recipient_restaurant_id`. |
| `recipient_restaurant_id` | `string` | **FK → RESTAURANT.id, NULLABLE** | Set when the invoice recipient is a restaurant (e.g. platform billing). Mutually exclusive with `recipient_user_id`. |
| `order_id` | `integer` | **FK → ORDER.id, NULLABLE** | The order that triggered this invoice. `NULL` for manual invoices. |
| `related_invoice_id` | `string` | **FK → INVOICE.id, NULLABLE** | For credit notes: references the original invoice being cancelled or corrected. |
| `status` | `string` (enum) | **NOT NULL, DEFAULT DRAFT** | Invoice lifecycle state. Allowed values: `DRAFT`, `ISSUED`, `CANCELLED`. See [`InvoiceStatus`](#invoicestatus). |
| `currency` | `string` | **NOT NULL** | ISO 4217 currency code (e.g. `EUR`). |
| `subtotal_ht` | `decimal(9,2)` | **NOT NULL** | Sum of all line `line_total_ht` values (excl. VAT). |
| `vat_amount` | `decimal(9,2)` | **NOT NULL** | Total VAT across all lines. |
| `total_ttc` | `decimal(9,2)` | **NOT NULL** | Grand total inclusive of VAT (`subtotal_ht + vat_amount`). |
| `notes` | `string` | NULLABLE | Free-text annotations printed on the invoice (payment terms, legal mentions, etc.). |
| `issued_at` | `datetime` | NULLABLE | Timestamp when the invoice was finalised and sent (UTC). `NULL` while in `DRAFT`. |
| `due_at` | `datetime` | NULLABLE | Payment due date (UTC). Applicable for B2B invoices. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Constraints:**

- Exactly one of `recipient_user_id` or `recipient_restaurant_id` must be set.
- `related_invoice_id` must be `NULL` unless `kind = CREDIT_NOTE`.
- `issuer_restaurant_id` must be set when `issuer_type = RESTAURANT`, and `NULL` when `issuer_type = PLATFORM`.

**Relationships:**

- `N → 0..1` **ORDER** — each invoice may reference one triggering order.
- `N → 0..1` **RESTAURANT** (issuer) — when issued by a restaurant.
- `N → 0..1` **USER** (recipient) — when billing a customer.
- `N → 0..1` **RESTAURANT** (recipient) — when billing a restaurant.
- `N → 0..1` **INVOICE** — credit note references the original invoice.
- `1 → 1..*` **INVOICE_LINE** — an invoice contains one or more line items (cascade delete).

---

## 28. INVOICE_LINE

A single billable line item within an invoice. Each line carries its own VAT rate to support mixed-rate invoices (e.g. food at 5.5% and alcohol at 20%).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `integer` | **PK** | Unique line item identifier. |
| `invoice_id` | `string` | **FK → INVOICE.id, NOT NULL** | The parent invoice. Cascade delete: removing an invoice removes all its lines. |
| `description` | `string` | **NOT NULL, MAX 500** | Human-readable label for the line (e.g. dish name, delivery fee, commission). |
| `quantity` | `int` | **NOT NULL, DEFAULT 1** | Number of units. Must be ≥ 1. |
| `unit_price_ht` | `decimal(9,4)` | **NOT NULL** | Unit price excluding VAT. Four decimal places to avoid rounding loss before aggregation. |
| `vat_rate` | `string` (enum) | **NOT NULL** | VAT rate for this line. Allowed values: `ZERO`, `SPECIAL2_1`, `REDUCED5_5`, `INTERMEDIATE10`, `NORMAL20`. See [`VatRate`](#vatrate). |
| `vat_amount` | `decimal(9,2)` | **NOT NULL** | Computed VAT amount for this line (`line_total_ht × rate`). |
| `line_total_ht` | `decimal(9,2)` | **NOT NULL** | Total excluding VAT (`quantity × unit_price_ht`). |
| `order_item_id` | `integer` | **FK → ORDER_ITEM.id, NULLABLE** | Source `ORDER_ITEM` if this line was generated from an order line. `NULL` for non-order lines (fees, adjustments). |

**Relationships:**

- `N → 1` **INVOICE** — each line belongs to one invoice.
- `N → 0..1` **ORDER_ITEM** — optionally traces back to the originating order item.

---

## 29. INVOICE_COUNTER

Lightweight sequence table used to generate monotonically increasing, gap-free invoice numbers per year and prefix. A single row is locked (`SELECT FOR UPDATE`) and incremented atomically at issuance time.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `year` | `integer` | **PK (composite with prefix)** | 4-digit calendar year (e.g. `2025`). |
| `prefix` | `string` | **PK (composite with year)** | Invoice series prefix (e.g. `FAC` for customer invoices, `AVO` for credit notes). |
| `last_sequence` | `integer` | **NOT NULL, DEFAULT 0** | The last sequence number issued for this year/prefix combination. Incremented by 1 on each issuance. |

**Notes:**

- The formatted invoice number is assembled as `{prefix}-{year}-{last_sequence:D5}` (e.g. `FAC-2025-00042`).
- Rows are created on first use (year/prefix pair) and never deleted.
- No foreign keys: this table is intentionally self-contained.

---

## 30. EVENT

A special dining or social event, optionally hosted at a restaurant. Events reuse the `ORDER` entity for reservations and can have custom menus and booking policies.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique event identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NULLABLE** | The host restaurant. `NULL` for customer-hosted or off-site events. |
| `created_by_user_id` | `string` | **FK → USER.id, NOT NULL** | The user who created the event. |
| `name` | `string` | **NOT NULL** | Event title. |
| `description` | `string` | NULLABLE | Detailed event description. |
| `starts_at` | `datetime` | **NOT NULL** | Event start date/time (UTC). |
| `ends_at` | `datetime` | **NOT NULL** | Event end date/time (UTC). |
| `max_guests` | `int` | NULLABLE | Maximum number of attendees. `NULL` = no cap. |
| `visibility` | `string` (enum) | **NOT NULL** | Who can see the event. Allowed values: `PUBLIC`, `PRIVATE`. |
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Soft-delete / visibility flag. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 0..1` **RESTAURANT** — optionally hosted at a restaurant.
- `N → 1` **USER** — created by one user.
- `1 → 0..*` **ORDER** — attendees register via orders with `is_event_booking = true`.
- `1 → 0..*` **EVENT_MENU_ITEM** — event-specific menu.
- `1 → 0..*` **EVENT_BOOKING_POLICY** — custom booking/payment rules.

---

## 31. EVENT_MENU_ITEM

Links a menu item to an event with an optional price override (e.g. prix-fixe pricing, event-only specials).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique event menu item identifier (UUID/ULID). |
| `event_id` | `string` | **FK → EVENT.id, NOT NULL** | The parent event. |
| `menu_item_id` | `string` | **FK → MENU_ITEM.id, NOT NULL** | The menu item included in the event. |
| `override_price` | `float` | NULLABLE | If set, overrides the menu item's `base_price` for this event. |

**Relationships:**

- `N → 1` **EVENT** — each row belongs to one event.
- `N → 1` **MENU_ITEM** — each row references one menu item.

---

## 32. EVENT_BOOKING_POLICY

Defines per-event booking and payment rules (minimum prepayment, custom policy schemas) that override or extend the restaurant's default `ORDER_RULE`.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique policy identifier (UUID/ULID). |
| `event_id` | `string` | **FK → EVENT.id, NOT NULL** | The event this policy applies to. |
| `min_confirm_amount` | `float` | NULLABLE | Minimum prepayment required to confirm an event order. |
| `policy_schema` | `string` (JSON) | NULLABLE | JSON describing per-event booking and payment rules. |

**Relationships:**

- `N → 1` **EVENT** — each policy belongs to one event.

---

## 33. RESTAURANT_RATING

A rating left by a customer for a restaurant following a completed order.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique rating identifier (UUID/ULID). |
| `order_id` | `string` | **FK → ORDER.id, NOT NULL** | The order this rating is associated with. |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant being rated. |
| `customer_user_id` | `string` | **FK → USER.id, NOT NULL** | The customer who submitted the rating. |
| `rating` | `int` | **NOT NULL** | Numeric score (e.g. 1–5). |
| `comment` | `string` | NULLABLE | Optional free-text review. |
| `created_at` | `datetime` | **NOT NULL** | Rating submission timestamp (UTC). |

**Relationships:**

- `N → 1` **ORDER** — each rating is linked to one order.
- `N → 1` **RESTAURANT** — each rating targets one restaurant.
- `N → 1` **USER** — each rating is authored by one customer.

---

## 34. CUSTOMER_RATING

A rating left by a restaurant (owner/staff) for a customer following a completed order (e.g. no-show, good guest).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique rating identifier (UUID/ULID). |
| `order_id` | `string` | **FK → ORDER.id, NOT NULL** | The order this rating is associated with. |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant providing the rating. |
| `rated_customer_user_id` | `string` | **FK → USER.id, NOT NULL** | The customer being rated. |
| `restaurant_user_id` | `string` | **FK → USER.id, NOT NULL** | The restaurant-side user who submitted the rating. |
| `rating` | `int` | **NOT NULL** | Numeric score (e.g. 1–5). |
| `comment` | `string` | NULLABLE | Optional free-text feedback. |
| `created_at` | `datetime` | **NOT NULL** | Rating submission timestamp (UTC). |

**Relationships:**

- `N → 1` **ORDER** — each rating is linked to one order.
- `N → 1` **RESTAURANT** — each rating comes from one restaurant.
- `N → 1` **USER** (rated_customer_user_id) — the customer being rated.
- `N → 1` **USER** (restaurant_user_id) — the restaurant user who submitted the rating.

---

## 35. NOTIFICATION

A notification delivered to a user (push, email, in-app, etc.) triggered by system events.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique notification identifier (UUID/ULID). |
| `user_id` | `string` | **FK → USER.id, NOT NULL** | The recipient user. |
| `type` | `string` (enum) | **NOT NULL** | Notification category. Allowed values: `ORDER_STATUS`, `PAYMENT_STATUS`, `EVENT_UPDATE`, `SYSTEM`, `DISPUTE` (100). See [`NotificationType`](#notificationtype). |
| `payload` | `string` (JSON) | **NOT NULL** | JSON object carrying contextual data (order ID, amounts, messages, etc.). |
| `is_read` | `boolean` | **NOT NULL, DEFAULT false** | Whether the user has acknowledged/read the notification. |
| `created_at` | `datetime` | **NOT NULL** | Notification creation timestamp (UTC). |

**Relationships:**

- `N → 1` **USER** — each notification is sent to one user.

---

## 36. MODERATION_ACTION

Audit log of administrative actions taken on platform content or users (approvals, rejections, bans, warnings).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique moderation action identifier (UUID/ULID). |
| `admin_user_id` | `string` | **FK → USER.id, NOT NULL** | The admin who performed the action. |
| `target_type` | `string` (enum) | **NOT NULL** | Entity type being moderated. Allowed values: `RESTAURANT`, `MENU_ITEM`, `EVENT`, `USER`. |
| `target_id` | `string` | **NOT NULL** | Identifier of the moderated entity (polymorphic reference). |
| `action_type` | `string` (enum) | **NOT NULL** | The moderation action taken. Allowed values: `APPROVE`, `REJECT`, `BAN`, `WARN`, `UNLIST`. |
| `reason` | `string` | NULLABLE | Justification for the action (for audit purposes). |
| `created_at` | `datetime` | **NOT NULL** | Action timestamp (UTC). Immutable. |

**Relationships:**

- `N → 1` **USER** — each action is performed by one admin user.

---

## Enumerations

### OrderStatus

Lifecycle states for an `ORDER`. New states introduced for Stripe integration are marked.

| Value | Description |
|---|---|
| `AWAITING_PAYMENT` | **New (Stripe).** Initial state when the order is created and a Stripe PaymentIntent has been initiated but payment has not yet been confirmed by the customer. |
| `PENDING` | Payment confirmed; order is awaiting acceptance by the restaurant. |
| `CONFIRMED` | Restaurant has accepted the order. |
| `PREPARING` | Kitchen is actively preparing the order. |
| `READY` | Order is ready for pick-up or delivery. |
| `DELIVERING` | Order is in transit (delivery orders only). |
| `DELIVERED` | Order has been delivered or served. |
| `CANCELLED` | Order was cancelled (by customer, restaurant, or admin). |
| `REFUSED` | Restaurant rejected the order. |

---

### PaymentStatus

Lifecycle states for the `payment_status` column on `ORDER`. New states introduced for Stripe integration are marked.

| Value | Description |
|---|---|
| `PENDING` | No payment action taken yet. |
| `AUTHORIZED` | **New (Stripe).** Card has been authorized (funds on hold) but not yet captured. Used with Stripe's manual capture flow. |
| `COMPLETED` | Payment has been successfully captured and settled. |
| `FAILED` | Payment attempt failed. |
| `REFUNDED` | Full refund has been issued. |
| `PARTIALLY_REFUNDED` | **New (Stripe).** One or more partial refunds have been issued but the total refunded amount is less than the original charge. |

---

### LoyaltyRedemptionStatus

Lifecycle states for the `status` column on `LOYALTY_TRANSACTION`. Introduced to support Stripe's payment authorization / capture flow, where points should only be definitively awarded or deducted once payment is confirmed.

| Value | Description |
|---|---|
| `PENDING` | Transaction has been reserved (e.g. points deducted from balance tentatively) but not yet finalized. |
| `COMMITTED` | Payment confirmed; points change is permanent. |
| `REVERSED` | Payment failed or order cancelled; the points operation has been undone. |

---

### DiscountRedemptionStatus

Lifecycle states for the `status` column on `DISCOUNT_CODE_REDEMPTION`. Mirrors `LoyaltyRedemptionStatus` to ensure discount code quota counters are only incremented once payment is confirmed.

| Value | Description |
|---|---|
| `PENDING` | Redemption has been reserved (counted against quota tentatively) but not yet finalized. |
| `COMMITTED` | Payment confirmed; the redemption is permanent and counts against the code's quota. |
| `REVERSED` | Payment failed or order cancelled; the redemption has been rolled back and the quota slot released. |

---

### VatRate

VAT rates applicable to menu items and invoice lines, reflecting French VAT legislation.

| Value | Decimal rate | Description |
|---|---|---|
| `ZERO` | 0 % | Zero-rated supplies (exports, certain social-sector goods). |
| `SPECIAL2_1` | 2.1 % | Special reduced rate (press publications, certain medicines). |
| `REDUCED5_5` | 5.5 % | Reduced rate (most food products, non-alcoholic beverages). |
| `INTERMEDIATE10` | 10 % | Intermediate rate (restaurant meals consumed on premises, take-away hot food, alcoholic beverages in restaurants). |
| `NORMAL20` | 20 % | Standard rate (alcohol sold off-premises, non-food items, services). |

---

### InvoiceKind

Distinguishes the nature of an `INVOICE` document.

| Value | Description |
|---|---|
| `ORDER_INVOICE` | Standard invoice issued following a completed order payment. |
| `CREDIT_NOTE` | Credit note (avoir) that partially or fully cancels a previous `ORDER_INVOICE`. References the original via `related_invoice_id`. |

---

### InvoiceIssuerType

Identifies the legal entity issuing an `INVOICE`.

| Value | Description |
|---|---|
| `PLATFORM` | The DeliverTable platform is the issuer (e.g. commission invoice billed to the restaurant). |
| `RESTAURANT` | A restaurant is the issuer (e.g. customer invoice for a completed order). |

---

### InvoiceStatus

Lifecycle states for an `INVOICE`.

| Value | Description |
|---|---|
| `DRAFT` | Invoice is being prepared and has not yet been sent. `issued_at` is `NULL`. |
| `ISSUED` | Invoice has been finalised, numbered, and sent to the recipient. `issued_at` is set. |
| `CANCELLED` | Invoice has been voided (typically superseded by a credit note). |

---

### DisputeState

Lifecycle states for a `DISPUTE`.

| Value | Description |
|---|---|
| `OPEN` | Dispute has been opened by Stripe (maps to Stripe `needs_response` / `under_review`). Restaurant balance has been debited via a `DISPUTE_REVERSAL` transaction. |
| `WON` | Dispute was resolved in the platform's favour. The balance is restored via a `DISPUTE_RESTORED` transaction. |
| `LOST` | Dispute was resolved against the platform. The reversal remains in place and (optionally) credit notes are generated via the invoicing service. |

---

### TransactionType

Transaction categories for `RESTAURANT_TRANSACTION.type`. Disputes extend the enum with reserved explicit values (100+) to leave room for future transaction kinds.

| Value | Numeric | Description |
|---|---|---|
| `CREDIT` | 0 | Credit from a successfully delivered order. |
| `WITHDRAWAL` | 1 | Funds withdrawn by the restaurant. |
| `DISPUTE_REVERSAL` | 100 | Debit applied when a Stripe dispute opens on an order. `net_amount` is negative (`-dispute.amount`). |
| `DISPUTE_RESTORED` | 101 | Credit applied when a Stripe dispute is won. `net_amount` mirrors the reversal with the opposite sign. |

---

### NotificationType

Categories for `NOTIFICATION.type`. Dispute notifications use an explicit value (100) to leave room for future notification kinds.

| Value | Numeric | Description |
|---|---|---|
| `ORDER_STATUS` | 0 | Order lifecycle changes. |
| `PAYMENT_STATUS` | 1 | Payment authorisation/capture/refund events. |
| `EVENT_UPDATE` | 2 | Updates to `EVENT` bookings. |
| `SYSTEM` | 3 | Generic system notifications. |
| `DISPUTE` | 100 | Stripe dispute events (open/won/lost). Raised for all admins and the restaurant owner. |
