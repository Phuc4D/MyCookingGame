# Customer AI System — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tạo Customer AI hoàn chỉnh: spawn, đi đến waiting spot, hiển thị order, nhận food từ player, react Joyful/Angry, đi ra exit.

**Architecture:** `CustomerSpawner` spawn prefab → `Customer.RunLifecycle()` chạy UniTask sequential (walk → wait → react → exit). `Customer` implements `IInteractable` nên PlayerInteraction detect không cần sửa. `WaitingSpot` là scene marker track occupied state.

**Tech Stack:** Unity 6, C# 9, UniTask, MonoBehaviour (no VContainer yet — project chưa wire DI), `IInteractable` pattern đã có sẵn.

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `Assets/Scripts/Runtime/WaitingSpot.cs` | Create | Scene marker, claim/release |
| `Assets/Scripts/Runtime/Customer.cs` | Create | Lifecycle + IInteractable + animator driver |
| `Assets/Scripts/Runtime/CustomerSpawner.cs` | Create | Spawn loop, assign spot + recipe |
| `Assets/Prefabs/MaleCustomer.prefab` | Create | Prefab với MaleCustomer_Animator |
| `Assets/Prefabs/FemaleCustomer.prefab` | Create | Prefab với FemaleCustomer_Animator |
| `Assets/Scripts/Runtime/PlayerInteraction.cs` | **No change** | IInteractable pattern đã detect Customer tự động |

---

## Task 1: WaitingSpot

**Files:**
- Create: `Assets/Scripts/Runtime/WaitingSpot.cs`

- [ ] **Step 1: Tạo WaitingSpot.cs**

```csharp
#nullable enable

using UnityEngine;

namespace MyOvercooked.Runtime
{
    public sealed class WaitingSpot : MonoBehaviour
    {
        public bool IsOccupied { get; private set; }

        public void Claim() => IsOccupied = true;

        public void Release() => IsOccupied = false;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
#endif
    }
}
```

- [ ] **Step 2: Verify compile**

Mở Unity Editor → đợi compile → Console không có error.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Runtime/WaitingSpot.cs Assets/Scripts/Runtime/WaitingSpot.cs.meta
git commit -m "feat: add WaitingSpot scene marker"
```

---

## Task 2: Customer.cs

**Files:**
- Create: `Assets/Scripts/Runtime/Customer.cs`

- [ ] **Step 1: Tạo Customer.cs**

```csharp
#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyOvercooked.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MyOvercooked.Runtime
{
    public sealed class Customer : MonoBehaviour, IInteractable
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 2f;
        [SerializeField] private float _arrivalThreshold = 0.05f;

        [Header("Order Bubble (child refs)")]
        [SerializeField] private GameObject _orderBubble = null!;
        [SerializeField] private Image _recipeImage = null!;
        [SerializeField] private Slider _patienceBar = null!;

        private Animator _animator = null!;
        private RecipeSO _recipe = null!;
        private WaitingSpot _waitingSpot = null!;
        private Vector2 _exitPoint;
        private Action? _onExit;

        private bool _served;
        private bool _isWaiting;

        // ── IInteractable ──────────────────────────────────────

        public bool CanInteract(PlayerInteraction player) =>
            _isWaiting && player.IsHoldingFood;

        public bool CanInteractHold(PlayerInteraction player) => false;

        public void Interact(PlayerInteraction player)
        {
            if (!CanInteract(player)) return;
            FoodItem? food = player.ReleaseFood();
            if (food == null) return;
            if (food.CurrentData == _recipe.finalMeal)
                _served = true;
            Destroy(food.gameObject);
        }

        public bool InteractHold(PlayerInteraction player, float deltaTime) => false;

        public void CancelInteract() { }

        // ── Init ───────────────────────────────────────────────

        public void Init(
            WaitingSpot spot,
            RecipeSO recipe,
            Vector2 exitPoint,
            Action onExit)
        {
            _waitingSpot = spot;
            _recipe = recipe;
            _exitPoint = exitPoint;
            _onExit = onExit;

            spot.Claim();

            if (_recipeImage != null) _recipeImage.sprite = recipe.recipeCardSprite;
            if (_patienceBar != null) _patienceBar.value = 1f;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            _waitingSpot?.Release();
        }

        // ── Lifecycle ──────────────────────────────────────────

        public void RunLifecycle()
        {
            RunLifecycleAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid RunLifecycleAsync(CancellationToken ct)
        {
            if (_orderBubble != null) _orderBubble.SetActive(false);

            await WalkToAsync(_waitingSpot.transform.position, ct);

            if (_orderBubble != null) _orderBubble.SetActive(true);
            _isWaiting = true;

            bool servedInTime = await WaitForServiceAsync(ct);

            _isWaiting = false;
            if (_orderBubble != null) _orderBubble.SetActive(false);

            if (servedInTime)
                _animator.SetBool("IsJoyful", true);
            else
                _animator.SetBool("IsAngry", true);

            await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: ct);

            _animator.SetBool("IsJoyful", false);
            _animator.SetBool("IsAngry", false);

            await WalkToAsync(_exitPoint, ct);

            _onExit?.Invoke();
            Destroy(gameObject);
        }

        private async UniTask WalkToAsync(Vector2 target, CancellationToken ct)
        {
            _animator.SetBool("IsMoving", true);

            while (Vector2.Distance(transform.position, target) > _arrivalThreshold)
            {
                Vector2 dir = (target - (Vector2)transform.position).normalized;
                _animator.SetFloat("MoveX", dir.x);
                _animator.SetFloat("MoveY", dir.y);
                transform.position = Vector2.MoveTowards(
                    transform.position, target, _walkSpeed * Time.deltaTime);
                await UniTask.Yield(ct);
            }

            transform.position = target;
            _animator.SetBool("IsMoving", false);
        }

        private async UniTask<bool> WaitForServiceAsync(CancellationToken ct)
        {
            float elapsed = 0f;
            float limit = _recipe.timeLimit;

            while (elapsed < limit)
            {
                if (_served) return true;
                elapsed += Time.deltaTime;
                if (_patienceBar != null)
                    _patienceBar.value = 1f - elapsed / limit;
                await UniTask.Yield(ct);
            }

            return false;
        }
    }
}
```

- [ ] **Step 2: Verify compile — Console không có error**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Runtime/Customer.cs Assets/Scripts/Runtime/Customer.cs.meta
git commit -m "feat: add Customer MonoBehaviour with UniTask lifecycle"
```

