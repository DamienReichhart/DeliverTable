## DeliverTable – ER Model (Mermaid)

This document describes a **conceptual / logical ER model** for DeliverTable using Mermaid.
It is designed to be **framework‑agnostic**, **Stripe‑ready**, and stable enough to evolve as the platform grows.

### Conventions & assumptions

- **ID types**: use opaque, non‑guessable identifiers (e.g. UUID/ULID) for primary keys exposed outside the system; integers are acceptable for purely internal keys.
- **Timestamps**: all date/time fields are stored as **UTC, timezone‑aware values** (e.g. PostgreSQL `timestamptz`); any local times are converted at the application/UI layer only.
- **Soft deletion**: where indicated, use flags (e.g. `is_active`, `deleted_at`) instead of hard deletes to preserve audit history.
- **Users & roles**: a single `USER` table with role(s) to support **customers**, **restaurant owners**, and **administrators**; specialized tables extend `USER` by composition (1‑to‑1).
- **Stripe integration**: `PAYMENT` persists Stripe identifiers (e.g. `stripe_payment_intent_id`) and never stores raw card data.
- **Status fields**: enumerations (e.g. `PENDING`, `CONFIRMED`, `CANCELLED`, `REJECTED`, `PAID`, `REFUNDED`) should be modeled as constrained types/enums in code and DB.

> This is a **domain‑oriented ER model**. Physical implementation details (indexes, partitions, etc.) are intentionally omitted from the diagram but should be added at DB design time.

---

### Full ER diagram (single Mermaid graph)

