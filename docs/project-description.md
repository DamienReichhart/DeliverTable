# DeliverTable – Project Specification

## Purpose

DeliverTable is a web platform connecting **customers**, **restaurants**, and **administrators** to enable restaurant discovery, table and event booking, meal pre‑ordering, as well as payment and event management.

---

## Functional Scope

### 1. Customer side

- **User account**: account creation, profile updates, and allergy information so unsuitable dishes can be hidden.
- **Search and discovery**: search and filter restaurants (type, distance), manage favourites, hide restaurants from results, and get restaurant suggestions via AI assistance.
- **Booking and ordering**: book tables, pre‑order meals, request specific services (seating preferences, dietary requirements), and pay online to confirm the booking.
- **Cart and checkout**: persistent cart per restaurant, choose between delivery (with address) or dine‑in (table at the restaurant), specify number of guests, add special instructions, and pay to confirm the order.
- **Follow‑up**: receive notifications when bookings are accepted or rejected, and rate restaurants after visits.

### 2. Restaurant side

- **Account**: create and manage the restaurant account to access the application.
- **Offering**: manage the menu (dishes), highlight a “dish of the day”, configure promotions and time‑limited discounts.
- **Booking and services**: configure booking rules (minimum amount to confirm, blocked time slots), offer delivery services, manage customers’ special requests, and receive notifications for new booking requests.
- **Events**: create and manage events (weddings, themed evenings, special dates such as Christmas), define dedicated menus, and allow venue booking for customer events.
- **Loyalty**: manage loyalty programs and discount codes.
- **Community**: rate customers.

### 3. Administration and moderation

- **Users**: manage customer and restaurant accounts, with the ability to ban users to maintain safety and trust on the platform.
- **Content**: moderate restaurant content (menus, daily specials, events, offers) to ensure quality and compliance with platform rules.

### 4. Payments

- **Customer payments**: handle customer payments required to validate bookings.
- **Business rules**: a booking is only considered confirmed after payment and once any restaurant‑specific constraints (e.g. minimum amount) are satisfied.

### 5. Events

- **Creation**: restaurants and customers can create and manage events for specific dates (weddings, parties, etc.), with dedicated menus and booking conditions.
- **Features**: event reservations rely on the same capabilities as standard bookings (pre‑ordering, online payment, special requests).

---

## Interface objective

The application provides a **single, user‑friendly interface** that allows:

- **customers** to discover restaurants, book tables or events, and pre‑order their meals;
- **restaurants** to manage their visibility, capacity, offers, events, and customer relationships;
- **administrators** to moderate content, manage users, and ensure the safety and reliability of the service.
