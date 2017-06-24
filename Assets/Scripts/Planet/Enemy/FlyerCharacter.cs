
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
    public class FlyerCharacter : MonoBehaviour, TwoDee.IKillable
    {
        public class StateArgs : CommonTickedStateArgs
        {
            public GameObject m_GameObject;
            public FlyerCharacter m_Character;

            public bool m_DoAttack;

            public Vector3 m_TargetPos;
            public void Clear()
            {
            }

            public StateArgs(GameObject go, FlyerCharacter character)
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
            public override void VirtualFixedUpdate(StateArgs args)
            {
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
                    args.m_Character.GetComponent<Rigidbody>().velocity = delta * 5.0f;
                }
                base.VirtualFixedUpdate(args);
            }
        }

        public class AttackingState : BaseState
        {
            public override void VirtualFixedUpdate(StateArgs args)
            {
                if (m_TimeInState > 1.0f)
                {
                    GameObject.Instantiate(args.m_Character.m_DamageExplosionPrefab, args.m_GameObject.transform.position, args.m_GameObject.transform.rotation);
                    args.m_Character.m_StateMachine.ChangeState(args.m_Character.m_MovingState);
                }
                args.m_Character.GetComponent<Rigidbody>().velocity = Vector3.zero;
                base.VirtualFixedUpdate(args);
            }
        }

        public AttackingState m_AttackingState = new AttackingState();
        public MovingState m_MovingState = new MovingState();
        public DeadState m_DeadState = new DeadState();

        StateMachine<StateArgs, BaseState> m_StateMachine = new StateMachine<StateArgs, BaseState>();

        public GameObject m_DamageExplosionPrefab;
        public GameObject m_CorpsePrefab;

        public void Kill()
        {
            GameObject.Instantiate(m_CorpsePrefab, transform.position, transform.rotation);
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

        public void ControlUpdate()
        {
            if (Args.m_DoAttack && m_StateMachine.CurrentState != m_DeadState)
            {
                m_StateMachine.ChangeState(m_AttackingState);
                Args.m_DoAttack = false;
            }
            m_StateMachine.CurrentState.FixedUpdate(Args);
            TouchDamage();
        }
    }
}