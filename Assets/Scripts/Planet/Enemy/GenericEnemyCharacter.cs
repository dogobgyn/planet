
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;
using TwoDee;

namespace Planet
{
    public class GenericEnemyCharacter : MonoBehaviour, TwoDee.IKillable, IMouseInfo, IProxy
    {
        public float m_MovementSpeed = 5.0f;
        public float MovementSpeed
        {
            get
            {
                return m_MovementSpeed * (GetPerkBool("fast") ? 1.5f : 1.0f);
            }
        }
        public float m_TurnSpeed = 60.0f;

        public class Perk
        {
            string name;
            int rank;
        }
        public SerializableDictionaryPod<string, int> m_Perks = new SerializableDictionaryPod<string, int>();
        static void AddPerk(SerializableDictionaryPod<string, int> perks, string name)
        {
            int oldValue;
            if (!perks.TryGetValue(name, out oldValue)) oldValue = 0;
            perks[name] = (oldValue + 1);
        }

        static public void AddPerk(SerializableDictionaryPod<string, int> perks, float normalizedRand)
        {
            string[] perkslist = { "fast", "scout", "deadly", "tanky" };
            AddPerk(perks, perkslist.RandomlyPick(normalizedRand));
        }

        public int GetPerk(string name)
        {
            int oldValue;
            if (!m_Perks.TryGetValue(name, out oldValue)) oldValue = 0;

            return oldValue;
        }

        public bool GetPerkBool(string name)
        {
            var intValue = GetPerk(name);
            return intValue > 0;
        }

        public float GetAggroRange(bool aggro)
        {
            if(aggro)
            {
                return 8.0f + (GetPerkBool("scout") ? 6.0f : 0.0f);
            }
            else
            {
                return 16.0f;
            }
        }

        public class StateArgs : CommonTickedStateArgs
        {
            public GameObject m_GameObject;
            public GenericEnemyCharacter m_Character;

            public Vector3 m_ShootPos;

            public Vector3 m_TargetPos;
            public Vector3 m_TargetFacing;

            public void Clear()
            {
            }

            public StateArgs(GameObject go, GenericEnemyCharacter character)
            {
                m_GameObject = go;
                m_Character = character;
            }
        }
        public interface IState : ICommonTickedState<StateArgs>
        {
        }

        public class BaseState : SimpleState<StateArgs>, IState
        {
        }

        public  class DeadState : BaseState
        {
            public override void VirtualFixedUpdate(StateArgs args)
            {
                args.m_Character.GetComponent<Rigidbody>().velocity = Vector3.zero;
                base.VirtualFixedUpdate(args);
            }
        }

        public class MovingState : BaseState
        {
            float m_WaitShootTime;
            public override void VirtualFixedUpdate(StateArgs args)
            {
                m_WaitShootTime -= args.DeltaTime;

                var rb = args.m_Character.GetComponent<Rigidbody>();
                rb.angularVelocity = Vector3.zero;
                if (args.m_TargetFacing != Vector3.zero)
                {
                    var dir = (args.m_TargetFacing - args.m_GameObject.transform.position).normalized;
                    Quaternion desiredRot = Quaternion.FromToRotation(Vector3.right, dir);

                    var totalAngleDifference = Quaternion.Angle(desiredRot, args.m_GameObject.transform.rotation);
                    float deltaAngle = args.DeltaTime * args.m_Character.m_TurnSpeed;
                    float percentageOfDiff = Mathf.Clamp01(deltaAngle / totalAngleDifference);

                    args.m_GameObject.transform.rotation = Quaternion.Lerp(args.m_GameObject.transform.rotation, desiredRot, percentageOfDiff);
                }
                if (args.m_TargetPos != Vector3.zero)
                {
                    Vector3 delta = args.m_TargetPos - args.m_GameObject.transform.position;
                    if (delta.magnitude < 1.0f)
                    {
                        delta = Vector3.zero;
                    }
                    else
                    {
                        delta.Normalize();
                    }
                    args.m_Character.GetComponent<Rigidbody>().velocity = delta * args.m_Character.MovementSpeed;
                }
                if (args.m_ShootPos != Vector3.zero)
                {
                    if (m_WaitShootTime <= 0.0f)
                    {
                        m_WaitShootTime = UnityEngine.Random.Range( (args.m_Character.GetPerkBool("deadly") ? 0.1f : 0.5f) + 0.5f, 3.0f);
                        var pos = args.m_GameObject.transform.position;
                        var dir = (args.m_ShootPos - args.m_GameObject.transform.position).normalized;
                        var throwSpeed = Mathf.Clamp(0.1f * args.m_Character.Level * 0.1f + (args.m_Character.GetPerkBool("deadly") ? 10.0f : 5.0f), 5.0f, 10.0f);
                        var throwIntensity = 1.0f;
                        pos.z = 0.1f;
                        TwoDee.EasySound.Play("enemyshoot", pos);

                        TwoDee.ThrownWeapon.DoThrow(args.m_Character.m_BulletPrefab, pos, dir, args.m_GameObject, throwSpeed, throwIntensity, 0.0f, false, 5.0f, 5.0f);
                    }
                }

                base.VirtualFixedUpdate(args);
            }
        }

 
        public MovingState m_MovingState = new MovingState();
        public DeadState m_DeadState = new DeadState();

