using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns bound objects and callbacks to one owner's activation.</summary>
internal sealed class SpawnScope
{
    private readonly UnityEngine.Object owner;
    private readonly List<GameObject> boundObjects = new List<GameObject>();
    private int generation;
    private bool active = true;

    public SpawnScope(UnityEngine.Object owner) { this.owner = owner; }
    public bool IsActive => active && owner != null;
    public IEnumerable<GameObject> BoundObjects => boundObjects;

    public void BeginActivation() { active = true; generation++; }
    public void EndActivation() { active = false; generation++; }

    public void Track(GameObject instance)
    {
        if (instance == null) return;
        boundObjects.RemoveAll(value => value == null);
        if (!IsActive)
        {
            instance.SetActive(false);
            UnityEngine.Object.Destroy(instance);
            return;
        }
        if (!boundObjects.Contains(instance)) boundObjects.Add(instance);
    }

    public void Forget(GameObject instance) { boundObjects.Remove(instance); }

    // Independent projectiles keep running; only their callback to this owner expires.
    public Action Guard(Action callback)
    {
        int capturedGeneration = generation;
        return () =>
        {
            if (IsActive && generation == capturedGeneration) callback?.Invoke();
        };
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, bool boundToOwner)
    {
        if (prefab == null) return null;
        GameObject instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
        if (boundToOwner) Track(instance);
        return instance;
    }

    public void ReleaseBoundObjects()
    {
        EndActivation();
        // Child cleanup may unregister itself from this scope during SetActive(false).
        GameObject[] releasing = boundObjects.ToArray();
        boundObjects.Clear();
        foreach (GameObject instance in releasing)
        {
            if (instance == null) continue;
            instance.SetActive(false);
            UnityEngine.Object.Destroy(instance);
        }
    }
}
