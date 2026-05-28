#nullable enable

using System;
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
        [SerializeField] private Transform _spawnPoint = null!;
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
                    TimeSpan.FromSeconds(_spawnInterval),
                    cancellationToken: ct);

                if (_activeCount >= _maxConcurrentCustomers) continue;
                if (_recipeDatabase.recipes.Count == 0) continue;

                WaitingSpot? freeSpot = Array.Find(
                    _waitingSpots, s => !s.IsOccupied);
                if (freeSpot == null) continue;

                RecipeSO recipe = _recipeDatabase.recipes[
                    UnityEngine.Random.Range(0, _recipeDatabase.recipes.Count)];
                GameObject prefab = _customerPrefabs[
                    UnityEngine.Random.Range(0, _customerPrefabs.Length)];

                Vector2 spawnPos = _spawnPoint != null
                    ? (Vector2)_spawnPoint.position
                    : (Vector2)freeSpot.transform.position + Vector2.down * 8f;
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
