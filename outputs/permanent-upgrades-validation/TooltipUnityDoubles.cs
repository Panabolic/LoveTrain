using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float b) => new Vector2(a.x * b, a.y * b);
        public static Vector2 Scale(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);
        public static implicit operator Vector3(Vector2 value) => new Vector3(value.x, value.y, 0);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static implicit operator Vector2(Vector3 value) => new Vector2(value.x, value.y);
    }
    public struct Rect
    {
        public float xMin, yMin, xMax, yMax;
        public Rect(float x, float y, float width, float height)
        { xMin = x; yMin = y; xMax = x + width; yMax = y + height; }
        public Vector2 size => new Vector2(xMax - xMin, yMax - yMin);
        public Vector2 center => new Vector2((xMin + xMax) / 2, (yMin + yMax) / 2);
    }
    public class Transform { }
    public class RectTransform : Transform
    {
        public Rect rect;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector3 position;
        public Vector2 worldScale = new Vector2(1, 1);
        public GameObject gameObject = new GameObject();
        public int lastSiblingCalls;
        public void SetAsLastSibling() { lastSiblingCalls++; }
        public Vector3 TransformPoint(Vector3 point) => new Vector3(position.x + point.x * worldScale.x, position.y + point.y * worldScale.y, position.z + point.z);
        public Vector3 InverseTransformPoint(Vector3 point) => new Vector3((point.x - position.x) / worldScale.x, (point.y - position.y) / worldScale.y, point.z - position.z);
    }
    public class GameObject
    {
        public bool activeSelf;
        public void SetActive(bool active) { activeSelf = active; }
    }
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
namespace UnityEngine.EventSystems
{
    public class BaseEventData { }
    public class PointerEventData : BaseEventData { public Vector2 position; }
}
public sealed class PermanentUpgradeNode { }
public sealed partial class PermanentUpgradeMenu
{
    private RectTransform panel, tooltip;
    private PermanentUpgradeNode hovered;
    public int RefreshCalls;
    public PermanentUpgradeNode Hovered => hovered;
    public PermanentUpgradeMenu(RectTransform panel, RectTransform tooltip) { this.panel = panel; this.tooltip = tooltip; }
    public void Position(Vector2 anchor) => PlaceTooltip(anchor);
    private void RefreshTooltip() { RefreshCalls++; }
}
public sealed partial class PermanentUpgradeCard
{
    private PermanentUpgradeMenu owner;
    private PermanentUpgradeNode node;
    private bool pointerInside;
    public bool PointerInside => pointerInside;
    public Transform transform;
    public PermanentUpgradeCard(PermanentUpgradeMenu owner, PermanentUpgradeNode node, RectTransform transform)
    { this.owner = owner; this.node = node; this.transform = transform; }
}
