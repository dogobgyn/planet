using System;
using UnityEngine;

namespace TwoDee
{
    public interface ISimpleState<TStateArgs>
    {
        void Enter(TStateArgs args);
        void Exit(TStateArgs args);
    }

    public class StateMachine<TStateArgs, TState>
        where TState:class,ISimpleState<TStateArgs>
    {
        public void ChangeState(TState newState)
        {
            if (m_CurrentState != newState)
            {
                if (m_CurrentState != null) m_CurrentState.Exit(m_Args);
                newState.Enter(m_Args);
                m_CurrentState = newState;
            }
        }
        
        public TState CurrentState
        {
            get
            {
                return m_CurrentState;
            }
        }

        public TStateArgs m_Args;
        TState m_CurrentState;
    }

    public class CommonTickedStateArgs
    {
        public float DeltaTime
        {
            get { return Time.fixedDeltaTime; }
        }
    }

    public interface ICommonTickedState<TStateArgs> : ISimpleState<TStateArgs>
        where TStateArgs: CommonTickedStateArgs
    {
        void FixedUpdate(TStateArgs args);
    }

    public class SimpleState<TStateArgs> : ICommonTickedState<TStateArgs>
        where TStateArgs : CommonTickedStateArgs
    {
        protected bool m_Active;
        protected float m_TimeInState;

        public void Enter(TStateArgs args)
        {
            VirtualEnter(args);
            m_Active = true;
            m_TimeInState = 0.0f;
        }

        public void Exit(TStateArgs args)
        {
            VirtualExit(args);
            m_Active = false;
        }

        public void FixedUpdate(TStateArgs args)
        {
            VirtualFixedUpdate(args);
            m_TimeInState += args.DeltaTime;
        }

        public virtual void VirtualFixedUpdate(TStateArgs args)
        {
        }


        public virtual void VirtualEnter(TStateArgs args)
        {
        }


        public virtual void VirtualExit(TStateArgs args)
        {
        }
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    public class ThirdPersonCharacter : MonoBehaviour
    {
        public class StateArgs : CommonTickedStateArgs
        {
            public GameObject m_GameObject;
            public ThirdPersonCharacter m_Character;

            public Vector3 m_RawControl_ws;
            public Vector3 m_RawControl_controller;
            public Vector3 m_Move;
            public bool m_Crouch;
            public bool m_Jump;
            public bool m_JumpBoost;

            public void Clear()
            {
                m_RawControl_ws = Vector3.zero;
                m_RawControl_controller = Vector3.zero;
                m_Move = Vector3.zero;
                m_Crouch = m_Jump = m_JumpBoost = false;
            }

            public StateArgs(GameObject go, ThirdPersonCharacter character)
            {
                m_GameObject = go;
                m_Character = character;
            }

            public Rigidbody RigidBody
            {
                get { return m_Character.m_Rigidbody; }
            }
            public Animator Animator
            {
                get { return m_Character.m_Animator; }
            }
            public void ChangeState(IState newState)
            {
                m_Character.ChangeState(newState);
            }

            public void UpdateAnimator(Vector3 move, bool isGrounded)
            {
                m_Character.UpdateAnimator(move, isGrounded);
            }
        }

        public interface IState : ICommonTickedState<StateArgs>
        {
            void AnimatorMove(StateArgs args);
            void UpdateAnimator(StateArgs args);
        }

        public class BaseState : SimpleState<StateArgs>, IState
        {
            public virtual void AnimatorMove(StateArgs args)
            {
            }

            protected virtual void BaseFixedUpdate(StateArgs args)
            {
                if (args.m_RawControl_controller.y > 0.8f && Input.GetKey(KeyCode.F1))
                {
                    //@TEST flying
                    args.m_Character.ChangeState(args.m_Character.m_FlyState);
                }
            }
            public override void VirtualFixedUpdate(StateArgs args)
            {
                BaseFixedUpdate(args);
            }

            public virtual void UpdateAnimator(StateArgs args)
            {
                args.UpdateAnimator(args.m_Move, args.m_Character.GroundedFlat);
            }
        }

        public class WalkingState : BaseState
        {
            public override void VirtualEnter(StateArgs args)
            {
            }

            public override void VirtualExit(StateArgs args)
            {
                args.m_Character.m_Capsule.material = args.m_Character.m_ZeroFrictionMaterial;
            }

            Vector3 m_LastPosition;
            public override void VirtualFixedUpdate(StateArgs args)
            {
                // Standing still- prevent sliding around for no reason.
                if (Mathf.Abs(args.m_RawControl_controller.x) < 0.05f)
                {
                    args.m_Character.m_Capsule.material = args.m_Character.m_MaxFrictionMaterial;
                    //float lastDist = (args.m_GameObject.transform.position - m_LastPosition).magnitude;
                    //if (lastDist < 0.1f) args.m_GameObject.transform.position = m_LastPosition;
                    //m_LastPosition = args.m_GameObject.transform.position;
                }
                else
                {
                    args.m_Character.m_Capsule.material = args.m_Character.m_ZeroFrictionMaterial;
                }

                args.m_Character.HandleGroundedMovement(args.m_Crouch, args.m_Jump);

                if (!args.m_Character.GroundedFlat)
                {
                    args.m_Character.ChangeState(args.m_Character.m_InAirState.Init(0.0f));
                }

                // Try to track the ground a bit if it's uneven
                var oldVel = Vector3.Dot(args.m_Character.transform.up, args.m_Character.m_Rigidbody.velocity);
                var velfwd = Vector3.Dot(args.m_Character.transform.forward, args.m_Character.m_Rigidbody.velocity);
                args.m_Character.m_Rigidbody.velocity = args.m_Character.transform.up * Mathf.Min(oldVel, -1.0f) + args.m_Character.transform.forward * velfwd;

                base.VirtualFixedUpdate(args);
            }

            float STEP_PERIOD = 1.4f;
            float m_MovedDistance = 0.0f;
            public override void AnimatorMove(StateArgs args)
            {
                // we implement this function to override the default root motion.
                // this allows us to modify the positional speed before it's applied.
                if (true && Time.deltaTime > 0)
                {
                    var deltaPos = args.m_Character.m_Animator.deltaPosition;
                    float deltaPosMag = deltaPos.magnitude;
                    m_MovedDistance += deltaPosMag;

                    if (m_MovedDistance > STEP_PERIOD)
                    {
                        EasySound.Play("step", args.m_GameObject);
                        m_MovedDistance -= STEP_PERIOD;
                    }
                    Vector3 v = (args.m_Character.m_Animator.deltaPosition * args.m_Character.MoveSpeedMultiplier) / Time.deltaTime;

                    // we preserve the existing y part of the current velocity.
                    Vector3 up = args.m_Character.RotationZ() * Vector3.up;
                    float upAmount = Vector3.Dot(args.m_Character.m_Rigidbody.velocity, up);
                    if (args.m_Character.GroundedFlat && Mathf.Abs(v.x) > 0.1f)
                    {
                        //upAmount -= 1.1f;
                    }
                    v += up * upAmount;
                    
                    args.m_Character.m_Rigidbody.velocity = v;
                }
            }
        }

        public class FlyState : BaseState
        {
            public override void VirtualEnter(StateArgs args)
            {
            }

            public override void VirtualExit(StateArgs args)
            {
            }

            public override void VirtualFixedUpdate(StateArgs args)
            {
                args.m_Character.m_Rigidbody.velocity = 5.0f * args.m_RawControl_ws;
                if (args.m_Jump) args.m_Character.ChangeState(args.m_Character.m_InAirState.Init(0.0f));
            }
        }

        public class InAirState : BaseState
        {
            float m_yVel = 0.0f;
            bool m_JumpBootsThrusters = false;

            public InAirState Init(float yvel)
            {
                if (!m_Active)
                {
                    m_yVel = yvel;
                }
                return this;
            }

            public override void VirtualEnter(StateArgs args)
            {
                m_Stuck = 0.0f;
                m_JumpBoostAllowed = 0.5f;
                m_JumpBootsThrusters = false;
                m_JumpBoostFirstAllowed = false;
            }

            public override void VirtualExit(StateArgs args)
            {
                SetJumpBootsThrusters(args, false);
            }

            public virtual bool ConsumeJumpBoots(StateArgs args)
            {
                return false;
            }

            Vector3 m_LastPosition;
            float m_Stuck;
            float m_JumpBoostAllowed;
            bool m_JumpBoostFirstAllowed;

            void SetJumpBootsThrusters(StateArgs args, bool newJumpBootsThrusters)
            {
                if (m_JumpBootsThrusters != newJumpBootsThrusters)
                {
                    var jb = args.m_Character.m_JumpBootsThrusters;
                    if (jb != null)
                    {
                        if (newJumpBootsThrusters)
                        {
                            jb.gameObject.SetActive(true);
                            jb.Play(true);
                        }
                        else
                        {
                            //hps.gameObject.SetActive(false);
                            jb.Stop();
                        }
                    }

                    m_JumpBootsThrusters = newJumpBootsThrusters;
                }
            }

            protected float ComputeFallDamage(float speed)
            {
                float minFall = 15.0f;
                float maxFall = 18.0f;
                if (speed < minFall) return 0.0f;

                float damage = Mathf.Clamp01((speed - minFall) / (maxFall - minFall));

                return damage;
            }

            protected virtual void FallDamage(StateArgs args, float speed)
            {
                // Take fall damage if falling hard
                var damage = ComputeFallDamage(speed);
                if (damage > 0.0f)
                {
                    args.m_GameObject.GetComponent<Health>().RawDamage(new DamageArgs(damage * 100.0f, DamageType.Pure, args.m_GameObject, args.m_GameObject.transform.position));
                }
            }

            public override void VirtualFixedUpdate(StateArgs args)
            {
                bool newJumpBootsThrusters = false;
                if (m_yVel < 3.0f)
                {
                    m_JumpBoostFirstAllowed = true;
                }
                if (args.m_JumpBoost && m_JumpBoostFirstAllowed && m_JumpBoostAllowed > 0.0f)
                {
                    bool consumed = ConsumeJumpBoots(args);
                    newJumpBootsThrusters = consumed;
                    if (consumed && m_yVel < 3.0f)
                    {
                        m_JumpBoostAllowed -= args.DeltaTime;
                        m_yVel += args.DeltaTime * 20.0f;
                    }
                }
                SetJumpBootsThrusters(args, newJumpBootsThrusters);

                //@TODO replace with sliding on steep ground state- allow jumping if there is ground below
                if (args.m_Character.m_IsGrounded && m_yVel < 0.0f && args.m_Jump)
                {
                    m_yVel = args.m_Character.m_JumpPower;
                }
                // Stuck?
                float lastDist = (args.m_GameObject.transform.position - m_LastPosition).magnitude;
                if (lastDist < 0.05f)
                {
                    if (m_yVel < -6.0f)
                        m_Stuck += args.DeltaTime;
                    else
                        m_Stuck = 0.0f;

                    if (m_Stuck > 0.4f)
                    {
                        m_yVel = 15.0f;
                        m_Stuck = 0.0f;
                    }
                }
                m_LastPosition = args.m_GameObject.transform.position;

                m_yVel -= args.DeltaTime * 9.8f;

                args.m_Character.HandleAirborneMovement();
                // Did we hit the ground?
                if (args.m_Character.GroundedFlat && m_yVel <= 0.0f)
                {
                    TwoDee.EasySound.Play("land", args.m_GameObject);
                    args.m_Character.ChangeState(args.m_Character.m_WalkingState);
                    FallDamage(args, -m_yVel);
                }
                var vel = args.m_Character.m_Rigidbody.velocity;

                // Take out forward velocity, put it back again
                var velup = Vector3.Dot(args.m_Character.transform.up, vel);
                vel = m_yVel * args.m_Character.transform.up;
                vel += args.m_Character.transform.forward * args.m_Move.z * 3.0f * args.m_Character.MoveSpeedMultiplier;
                args.m_Character.m_Rigidbody.velocity = vel;

                base.VirtualFixedUpdate(args);
            }

            public override void UpdateAnimator(StateArgs args)
            {
                args.UpdateAnimator(args.m_Move, false);
            }
        }

        public bool IsWalking
        {
            get { return CurrentState == m_WalkingState; }
        }
        protected WalkingState m_WalkingState = new WalkingState();
        protected InAirState m_InAirState = new InAirState();
        protected FlyState m_FlyState = new FlyState();

        StateMachine<StateArgs, IState> m_StateMachine = new StateMachine<StateArgs, IState>();
        protected IState CurrentState
        {
            get { return m_StateMachine.CurrentState; }
        }

        public PhysicMaterial m_ZeroFrictionMaterial;
        public PhysicMaterial m_MaxFrictionMaterial;

        [SerializeField]
        float m_MovingTurnSpeed = 360;
        [SerializeField]
        float m_StationaryTurnSpeed = 180;
        [SerializeField]
        float m_JumpPower = 12f;
        [Range(1f, 4f)]
        [SerializeField]
        float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
        [SerializeField]
        float m_MoveSpeedMultiplier = 1f;
        [SerializeField]
        float m_AnimSpeedMultiplier = 1f;
        [SerializeField]
        float m_GroundCheckDistance = 0.6f;

        public ParticleSystem m_JumpBootsThrusters;

        public virtual float JumpPower
        {
            get
            {
                return m_JumpPower;
            }
        }

        public virtual float MoveSpeedMultiplier
        {
            get
            {
                return m_MoveSpeedMultiplier;
            }
        }

        protected RadialGravity m_RadialGravity;
        protected Rigidbody m_Rigidbody;
        Animator m_Animator;
        bool m_IsGrounded;
        const float k_Half = 0.5f;
        protected float m_TurnAmount;
        float m_ForwardAmount;
        Vector3 m_GroundNormal;
        float m_CapsuleHeight;
        Vector3 m_CapsuleCenter;
        CapsuleCollider m_Capsule;
        bool m_Crouching;
        float m_yVel = 0.0f;

        public bool GroundedFlat
        {
            // For ref 1/sqrt2 = 0.7071067811865475
            get { return m_IsGrounded && Vector3.Dot(Up, m_GroundNormal) > 0.6f; }
        }

        protected void ChangeState(IState newState)
        {
            m_StateMachine.ChangeState(newState);
        }

        protected virtual void VirtualStart()
        {
        }

        protected virtual void CreateStates()
        {

        }

        protected virtual StateArgs CreateStateArgs()
        {
            return new StateArgs(gameObject, this);
        }

        void Start()
        {
            m_Animator = GetComponent<Animator>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            m_RadialGravity = GetComponent<RadialGravity>();
            m_CapsuleHeight = m_Capsule.height;
            m_CapsuleCenter = m_Capsule.center;

            //m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            m_Args = m_StateMachine.m_Args = CreateStateArgs();
            CreateStates();
            ChangeState(m_InAirState.Init(0.0f));
            VirtualStart();
        }

        StateArgs m_Args;

        public Vector3 Up
        {
            get
            {
                return transform.rotation * Vector3.up;
            }
        }
        public Quaternion RotationZ()
        {
            return Quaternion.FromToRotation(Vector3.up, transform.rotation * Vector3.up);
        }

        protected virtual void VirtualFixedUpdateControl()
        {
        }

        Vector3 m_LastPosition;
        public void FixedUpdateMove(Vector3 rawControl_controller, Vector3 rawControl_ws, Vector3 move, bool crouch, bool jump, bool jumpBoost)
        {

            // Rotate to current gravity
            if (m_RadialGravity != null)
            {
                Quaternion rot = transform.rotation;
                Vector3 src = rot * Vector3.down;
                src = Vector3.Lerp(src, m_RadialGravity.m_GravityDir, 0.5f);
                var diff = Quaternion.FromToRotation(src, m_RadialGravity.m_GravityDir);
                rot = diff * rot;
                transform.rotation = rot;
            }

            CheckGroundStatus();

            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            if (move.magnitude > 1f) move.Normalize();
            move = transform.InverseTransformDirection(move);

            //move = Vector3.ProjectOnPlane(move, RotationZ() * m_GroundNormal);

            m_Args.m_RawControl_controller = rawControl_controller;
            m_Args.m_RawControl_ws = rawControl_ws;
            m_Args.m_Move = move;
            m_Args.m_Crouch = crouch;
            m_Args.m_Jump = jump;
            m_Args.m_JumpBoost = jumpBoost;

            VirtualFixedUpdateControl();

            move = m_Args.m_Move;
            m_TurnAmount = Mathf.Atan2(move.x, move.z);
            m_ForwardAmount = move.z;
            ApplyExtraTurnRotation();
            m_StateMachine.CurrentState.FixedUpdate(m_Args);

            ScaleCapsuleForCrouching(crouch);
            PreventStandingInLowHeadroom();

            // send input and other state parameters to the animator
            m_StateMachine.CurrentState.UpdateAnimator(m_Args);
        }

        void ScaleCapsuleForCrouching(bool crouch)
        {
            if (m_IsGrounded && crouch)
            {
                if (m_Crouching) return;
                m_Capsule.height = m_Capsule.height / 2f;
                m_Capsule.center = m_Capsule.center / 2f;
                m_Crouching = true;
            }
            else
            {
                Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
                float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
                if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, GetCrouchMask(), QueryTriggerInteraction.Ignore))
                {
                    m_Crouching = true;
                    return;
                }
                m_Capsule.height = m_CapsuleHeight;
                m_Capsule.center = m_CapsuleCenter;
                m_Crouching = false;
            }
        }

