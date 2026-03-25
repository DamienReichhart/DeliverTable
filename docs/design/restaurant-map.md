# Restaurant Map Feature — Design Document

## Understanding Summary

- **What:** A full-page map view on the Restaurants page, toggled via a button (replaces the list view). Uses Leaflet.js with OpenStreetMap tiles.
- **Why:** Let users visually discover restaurants by location rather than scrolling a grid.
- **Who:** All users browsing restaurants.
- **Interaction:** User clicks on the map to set a search center, adjusts radius via a slider overlay. A circle visualizes the radius. Restaurants within range load automatically (live refresh). Clicking a marker shows a popup with name, type, and link to detail page.
- **Filters:** Name and type filters remain available on the map view. City filter and existing radius selector are removed from the list view.
- **Non-goals:** No geolocation auto-center, no split view, no side panel.

## Assumptions

- Leaflet.js loaded via CDN in `index.html`, called through JS interop
- Existing `GET /api/v1/restaurant` endpoint untouched — new `GET /api/v1/restaurant/map` endpoint added
- Default radius slider range: 1–50 km
- Map starts with a default view of France (center ~46.6, 2.2, zoom ~6) — no query until user clicks
- No state persistence between list/map toggles
- Removing the city filter and radius selector from the list view is intentional

## Decision Log

| # | Decision | Alternatives Considered | Reason |
|---|----------|------------------------|--------|
| 1 | Full-page map view (toggle) | Split view, modal, replacement | User preference — clean UX |
| 2 | Leaflet.js + OpenStreetMap | Google Maps, Mapbox | Free, no API key, aligns with existing gov API usage |
| 3 | Marker popup with name/type/link | Direct navigation, side panel | Simple, non-disruptive |
| 4 | Radius slider on the map | Reuse existing selector, both | Single location for radius control, existing one removed |
| 5 | Click-to-set center (no geolocation) | Auto-geolocation, both | User preference |
| 6 | Visual radius circle on map | No circle | Clear spatial feedback |
| 7 | Live refresh on center/radius change | Manual search button | Fluid UX |
| 8 | Keep name + type filters, remove city | Keep all, no filters | City is redundant when using a map |
| 9 | JS Interop bridge | Blazor wrapper library, iframe | Full Leaflet control, no risky dependencies |
| 10 | New `RestaurantMapDto` | Add coords to existing `RestaurantDto` | Keeps DTOs focused on their use case |
| 11 | Dedicated `GET /api/v1/restaurant/map` endpoint | Query param on existing endpoint | Clean separation, no pagination needed for map |

## Architecture

### Component Structure

```
Restaurants.razor (existing page)
├── Filter bar: Name input, Type dropdown, "Carte" toggle button
├── [List View] — existing restaurant card grid (when map is off)
└── [Map View] — new (when map is on)
    └── RestaurantMap.razor (new component)
        ├── <div id="restaurant-map"> (Leaflet target)
        ├── Radius slider overlay (HTML range input positioned over map)
        └── JS interop via restaurant-map.js
```

### Data Flow

```
User clicks map / moves slider
       │
       ▼
  restaurant-map.js captures event
       │
       ▼
  DotNet.invokeMethodAsync → RestaurantMap.razor [JSInvokable]
       │
       ▼
  Debounce (300ms via Timer)
       │
       ▼
  IRestaurantService.GetRestaurantsForMap(query)
  → GET /api/v1/restaurant/map?Latitude=..&Longitude=..&RadiusKm=..&Name=..&Type=..
       │
       ▼
  Returns List<RestaurantMapDto>
       │
       ▼
  JS interop: updateMarkers(restaurants)
  JS updates circle + markers
```

### New DTO

```csharp
public record RestaurantMapDto(
    int Id,
    string Name,
    string Type,
    double Latitude,
    double Longitude
);
```

### New Endpoint

```
GET /api/v1/restaurant/map
    ?Latitude=<lat>&Longitude=<lon>&RadiusKm=<radius>&Name=<name>&Type=<type>
    → Returns List<RestaurantMapDto> (no pagination)
```

## UX Details

### Initial State (map opens, no click yet)
- Default view of France (center ~46.6, 2.2, zoom ~6)
- No markers, no circle
- Slider visible but inactive — hint text: "Cliquez sur la carte pour rechercher"

### After User Clicks
- Circle draws around clicked point with current slider radius (default: 10 km)
- API call fires, markers appear for matching restaurants
- Slider becomes active

### Radius Slider
- Range: 1–50 km, step: 1
- Overlay positioned on the map (bottom or top-right)
- Shows current value label (e.g., "15 km")

### Marker Popups
- Restaurant name (bold)
- Type badge
- "Voir le restaurant" link → navigates to `/restaurant/{id}`

### Filter Interaction
- Name/Type filters sit above the map (same position as list view)
- Changing a filter triggers a re-fetch (same debounce)

## Error Handling & Edge Cases

- **No results:** Clear markers, show overlay: "Aucun restaurant trouvé dans cette zone."
- **API failure:** Toast with generic error, keep map interactive.
- **Click outside France:** Returns no results — handled by "no results" case.
- **Rapid interactions:** 300ms debounce + stale response tracking (request counter).
- **Toggle back to list:** Map component disposes (Leaflet instance + DotNetObjectReference). Re-opening reinitializes with default France view.

## File Changes

### New Files

| File | Purpose |
|------|---------|
| `DeliverTableSharedLibrary/Dtos/Restaurant/RestaurantMapDto.cs` | Map DTO |
| `DeliverTableClient/Pages/Explore/Restaurants/RestaurantMap.razor` | Blazor map component |
| `DeliverTableClient/Pages/Explore/Restaurants/RestaurantMap.razor.scss` | Map styling |
| `DeliverTableClient/wwwroot/js/restaurant-map.js` | Leaflet JS interop module |
| `DeliverTableTests/Controllers/RestaurantControllerMapTests.cs` | Controller tests |
| `DeliverTableTests/Services/RestaurantServiceMapTests.cs` | Service tests |

### Modified Files

| File | Change |
|------|--------|
| `DeliverTableClient/wwwroot/index.html` | Add Leaflet CSS + JS CDN |
| `DeliverTableSharedLibrary/Constants/ApiRoutes.cs` | Add `Map` route constant |
| `DeliverTableServer/Mappers/RestaurantMappers.cs` | Add `ToMapDto()` |
| `DeliverTableServer/Repositories/RestaurantRepository.cs` | New map query method |
| `DeliverTableServer/Repositories/Interfaces/IRestaurantRepository.cs` | Interface update |
| `DeliverTableServer/Services/RestaurantService.cs` | New `GetForMapAsync` |
| `DeliverTableServer/Services/Interfaces/IRestaurantService.cs` | Interface update |
| `DeliverTableServer/Controllers/RestaurantController.cs` | New `GetForMap` action |
| `DeliverTableClient/Services/RestaurantService.cs` | New `GetRestaurantsForMap` |
| `DeliverTableClient/Services/Interfaces/IRestaurantService.cs` | Interface update |
| `DeliverTableClient/Pages/Explore/Restaurants/Restaurants.razor` | Add toggle, remove city filter + radius selector |
| `DeliverTableClient/Pages/Explore/Restaurants/Restaurants.razor.scss` | Toggle button style |
