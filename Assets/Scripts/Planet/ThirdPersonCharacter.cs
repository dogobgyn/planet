using System;
using TwoDee;
using UnityEngine;
using UnityEngine.Networking;

namespace Planet
{
	public class ThirdPersonCharacter : TwoDee.ThirdPersonCharacter, TwoDee.IKillable, IInventory
    {
        public GameObject m_CorpsePrefab;
        public float m_MaxWeight = 90.0f;

        public bool m_InitialStuffDisable;
        public string[] m_InitialStuff;

        Inventory m_Inventory = new Inventory(20);
        Inventory m_InventoryEquipment = new Inventory(3);

        public Inventory[] Inventories
        {
            get
            {
                return new Inventory[] { m_Inventory, m_InventoryEquipment };
            }
        }

        public Inventory Inventory
        {
            get
            {
                return this.FirstInventory();
            }
        }

        public Inventory InventoryEquipment
        {
            get
            {
                return m_InventoryEquipment;
            }
        }

        public UseContext DeferredItemUse
        {
            get
            {
                return m_DeferredItemUse;
            }
        }
        UseContext m_DeferredItemUse;
        float m_ItemUseCooldown;

        public bool TryUse(int slot, UseContext args)
        {
            if (m_ItemUseCooldown > 0.0f)
            {
                return false;
            }
            if (m_DeferredItemUse != null) return false;
            var entry = slot != -1 ? Inventory.GetSlot(slot) : null;
            if (null == entry) return false;
            var selectedItem = Inventory.GetEntryInSlot(slot);

            var itemObject = args.GetItemObject(args.m_Secondary);

            if (itemObject.RequiresStanding && !(CurrentState is TwoDee.ThirdPersonCharacter.WalkingState)) return false;

            m_DeferredItemUse = args;
            m_DeferredItemUse.m_Error = false;
            itemObject.BeginUse(m_DeferredItemUse);

            if (m_DeferredItemUse.m_Error)
            {
                TwoDee.EasySound.Play("uierror", args.m_OriginPos);
                return false;
            }
            return true;
        }

        public float WeightMultiplier
        {
            get
            {
                return Mathf.Clamp(0.5f + 0.5f * (1.0f - Inventory.ComputeWeight() / m_MaxWeight), 0.3f, 1.0f);
            }
        }

        public override float JumpPower
        {
            get
            {
                return (0.5f + 0.5f * WeightMultiplier) * base.JumpPower;
            }
        }

        public override float MoveSpeedMultiplier
        {
            get
            {
                return WeightMultiplier * base.MoveSpeedMultiplier;
            }
        }

        public class ClimbState : BaseState
        {
            float m_TimeSinceLastMove = 0.0f;
            HingeJoint m_CreatedHingeJoint;

            public override void VirtualEnter(StateArgs args)
            {
                var ropePos_ws = m_Rope.RopePositionToWorld(m_RopePosition);
                var link = m_Rope.GetLinkFromRopePosition(m_RopePosition);
                var hj = m_CreatedHingeJoint = Rope.StandardInitHinge(args.m_Character.gameObject);

                hj.autoConfigureConnectedAnchor = false;
                hj.connectedBody = link.GetComponent<Rigidbody>();
                // This needs to always be in local space which is sort of annoying for us but we can do it.
                //hj.connectedAnchor = new Vector3(0.0f, 0.0f);
                hj.connectedAnchor = link.transform.InverseTransformPoint(ropePos_ws);
                hj.anchor = new Vector3(0.0f, 0.0f);
            }

            void RemoveHingeJoint()
            {
                if (m_CreatedHingeJoint != null)
                {
                    GameObject.DestroyImmediate(m_CreatedHingeJoint);
                    m_CreatedHingeJoint = null;
                }
            }
            public override void VirtualExit(StateArgs args)
            {
                RemoveHingeJoint();
            }

