#nullable enable

using UnityEngine;
using VContainer;
using MyOvercooked.Data;

namespace MyOvercooked.Runtime
{
    public sealed class TimedStation : StationBase, IInteractable
    {
        [Header("Station Settings")]
        public StationType stationType = StationType.Stove;

        [Header("UI")]
        public ProgressUI progressUI = null!;

        [SerializeField] private RecipeDatabaseSO _recipeDatabase = null!;

        private TransformationSO? _currentRecipe;
        private float _cookingProgress;
        private float _cookingDuration;
        private bool _isCooking;

        [Inject]
        public void Construct(RecipeDatabaseSO recipeDatabase)
        {
            if (recipeDatabase != null) _recipeDatabase = recipeDatabase;
        }

        public bool CanInteract(PlayerInteraction player) =>
            (!player.IsHoldingFood && HasFood) || (player.IsHoldingFood && !HasFood);

        public bool CanInteractHold(PlayerInteraction player) =>
            HasFood && _currentRecipe != null && !_isCooking;

        public void Interact(PlayerInteraction player)
        {
            if (!player.IsHoldingFood && HasFood)
            {
                FullReset();
                player.PickupFood(TryRemoveFood());
                return;
            }

            if (player.IsHoldingFood && !HasFood)
            {
                TryAddFood(player.ReleaseFood());
                CacheRecipe();
                return;
            }

            if (player.IsHoldingFood && HasFood)
            {
                if (!_isCooking)
                {
                    var inputs = new[] { CurrentFood.CurrentData, player.CurrentHeldFood.CurrentData };
                    if (_recipeDatabase.TryGetTransformation(inputs, stationType, out var combo) && combo.duration == 0)
                    {
                        var heldFood = player.ReleaseFood();
                        Object.Destroy(heldFood.gameObject);
                        CurrentFood.Setup(combo.output);
                        CacheRecipe();
                        return;
                    }
                }

                var plateOnStove = CurrentFood.GetComponent<Plate>();
                if (plateOnStove != null && plateOnStove.CanInteract(player))
                {
                    plateOnStove.Interact(player);
                    return;
                }

                var heldPlate = player.CurrentHeldFood.GetComponent<Plate>();
                if (heldPlate != null)
                {
                    FullReset();
                    heldPlate.AddIngredient(TryRemoveFood());
                }
            }
        }

        public bool InteractHold(PlayerInteraction player, float deltaTime)
        {
            if (!HasFood || _currentRecipe == null) return false;
            _isCooking = true;
            return true;
        }

        public void CancelInteract() { }

        private void Update()
        {
            if (!_isCooking || !HasFood || _currentRecipe == null) return;

            _cookingProgress += Time.deltaTime;
            progressUI?.ShowProgress(_cookingProgress, _cookingDuration);

            if (_cookingProgress >= _cookingDuration)
            {
                CurrentFood.Setup(_currentRecipe.output);
                _isCooking = false;
                _cookingProgress = 0f;
                progressUI?.Hide();
                CacheRecipe();
            }
        }

        private void CacheRecipe()
        {
            _currentRecipe = null;
            if (!HasFood) return;

            var inputs = new[] { CurrentFood.CurrentData };
            if (_recipeDatabase.TryGetTransformation(inputs, stationType, out var result))
            {
                _currentRecipe = result;
                _cookingDuration = result.duration > 0 ? result.duration : 1f;
            }
        }

        private void FullReset()
        {
            _isCooking = false;
            _cookingProgress = 0f;
            _currentRecipe = null;
            progressUI?.Hide();
        }
    }
}
