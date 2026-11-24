using UnityEngine;

namespace DemonIdol.BlazBlue.Visual
{
    /// <summary>
    /// Lightweight marker component attached to all weapon root instances and ScaleSockets
    /// spawned by BBWeaponRig. Enables safe, deterministic cleanup on character swap or respawn
    /// without relying on fragile substring name matching.
    /// </summary>
    [DisallowMultipleComponent]
    public class BBSpawnedWeaponMarker : MonoBehaviour
    {
    }
}