            public override void VirtualFixedUpdate(StateArgs args)
            {
                const float CLIMB_SPEED = 3.0f;

                var pargs = args as PlanetsStateArgs;

                m_TimeSinceLastMove += args.DeltaTime;
                if (m_TimeSinceLastMove > 0.1f)
                {
                    var oldPosOnRope_ws = m_Rope.RopePositionToWorld(m_RopePosition);
                    m_RopePosition = m_Rope.FindClosestRopePosition(oldPosOnRope_ws + args.DeltaTime * CLIMB_SPEED * args.m_RawControl_ws);

                    var link = m_Rope.GetLinkFromRopePosition(m_RopePosition);
                    if (link != null)
                    {
                        var ropePos_ws = m_Rope.RopePositionToWorld(m_RopePosition);
                        m_CreatedHingeJoint.connectedBody = link.GetComponent<Rigidbody>();
                        m_CreatedHingeJoint.connectedAnchor = link.transform.InverseTransformPoint(ropePos_ws);
                    }
                }

                // Hacky ways to keep player on rope
                //args.RigidBody.velocity = m_Rope.RopePositionToWorld(m_RopePosition) - args.m_Character.transform.position;
                //args.RigidBody.velocity = Vector3.zero;
                //args.m_Character.transform.position = m_Rope.RopePositionToWorld(m_RopePosition);

                if (args.m_Jump)
                {
                    RemoveHingeJoint();
                    if (args.m_RawControl_controller.y >= 0.0f)
                    {
                        pargs.m_PlanetsCharacter.DoJump(args.m_RawControl_ws.x * 5.0f);
                    }
                    else
                    {
                        // Drop?
                        args.ChangeState(pargs.m_PlanetsCharacter.m_InAirState.Init(0.0f));
                    }
                }
            }

            public override void UpdateAnimator(StateArgs args)
            {
                var pargs = args as PlanetsStateArgs;

                pargs.UpdateAnimator(args.m_Move, false);
                args.Animator.SetFloat("Jump", 0.0f);
            }

            public Rope m_Rope;
            public float m_RopePosition;
        }

        ClimbState m_ClimbState = new ClimbState();

        public class DeathState : BaseState
        {
            public float m_Timer;
            public GameObject m_Corpse;

            public void EnableCharacter(StateArgs args, bool enabled)
            {
                foreach (var renderer in args.m_GameObject.GetComponentsInSelfOrChildren<Renderer>())
                {
                    renderer.enabled = enabled;
                }
                foreach (var rb in args.m_GameObject.GetComponentsInSelfOrChildren<Rigidbody>())
                {
                    rb.isKinematic = !enabled;
                }
            }

            public override void VirtualEnter(StateArgs args)
            {
                m_Timer = 5.0f;
                m_Corpse = GameObject.Instantiate<GameObject>((args as PlanetsStateArgs).m_PlanetsCharacter.m_CorpsePrefab, args.m_GameObject.transform.position, args.m_GameObject.transform.rotation);
                var cont = m_Corpse.GetComponentInSelfOrParents<Container>();
                (args as PlanetsStateArgs).m_PlanetsCharacter.FirstInventory().MoveAllTo(cont.FirstInventory(), false);

                Vector3 deathVelocity = 0.5f * args.m_Character.GetComponent<Rigidbody>().velocity + 2.0f * args.m_GameObject.transform.forward;
                foreach (var rb in m_Corpse.GetComponentsInSelfOrChildren<Rigidbody>())
                {
                    rb.velocity = deathVelocity;
                }

                // hide this
                EnableCharacter(args, false);
            }

            public override void VirtualExit(StateArgs args)
            {
                EnableCharacter(args, true);
            }

            public override void VirtualFixedUpdate(StateArgs args)
            {
                var pargs = args as PlanetsStateArgs;
                m_Timer -= args.DeltaTime;
                if (m_Timer <= 0.0f)
                {
                    // Respawn
                    pargs.m_PlanetsCharacter.Respawn();

                    args.ChangeState((args as PlanetsStateArgs).m_PlanetsCharacter.m_InAirState.Init(0.0f));
                }
            }
        }

        DeathState m_DeadState = new DeathState();

        public class PlanetsStateArgs : StateArgs
        {
            public ThirdPersonCharacter m_PlanetsCharacter;
            public Vector3 SpawnPoint
            {
                get
                {
                    foreach(var pos in FindObjectsOfType<NetworkStartPosition>())
                    {
                        return pos.transform.position;
                    }

                    return Vector3.zero;
                }
            }
            public PlanetsStateArgs(GameObject go, ThirdPersonCharacter character) : base(go, character)
            {
                m_PlanetsCharacter = character;
            }
        }

        PlanetsStateArgs m_Args;
        protected override StateArgs CreateStateArgs()
        {
            m_Args = new PlanetsStateArgs(gameObject, this);
            return m_Args;
        }

        public class PlanetsStateHelper
        {
            public static void SharedFixedUpdate(StateArgs args)
            {
                var pargs = args as PlanetsStateArgs;
                if (pargs.m_RawControl_controller.y > 0.8f)
                {
                    // Check if there is a link to climb on.
                    var rl = GameObjectExt.GetNearestObject<RopeLink>(args.m_Character.transform.position, 1.0f, GameObjectExt.GetLayerMask("Rope"));
                    if (rl != null)
                    {
                        pargs.m_PlanetsCharacter.m_ClimbState.m_Rope = rl.m_Rope;
                        pargs.m_PlanetsCharacter.m_ClimbState.m_RopePosition = rl.m_Rope.FindClosestRopePosition(args.m_Character.transform.position);
                        pargs.ChangeState(pargs.m_PlanetsCharacter.m_ClimbState);
                    }
                }
            }
        }

