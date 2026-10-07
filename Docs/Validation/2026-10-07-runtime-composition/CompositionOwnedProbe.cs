using System;
using UnityEngine;

// Validation-only native callback probe, copied to the isolated project.
public sealed class CompositionOwnedProbe : MonoBehaviour
{
    [NonSerialized] public Action Disabled;
    private void OnDisable() { Disabled?.Invoke(); }
}
