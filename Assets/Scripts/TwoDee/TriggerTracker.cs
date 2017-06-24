using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TwoDee
{
    public class TriggerTracker : MonoBehaviour
    {
        List<GameObject> m_CollidingObjects = new List<GameObject>();
        public IEnumerable<GameObject> CollidingObjects
        {
            get
            {                
                foreach(var ob in m_CollidingObjects)
                {
                    var col = ob.GetComponent<Collider>();
                    if (!col.isTrigger) yield return ob;
                }
            }
        }
        void OnTriggerEnter(Collider other)
        {
            m_CollidingObjects.Add(other.gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            m_CollidingObjects.Remove(other.gameObject);
        }
    }
}