```mermaid
erDiagram
    %% Core accounts, profiles & restaurants
    USER {
        string  id PK
        string  email
        string  password_hash
        string  role                "CUSTOMER | RESTAURANT_OWNER | ADMIN"
        string  status              "ACTIVE | SUSPENDED | BANNED"
        string  first_name
        string  last_name
        string  stripe_customer_id  "optional – Stripe Customer object ID (cus_...)"
        datetime created_at
        datetime updated_at
    }

    CUSTOMER_PROFILE {
        string  user_id PK, FK
        string  allergy_notes       "Free‑text and/or structured JSON"
        string  dietary_preferences
        boolean marketing_opt_in
        datetime created_at
        datetime updated_at
    }

    RESTAURANT_OWNER_PROFILE {
        string  user_id PK, FK
        string  company_name
        string  vat_number
        string  contact_phone
        datetime created_at
        datetime updated_at
    }

    RESTAURANT {
        string  id PK
        string  owner_user_id FK
        string  name
        string  restaurant_type     "e.g. ITALIAN, STEAKHOUSE, VEGAN"
        string  description
        string  address_line1
        string  address_line2
        string  city
        string  postal_code
        string  country
        float   latitude
        float   longitude
        float   balance             "credited on delivery, minus commission"
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    RESTAURANT_TRANSACTION {
        int     id PK
        int     restaurant_id FK
        int     order_id FK          "nullable – null for withdrawals"
        string  type                 "CREDIT | WITHDRAWAL"
        float   gross_amount
        float   commission_amount
        float   net_amount
        float   balance_after
        datetime created_at
    }

    RESTAURANT_TABLE {
        string  id PK
        string  restaurant_id FK
        string  label              "e.g. T1, Terrace‑A"
        int     capacity
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    %% Menus, promotions, loyalty & preferences
    MENU_ITEM {
        string  id PK
        string  restaurant_id FK
        string  name
        string  description
        string  type_of_dish       "STARTER | MAIN | DESSERT | APERITIF | BEVERAGE"
        float   base_price
        boolean is_vegetarian
        boolean is_vegan
        boolean is_gluten_free
        boolean is_allergen_hazard  "true if must be hidden for some customers"
        boolean is_dish_of_the_day
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    PROMOTION {
        int     id PK
        int     restaurant_id FK
        string  name
        string  description
        string  promotion_type     "AUTOMATIC | ITEM_BASED | THRESHOLD"
        string  discount_type      "PERCENTAGE | FIXED_AMOUNT"
        float   discount_value
        float   min_order_amount   "nullable – for THRESHOLD type"
        datetime starts_at
        datetime ends_at
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    PROMOTION_DISH {
        int     id PK
        int     promotion_id FK
        int     dish_id FK          "links ITEM_BASED promotions to dishes"
    }

    LOYALTY_PROGRAM {
        int     id PK
        int     restaurant_id FK    "unique – one per restaurant"
        float   points_per_euro
        float   euros_per_point
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    LOYALTY_ACCOUNT {
        int     id PK
        int     loyalty_program_id FK
        int     customer_user_id FK "unique per program+customer"
        int     points_balance
        datetime created_at
        datetime updated_at
    }

    LOYALTY_TRANSACTION {
        int     id PK
        int     loyalty_account_id FK
        string  type               "EARN | REDEEM | ADJUST"
        string  status             "PENDING | COMMITTED | REVERSED"
        int     points
        int     order_id FK         "nullable – null for adjustments"
        datetime created_at
    }

    DISCOUNT_CODE {
        int     id PK
        int     restaurant_id FK
        string  code                "unique per restaurant"
        string  description
        string  discount_type       "PERCENTAGE | FIXED_AMOUNT"
        float   discount_value
        float   min_order_amount    "nullable"
        datetime valid_from
        datetime valid_until
        int     max_redemptions     "nullable – null = unlimited"
        int     per_user_limit      "default 1"
        int     current_redemptions "default 0"
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    DISCOUNT_CODE_REDEMPTION {
        int     id PK
        int     discount_code_id FK
        int     customer_user_id FK
        int     order_id FK
        string  status             "PENDING | COMMITTED | REVERSED"
        datetime created_at
    }

    ORDER_DISCOUNT {
        int     id PK
        int     order_id FK
        string  source              "PROMOTION | DISCOUNT_CODE | LOYALTY_POINTS"
        int     source_id           "nullable – FK to source entity"
        string  description
        float   amount
    }

    CUSTOMER_FAVOURITE_RESTAURANT {
        string  customer_user_id FK
        string  restaurant_id FK
        datetime created_at
    }

    CUSTOMER_HIDDEN_RESTAURANT {
        string  customer_user_id FK
        string  restaurant_id FK
        datetime created_at
    }

    %% Cart & orders (implemented)
    CART {
        int     id PK
        int     customer_user_id FK
        int     restaurant_id FK
        datetime created_at
        datetime updated_at
    }

    CART_ITEM {
        int     id PK
        int     cart_id FK
        int     dish_id FK
        int     quantity
        float   unit_price          "snapshot at add‑to‑cart time"
        string  special_instructions
        datetime created_at
        datetime updated_at
    }

    ORDER {
        int     id PK
        int     customer_user_id FK
        int     restaurant_id FK
        string  order_type          "DELIVERY | DINE_IN"
        string  status              "AWAITING_PAYMENT | PENDING | CONFIRMED | PREPARING | READY | DELIVERING | DELIVERED | CANCELLED | REFUSED"
        string  payment_status      "PENDING | AUTHORIZED | COMPLETED | FAILED | REFUNDED | PARTIALLY_REFUNDED"
        float   original_amount     "sum of items before discounts"
        float   discount_amount     "total discount applied"
        float   total_amount        "original - discount (what customer pays)"
        int     loyalty_points_used
        int     loyalty_points_earned
        int     discount_code_id FK "nullable"
        int     guest_count
        string  delivery_address    "required for DELIVERY only"
        string  notes
        string  source              "CUSTOMER_APP | RESTAURANT_PORTAL | ADMIN"
        datetime scheduled_at       "nullable – date/time of reservation/delivery"
        int     restaurant_table_id FK "nullable – for dine-in table assignment"
        boolean is_event_booking    "default false"
        int     event_id FK         "nullable – link to EVENT"
        datetime created_at
        datetime updated_at
    }

    ORDER_ITEM {
        int     id PK
        int     order_id FK
        int     dish_id FK
        string  dish_name           "snapshot at order time"
        int     quantity
        float   unit_price          "snapshot at order time"
        string  special_instructions
    }

    %% Scheduling, payments & events (Stripe‑ready)
    ORDER_RULE {
        string  id PK
        string  restaurant_id FK
        float   min_confirm_amount
        int     min_lead_time_hours     "no booking before X hours in advance"
        int     max_advance_days        "no booking beyond Y days"
        int     slot_duration_minutes   "duration of one slot"
        string  availability_ranges     "JSON: time ranges per day"
        boolean allow_preorder
        boolean allow_delivery
        datetime created_at
        datetime updated_at
    }

    ORDER_BLOCKED_SLOT {
        string  id PK
        string  restaurant_id FK
        string  restaurant_table_id FK "optional; null = whole restaurant"
        datetime starts_at
        datetime ends_at
        string  reason
        datetime created_at
    }

    PAYMENT {
        string  id PK
        int     order_id FK
        string  provider          "STRIPE"
        string  stripe_payment_intent_id
        string  stripe_charge_id  "optional, depending on Stripe mode"
        float   amount
        string  currency
        string  status            "REQUIRES_PAYMENT_METHOD | REQUIRES_CONFIRMATION | AUTHORIZED | SUCCEEDED | CANCELED | REFUNDED"
        datetime authorized_at
        datetime captured_at
        datetime canceled_at
        datetime created_at
        datetime updated_at
    }

    REFUND {
        string  id PK
        string  payment_id FK
        string  stripe_refund_id  "Stripe Refund object ID (re_...)"
        float   amount
        string  currency
        string  reason            "REQUESTED_BY_CUSTOMER | DUPLICATE | FRAUDULENT | OTHER"
        string  status            "PENDING | SUCCEEDED | FAILED | CANCELED"
        string  created_by_user_id FK "optional – admin or system who initiated the refund"
        datetime created_at
        datetime updated_at
    }

    PROCESSED_STRIPE_EVENT {
        string  id PK             "Stripe event ID (evt_...) – natural PK for idempotency"
        string  event_type        "e.g. payment_intent.succeeded"
        datetime processed_at
    }

    %% Events, ratings, notifications & moderation
    EVENT {
        string  id PK
        string  restaurant_id FK    "nullable when customer‑hosted, off‑site"
        string  created_by_user_id FK
        string  name
        string  description
        datetime starts_at
        datetime ends_at
        int     max_guests
        string  visibility          "PUBLIC | PRIVATE"
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    EVENT_MENU_ITEM {
        string  id PK
        string  event_id FK
        string  menu_item_id FK
        float   override_price
    }

    EVENT_BOOKING_POLICY {
        string  id PK
        string  event_id FK
        float   min_confirm_amount
        string  policy_schema       "JSON for per‑event booking/payment rules"
    }

    RESTAURANT_RATING {
        string  id PK
        int     order_id FK
        string  restaurant_id FK
        string  customer_user_id FK
        int     rating              "e.g. 1‑5"
        string  comment
        datetime created_at
    }

    CUSTOMER_RATING {
        string  id PK
        int     order_id FK
        string  restaurant_id FK
        string  rated_customer_user_id FK
        string  restaurant_user_id FK "who rated"
        int     rating
        string  comment
        datetime created_at
    }

    NOTIFICATION {
        string  id PK
        string  user_id FK
        string  type               "ORDER_STATUS | PAYMENT_STATUS | EVENT_UPDATE | SYSTEM"
        string  payload            "JSON with contextual data"
        boolean is_read
        datetime created_at
    }

    MODERATION_ACTION {
        string  id PK
        string  admin_user_id FK
        string  target_type        "RESTAURANT | MENU_ITEM | EVENT | USER"
        string  target_id
        string  action_type        "APPROVE | REJECT | BAN | WARN | UNLIST"
        string  reason
        datetime created_at
    }

    %% Relationships – Cart & Orders
    USER ||--o{ CART : "has carts"
    RESTAURANT ||--o{ CART : "carts target"
    CART ||--o{ CART_ITEM : "contains"
    MENU_ITEM ||--o{ CART_ITEM : "is added to cart"

    USER ||--o{ ORDER : "places orders"
    RESTAURANT ||--o{ ORDER : "receives orders"
    RESTAURANT_TABLE ||--o{ ORDER : "can host"
    ORDER ||--o{ ORDER_ITEM : "contains"
    MENU_ITEM ||--o{ ORDER_ITEM : "is ordered in"
    ORDER ||--o{ PAYMENT : "has payments"
    PAYMENT ||--o{ REFUND : "has refunds"
    USER ||--o{ REFUND : "initiated by (optional)"
    ORDER ||--o{ RESTAURANT_RATING : "yields rating"
    ORDER ||--o{ CUSTOMER_RATING : "yields customer rating"

    %% Relationships – Restaurant Account
    RESTAURANT ||--o{ RESTAURANT_TRANSACTION : "has transactions"
    ORDER ||--o{ RESTAURANT_TRANSACTION : "generates credit"

    %% Relationships – Core
    USER ||--o{ CUSTOMER_PROFILE : "extends (customer only)"
    USER ||--o{ RESTAURANT_OWNER_PROFILE : "extends (restaurant owner only)"
    USER ||--o{ RESTAURANT : "owns"
    RESTAURANT ||--o{ RESTAURANT_TABLE : "has"

    USER ||--o{ CUSTOMER_FAVOURITE_RESTAURANT : "favourites"
    RESTAURANT ||--o{ CUSTOMER_FAVOURITE_RESTAURANT : "is favourited by"

    USER ||--o{ CUSTOMER_HIDDEN_RESTAURANT : "hides"
    RESTAURANT ||--o{ CUSTOMER_HIDDEN_RESTAURANT : "is hidden by"

    RESTAURANT ||--o{ MENU_ITEM : "offers"
    RESTAURANT ||--o{ PROMOTION : "configures"
    PROMOTION ||--o{ PROMOTION_DISH : "targets dishes"
    MENU_ITEM ||--o{ PROMOTION_DISH : "is targeted by"

    RESTAURANT ||--o{ LOYALTY_PROGRAM : "defines"
    LOYALTY_PROGRAM ||--o{ LOYALTY_ACCOUNT : "has accounts"
    USER ||--o{ LOYALTY_ACCOUNT : "owns account"
    LOYALTY_ACCOUNT ||--o{ LOYALTY_TRANSACTION : "logs"
    ORDER ||--o{ LOYALTY_TRANSACTION : "triggers"

    RESTAURANT ||--o{ DISCOUNT_CODE : "issues"
    DISCOUNT_CODE ||--o{ DISCOUNT_CODE_REDEMPTION : "tracks usage"
    USER ||--o{ DISCOUNT_CODE_REDEMPTION : "redeems"
    ORDER ||--o{ DISCOUNT_CODE_REDEMPTION : "uses code"
    ORDER ||--o{ ORDER_DISCOUNT : "has discounts"
    DISCOUNT_CODE ||--o{ ORDER : "applied to"

    RESTAURANT ||--o{ ORDER_RULE : "configures rules"
    RESTAURANT ||--o{ ORDER_BLOCKED_SLOT : "blocks slots"
    RESTAURANT_TABLE ||--o{ ORDER_BLOCKED_SLOT : "can block"

    %% Relationships – Events
    EVENT ||--o{ ORDER : "uses order flow"
    EVENT ||--o{ EVENT_MENU_ITEM : "defines dedicated menu"
    EVENT ||--o{ EVENT_BOOKING_POLICY : "custom rules"

    USER ||--o{ NOTIFICATION : "receives"
    USER ||--o{ MODERATION_ACTION : "performs (admin)"
```