        StateMachine<StateArgs, BaseState> m_StateMachine = new StateMachine<StateArgs, BaseState>();

        public GameObject m_DeathParticlePrefab;
        public GameObject m_CorpsePrefab;
        public GameObject m_BulletPrefab;

        public void Kill()
        {            
            GameObject.Instantiate(m_DeathParticlePrefab, transform.position, transform.rotation);
            var corpse = GameObject.Instantiate(m_CorpsePrefab, transform.position, transform.rotation);
            corpse.GetComponent<Container>().FirstInventory().AddStuff(new string[]{ "experience "+Level});
        }

        public void TouchDamage()
        {
            // Touch Damage

            var didHitAlready = new Dictionary<GameObject, bool>();

            foreach (var hit in Physics.OverlapSphere(transform.position, 0.45f))
            {
                var go = hit.gameObject;
                if (go.GetComponent<ThirdPersonUserControl>() != null)
                {
                    if (!didHitAlready.ContainsKey(go))
                    {
                        didHitAlready[go] = true;
                        Health.ApplyDamageIfEnemy(go, gameObject, new TwoDee.DamageArgs(10.0f, TwoDee.DamageType.Physical, gameObject, go.transform.position));
                    }
                }
            }
        }

        public StateArgs Args
        {
            get { return m_StateMachine.m_Args; }
        }
        private void Awake()
        {
            m_StateMachine.m_Args = new StateArgs(gameObject, this);
            m_StateMachine.ChangeState(m_MovingState);
        }

        int RandSeed
        {
            get
            {
                return 0;
            }
        }
        int Level
        {
            get
            {
                var proxied = GetComponent<Proxied>();
                return Math.Max(1, proxied.Level);
            }
        }

        private void Start()
        {
            var level = Level;
            transform.localScale = new Vector3(1.0f,1.0f,1.0f)*(1.0f + 0.1f*level);
            //@HACK
            if (m_TurnSpeed < 50.0f)
            {
                transform.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 4.0f;
            }
            var health = GetComponent<Health>();
            float healthModifier = level * (GetPerkBool("tanky") ? 1.5f : 1.0f);

            health.m_MaxHealth *= healthModifier;
            health.m_Health = health.m_MaxHealth;
            var resist = GetComponent<Resistances>();
            var oldResist = new List<Resistance>(resist.m_Resistances);
            oldResist.Add(new Resistance() { m_Block = level-1,m_Type=DamageType.Any });
            resist.m_Resistances = oldResist.ToArray();
        }

        public void ControlUpdate()
        {
            m_StateMachine.CurrentState.FixedUpdate(Args);
            // Don't really want to do this since combat needs to be more tactical and only hits will cause damage
            //TouchDamage();
        }

        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            var health = GetComponent<Health>();
            var maxhp = health.m_MaxHealth;
            string perkstr = "";
            if(m_Perks.Count > 0)
            {
                perkstr += "(";
                foreach(var perk in m_Perks)
                {
                    perkstr += perk.Key + " ";
                }
                perkstr += ")";
            }
            args.Value = string.Format("Level {0} {1} hp" + perkstr, Level, maxhp);
        }

        IProxyData IProxy.CreateData()
        {
            return new Proxy();
        }

        public class Proxy : TwoDee.ProxyDataComp<GenericEnemyCharacter>
        {
            public SerializableDictionaryPod<string, int> m_Perks;

            public void AddPerk(float normalizedRand)
            {
                GenericEnemyCharacter.AddPerk(m_Perks, normalizedRand);
            }
            protected override void SaveLoad(bool save, GenericEnemyCharacter comp)
            {
                if (save)
                {
                    m_Perks = comp.m_Perks.CloneTyped();
                }
                else
                {
                    comp.m_Perks = m_Perks;
                }
            }
        }
    }
}