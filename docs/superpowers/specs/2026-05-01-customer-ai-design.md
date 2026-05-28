# Customer AI System — Design Spec

**Date:** 2026-05-01
**Project:** MyOvercooked2 (2D Top-Down Cooking Game)
**Scope:** Customer lifecycle, order display, spawning

---

## Context & Constraints

- Unity 6, C# 9, VContainer + SignalBus, UniTask (no coroutines)
- AnimatorControllers already generated: MaleCustomer, FemaleCustomer
  - Params: `MoveX` (float), `MoveY` (float), `IsMoving` (bool), `IsAngry` (bool), `IsJoyful` (bool)
- `RecipeSO` has: `finalMeal (FoodStateSO)`, `recipeCardSprite (Sprite)`, `scoreValue (int)`, `timeLimit (float)`
- `RecipeDatabaseSO.TryMatchRecipe(FoodStateSO, out RecipeSO)` already works
- Player serves by carrying a plate directly to the customer (Overcooked 1 style)
- Customer walks to a fixed WaitingSpot, stands until served or timed out, then walks to exit

---

## Architecture Overview

### Components

| File | Purpose |
|------|---------|
| `Customer.cs` | Lifecycle state machine + animator driver |
| `IServeable.cs` | Interface for player interaction |
| `WaitingSpot.cs` | Scene marker — tracks occupied state |
| `CustomerSpawner.cs` | Spawns customers on interval, assigns spot + recipe |
| `CustomerServedSignal.cs` | SignalBus signal fired on successful serve |
| `CustomerAngrySignal.cs` | SignalBus signal fired on patience timeout |

### Prefabs

Two separate prefabs (differ only in Animator controller):
```
MaleCustomer (GameObject)
  ├─ SpriteRenderer
  ├─ Animator  ← MaleCustomer_Animator.controller
  ├─ BoxCollider2D (IsTrigger = true)
  └─ OrderBubble (Canvas, WorldSpace, Scale ~0.01)
       ├─ RecipeImage (Image)
       └─ PatienceBar (Slider, value 1→0)

FemaleCustomer (same structure, FemaleCustomer_Animator.controller)
```

### Data Flow

```
CustomerSpawner
  → find free WaitingSpot
  → Instantiate random customer prefab
  → customer.Init(spot, recipe, exitPoint)
  → customer.RunLifecycle()

Player interaction:
  Player holds plate → Physics2D.OverlapCircle → hits Customer collider
  → IServeable.TryServe(foodState)
  → match recipe.finalMeal → success

Signals (via SignalBus):
  CustomerServedSignal  { int score }
  CustomerAngrySignal   { }
```

---

## `IServeable.cs`

```csharp
public interface IServeable
{
    bool TryServe(FoodStateSO food);
}
```

`PlayerInteraction` — khi player **đang cầm food** và nhấn E: dùng `Physics2D.OverlapCircle` tại interact point, tìm collider có `IServeable`, gọi `TryServe(heldFood.FoodState)`. Nếu `TryServe` trả về true → drop food (destroy held item).

---

## `Customer.cs`

### Lifecycle (UniTask sequential)

```
RunLifecycle(CancellationToken ct)
  ├─ WalkToAsync(waitingSpot.position, ct)
  ├─ WaitForServiceAsync(ct)
  │     uses UniTask.WhenAny(PatienceTimerAsync, servedTcs.Task)
  │     → served:  IsJoyful=true, fire CustomerServedSignal, await 1.5s
  │     → timeout: IsAngry=true,  fire CustomerAngrySignal,  await 1.5s
  └─ WalkToAsync(exitPoint.position, ct)
        → spot.Release(), Destroy(gameObject)
```

### Animator Driver

- **Walking:** each frame compute `dir = (target - pos).normalized`, set `MoveX = dir.x`, `MoveY = dir.y`, `IsMoving = true`
- **Arrived:** `IsMoving = false`, keep last MoveX/MoveY (Idle faces correct direction)
- **React:** set `IsJoyful` or `IsAngry`, clear after reaction delay

### WalkToAsync detail

```
while distance > arrivalThreshold (0.05f):
    move toward target at walkSpeed
    update animator params
    await UniTask.Yield(ct)
snap position to target
set IsMoving = false
```

### OrderBubble

- `RecipeImage.sprite` = `recipe.recipeCardSprite`
- `PatienceBar.value` updated each frame: `elapsed / recipe.timeLimit`
- OrderBubble hides after customer reacts (SetActive false before walking to exit)

### Injected Dependencies

```csharp
[Inject] private readonly SignalBus signalBus;
```

`RecipeSO` assigned by `CustomerSpawner` via `Init()` — not injected (runtime data, not service).

---

## `WaitingSpot.cs`

```csharp
public bool IsOccupied { get; private set; }
public void Claim()   { IsOccupied = true; }
public void Release() { IsOccupied = false; }
```

- Place 3–5 instances in Map1 scene at counter-facing positions
- `OnDrawGizmos`: draw yellow sphere (radius 0.3) for editor visibility

---

## `CustomerSpawner.cs`

### Fields
```csharp
[SerializeField] float spawnInterval = 15f;
[SerializeField] int maxConcurrentCustomers = 3;
[SerializeField] GameObject[] customerPrefabs;   // Male, Female
[SerializeField] WaitingSpot[] waitingSpots;
[SerializeField] Transform exitPoint;
[SerializeField] RecipeDatabaseSO recipeDatabase;
```

### Spawn Loop (UniTask)

```
SpawnLoopAsync(ct):
  loop:
    await UniTask.Delay(spawnInterval seconds, ct)
    if activeCount >= max → continue
    spot = first free WaitingSpot → if none → continue
    recipe = random from recipeDatabase.recipes
    prefab = random from customerPrefabs
    customer = Instantiate(prefab)
    customer.Init(spot, recipe, exitPoint.position)
    customer.RunLifecycle()
    activeCount++
```

`activeCount` decrements via `Action onExit` callback passed into `customer.Init(...)` — no back-reference to Spawner needed:
```csharp
customer.Init(spot, recipe, exitPoint.position, onExit: () => activeCount--);
```

### VContainer Registration

```csharp
// In LifetimeScope:
builder.RegisterComponentInHierarchy<CustomerSpawner>();
builder.DeclareSignal<CustomerServedSignal>();
builder.DeclareSignal<CustomerAngrySignal>();
```

---

## Signals

```csharp
public readonly struct CustomerServedSignal
{
    public readonly int Score;
    public CustomerServedSignal(int score) { Score = score; }
}

public readonly struct CustomerAngrySignal { }
```

---

## Files to Create

```
Assets/Scripts/Runtime/Customer.cs
Assets/Scripts/Runtime/IServeable.cs
Assets/Scripts/Runtime/WaitingSpot.cs
Assets/Scripts/Runtime/CustomerSpawner.cs
Assets/Scripts/Signals/CustomerServedSignal.cs
Assets/Scripts/Signals/CustomerAngrySignal.cs
Assets/Prefabs/MaleCustomer.prefab        (new, replaces empty Customer.prefab)
Assets/Prefabs/FemaleCustomer.prefab      (new)
```

---

## Out of Scope

- Score UI / HUD (separate system)
- Multiple maps (only Map1)
- Customer queue ordering (random spot, first-come)
- Sound effects