---

### Design notes & alternatives considered

- **Single vs multiple user tables**
  A split `CUSTOMER` / `RESTAURANT_USER` / `ADMIN` table architecture was considered but rejected in favour of a unified `USER` with role‑based extensions to simplify authentication, Stripe customer mapping, and future role changes.

- **Embedding vs normalizing allergies and dietary preferences**
  A fully normalized `ALLERGEN`, `CUSTOMER_ALLERGEN`, `MENU_ITEM_ALLERGEN` model was considered. The chosen design keeps room for structured JSON schemas while avoiding premature complexity; it can be evolved into a fully normalized model without breaking the high‑level ER structure.

- **Orders vs events**
  Modeling events with a separate table was considered. Instead, `ORDER` is reused with an `is_event_booking` flag and per‑event booking policies, which keeps payment and Stripe integration unified while still supporting event‑specific rules.

- **Payments and Stripe integration**
  An alternative was to make `PAYMENT` Stripe‑agnostic with a link table to provider‑specific details. The current model inlines Stripe identifiers but isolates all payment gateway fields inside `PAYMENT`, making it straightforward to:
  - map `PAYMENT` records to Stripe `PaymentIntent`/`Charge` objects;
  - add another provider later by adding new columns or a secondary detail table without touching orders.
  - `REFUND` stores each Stripe `Refund` object as a child of `PAYMENT`, supporting both full and partial refunds (`PaymentStatus.PartiallyRefunded`).
  - `PROCESSED_STRIPE_EVENT` is a standalone idempotency log keyed on the Stripe event ID (`evt_...`), preventing duplicate processing of webhook events.
  - `USER.stripe_customer_id` maps platform users to Stripe `Customer` objects, enabling saved payment methods and Stripe-side customer management.

- **Moderation & audit**
  Per‑entity moderation tables (e.g. `RESTAURANT_MODERATION`, `EVENT_MODERATION`) were considered. A single `MODERATION_ACTION` table is more flexible and easier to extend as new content types appear.

This ER model is intentionally **extensible** and aligned with upcoming **Stripe integration**.