---

## Task 3: CustomerSpawner.cs

**Files:**
- Create: `Assets/Scripts/Runtime/CustomerSpawner.cs`

- [ ] **Step 1: Tạo CustomerSpawner.cs**

```csharp
#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using MyOvercooked.Data;
using UnityEngine;

namespace MyOvercooked.Runtime
{
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float _spawnInterval = 15f;
        [SerializeField] private int _maxConcurrentCustomers = 3;

        [Header("References")]
        [SerializeField] private GameObject[] _customerPrefabs = null!;
        [SerializeField] private WaitingSpot[] _waitingSpots = null!;
        [SerializeField] private Transform _exitPoint = null!;
        [SerializeField] private RecipeDatabaseSO _recipeDatabase = null!;

        private int _activeCount;

        private void Start()
        {
            SpawnLoopAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid SpawnLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(_spawnInterval),
                    cancellationToken: ct);

                if (_activeCount >= _maxConcurrentCustomers) continue;
                if (_recipeDatabase.recipes.Count == 0) continue;

                WaitingSpot? freeSpot = System.Array.Find(
                    _waitingSpots, s => !s.IsOccupied);
                if (freeSpot == null) continue;

                RecipeSO recipe = _recipeDatabase.recipes[
                    Random.Range(0, _recipeDatabase.recipes.Count)];
                GameObject prefab = _customerPrefabs[
                    Random.Range(0, _customerPrefabs.Length)];

                // Spawn phía trên spot, lifecycle sẽ walk vào
                Vector2 spawnPos = (Vector2)freeSpot.transform.position + Vector2.up * 3f;
                GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
                Customer customer = go.GetComponent<Customer>();
                customer.Init(freeSpot, recipe, _exitPoint.position, OnCustomerExit);

                _activeCount++;
                customer.RunLifecycle();
            }
        }

        private void OnCustomerExit()
        {
            _activeCount--;
        }
    }
}
```