        int GetGroundMask()
        {
            return (GameObjectExt.GetLayerMask("Ground")) | (GameObjectExt.GetLayerMask("StructurePlatform"));
        }
        int GetCrouchMask()
        {
            return 1 << LayerMask.GetMask("Ground");
        }
        void PreventStandingInLowHeadroom()
        {
            // prevent standing up in crouch-only zones
            if (!m_Crouching)
            {
                Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
                float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
                if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, GetCrouchMask(), QueryTriggerInteraction.Ignore))
                {
                    m_Crouching = true;
                }
            }
        }


        void UpdateAnimator(Vector3 move, bool isGrounded)
        {
            // update the animator parameters
            m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
            m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
            m_Animator.SetBool("Crouch", m_Crouching);
            m_Animator.SetBool("OnGround", isGrounded);
            if (!isGrounded)
            {
                m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
            }

            // calculate which leg is behind, so as to leave that leg trailing in the jump animation
            // (This code is reliant on the specific run cycle offset in our animations,
            // and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
            float runCycle =
                Mathf.Repeat(
                    m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
            float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
            if (isGrounded)
            {
                m_Animator.SetFloat("JumpLeg", jumpLeg);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            if (isGrounded && move.magnitude > 0)
            {
                m_Animator.speed = m_AnimSpeedMultiplier;
            }
            else
            {
                // don't use that while airborne
                m_Animator.speed = 1;
            }
        }


        void HandleAirborneMovement()
        {
        }

        public void DoJump(float addVelocityX)
        {
            // jump!
            m_Rigidbody.velocity = RotationZ() * new Vector3(addVelocityX + m_Rigidbody.velocity.x, JumpPower, m_Rigidbody.velocity.z);
            m_IsGrounded = false;
            TwoDee.EasySound.Play("jump", gameObject);
            ChangeState(m_InAirState.Init(JumpPower));
            m_Animator.applyRootMotion = false;
        }

        void HandleGroundedMovement(bool crouch, bool jump)
        {
            // check whether conditions are right to allow a jump:
            if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
            {
                DoJump(0.0f);
            }
        }

        void ApplyExtraTurnRotation()
        {
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
            transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
        }


        public void OnAnimatorMove()
        {
            m_StateMachine.CurrentState.AnimatorMove(m_Args);
        }


        void CheckGroundStatus()
        {
            RaycastHit hitInfo;

            Vector3 up = RotationZ() * Vector3.up;
            Vector3 down = RotationZ() * Vector3.down;

#if UNITY_EDITOR
            // helper to visualise the ground check ray in the scene view
            Debug.DrawLine(transform.position + (up * 0.1f), transform.position + (up * 0.1f) + (down * m_GroundCheckDistance));
#endif
            // 0.1f is a small offset to start the ray from inside the character
            // it is also good to note that the transform position in the sample assets is at the base of the character
            if (Physics.Raycast(transform.position + (up * 0.1f), down, out hitInfo, m_GroundCheckDistance, GetGroundMask()))
            {
                m_GroundNormal = hitInfo.normal;
                m_IsGrounded = true;
                if(m_Animator) m_Animator.applyRootMotion = true;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundNormal = up;
                if (m_Animator) m_Animator.applyRootMotion = false;
            }
        }
    }
}
