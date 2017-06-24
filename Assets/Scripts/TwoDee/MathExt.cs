using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public static class MathExt
{
    public static Rect MakeRect(this Vector2 v, float ex, float ey)
    {
        return new Rect(v.x - ex, v.y - ey, ex * 2, ey * 2);
    }
    public static int RoundToMultiple(this int x, int m)
    {
        return ((x + m / 2) / m) * m;
    }
    public static int FloorToMultiple(this int x, int m)
    {
        return ((x) / m) * m;
    }
    public static int CeilToMultiple(this int x, int m)
    {
        return ((x + m -1 ) / m) * m;
    }
    public static Ray RayTo(this Vector3 start, Vector3 end)
    {
        Vector3 delta = (end - start);
        Vector3 fwd = delta.normalized;
        return new Ray(start, fwd);
    }
    public static float DistanceTo(this Vector3 start, Vector3 end)
    {
        Vector3 delta = (end - start);
        return delta.magnitude;
    }
    public static Bounds CreateBounds(Vector3 ll, Vector3 ur)
    {
        var center = (ll + ur) * 0.5f;
        var delta = (ur - ll) * 0.5f;
        return new Bounds(center, delta);
    }
}
