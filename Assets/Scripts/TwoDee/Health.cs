using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

namespace TwoDee
{
    public enum DamageType
    {
        Pure = 0,
        Physical = 1,
        Explosion = 2,
        Radiation = 3,
        Any = 100
    };

    [Serializable]
    public class Resistance
    {
        public DamageType m_Type;
        public float m_Percent;
        public float m_Block;

        public Resistance()
        {
            m_Type = DamageType.Any;
            m_Percent = 1.0f;
            m_Block = 0.0f;
        }
    }

    public class DamageNotificationArgs
    {
        public DamageArgs m_Args;
        public float m_InitialHealth;
        public float m_FinalHealth;
    }

    public interface IDamageNotification
    {
        void DamageNotification(DamageNotificationArgs args);
    }

    public class ResistanceData
    {
        public ResistanceData()
        {
            m_Priority = 0;
        }

        public int m_Priority;
        public List<Resistance> m_Resistances;
    }

    public interface IResistance
    {
        void GetResistances(ResistanceData data);
    }

    [Serializable]
    public class DamageArgs
    {
        public float m_Amount;
        public DamageType m_Type;
        public GameObject m_Hitter;
        public Vector3 m_Location;
        public float m_FinalAmount = 0.0f;

        public DamageArgs()
        {
        }

        public DamageArgs(float f, DamageType dt, GameObject hitter, Vector3 loc)
        {
            m_Amount = f;
            m_Type = dt;
            m_Hitter = hitter;
            m_Location = loc;
        }
    }

    public interface IDamageable
    {
        void Damage(DamageArgs args);
    }

    public class Health : NetworkBehaviour, IDamageable, IRenderEffects, IResistance, IDamageNotification
    {
        public float RadiationPercent
        {
            get { return m_Radiation / m_Radiation; }
        }

        float m_LastRadiationTime;
        public bool m_RadiationVulnerable;
        [SyncVar]
        public float m_Radiation = 100;
        [SyncVar]
        public float m_MaxRadiation = 100;


        public float HealthPercent
        {
            get { return m_Health / m_MaxHealth; }
        }
        [SyncVar]
        public float m_Health = 100;
        [SyncVar]
        public float m_MaxHealth = 100;
        [SyncVar]
        public bool m_Dead = false;

        public bool Dead
        {
            get
            {
                return m_Dead;
            }
        }
        public float m_InvulnerableWindow = 0.0f;

        public bool m_ShowHealthBar = false;
        public bool m_Invulnerable = false;
        public bool m_IgnoreThrown;

        float m_InvulnerableTime;

        public bool Invulnerable
        {
            get
            {
                return (m_Invulnerable) || (m_InvulnerableTime > 0.0f);
            }
        }

        List<IResistance> m_ResistanceComponents = new List<IResistance>();
        List<Resistance> m_Resistances = new List<Resistance>();
        float m_TookDamage = 0;
        float m_LastKnownHealth = 0;