- [ ] **Step 2: Verify compile — Console không có error**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Runtime/CustomerSpawner.cs Assets/Scripts/Runtime/CustomerSpawner.cs.meta
git commit -m "feat: add CustomerSpawner with UniTask spawn loop"
```

---

## Task 4: Tạo MaleCustomer Prefab (MCP)

**Files:**
- Create: `Assets/Prefabs/MaleCustomer.prefab`

Dùng MCP tools theo thứ tự:

- [ ] **Step 1: Tạo GameObject tạm trong scene để build prefab**

Dùng `unity_gameobject_create` → name = "MaleCustomer_TEMP"

- [ ] **Step 2: Add SpriteRenderer**

Dùng `unity_component_add` → component = "SpriteRenderer"

- [ ] **Step 3: Add Animator, gán controller**

Dùng `unity_component_add` → "Animator"
Dùng `unity_component_set_reference` → property = "runtimeAnimatorController", value = "Assets/Animations2/MaleCustomer/MaleCustomer_Animator.controller"

- [ ] **Step 4: Add BoxCollider2D (IsTrigger=true), set layer = Interactable**

Dùng `unity_component_add` → "BoxCollider2D"
Dùng `unity_component_set_property` → "isTrigger" = true
Dùng `unity_component_set_property` trên GameObject → layer = Interactable (layer index phù hợp)

- [ ] **Step 5: Tạo OrderBubble child (Canvas WorldSpace)**

Dùng `unity_gameobject_create` → name = "OrderBubble", parent = MaleCustomer_TEMP
Dùng `unity_component_add` → "Canvas"
Set Canvas renderMode = WorldSpace, scale ~ (0.01, 0.01, 0.01)

- [ ] **Step 6: Thêm RecipeImage (Image) và PatienceBar (Slider) vào OrderBubble**

Tạo 2 child GameObjects: "RecipeImage", "PatienceBar"
Add "UnityEngine.UI.Image" vào RecipeImage
Add "UnityEngine.UI.Slider" vào PatienceBar
Set Slider direction = BottomToTop hoặc LeftToRight, minValue=0, maxValue=1

- [ ] **Step 7: Add Customer component, wire serialized refs**

Dùng `unity_component_add` → "MyOvercooked.Runtime.Customer"
Wire: `_orderBubble` = OrderBubble child, `_recipeImage` = RecipeImage, `_patienceBar` = PatienceBar

- [ ] **Step 8: Lưu thành prefab**

Dùng `unity_asset_create_prefab` → path = "Assets/Prefabs/MaleCustomer.prefab", source = MaleCustomer_TEMP
Xóa MaleCustomer_TEMP khỏi scene

- [ ] **Step 9: Commit**

```bash
git add Assets/Prefabs/MaleCustomer.prefab Assets/Prefabs/MaleCustomer.prefab.meta
git commit -m "feat: create MaleCustomer prefab with Animator + OrderBubble"
```

---

## Task 5: Tạo FemaleCustomer Prefab (MCP)

**Files:**
- Create: `Assets/Prefabs/FemaleCustomer.prefab`

Lặp lại Task 4 nhưng:
- name = "FemaleCustomer_TEMP"
- Animator controller = `Assets/Animations2/FemaleCustomer/FemaleCustomer_Animator.controller`
- Output = `Assets/Prefabs/FemaleCustomer.prefab`

- [ ] **Step 1–8: Giống Task 4, thay MaleCustomer → FemaleCustomer**

- [ ] **Step 9: Commit**

```bash
git add Assets/Prefabs/FemaleCustomer.prefab Assets/Prefabs/FemaleCustomer.prefab.meta
git commit -m "feat: create FemaleCustomer prefab"
```

---

## Task 6: Wire Scene — WaitingSpots + CustomerSpawner

**Scene:** Map1

- [ ] **Step 1: Tạo 3 WaitingSpot GameObjects trong scene**

Dùng `unity_gameobject_create` 3 lần:
- "WaitingSpot_1" tại vị trí phù hợp trước counter (VD: x=-2, y=-1)
- "WaitingSpot_2" tại x=0, y=-1
- "WaitingSpot_3" tại x=2, y=-1

Mỗi cái: `unity_component_add` → "MyOvercooked.Runtime.WaitingSpot"

- [ ] **Step 2: Tạo ExitPoint**

`unity_gameobject_create` → "ExitPoint" tại góc ngoài map (VD: x=-6, y=3)
Không cần component, chỉ cần Transform.

- [ ] **Step 3: Tạo CustomerSpawner**

`unity_gameobject_create` → "CustomerSpawner"
`unity_component_add` → "MyOvercooked.Runtime.CustomerSpawner"

- [ ] **Step 4: Wire CustomerSpawner refs trong Inspector**

Dùng `unity_component_set_reference`:
- `_customerPrefabs` → [MaleCustomer prefab, FemaleCustomer prefab]
- `_waitingSpots` → [WaitingSpot_1, WaitingSpot_2, WaitingSpot_3]
- `_exitPoint` → ExitPoint transform
- `_recipeDatabase` → RecipeDatabase asset

Set `_spawnInterval` = 15, `_maxConcurrentCustomers` = 3

- [ ] **Step 5: Save scene**

`unity_scene_save`

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Map1.unity
git commit -m "feat: place WaitingSpots + CustomerSpawner in Map1"
```

---

## Task 7: Smoke Test

- [ ] **Step 1: Enter PlayMode**

`unity_play_mode` → enter

- [ ] **Step 2: Chờ `_spawnInterval` giây (15s)**

Giảm tạm `_spawnInterval` = 3 trong Inspector để test nhanh.

- [ ] **Step 3: Verify customer spawn và walk đến spot**

Console không có NullRef. Customer GameObject xuất hiện, di chuyển về WaitingSpot. Animator IsMoving=true khi di chuyển, false khi đến nơi.

- [ ] **Step 4: OrderBubble hiển thị sau khi đến nơi**

RecipeImage hiện recipeCardSprite. PatienceBar đang giảm dần.

- [ ] **Step 5: Player đến gần, bấm F giữ để lấy ingredient, mang lại customer, bấm E**

PlayerInteraction phát hiện Customer (IInteractable). CanInteract = true khi đang cầm food. Interact() gọi → nếu food đúng → customer react Joyful → đi ra exit → Destroy.

- [ ] **Step 6: Để patience hết → verify Angry animation → exit**

- [ ] **Step 7: Exit PlayMode, restore _spawnInterval = 15**

- [ ] **Step 8: Commit**

```bash
git add Assets/Scenes/Map1.unity
git commit -m "test: verify Customer AI smoke test passes"
```
