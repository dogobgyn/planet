using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;



namespace TwoDee
{
    public class ComponentCacheSingle<T>
    {
        T m_Value;
        public T Get()
        {
            return m_Value;
        }

        public ComponentCacheSingle(GameObject obj)
        {
            m_Value = obj.GetComponent<T>();
            if (m_Value == null)
            {
                m_Value = obj.GetComponentInParent<T>();
            }
            if (m_Value == null)
            {
                m_Value = obj.GetComponentInChildren<T>();
            }
        }
    }

    public class ComponentCache
    {
        GameObject m_Object;


        public ComponentCacheSingle<Collider> m_Collider;
        public Collider Collider
        {
            get
            {
                if (m_Collider == null)
                {
                    m_Collider = new ComponentCacheSingle<Collider>(m_Object);
                }
                return m_Collider.Get();
            }
        }

        public ComponentCacheSingle<Rigidbody> m_Rigidbody;
        public Rigidbody Rigidbody
        {
            get
            {
                if (m_Rigidbody == null)
                {
                    m_Rigidbody = new ComponentCacheSingle<Rigidbody>(m_Object);
                }
                return m_Rigidbody.Get();
            }
        }

        public ComponentCacheSingle<Renderer> m_Renderer;
        public Renderer Renderer
        {
            get
            {
                if (m_Renderer == null)
                {
                    m_Renderer = new ComponentCacheSingle<Renderer>(m_Object);
                }
                return m_Renderer.Get();
            }
        }

        public ComponentCacheSingle<Team> m_Team;
        public Team Team
        {
            get
            {
                if (m_Team == null)
                {
                    m_Team = new ComponentCacheSingle<Team>(m_Object);
                }
                return m_Team.Get();
            }
        }

        public ComponentCacheSingle<Health> m_Health;
        public Health Health
        {
            get
            {
                if (m_Health == null)
                {
                    m_Health = new ComponentCacheSingle<Health>(m_Object);
                }
                return m_Health.Get();
            }
        }

        public ComponentCache(GameObject obj)
        {
            m_Object = obj;
        }
    }

    public static class ComponentCacheExtension
    {
        static Dictionary<GameObject, ComponentCache> m_Cache = new Dictionary<GameObject, ComponentCache>();

        public static ComponentCache ComponentCache(this GameObject obj)
        {
            ComponentCache cache;
            if (!m_Cache.TryGetValue(obj, out cache))
            {
                cache = new TwoDee.ComponentCache(obj);
                m_Cache[obj] = cache;
            }
            return cache;
        }
    }

    public static class ComponentList
    {
        static Dictionary<Type, List<object>> m_FindObjectsOfTypeCache = new Dictionary<Type, List<object>>();

        public static List<object> GetListOfType(Type type)
        {
            List<object> list;
            if (!m_FindObjectsOfTypeCache.TryGetValue(type, out list))
            {
                list = new List<object>();
                m_FindObjectsOfTypeCache[type] = list;
            }

            return list;
        }

        public static T GetFirst<T>() where T:class
        {
            var list = GetCopiedListOfType(typeof(T));
            return list.Count == 0 ? null : (T)list[0];
        }

        public static List<object> GetCopiedListOfType(Type type)
        {
            var origList = GetListOfType(type);
            return new List<object>(origList);
        }

        public static IEnumerable<T> GetCopiedListOfType<T>() where T : class
        {
            foreach(var obj in GetCopiedListOfType(typeof(T)))
            {
                yield return obj as T;
            }
        }

        public static T GetClosest<T>(Vector3 pos) where T : MonoBehaviour
        {
            T result = null;
            float closestDist = float.MaxValue;
            foreach(var obj in GetCopiedListOfType<T>())
            {
                var dist = (pos - obj.transform.position).magnitude;
                if (dist < closestDist)
                {
                    result = obj;
                    closestDist = dist;
                }
            }

            return result;
        }

        public static IEnumerable<Type> GetInheritanceHierarchy (this Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
                yield return current;
        }

        private static void OnStartEnd(object obj, bool start)
        {
            int timeout = 3;
            bool first = true;
            foreach (var type in obj.GetType().GetInheritanceHierarchy())
            {
                var list = GetListOfType(type);
                if (first)
                {
                    foreach (Type tinterface in type.GetInterfaces())
                    {
                        var listi = GetListOfType(tinterface);
                        if (start)
                        {
                            listi.Add(obj);
                        }
                        else
                        {
                            listi.Remove(obj);
                        }
                    }
                    first = false;
                }
                if (start)
                {
                    list.Add(obj);
                }
                else
                {
                    list.Remove(obj);
                }
                timeout--;
                if (timeout == 0) return;
            }
        }

        public static void OnStart(object obj)
        {
            OnStartEnd(obj, true);
        }

        public static void OnEnd(object obj)
        {
            OnStartEnd(obj, false);
        }
    }
}