        void Start()
        {
            foreach(var comp in GetComponents<IResistance>())
            {
                m_ResistanceComponents.Add(comp);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (m_InvulnerableTime > 0)
            {
                m_InvulnerableTime -= dt;
            }
            if (m_TookDamage > 0)
            {
                m_TookDamage -= dt;
            }
            if (m_LastKnownHealth != m_Health)
            {
                float delta = m_LastKnownHealth - m_Health;
                if (delta > 5)
                {
                    m_TookDamage = 0.4f;
                }
                m_LastKnownHealth = m_Health;
            }
            if (m_RadiationVulnerable)
            {
                if (m_LastRadiationTime > 0.0f)
                {
                    m_LastRadiationTime -= dt;
                }
                else
                {
                    m_Radiation += 10.0f * dt;
                    if (m_Radiation > m_MaxRadiation)
                    {
                        m_Radiation = m_MaxRadiation;
                    }
                }
            }
        }

        public void RawDamage(DamageArgs args)
        {
            var baseAmount = args.m_Amount;

            if (baseAmount > 0.0f && Invulnerable) return;
            if (m_Dead) return;

            if (m_InvulnerableWindow > 0.0f)
                m_InvulnerableTime = m_InvulnerableWindow;

            RawDamageUnchecked(args);
        }

        public void RawDamageUnchecked(DamageArgs args)
        {
            var baseAmount = args.m_Amount;

            List<ResistanceData> listRd = new List<ResistanceData>();
            foreach(var comp in m_ResistanceComponents)
            {
                ResistanceData rd = new ResistanceData();
                comp.GetResistances(rd);
                listRd.Add(rd);
            }
            listRd.Sort((x, y) => x.m_Priority.CompareTo(y.m_Priority));

            float finalPercent = 1.0f;
            float finalBlock = 0.0f;
            foreach(var rd in listRd)
            {
                foreach(var resist in rd.m_Resistances)
                {
                    if (resist.m_Type == args.m_Type || resist.m_Type == DamageType.Any)
                    {
                        finalPercent = resist.m_Percent;
                        finalBlock = resist.m_Block;
                        if(resist.m_Type != DamageType.Any) break;
                    }
                }
            }

            float oldHealth = m_Health;

            if (baseAmount > 0.0f)
            {
                args.m_FinalAmount = Mathf.Clamp(finalPercent * baseAmount - finalBlock, 0.0f, 99999.0f);
                if (args.m_Type == DamageType.Radiation)
                {
                    if (m_RadiationVulnerable)
                    {
                        m_LastRadiationTime = 0.9f;
                        m_Radiation -= args.m_FinalAmount;
                    }
                }
                else
                {
                    m_Health -= args.m_FinalAmount;
                }
            }
            else
            {
                args.m_FinalAmount = Mathf.Clamp(finalPercent * baseAmount - finalBlock, -99999.0f, 0.0f);
                if (args.m_Type == DamageType.Radiation)
                {
                    if (m_RadiationVulnerable)
                        m_Radiation -= args.m_FinalAmount;
                }
                else
                {
                    m_Health -= args.m_FinalAmount;
                }
            }

            DamageNotificationArgs notificationArgs = new DamageNotificationArgs() { m_Args=args, m_InitialHealth = oldHealth, m_FinalHealth = Mathf.Clamp(m_Health, 0.0f, m_MaxHealth) };
            foreach (var notification in GetComponents<IDamageNotification>())
            {
                notification.DamageNotification(notificationArgs);
            }

            if (m_Health <= 0)
            {
                m_Health = 0;
                m_Dead = true;
                TwoDee.EasySound.Play("die1", gameObject);

                foreach (var killable in GetComponents<IKillable>())
                {
                    if(isServer)
                    {
                        killable.Kill();
                    }
                }
            }
            if (m_Health > m_MaxHealth) m_Health = m_MaxHealth;
            if (m_Radiation < 0.0f && m_RadiationVulnerable)
            {
                float excess = -m_Radiation;
                m_Radiation = 0.0f; ;
                RawDamageUnchecked(new DamageArgs(excess, DamageType.Pure, args.m_Hitter, args.m_Location));
            }
        }

        public void Respawn()
        {
            bool wasDead = m_Dead;
            CmdAlive();
            if (wasDead)
            {
                CmdDamage(new DamageArgs(-9999.0f, DamageType.Pure, gameObject, gameObject.transform.position));
            }
        }

        //[Command]
        public void CmdAlive()
        {
            m_Dead = false;
        }

        //[Command]
        public void CmdDamage(DamageArgs args)
        {
            RawDamage(args);
        }

        public void Damage(DamageArgs args)
        {
            if (isServer)
            {
                CmdDamage(args);
            }
        }

        public static bool IsAlive(GameObject ob)
        {
            var health = ob.ComponentCache().Health;
            if (health != null)
            {
                return !health.m_Dead;
            }

            return false;
        }

        public void GetRenderEffects(ref RenderEffectsOut effects)
        {
            if (m_TookDamage > 0)
            {
                effects.m_Color = Color.yellow;
            }
            effects.m_Priority = 2;
        }

        public void GetResistances(ResistanceData data)
        {
            data.m_Resistances = m_Resistances;
        }

        // Return true if any damage was done
        public static bool ApplyDamageIfEnemy(GameObject target, GameObject instigator, DamageArgs damage)
        {
            if (!Team.IsEnemy(target, instigator)) return false;

            ApplyDamage(target, damage);
            return true;
        }

        public static void ApplyDamage(GameObject go, DamageArgs damage)
        {
            var li = new List<DamageArgs>();
            li.Add(damage);
            ApplyDamage(go, li);
        }

        public static void ApplyDamage(GameObject go, List<DamageArgs> damages)
        {
            foreach (var dp in damages)
            {
                var comps = go.GetComponentsInSelfOrParents<IDamageable>();

                foreach (var dam in comps)
                {
                    dam.Damage(dp);
                }
            }
        }

        public void DamageNotification(DamageNotificationArgs args)
        {
            var changed = args.m_FinalHealth - args.m_InitialHealth;
            var intAmount = Mathf.FloorToInt(args.m_Args.m_FinalAmount);
            if (args.m_Args.m_Type == DamageType.Radiation)
            {
                if (intAmount > 0) TwoDee.EasySound.Play("poweron", gameObject);
                return;
            }

            var prefix = (intAmount < 0) ? "+" : "";
            var absAmount = Math.Abs(intAmount);

            FloatingTextManager.Instance.AddEntry(prefix + absAmount, args.m_Args.m_Location);
            if (intAmount > 0) TwoDee.EasySound.Play("hit1", gameObject);
            else if (intAmount < 0) TwoDee.EasySound.Play("heal", transform.position);

        }
    }
}