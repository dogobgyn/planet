using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace TwoDee
{
    [Serializable]
    public class PlayerProxy
    {
        Vector3 m_Position;
        Quaternion m_Rotation;

        public virtual void Save(ThirdPersonUserControl player)
        {
            m_Position = player.transform.position;
            m_Rotation = player.transform.rotation;
        }

        public virtual void Load(ThirdPersonUserControl player)
        {
            player.transform.position = m_Position;
            player.transform.rotation = m_Rotation;
        }
    }

    [RequireComponent(typeof (ThirdPersonCharacter))]
    public class ThirdPersonUserControl : MonoBehaviour
    {
        protected ThirdPersonCharacter m_Character; // A reference to the ThirdPersonCharacter on the object
        private Transform m_Cam;                  // A reference to the main camera in the scenes transform
        private Vector3 m_CamForward;             // The current forward direction of the camera
        private Vector3 m_Move;
        private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.
        protected bool m_JumpBoost;

        public virtual object SaveProxy()
        {
            var result = new PlayerProxy();
            result.Save(this);
            return result;
        }

        public virtual void LoadProxy(object proxy)
        {
            (proxy as PlayerProxy).Load(this);
        }

        protected virtual void VirtualStart()
        {
        }

        private void OnDestroy()
        {
            ComponentList.OnEnd(this);
        }

        private GameObject m_LightOb;
        private void Start()
        {
            foreach(var child in transform.GetChildren())
            {
                if (child.name.Contains("ight"))
                {
                    m_LightOb = child.gameObject;
                }
            }

            ComponentList.OnStart(this);
            // get the transform of the main camera
            if (Camera.main != null)
            {
                m_Cam = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning(
                    "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.");
                // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
            }

            // get the third person character ( this should never be null due to require component )
            m_Character = GetComponent<ThirdPersonCharacter>();

            VirtualStart();
        }

        protected virtual void UpdateInput()
        {

        }

        private void Update()
        {
            m_LightOb.transform.position = transform.position + 2.0f*Vector3.back;
            var camera = UnityEngine.Camera.main;
            if (camera != null)
            {
                var pos = camera.transform.position;
                pos.x = transform.position.x;
                pos.y = transform.position.y;
                // Shift slightly based on cursor position
                /*
                float scx = Screen.width;
                float scy = Screen.height;
                float cursoffx = ((scx / 2) - Input.mousePosition.x) * -(1.0f / scx);
                float cursoffy = ((scy / 2) - Input.mousePosition.y) * -(1.0f / scy);
                pos.x += cursoffx * cursoffx;
                pos.y += cursoffy * cursoffy;
                */
                camera.transform.position = pos;
                camera.transform.rotation = Quaternion.FromToRotation(Vector3.up, transform.rotation * Vector3.up);
            }
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            UpdateInput();
        }

        // Fixed update is called in sync with physics
        private void FixedUpdate()
        {
            // read inputs
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");
            Vector3 rawInputController = new Vector3(h, v);
            Vector3 rawInputws = m_Cam.transform.TransformVector(rawInputController);

            bool crouch = Input.GetKey(KeyCode.S);

            // calculate move direction to pass to character
            if (m_Cam != null)
            {
                // calculate camera relative direction to move:
                m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
                m_Move = h*m_Cam.right; // + v*m_CamForward  Removed, want pure 2d
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                m_Move = v*Vector3.forward + h*Vector3.right;
            }
#if !MOBILE_INPUT
			// walk speed multiplier
	        if (Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif

            // pass all parameters to the character control script
            m_Character.FixedUpdateMove(rawInputController, rawInputws, m_Move, crouch, m_Jump, m_JumpBoost);
            m_JumpBoost = false;
            m_Jump = false;
        }
    }
}
