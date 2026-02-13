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
6. [MENU_ITEM](#6-menu_item)
7. [PROMOTION](#7-promotion)
8. [LOYALTY_PROGRAM](#8-loyalty_program)
9. [LOYALTY_ACCOUNT](#9-loyalty_account)
10. [LOYALTY_TRANSACTION](#10-loyalty_transaction)
11. [DISCOUNT_CODE](#11-discount_code)
12. [CUSTOMER_FAVOURITE_RESTAURANT](#12-customer_favourite_restaurant)
13. [CUSTOMER_HIDDEN_RESTAURANT](#13-customer_hidden_restaurant)
14. [BOOKING](#14-booking)
15. [BOOKING_RULE](#15-booking_rule)
16. [BOOKING_BLOCKED_SLOT](#16-booking_blocked_slot)
17. [BOOKING_ITEM](#17-booking_item)
18. [PAYMENT](#18-payment)
19. [BOOKING_DISCOUNT_CODE](#19-booking_discount_code)
20. [EVENT](#20-event)
21. [EVENT_MENU_ITEM](#21-event_menu_item)
22. [EVENT_BOOKING_POLICY](#22-event_booking_policy)
23. [RESTAURANT_RATING](#23-restaurant_rating)
24. [CUSTOMER_RATING](#24-customer_rating)
25. [NOTIFICATION](#25-notification)
26. [MODERATION_ACTION](#26-moderation_action)
27. [Relationship Summary](#relationship-summary)

---

## 1. USER

Central account table for all platform participants. Role-based extensions (see `CUSTOMER_PROFILE`, `RESTAURANT_OWNER_PROFILE`) provide role-specific attributes via 1-to-1 composition.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique user identifier (UUID/ULID). |
| `email` | `string` | **NOT NULL, UNIQUE** | User's email address; used for authentication. |
| `password_hash` | `string` | **NOT NULL** | Hashed password. Raw passwords are never stored. |
| `role` | `string` (enum) | **NOT NULL** | User role. Allowed values: `CUSTOMER`, `RESTAURANT_OWNER`, `ADMIN`. |
| `status` | `string` (enum) | **NOT NULL** | Account status. Allowed values: `ACTIVE`, `SUSPENDED`, `BANNED`. |
| `first_name` | `string` | **NOT NULL** | User's first name. |
| `last_name` | `string` | **NOT NULL** | User's last name. |
| `created_at` | `datetime` | **NOT NULL** | Timestamp of account creation (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Timestamp of last update (UTC). |

**Relationships:**

- `1 → 0..1` **CUSTOMER_PROFILE** — extended profile for customers.
- `1 → 0..1` **RESTAURANT_OWNER_PROFILE** — extended profile for restaurant owners.
- `1 → 0..*` **RESTAURANT** — a restaurant owner can own multiple restaurants.
- `1 → 0..*` **BOOKING** — a customer can make multiple bookings.
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
| `user_id` | `string` | **PK, FK → USER.id** | References the parent user account. |
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
| `user_id` | `string` | **PK, FK → USER.id** | References the parent user account. |
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
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Soft-delete / visibility flag. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **USER** — each restaurant is owned by one user (restaurant owner).
- `1 → 0..*` **RESTAURANT_TABLE** — a restaurant has zero or more tables.
- `1 → 0..*` **MENU_ITEM** — a restaurant offers menu items.
- `1 → 0..*` **PROMOTION** — a restaurant can configure promotions.
- `1 → 0..*` **LOYALTY_PROGRAM** — a restaurant can define loyalty programs.
- `1 → 0..*` **DISCOUNT_CODE** — a restaurant can issue discount codes.
- `1 → 0..*` **BOOKING_RULE** — a restaurant configures booking rules.
- `1 → 0..*` **BOOKING_BLOCKED_SLOT** — a restaurant can block booking slots.
- `1 → 0..*` **BOOKING** — a restaurant receives bookings.
- `1 → 0..*` **CUSTOMER_FAVOURITE_RESTAURANT** — customers can favourite the restaurant.
- `1 → 0..*` **CUSTOMER_HIDDEN_RESTAURANT** — customers can hide the restaurant.

---

## 5. RESTAURANT_TABLE

A physical or logical table within a restaurant, used for seating allocation during bookings.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique table identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant this table belongs to. |
| `label` | `string` | **NOT NULL** | Human-readable label (e.g. `T1`, `Terrace-A`). |
| `capacity` | `int` | **NOT NULL** | Maximum number of guests the table can seat. |
| `is_active` | `boolean` | **NOT NULL, DEFAULT true** | Whether the table is currently available for booking. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each table belongs to one restaurant.
- `1 → 0..*` **BOOKING** — a table can host multiple bookings (over time).

---

## 6. MENU_ITEM

A dish or beverage offered by a restaurant. Used in pre-orders (via `BOOKING_ITEM`) and event menus (via `EVENT_MENU_ITEM`).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique menu item identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant that offers this item. |
| `name` | `string` | **NOT NULL** | Display name of the menu item. |
| `description` | `string` | NULLABLE | Description, ingredients, preparation notes. |
| `type_of_dish` | `string` (enum) | **NOT NULL** | Course category for menu structure and filtering. Allowed values: `STARTER`, `MAIN`, `DESSERT`, `APERITIF`, `BEVERAGE`. |
| `base_price` | `float` | **NOT NULL** | Standard price. Use `DECIMAL` in the physical schema for precision. |
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
- `1 → 0..*` **BOOKING_ITEM** — a menu item can be pre-ordered in many bookings.
- `1 → 0..*` **EVENT_MENU_ITEM** — a menu item can appear in event-specific menus.

---

## 7. PROMOTION

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

---

## 8. LOYALTY_PROGRAM

Defines a loyalty rewards program for a restaurant (e.g. points per booking, tiered rewards).

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

## 9. LOYALTY_ACCOUNT

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

## 10. LOYALTY_TRANSACTION

Immutable ledger entry recording points earned, redeemed, or manually adjusted on a loyalty account.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique transaction identifier (UUID/ULID). |
| `loyalty_account_id` | `string` | **FK → LOYALTY_ACCOUNT.id, NOT NULL** | The loyalty account affected. |
| `type` | `string` (enum) | **NOT NULL** | Transaction type. Allowed values: `EARN`, `REDEEM`, `ADJUST`. |
| `points` | `int` | **NOT NULL** | Number of points (positive for earn/adjust, negative for redeem). |
| `source` | `string` | NULLABLE | Origin reference (e.g. a booking ID, or "admin adjustment"). |
| `created_at` | `datetime` | **NOT NULL** | Transaction timestamp (UTC). Immutable. |

**Relationships:**

- `N → 1` **LOYALTY_ACCOUNT** — each transaction belongs to one account.

---

## 11. DISCOUNT_CODE

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
- `1 → 0..*` **BOOKING_DISCOUNT_CODE** — a code can be applied to multiple bookings.

---

## 12. CUSTOMER_FAVOURITE_RESTAURANT

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

## 13. CUSTOMER_HIDDEN_RESTAURANT

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

## 14. BOOKING

Core transactional entity representing a reservation at a restaurant (or for an event). Reused for both standard bookings and event bookings via the `is_event_booking` flag.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique booking identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant being booked. |
| `customer_user_id` | `string` | **FK → USER.id, NOT NULL** | The customer who placed the booking. |
| `restaurant_table_id` | `string` | **FK → RESTAURANT_TABLE.id, NULLABLE** | Assigned table. `NULL` for generic / unassigned bookings. |
| `scheduled_at` | `datetime` | **NOT NULL** | Date and time of the reservation (UTC). |
| `party_size` | `int` | **NOT NULL** | Number of guests in the party. |
| `status` | `string` (enum) | **NOT NULL** | Booking lifecycle state. Allowed values: `PENDING`, `CONFIRMED`, `CANCELLED`, `REJECTED`, `COMPLETED`. |
| `special_requests` | `string` | NULLABLE | Free-text notes (seating preferences, dietary requirements, etc.). |
| `source` | `string` (enum) | **NOT NULL** | Channel the booking was placed from. Allowed values: `CUSTOMER_APP`, `RESTAURANT_PORTAL`, `ADMIN`. |
| `is_event_booking` | `boolean` | **NOT NULL, DEFAULT false** | Whether this booking is associated with an event. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each booking belongs to one restaurant.
- `N → 1` **USER** — each booking belongs to one customer.
- `N → 0..1` **RESTAURANT_TABLE** — each booking may be assigned to a table.
- `1 → 0..*` **BOOKING_ITEM** — a booking can include pre-ordered items.
- `1 → 0..*` **PAYMENT** — a booking can have one or more payment attempts.
- `1 → 0..*` **BOOKING_DISCOUNT_CODE** — discount codes applied to this booking.
- `1 → 0..*` **RESTAURANT_RATING** — a booking can yield a restaurant rating.
- `1 → 0..*` **CUSTOMER_RATING** — a booking can yield a customer rating.
- `N → 0..1` **EVENT** — an event-linked booking uses the event's booking flow.

---

## 15. BOOKING_RULE

Configurable rules that govern how bookings work for a given restaurant (lead time, advance window, slot duration, availability, pre-order, delivery, minimum confirmation amount).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique booking rule identifier (UUID/ULID). |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant these rules apply to. |
| `min_confirm_amount` | `float` | NULLABLE | Minimum prepayment amount required to confirm a booking. |
| `min_lead_time_hours` | `int` | NULLABLE | No booking allowed before this many hours in advance (e.g. 24 = book at least 24h ahead). |
| `max_advance_days` | `int` | NULLABLE | No booking allowed beyond this many days in the future (e.g. 30 = up to 30 days ahead). |
| `slot_duration_minutes` | `int` | NULLABLE | Duration of one bookable slot in minutes (e.g. 120 for 2-hour slots). |
| `availability_ranges` | `string` (JSON) | NULLABLE | Time ranges when the restaurant accepts bookings, per day (e.g. opening hours per weekday). |
| `allow_preorder` | `boolean` | **NOT NULL, DEFAULT false** | Whether customers can pre-order menu items with their booking. |
| `allow_delivery` | `boolean` | **NOT NULL, DEFAULT false** | Whether delivery is supported for bookings at this restaurant. |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **RESTAURANT** — each booking rule set belongs to one restaurant.

---

## 16. BOOKING_BLOCKED_SLOT

Represents a time range during which a restaurant (or a specific table) is unavailable for bookings (e.g. maintenance, private events, holidays).

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

## 17. BOOKING_ITEM

Line item in a booking's pre-order, linking a booking to one or more menu items with quantity and pricing.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique booking item identifier (UUID/ULID). |
| `booking_id` | `string` | **FK → BOOKING.id, NOT NULL** | The parent booking. |
| `menu_item_id` | `string` | **FK → MENU_ITEM.id, NOT NULL** | The menu item being ordered. |
| `quantity` | `int` | **NOT NULL** | Number of units ordered. Must be ≥ 1. |
| `unit_price` | `float` | **NOT NULL** | Price per unit at the time of booking (snapshot). |
| `total_price` | `float` | **NOT NULL** | `quantity × unit_price`. Stored to avoid recalculation and preserve historical accuracy. |

**Relationships:**

- `N → 1` **BOOKING** — each item belongs to one booking.
- `N → 1` **MENU_ITEM** — each item references one menu item.

---

## 18. PAYMENT

Records a payment transaction against a booking. Designed to be Stripe-ready: persists Stripe identifiers while never storing raw card data.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique payment identifier (UUID/ULID). |
| `booking_id` | `string` | **FK → BOOKING.id, NOT NULL** | The booking this payment is for. |
| `provider` | `string` | **NOT NULL** | Payment provider name. Current value: `STRIPE`. |
| `stripe_payment_intent_id` | `string` | NULLABLE | Stripe `PaymentIntent` ID (e.g. `pi_...`). |
| `stripe_charge_id` | `string` | NULLABLE | Stripe `Charge` ID, populated depending on Stripe integration mode. |
| `amount` | `float` | **NOT NULL** | Payment amount. Use `DECIMAL` in physical schema. |
| `currency` | `string` | **NOT NULL** | ISO 4217 currency code (e.g. `EUR`, `USD`). |
| `status` | `string` (enum) | **NOT NULL** | Payment lifecycle state. Allowed values: `REQUIRES_PAYMENT_METHOD`, `REQUIRES_CONFIRMATION`, `SUCCEEDED`, `CANCELED`, `REFUNDED`. |
| `authorized_at` | `datetime` | NULLABLE | Timestamp when the payment was authorized (UTC). |
| `captured_at` | `datetime` | NULLABLE | Timestamp when the payment was captured (UTC). |
| `canceled_at` | `datetime` | NULLABLE | Timestamp when the payment was canceled (UTC). |
| `created_at` | `datetime` | **NOT NULL** | Row creation timestamp (UTC). |
| `updated_at` | `datetime` | **NOT NULL** | Last update timestamp (UTC). |

**Relationships:**

- `N → 1` **BOOKING** — each payment belongs to one booking.

---

## 19. BOOKING_DISCOUNT_CODE

Join table that links a discount code to a booking and records the applied discount amount.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `booking_id` | `string` | **FK → BOOKING.id, PK (composite)** | The booking the code was applied to. |
| `discount_code_id` | `string` | **FK → DISCOUNT_CODE.id, PK (composite)** | The discount code that was used. |
| `applied_amount` | `float` | **NOT NULL** | The actual monetary discount applied to the booking. |

**Relationships:**

- `N → 1` **BOOKING** — each row applies to one booking.
- `N → 1` **DISCOUNT_CODE** — each row references one discount code.

---

## 20. EVENT

A special dining or social event, optionally hosted at a restaurant. Events reuse the `BOOKING` entity for reservations and can have custom menus and booking policies.

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
- `1 → 0..*` **BOOKING** — attendees book via the standard booking flow.
- `1 → 0..*` **EVENT_MENU_ITEM** — event-specific menu.
- `1 → 0..*` **EVENT_BOOKING_POLICY** — custom booking/payment rules.

---

## 21. EVENT_MENU_ITEM

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

## 22. EVENT_BOOKING_POLICY

Defines per-event booking and payment rules (minimum prepayment, custom policy schemas) that override or extend the restaurant's default `BOOKING_RULE`.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique policy identifier (UUID/ULID). |
| `event_id` | `string` | **FK → EVENT.id, NOT NULL** | The event this policy applies to. |
| `min_confirm_amount` | `float` | NULLABLE | Minimum prepayment required to confirm an event booking. |
| `policy_schema` | `string` (JSON) | NULLABLE | JSON describing per-event booking and payment rules. |

**Relationships:**

- `N → 1` **EVENT** — each policy belongs to one event.

---

## 23. RESTAURANT_RATING

A rating left by a customer for a restaurant following a completed booking.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique rating identifier (UUID/ULID). |
| `booking_id` | `string` | **FK → BOOKING.id, NOT NULL** | The booking this rating is associated with. |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant being rated. |
| `customer_user_id` | `string` | **FK → USER.id, NOT NULL** | The customer who submitted the rating. |
| `rating` | `int` | **NOT NULL** | Numeric score (e.g. 1–5). |
| `comment` | `string` | NULLABLE | Optional free-text review. |
| `created_at` | `datetime` | **NOT NULL** | Rating submission timestamp (UTC). |

**Relationships:**

- `N → 1` **BOOKING** — each rating is linked to one booking.
- `N → 1` **RESTAURANT** — each rating targets one restaurant.
- `N → 1` **USER** — each rating is authored by one customer.

---

## 24. CUSTOMER_RATING

A rating left by a restaurant (owner/staff) for a customer following a completed booking (e.g. no-show, good guest).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique rating identifier (UUID/ULID). |
| `booking_id` | `string` | **FK → BOOKING.id, NOT NULL** | The booking this rating is associated with. |
| `restaurant_id` | `string` | **FK → RESTAURANT.id, NOT NULL** | The restaurant providing the rating. |
| `rated_customer_user_id` | `string` | **FK → USER.id, NOT NULL** | The customer being rated. |
| `restaurant_user_id` | `string` | **FK → USER.id, NOT NULL** | The restaurant-side user who submitted the rating. |
| `rating` | `int` | **NOT NULL** | Numeric score (e.g. 1–5). |
| `comment` | `string` | NULLABLE | Optional free-text feedback. |
| `created_at` | `datetime` | **NOT NULL** | Rating submission timestamp (UTC). |

**Relationships:**

- `N → 1` **BOOKING** — each rating is linked to one booking.
- `N → 1` **RESTAURANT** — each rating comes from one restaurant.
- `N → 1` **USER** (rated_customer_user_id) — the customer being rated.
- `N → 1` **USER** (restaurant_user_id) — the restaurant user who submitted the rating.

---

## 25. NOTIFICATION

A notification delivered to a user (push, email, in-app, etc.) triggered by system events.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `string` | **PK** | Unique notification identifier (UUID/ULID). |
| `user_id` | `string` | **FK → USER.id, NOT NULL** | The recipient user. |
| `type` | `string` (enum) | **NOT NULL** | Notification category. Allowed values: `BOOKING_STATUS`, `PAYMENT_STATUS`, `EVENT_UPDATE`, `SYSTEM`. |
| `payload` | `string` (JSON) | **NOT NULL** | JSON object carrying contextual data (booking ID, amounts, messages, etc.). |
| `is_read` | `boolean` | **NOT NULL, DEFAULT false** | Whether the user has acknowledged/read the notification. |
| `created_at` | `datetime` | **NOT NULL** | Notification creation timestamp (UTC). |

**Relationships:**

- `N → 1` **USER** — each notification is sent to one user.

---

## 26. MODERATION_ACTION

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
