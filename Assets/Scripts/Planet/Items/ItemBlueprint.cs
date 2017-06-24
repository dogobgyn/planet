
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    // Item that shows a ghost gameobject to preview where the action will happen
    public class GhostItem : Item
    {
        public GameObject m_GhostPrefab;

        public float m_PlacementRange = 1.0f;
        public Vector3 m_PlacementOffset = new Vector3();

        protected GameObject m_Ghost;
        protected TwoDee.TriggerTracker m_GhostTT;
        protected bool m_ValidPlacement = false;
        protected int m_Rotated = 0;

        protected List<Collider> m_OriginalGeo = new List<Collider>();

        protected virtual GameObject GhostPrefab
        {
            get
            {
                return m_GhostPrefab;
            }
        }

        protected void SetupGhost()
        {
            foreach(var child in m_Ghost.GetSelfAndChildrenRecursive())
            {
                foreach(var col in child.gameObject.GetComponents<Collider>())
                {
                    col.isTrigger = true;
                    m_OriginalGeo.Add(col);
                }
            }
            m_GhostTT = m_Ghost.AddComponent<TwoDee.TriggerTracker>();
            m_Ghost.SetLayerRecursive(LayerMask.NameToLayer("StructurePlatform"));
        }

        public override void UpdateSelected(UseContext args)
        {
            m_Ghost.transform.position = args.m_TargetPos + m_PlacementOffset;
            Vector3 userUpDir = args.m_ItemUserGameObject.transform.rotation * Vector3.up;
            m_Ghost.transform.rotation = Quaternion.FromToRotation(Vector3.up, userUpDir) * Quaternion.Euler(0.0f, 0.0f, m_Rotated);

            foreach (var renderer in m_Ghost.GetComponentsInSelfOrChildren<Renderer>())
            {
                renderer.material = m_ValidPlacement ? args.m_GhostMaterialGood : args.m_GhostMaterialBad;
            }
        }

        public override void Selected(UseContext args)
        {
            m_Rotated = 0;
            m_ValidPlacement = false;
            m_Ghost = GameObject.Instantiate(GhostPrefab, args.m_TargetPos, Quaternion.identity);
            SetupGhost();
        }

        public override void Unselected(UseContext args)
        {
            GameObject.DestroyImmediate(m_Ghost);
        }

    }


    public interface IBlueprintPlaced
    {
        void BlueprintPlaced(BlueprintPlaceArgs args);
    }

    public class BlueprintPlaceArgs
    {
        public BlueprintPlaceArgs(GameObject ob)
        {
            m_ob = ob;
        }
        public GameObject m_ob;
    }

    public class ItemBlueprint : GhostItem
    {
        public enum PlacementType
        {
            Default
        };
        public PlacementType m_PlacementType;
        public GameObject m_StructurePrefab;
        public bool m_CanRotate = false;

        public override bool RequiresStanding
        {
            get
            {
                return true;
            }
        }

        public virtual void OnPlaced(UseContext args, GameObject newObj)
        {
        }

        public override void Use(UseContext args)
        {
            if (m_CanRotate && args.m_ButtonPressed == 1)
            {
                m_Rotated = (m_Rotated + 30) % 360;
                return;
            }
            if (!m_ValidPlacement)
            {
                TwoDee.EasySound.Play("uierror", args.m_OriginPos);
                return;
            }
            if (m_Ghost == null || !m_Ghost) return;
            var pos = ClampedPos(args);
            var rot = m_Ghost.transform.rotation;
            var newObj = GameObject.Instantiate(m_StructurePrefab, pos, rot);
            var bp = newObj.GetComponent<IBlueprintPlaced>();
            if (bp != null)
            {
                bp.BlueprintPlaced(new BlueprintPlaceArgs(gameObject));
            }
            TwoDee.EasySound.Play("uiplacement", args.m_TargetPos);

            OnPlaced(args, newObj);
            NetworkServer.Spawn(newObj);
            

            UseQuantity(args, 1);
        }

        public override void UpdateSelected(UseContext args)
        {
            base.UpdateSelected(args);

            var clampedPos = ClampedPos(args);

            // For blueprint, we can only place nearby us.
            m_Ghost.transform.position = clampedPos;

            int consideredGround = (GameObjectExt.GetLayerMask("Ground")) | (GameObjectExt.GetLayerMask("StructurePlatform"));

            // Should be clear to the target as well
            var origin = args.m_OriginPos;
            var delta = args.m_TargetPos - args.m_OriginPos;
            RaycastHit hitInfo;
            m_ValidPlacement = m_GhostTT.CollidingObjects.ToList().Count == 0;
            if (Physics.Raycast(origin, delta.normalized, out hitInfo, delta.magnitude, consideredGround))
            {
               // m_ValidPlacement = false;
            }
        }

        public Vector3 ClampedPos(UseContext args)
        {
            //var pos = args.m_TargetPos + m_PlacementOffset;
            float dist = args.DirectionDistance;
            dist = Mathf.Min(dist, m_PlacementRange);

            return args.m_OriginPos + args.Direction * dist + m_PlacementOffset;
        }

        public override void Selected(UseContext args)
        {
            base.Selected(args);
        }

        public override void Unselected(UseContext args)
        {
            base.Unselected(args);
        }
    }
}