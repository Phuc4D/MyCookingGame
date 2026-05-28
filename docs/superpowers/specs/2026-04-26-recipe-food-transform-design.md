# Recipe & Food Transformation System — Design Spec

**Date:** 2026-04-26
**Project:** MyOvercooked (2D Top-Down Cooking Game)
**Scope:** Hệ thống công thức và biến đổi thức ăn

---

## Context & Constraints

- Game 2D Top-Down tương tự Overcooked
- 5 map, nhiều loại món ăn (xem sprite list bên dưới)
- 1 player hiện tại, thiết kế multiplayer-ready
- Unity 6, C# 9, VContainer + SignalBus

---

## Quyết định thiết kế cốt lõi

| Câu hỏi | Quyết định | Lý do |
|---|---|---|
| Cấu trúc recipe | **Hybrid pipeline** | Mỗi nguyên liệu xử lý riêng (chặt/nấu), kết hợp ở bước cuối |
| Cơ chế kết hợp | **Station Slot** | Station có slots, auto-match recipe khi slots thay đổi |
| Multiplayer | **1 player trước, mở rộng sau** | Station/FoodItem không phụ thuộc input → thêm player 2 không cần sửa core |

---

## Architecture: 3 Layers

### Layer 1 — Data (ScriptableObjects)

Toàn bộ data định nghĩa bằng SO assets. Thêm món mới = tạo file asset, không sửa code.

#### `FoodStateSO : ScriptableObject`
Đại diện cho **một trạng thái** của food.

```
string id              // "fish", "chopped_fish", "cooked_fish_tomato", "fish_meal_tomato"
string displayName
Sprite sprite
FoodCategory category  // enum: Raw | Chopped | Cooked | Meal
```

#### `TransformationSO : ScriptableObject`
Đại diện cho **một phép biến đổi**.

```
FoodStateSO[] inputs        // nguyên liệu đầu vào (1 hoặc nhiều)
StationType requiredStation  // enum: ChoppingBoard | Stove | PlatingTable | Fridge
float duration               // giây. 0 = instant
FoodStateSO output           // kết quả
```

Ví dụ assets:
- `chop_fish`: `[fish]` + ChoppingBoard (0s) → `chopped_fish`
- `cook_fish_tomato`: `[chopped_fish, chopped_tomato]` + Stove (3s) → `cooked_fish_tomato`
- `plate_fish_tomato`: `[cooked_fish_tomato]` + PlatingTable (0s) → `fish_meal_tomato`

#### `RecipeSO : ScriptableObject`
Dùng cho **Order System** — định nghĩa đơn hàng.

```
string recipeName
FoodStateSO finalMeal    // trỏ đến FoodStateSO category = Meal
Sprite recipeCardSprite
int scoreValue
float timeLimit          // giây để hoàn thành đơn
```

#### `RecipeDatabaseSO : ScriptableObject`
**Điểm trung tâm duy nhất** chứa toàn bộ transformations và recipes. Mọi station đều reference SO này qua Inspector.

```
List<TransformationSO> allTransformations
List<RecipeSO> allRecipes

bool TryGetTransformation(
    FoodStateSO[] inputs,
    StationType station,
    out TransformationSO result)

bool TryMatchRecipe(
    FoodStateSO meal,
    out RecipeSO result)
```

---

### Layer 2 — Runtime (MonoBehaviours)

Các component sống trong scene. Không chứa data — chỉ thực thi logic và query RecipeDatabaseSO.

#### `FoodItem : MonoBehaviour`
Gắn vào mỗi food GameObject trong scene.

**Trách nhiệm:** Track `currentState`, cập nhật sprite khi state thay đổi.

```
FoodStateSO currentState       // readonly từ ngoài
SpriteRenderer spriteRenderer

void ChangeState(FoodStateSO next)   // cập nhật state + sprite
```

#### `StationBase : MonoBehaviour` (abstract)
Base class cho mọi trạm làm việc.

**Trách nhiệm:** Quản lý slots, gọi `TryProcess()` khi slots thay đổi.

