using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FPSPlayer
{
    [RequireComponent(typeof(CharacterController))]
    public class Player : Entity
    {
        bool heDead = false;
        bool doubleCheckDead = false;

        float timeSinceLastUpdate = 0f;
        /*
         *  For a FSM representation of the player.
         */
        public enum PlayerState
        {
            IDLE,
            WALKING,
            RUNNING,
            JUMPING,

            NUM_STATES
        }

        private CharacterController m_cControl = null;
        public Vector3 spawnpoint { get; set; } = Vector3.zero;

        #region Serialised Members
        [Header("References")]
        public Camera m_camera = null;


        [Header("Physics")]
        public float m_accel = 25.0f;
        public float m_drag = 1.0f;
        public float m_speedCap = 3.0f;
        public float m_runSpeedMod = 2.0f;
        public float m_stickToGroundDistance = 0.25f;
        public float m_jumpSpeed = 4.0f;
        public float m_cameraHeight = 1.5f;

        [Header("FX")]
        public float m_fovIncrease = 1.25f;
        public float m_fovLerpSpeed = 10.0f;
        public float m_pitch = 0.0f, m_yaw = 0.0f;

        public GameObject interactTimerSlider;
        #endregion

        #region Private Members
        //Camera's FOV (before FOV lerp)
        private float m_fov;
        //Position of the character from the previous FixedUpdate
        private Vector3 m_oldPosition = Vector3.zero;
        //Game-time that oldPosition was originally calculated.
        private float m_oldPositionT = 0.0f;
        //Current velocity of the player.
        private Vector3 m_velocity = Vector3.zero;
        //Camera angles
        //Used to check whether or not the player was grounded before moving.
        private bool m_wasGrounded = false;

        private Rigidbody rb;

        private float interactStartTime = 0f;
        private float holdTime = 3.0f;
        private float interactTimer = 0f;
        private float heldTime = 0f;

        public FirearmHandler firearmHandler { get; private set; }

        //The current state of the player.
        public PlayerState m_state;
        #endregion

        public Animator anim;

        /* 
         *  Use this to manually set the player's location.
         *  Not using this will likely cause issues due to the interpolation.
         */

        public void SetLocation(Vector3 _location)
        {

            if (type == EntityType.Player)
                m_cControl.enabled = false;

            transform.position = _location;
            m_oldPosition = _location;

            if (type == EntityType.Player)
                m_cControl.enabled = true;
        }

        /*
         *  Sets the player object's rotation based on yaw only to ensure correct forward vector on the player object.
         *  Uses the camera object's rotation for pitch.
         */
        public void SetRotation(Vector3 _rotation)
        {
            if (type == EntityType.Player)
                m_cControl.enabled = false;

            transform.localRotation = Quaternion.Euler(0, _rotation.y, 0);
            m_pitch = 0f;
            m_yaw = 0f;
            if (type == EntityType.Player)
            {
                m_camera.transform.localRotation = Quaternion.Euler(_rotation.x, 0, 0);
            }

            if (type == EntityType.Player)
                m_cControl.enabled = true;
        }

        /*
         *  Get the current state of the player controller.
         */
        public PlayerState GetState()
        {
            return m_state;
        }

        public void SetState(int state)
        {
            m_state = (PlayerState)state;
        }

        protected override void BaseAwake()
        {
            base.BaseAwake();
            firearmHandler = GetComponentInChildren<FirearmHandler>();
            //Get a reference to the unity character controller (basically a capsule collider with bonus features).
            if (type == EntityType.Player)
            {
                m_cControl = GetComponent<CharacterController>();
                m_oldPosition = transform.position;

                Cursor.lockState = CursorLockMode.Locked;
                m_fov = m_camera.fieldOfView;
                m_state = PlayerState.IDLE;
            }
            else
            {
                rb = GetComponent<Rigidbody>();
            }

            timeSinceLastUpdate = Time.time;

            anim = GetComponentInChildren<Animator>();
        }

        //Set the upward velocity of the player.
        private void Jump()
        {
            if (GroundTest())
                m_velocity.y = m_jumpSpeed;
        }

        //Like a ground test, but for the head on the ceiling.
        private bool HeadTest()
        {
            return Physics.SphereCast(new Ray(transform.position + m_cControl.center, Vector3.up), m_cControl.radius, m_cControl.center.y - m_cControl.radius + m_cControl.skinWidth + 0.001f);
        }

        //Test if the player is on the ground.
        private bool GroundTest()
        {
            return Physics.SphereCast(new Ray(transform.position + m_cControl.center, Vector3.down), m_cControl.radius, m_cControl.center.y - m_cControl.radius + m_cControl.skinWidth + 0.001f);
        }

        private void Update()
        {
            //Jump.

            if (type == EntityType.Player)
            {
                if (Input.GetKeyDown(KeyCode.Space)) Jump();

                //Interpolate camera world position
                m_camera.transform.position = Vector3.Lerp(m_oldPosition + m_cameraHeight * Vector3.up, transform.position + m_cameraHeight * Vector3.up, (Time.time - m_oldPositionT) / Time.fixedDeltaTime);

                //if (Input.GetKeyDown(KeyCode.Escape)) {
                //    Cursor.lockState = CursorLockMode.None;
                //}
                //if (Input.GetMouseButtonDown(0)) {
                //    Cursor.lockState = CursorLockMode.Locked;
                //}

                //Mouse-look
                Vector2 look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                m_pitch += -look.y;
                m_yaw += look.x;

                //Clamp camera angles.
                m_pitch = Mathf.Clamp(m_pitch, -90, 90);

                //Update camera rotation.
                m_camera.transform.localRotation = Quaternion.Euler(m_pitch, 0, 0);
                transform.localRotation = Quaternion.Euler(0, m_yaw, 0);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactTimerSlider.GetComponentInChildren<Slider>().value = 0;
                    interactStartTime = Time.time;
                    interactTimer = interactStartTime;
                    heldTime = 0;
                }
                if (Input.GetKey(KeyCode.E))
                {

                    Ray interactRay = new Ray(m_camera.transform.position, m_camera.transform.forward);
                    RaycastHit hit;
                    if (Physics.Raycast(interactRay, out hit))
                    {

                        Terminal currentTerminal = hit.transform.GetComponent<Terminal>();
                        if (currentTerminal != null)
                        {

                            interactTimer += Time.deltaTime;
                            heldTime += Time.deltaTime;
                            interactTimerSlider.SetActive(true);
                            interactTimerSlider.GetComponentInChildren<Slider>().value = heldTime;
                            Debug.DrawRay(m_camera.transform.position, m_camera.transform.forward * 10, Color.green, 10, false);
                            if (interactTimer > (interactStartTime + holdTime))
                            {
                                if (Networking.NetworkManager.isConnected)
                                {
                                    Networking.NetworkManager.SendPacketGateOpen(currentTerminal.gateNumber);
                                }
                                currentTerminal.openGate(currentTerminal.gate);
                            }
                        }
                        else
                        {
                            heldTime = 0;
                            interactTimer = 0;
                            interactTimerSlider.GetComponentInChildren<Slider>().value = 0;
                            interactTimerSlider.SetActive(false);

                        }

                    }


                }
                if (Input.GetKeyUp(KeyCode.E))
                {
                    interactTimerSlider.GetComponentInChildren<Slider>().value = 0;
                    heldTime = 0;
                    interactTimerSlider.SetActive(false);
                }



            }
        }

        void DebugDead()
        {
            if (heDead)
            {
                Debug.Log(transform.position + ", " + m_oldPosition);
                heDead = false;
            }
            else if (doubleCheckDead)
            {
                Debug.Log(transform.position + ", " + m_oldPosition);
                doubleCheckDead = false;
            }
        }

        private void FixedUpdate()
        {


            if (type == EntityType.Player)
            {
                //Process movement input.
                Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
                Vector3 intendedMovement = transform.rotation * new Vector3(movement.x, 0, movement.y);

                //Apply Gravity
                m_velocity.y += Physics.gravity.y * Time.fixedDeltaTime;

                //Grounded.
                if (m_cControl.isGrounded && m_velocity.y < 0)
                {
                    m_velocity.y = 0;
                }

                //Bump head
                if (HeadTest() && m_velocity.y > 0)
                {
                    m_velocity.y = 0;
                }

                //Apply Drag (horizontally).
                Vector3 hVel = m_velocity;
                hVel.y = 0;
                hVel *= 1.0f - m_drag * Time.fixedDeltaTime;
                m_velocity.x = 0;
                m_velocity.z = 0;
                m_velocity += hVel;

                //Move.
                m_velocity += intendedMovement * m_accel * Time.fixedDeltaTime;

                //Cap horizontal speed
                float currentSpeedCap = m_speedCap;

                if (Input.GetKey(KeyCode.Tab) && movement.y > 0)
                {
                    currentSpeedCap *= m_runSpeedMod;
                    Debug.Log("Running");
                }

                //Grab the horizontal velocity components and clamp them.
                Vector3 vTest = m_velocity;
                vTest.y = 0;
                if (vTest.magnitude > currentSpeedCap)
                {
                    vTest = vTest.normalized * currentSpeedCap;
                    m_velocity.x = 0;
                    m_velocity.z = 0;
                    m_velocity += vTest;
                }

                //Lerp Camera FOV on sprint
                if (vTest.magnitude > m_speedCap + 0.01f)
                {
                    m_camera.fieldOfView = Mathf.Lerp(m_camera.fieldOfView, m_fov * m_fovIncrease, Time.fixedDeltaTime * m_fovLerpSpeed);
                }
                else
                {
                    m_camera.fieldOfView = Mathf.Lerp(m_camera.fieldOfView, m_fov, Time.fixedDeltaTime * m_fovLerpSpeed);
                }


                //Update the player's state based on physical conditions.
                if (m_cControl.isGrounded)
                {
                    //If no input is being given...
                    if (movement.magnitude == 0)
                    {
                        //Idle.
                        m_state = PlayerState.IDLE;
                    }
                    //If you're moving at the standard speed capacity...
                    else if (currentSpeedCap == m_speedCap)
                    {
                        //Walking.
                        m_state = PlayerState.WALKING;
                    }
                    //Last possible case.
                    else
                    {
                        //Running.
                        m_state = PlayerState.RUNNING;
                    }
                }

                //Save whether or not the player was just grounded before the movement.
                m_wasGrounded = m_cControl.isGrounded;

                //DebugDead();
                //Move the player
                m_oldPosition = transform.position;
                m_oldPositionT = Time.time;
                m_cControl.Move(m_velocity * Time.deltaTime);

                //DebugDead();

                //Check if the player just unwillingly left the ground.
                if (!GroundTest() && m_wasGrounded && m_velocity.y <= 0)
                {
                    //Test to see if the fall the player would take here is within the
                    //"Stick-to-ground distance"
                    RaycastHit hit;
                    if (Physics.SphereCast(new Ray(transform.position + m_cControl.center, Vector3.down), m_cControl.radius, out hit, m_cControl.center.y - m_cControl.radius + m_cControl.skinWidth + m_stickToGroundDistance))
                    {
                        //Stick to ground.
                        m_cControl.Move(new Vector3(0, -hit.distance, 0));
                    }
                }

                if (Networking.NetworkManager.isConnected)
                {
                    Networking.NetworkManager.SendPacketEntities();
                }
            }
        }

        public override void OnDeath(bool networkData)
        {
            ResetValues();
            if (networkData)
                Networking.NetworkManager.SendPacketDeath(this.id, killerID);
            Debug.Log("U DEAD");
        }

        public override void ResetValues()
        {
            SetLocation(spawnpoint);
            SetRotation(Vector3.zero);
            this.currentHealth = maxHealth;
            heDead = true;
            doubleCheckDead = true;
        }

        public override void OnDeActivate()
        {

        }

        /*
		public override void OnDamage(int num)
		{
			if (destructable)
			{
				currentHealth -= num;
			}
			if (currentHealth <= 0 && GameSceneController.Instance.type == PlayerType.FPS)
			{
				OnDeath();
			}
		}
        */

        public override void OnDamage(float num, int kID, int entityLife)
        {
            if (type == EntityType.Player)
            {
                BloodSettings.v3[BloodSettings.currentBlood] = Quaternion.Inverse(m_camera.transform.rotation) * (-EntityManager.Instance.AllEntities[kID].transform.position + m_camera.transform.position);
                BloodSettings.v3[BloodSettings.currentBlood].z = 1f;
                BloodSettings.currentBlood = (BloodSettings.currentBlood + 1) % BloodSettings.v3.Length;
            }

            Debug.Log("DAMAGE: " + num);
            currentHealth -= num;
            if (currentHealth <= 0 && GameSceneController.Instance.type == PlayerType.FPS)
            {
                if (destructable)
                {
                    OnDeath(true);
                }
                else
                {
                    currentHealth = maxHealth;
                }
            }
        }

        public override void UpdateEntityStats(Networking.EntityData ed)
        {
            Vector3 oldPos = transform.position;

            SetLocation(ed.position);
            SetRotation(ed.rotation);

            Vector3 vel = (ed.position - oldPos) / (Time.time - timeSinceLastUpdate);

            anim.SetFloat("Walk", Mathf.Clamp(Vector3.Dot((ed.position - oldPos) / (Time.time - timeSinceLastUpdate), transform.forward), -1, 1));
            anim.SetFloat("Turn", Mathf.Clamp(Vector3.Dot((ed.position - oldPos) / (Time.time - timeSinceLastUpdate), transform.right), -1, 1));

            timeSinceLastUpdate = Time.time;

            rb.velocity = vel;

            //transform.position = ed.position;
            //transform.localRotation = Quaternion.Euler(new Vector3(0, ed.rotation.y, 0));
            //m_pitch = ed.rotation.x;
            //m_yaw = ed.rotation.y;
        }
    }


}