        public class PlanetsWalkingState : WalkingState
        {
            public override void VirtualFixedUpdate(StateArgs args)
            {
                base.VirtualFixedUpdate(args);
                PlanetsStateHelper.SharedFixedUpdate(args);
            }
        }

        public class PlanetsInAirState : InAirState
        {
            protected override void FallDamage(StateArgs args, float speed)
            {
                var pargs = args as PlanetsStateArgs;
                var damage = ComputeFallDamage(speed);
                if (damage > 0.0f && pargs.m_PlanetsCharacter.Inventory.Contains("fallinsurance"))
                {
                    pargs.m_PlanetsCharacter.Inventory.DropInventoryN("fallinsurance", 1);
                    return;
                }

                base.FallDamage(args, speed);
            }

            public override bool ConsumeJumpBoots(StateArgs args)
            {
                var pargs = args as PlanetsStateArgs;
                var useContext = new UseContext()
                {
                    m_DeltaTime = Time.fixedDeltaTime,
                    m_Inventory = pargs.m_PlanetsCharacter.InventoryEquipment,
                    m_Inventories = new Inventory[] { pargs.m_PlanetsCharacter.Inventory, pargs.m_PlanetsCharacter.InventoryEquipment }
                };

                for (int i=0;i< pargs.m_PlanetsCharacter.InventoryEquipment.Count;i++)
                {
                    useContext.m_Entry = pargs.m_PlanetsCharacter.InventoryEquipment.GetSlot(i);
                    if (useContext.m_Entry == null) continue;
                    useContext.m_InventorySlot = i;
                    var slotEntry = pargs.m_PlanetsCharacter.InventoryEquipment.GetEntryInSlot(i);
                    if (slotEntry.m_Item.JumpBoost(useContext))
                    {
                        EasySound.Play("thrust", args.m_GameObject);
                        return true;
                    }
                }

                return false;
            }

            public override void VirtualFixedUpdate(StateArgs args)
            {
                base.VirtualFixedUpdate(args);
                PlanetsStateHelper.SharedFixedUpdate(args);
            }
        }

        protected override void CreateStates()
        {
            m_WalkingState = new PlanetsWalkingState();
            m_InAirState = new PlanetsInAirState();
        }

        protected override void VirtualStart()
        {
            m_Inventory.AddStuff(new string[] { "tractorbeam" });
            m_InventoryEquipment.AddStuff(new string[] { "rocketboots" });
            if (UnityEngine.Debug.isDebugBuild && !m_InitialStuffDisable && m_InitialStuff != null)
            {
                m_Inventory.AddStuff(m_InitialStuff);
            }

            Respawn();
        }

        protected override void VirtualFixedUpdateControl()
        {
            if (m_DeferredItemUse != null)
            {
                /* Only for items that lock you in place
                 * m_Args.clear();
                // Face towards usage direction
                Vector3 delta = m_DeferredItemUse.m_TargetPos - m_DeferredItemUse.m_OriginPos;
                m_Args.m_Move = transform.InverseTransformDirection(delta).normalized * 0.01f;
                */

                bool done = false;
                bool destroy = false;

                var itemObject = m_DeferredItemUse.m_ItemObject;

                // Cancel if we fell while animation is playing
                bool canceled = (itemObject.RequiresStanding && !(CurrentState is TwoDee.ThirdPersonCharacter.WalkingState));

                itemObject.Using(m_DeferredItemUse);
                if (m_DeferredItemUse.m_Done)
                {
                    itemObject.Use(m_DeferredItemUse);
                    if (m_DeferredItemUse.m_Destroy)
                    {
                        destroy = true;
                    }
                    done = true;
                }

                if (canceled || done)
                {
                    m_DeferredItemUse.m_Canceled = canceled;
                    itemObject.EndUse(m_DeferredItemUse);

                    if (destroy)
                    {
                        Inventory.DropInventory(m_DeferredItemUse.m_InventorySlot);
                    }
                    m_DeferredItemUse = null;
                }
            }
            else if (m_ItemUseCooldown > 0.0f) m_ItemUseCooldown -= m_Args.DeltaTime;
        }

        public void Respawn()
        {
            gameObject.transform.position = m_Args.SpawnPoint;
            gameObject.GetComponent<Health>().Respawn();
            TwoDee.EasySound.Play("respawn", gameObject);
        }

        void IKillable.Kill()
        {
            ChangeState(m_DeadState);
        }
    }
}