```
RecipeDatabaseSO database   // assign qua Inspector
StationType stationType
List<FoodItem> slots

void AddFood(FoodItem food)      // thêm food vào slot → gọi TryProcess()
void RemoveFood(FoodItem food)   // xóa food khỏi slot → gọi TryProcess()
protected abstract void TryProcess()
```

#### `InstantStation : StationBase`
Dùng cho: **ChoppingBoard**, **PlatingTable**

`TryProcess()`: query database → nếu match, destroy input FoodItems, spawn output FoodItem ngay lập tức.

#### `TimedStation : StationBase`
Dùng cho: **Stove**, **Oven**

```
float progress
bool isCooking
```

`TryProcess()`: query database → nếu match, bắt đầu đếm giờ (UniTask) → hết giờ spawn output. Hiển thị progress bar cho player.

**Edge cases cần xử lý:**
- Player lấy food ra trong khi đang nấu → cancel timer, reset progress
- Nấu quá lâu (burnt) → optional: đổi output thành `burnt.asset` (mở rộng sau)

#### `IngredientSource : MonoBehaviour`
Gắn vào kệ nguyên liệu trong map.

```
FoodStateSO ingredient   // assign qua Inspector (fish.asset, meat.asset...)
void OnInteract()        // Chef tương tác → Instantiate FoodItem với state = ingredient
```

---

### Layer 3 — Data Flow (Ví dụ hoàn chỉnh)

**Nấu `fish_meal_tomato`:**

```
1. Chef tương tác IngredientSource(fish)
   → Spawn FoodItem(state=fish)

2. Chef đặt fish vào ChoppingBoard
   → AddFood(fish) → TryProcess()
   → Database: [fish] + ChopBoard → match ✅
   → Destroy fish, spawn FoodItem(state=chopped_fish)

3. Tương tự: tomato → chopped_tomato

4. Chef đặt chopped_fish vào Stove
   → TryProcess(): [chopped_fish] + Stove → no match ❌

5. Chef đặt chopped_tomato vào Stove
   → TryProcess(): [chopped_fish, chopped_tomato] + Stove → match ✅
   → Start timer 3s → spawn FoodItem(state=cooked_fish_tomato)

6. Chef đặt cooked_fish_tomato vào PlatingTable
   → TryProcess(): [cooked_fish_tomato] + Plating → match ✅
   → Spawn FoodItem(state=fish_meal_tomato)

7. Chef giao fish_meal_tomato lên Serving Counter
   → TryMatchRecipe(fish_meal_tomato) → tìm RecipeSO → cộng điểm
```

---

## Dependency Map

```
Chef/PlayerController
  └── gọi AddFood() / RemoveFood() trên StationBase
         └── query RecipeDatabaseSO.TryGetTransformation()
                └── đọc TransformationSO assets
                └── tạo/xóa FoodItem (chỉ biết FoodStateSO)

ServingCounter
  └── query RecipeDatabaseSO.TryMatchRecipe()
         └── đọc RecipeSO assets
         └── thông báo OrderSystem (scope ngoài spec này)
```

**Nguyên tắc:** Chef không biết về Recipe. Station không biết về Player. RecipeDatabaseSO là điểm duy nhất có matching logic.

---

## Multiplayer Readiness

Khi mở rộng lên 2 player:
- Thêm `PlayerController` thứ 2 với input bindings riêng
- Cả 2 controller gọi cùng `AddFood()` / `RemoveFood()` API
- Station, FoodItem, RecipeDatabase **không thay đổi gì**

---

## Enums cần định nghĩa

```csharp
public enum FoodCategory { Raw, Chopped, Cooked, Meal }
public enum StationType  { ChoppingBoard, Stove, PlatingTable, Fridge }
```

---

## Ngoài scope spec này

- OrderSystem (sinh đơn hàng, đếm thời gian, tính điểm)
- PlayerController & input handling
- Animation system (Chef states)
- Map loading từ Tiled (.tmx)
- UI (recipe cards, score, timer)
