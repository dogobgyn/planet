
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemMelee : Item
    {
        public string m_HarvestType;
        public GameObject m_WeaponPrefab;
        public float m_MeleeDamage = 5.0f;
        public float m_ChopAmount = 0.0f;

        public float m_BreakChance = 0.5f;
        public GameObject m_BreakParticlePrefab;

        GameObject m_Weapon;

        GameObject CreateWeapon(Vector3 origin, Vector3 target)
        {
            float POSITION_WEAPON_AWAY = 1.0f; //0.7f

            var delta = target - origin;
            delta.Normalize();
            bool flipped = (delta.x < 0.0f);
            var weaponLoc = origin + POSITION_WEAPON_AWAY * delta;
            var weapon = GameObject.Instantiate<GameObject>(m_WeaponPrefab, weaponLoc,
                Quaternion.FromToRotation(Vector3.up, delta) * Quaternion.Euler(0.0f, flipped ? 180.0f : 0.0f, 0.0f)
               );
            weapon.GetComponent<Rigidbody>().isKinematic = true;

            return weapon;
        }

        bool m_DidSomething = false;
        protected override void VirtualBeginUse(UseContext args)
        {
            m_Weapon = CreateWeapon(args.m_OriginPos, args.m_TargetPos);
            TwoDee.EasySound.Play("swing", gameObject);

            // What stuff did we smack
            var didHitAlready = new Dictionary<GameObject, bool>();
            var weaponLoc = m_Weapon.transform.position;
            m_DidSomething = false;
            foreach (var hit in Physics.OverlapSphere(weaponLoc, 1.0f))
            {
                var goSelf = hit.GetComponent<Collider>().gameObject;
                var go = goSelf.GetTopParent();

                if (go == m_Weapon) continue;
                if (go == args.m_ItemUserGameObject) continue;


                if (!didHitAlready.ContainsKey(go))
                {
                    didHitAlready[go] = true;

                    if (TwoDee.Health.ApplyDamageIfEnemy(go, args.m_ItemUserGameObject, new TwoDee.DamageArgs(m_MeleeDamage, TwoDee.DamageType.Physical, args.m_ItemUserGameObject, weaponLoc)))
                    {
                        m_DidSomething = true;
                    }

                    if (m_ChopAmount > 0.0f)
                    {
                        var tree = go.GetComponentInSelfOrParents<Tree>();
                        if (tree != null)
                        {
                            ChargeTimeCost();
                            tree.Chop(m_ChopAmount, args.m_OriginPos);
                            m_DidSomething = true;
                        }
                    }
                }
            }


        }

        protected override void VirtualEndUse(UseContext args)
        {
            if (m_DidSomething)
            {
                if (UnityEngine.Random.value < m_BreakChance)
                {
                    if (m_BreakParticlePrefab != null) GameObject.Instantiate<GameObject>(m_BreakParticlePrefab, m_Weapon.transform.position, Quaternion.identity);
                    UseQuantity(args, 1);
                }
            }
            DestroyImmediate(m_Weapon);
        }

        public override void Use(UseContext args)
        {
            float power = args.m_Entry.DbEntry.GetCombinedPropFloat("power", args.m_Entry.m_Properties);
        }
    }
}