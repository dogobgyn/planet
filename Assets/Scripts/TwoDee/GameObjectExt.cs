using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public static class GameObjectExt
{
    public static int GetLayerMask(string name)
    {
        return 1 << LayerMask.NameToLayer(name);
    }
    public static float SmoothPairInterp(this float[] data, float t)
    {
        if (data == null || (data.Length %2 != 0))
        {
            Debug.LogError("SmoothPairInterp bad");
        }
        int curLoc = 0;
        while(data[curLoc+2] < t)
        {
            curLoc += 2;
        }

        float normalizedT = (t - data[curLoc]) / (data[curLoc + 2] - data[curLoc]);
        return Mathf.SmoothStep(data[curLoc + 1], data[curLoc + 3], normalizedT);
    }
    public static Ray TransformRay(this Transform t, Ray ray_ws)
    {
        return new Ray(t.InverseTransformPoint(ray_ws.origin), t.InverseTransformDirection(ray_ws.direction));
    }

    public static Rect Expand(this Rect r, int x, int y)
    {
        return new Rect(r.x - x, r.y - y, r.width + 2 * x, r.height + 2 * y);
    }

    public static bool IntersectLineSegment(this Bounds bounds, Vector3 start, Vector3 end, out Vector3 hit)
    {
        hit = new Vector3();

        var delta = (end - start);
        var length = delta.magnitude;
        var dir = delta.normalized;
        var ray = new Ray(start, dir);
        float dist;
        if (bounds.IntersectRay(ray, out dist))
        {
            if (dist > length) return false;

            hit = start + dir * dist;
            return true;
        }

        return false;
    }

    public static Vector3 NearestPointOnFiniteLine(Vector3 start, Vector3 end, Vector3 pnt)
    {
        var line = (end - start);
        var len = line.magnitude;
        line.Normalize();

        var v = pnt - start;
        var d = Vector3.Dot(v, line);
        d = Mathf.Clamp(d, 0f, len);
        return start + line * d;
    }

    public static T GetNearestObject<T>(Vector3 pos, float radius, int layerMask) where T :MonoBehaviour
    {
        var objs = GetNearbyObjects<T>(pos, radius, layerMask);
        T result = default(T);
        foreach(var comp in objs)
        {
            if (result == null) result = comp;
            else if( (result.gameObject.transform.position - pos).sqrMagnitude > (comp.gameObject.transform.position - pos).sqrMagnitude)
            {
                result = comp;
            }
        }

        return result;
    }

    static void ParticleAddAutoDestroy(this GameObject obj, bool detachAndStop)
    {
        var particleChildren = obj.transform.GetChildrenRecursive();
        foreach (var child in particleChildren)
        {
            var parti = child.GetComponent<ParticleSystem>();
            if (parti != null)
            {
                if (detachAndStop)
                {
                    child.SetParent(null, true);
                    parti.Stop();
                }
                var stopper = parti.GetComponent<TwoDee.ParticleAutoDestroy>();
                if (stopper == null)
                {
                    parti.gameObject.AddComponent<TwoDee.ParticleAutoDestroy>();
                }
            }
        }
    }

    public static void StartParticle(this GameObject obj)
    {
        ParticleAddAutoDestroy(obj, false);
    }

    public static void SetLayerRecursive(this GameObject obj, int layer)
    {
        foreach(var child in obj.transform.GetChildrenRecursive())
        {
            child.gameObject.layer = layer;
        }
    }

    public static void DestroyReleaseParticles(this GameObject obj)
    {
        ParticleAddAutoDestroy(obj,true);
       
        GameObject.Destroy(obj);
    }

    public static IEnumerable<T> GetComponentsInSelfOrChildren<T>(this GameObject obj)
    {
        foreach(var cur in obj.GetComponentsInChildren<T>())
        {
            yield return cur;
        }
    }

    public static T GetComponentInSelfOrChildren<T>(this GameObject obj) where T:class
    {
        foreach (var cur in obj.GetComponentsInChildren<T>())
        {
            return cur;
        }

        return null;
    }

    public static T GetComponentInSelfOrParents<T>(this GameObject obj) where T:class
    {
        var result = obj.GetComponent<T>();
        if (result is Component)
        {
            if (result != null && (result as Component)) return result;
        }
        else if(result != null)
        {
            return result;
        }

        return obj.GetComponentInParent<T>();
    }

    public static IEnumerable<T> GetComponentsInSelfOrParents<T>(this GameObject obj)
    {
        // Confusingly, GetComponentsInParent is parents OR self.
        foreach (var cur in obj.GetComponentsInParent<T>())
        {
            yield return cur;
        }
    }

    public static List<T> GetNearbyObjects<T>(Vector3 pos, float radius, int layerMask)
    {
        var result = new List<T>();
        foreach (var col in Physics.OverlapSphere(pos, radius, layerMask))
        {
            foreach (var comp in col.gameObject.GetComponentsInSelfOrParents<T>())
            {
                result.Add(comp);
            }
        }

        return result;
    }

    public static GameObject GetTopParent(this GameObject obj)
    {
        GameObject lastParent = obj;
        GameObject currentParent = lastParent.GetParent();
        while(currentParent != null)
        {
            lastParent = currentParent;
            currentParent = currentParent.GetParent();
        }
        return lastParent;
    }

    public static GameObject GetParent(this GameObject obj)
    {
        if (obj == null) return null;
        var transparent = obj.transform.parent;
        if (transparent == null) return null;

        return transparent.gameObject;
    }

    public static T RandomlyPick<T>(this T[] array, float value, float[] percents=null)
    {
        if (percents == null)
        {
            int index = Mathf.FloorToInt(value * array.Length);
            if (index >= array.Length) index = array.Length - 1;
            return array[index];
        }
        if (array.Length != percents.Length)
        {
            Debug.LogError("RandomlyPick array size mismatch");
        }
        float intervalStart = 0.0f;
        float intervalEnd = 0.0f;
        for(int i=0;i<percents.Length;i++)
        {
            intervalEnd = intervalStart + percents[i];
            if (value >= intervalStart && value <= intervalEnd) return array[i];
            intervalStart = intervalEnd;
        }

        return array[0];
    }

    public static List<T> ToList<T>(this IEnumerable<T> enumera)
    {
        List<T> result = new List<T>();
        foreach(var x in enumera)
        {
            result.Add(x);
        }
        return result;
    }

    public enum ExtBlendMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent
    }
    public static void SetBlendMode(this Material material, ExtBlendMode blendMode)
    {
        switch (blendMode)
        {
            case ExtBlendMode.Opaque:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case ExtBlendMode.Cutout:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                break;
            case ExtBlendMode.Fade:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
            case ExtBlendMode.Transparent:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
        }
    }
}
