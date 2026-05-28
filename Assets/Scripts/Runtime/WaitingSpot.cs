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
