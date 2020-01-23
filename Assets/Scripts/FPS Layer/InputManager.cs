using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FPSLayer
{
    public class InputManager : MonoBehaviour
    {
        #region SingletonCode
        private static InputManager _instance;
        public static InputManager Instance
        {
            get
            {
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
                _previousMousePosition = Input.mousePosition;
                WFEOF = new WaitForEndOfFrame();
                StartCoroutine(FrameUpdate());
            }
        }
        //single pattern ends here
        #endregion

        #region INPUT_GROUPS

        [Serializable]
        public struct ButtonGroups
        {
            public MOVEMENT playerMovement;
            public ROTATE_CAMERA playerRotation;
            public SHOOT playerShoot;
            public AIM_DOWN_SIGHT playerAim;
            public JUMP playerJump;
            public SWAP_WEAPON playerSwap;
            public DIRECT_SWAP_WEAPON playerDirectSwap;
            public RELOAD playerReload;
        }

        public enum MOVEMENT
        {
            NONE,
            WASD,
            ARROWS,
            IJKL
        }

        public enum ROTATE_CAMERA
        {
            NONE,
            MOUSE,
            MOUSE_Y_INVERTED
        }

        public enum SHOOT
        {
            NONE,
            LEFT_CLICK
        }

        public enum AIM_DOWN_SIGHT
        {
            NONE,
            RIGHT_CLICK
        }

        public enum JUMP
        {
            NONE,
            SPACE
        }

        public enum SWAP_WEAPON
        {
            NONE,
            SCROLL,
            QE
        }

        public enum DIRECT_SWAP_WEAPON
        {
            NONE,
            NUM_KEYS
        }

        public enum RELOAD
        {
            NONE,
            R
        }
        #endregion

        [SerializeField]
        ButtonGroups buttons;

        #region SUB_INPUTS
        bool inputsUpdated = false;

        Vector3 _move = Vector3.zero;
        Vector3 _rotate = Vector3.zero;
        Vector3 _previousMousePosition;
        bool _jump = false;
        bool _shoot = false;
        int _cycle = 0;
        int _directSwap = -1;
        bool _aim = false;
        bool _reload = false;
        #endregion

        #region INPUT_UPDATERS
        void UpdateInputs()
        {
            inputsUpdated = true;

            _move = Vector3.zero;
            switch (buttons.playerMovement)
            {
                case MOVEMENT.WASD:
                    _move = ReturnByKeys(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D);
                    break;
                case MOVEMENT.IJKL:
                    _move = ReturnByKeys(KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L);
                    break;
                case MOVEMENT.ARROWS:
                    _move = ReturnByKeys(KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow);
                    break;

                default:
                    _move = Vector3.zero;
                    break;
            }

            _rotate = Input.mousePosition - _previousMousePosition;
            switch (buttons.playerRotation)
            {
                case ROTATE_CAMERA.MOUSE:
                    break;
                case ROTATE_CAMERA.MOUSE_Y_INVERTED:
                    _rotate.y *= -1f;
                    break;

                default:
                    _rotate = Vector3.zero;
                    break;
            }
            _previousMousePosition = Input.mousePosition;

            switch (buttons.playerJump)
            {
                case JUMP.SPACE:
                    _jump = Input.GetKey(KeyCode.Space);
                    break;

                default:
                    _jump = false;
                    break;
            }

            switch (buttons.playerShoot)
            {
                case SHOOT.LEFT_CLICK:
                    _shoot = Input.GetMouseButton(0);
                    break;

                default:
                    _shoot = false;
                    break;
            }

            switch (buttons.playerReload)
            {
                case RELOAD.R:
                    _reload = Input.GetKey(KeyCode.R);
                    break;

                default:
                    _reload = false;
                    break;
            }

            switch (buttons.playerAim)
            {
                case AIM_DOWN_SIGHT.RIGHT_CLICK:
                    _aim = Input.GetMouseButton(2);
                    break;

                default:
                    _aim = false;
                    break;
            }

            switch (buttons.playerSwap)
            {
                case SWAP_WEAPON.QE:
                    _cycle = (Input.GetKeyDown(KeyCode.Q) ? 1 : 0) - (Input.GetKeyDown(KeyCode.E) ? 1 : 0);
                    break;
                case SWAP_WEAPON.SCROLL:
                    _cycle = (int)Mathf.Clamp(Input.mouseScrollDelta.y, -1, 1);
                    break;

                default:
                    _cycle = 0;
                    break;
            }

            switch (buttons.playerDirectSwap)
            {
                case DIRECT_SWAP_WEAPON.NUM_KEYS:

                    for (int i = 0; i < 10; ++i)
                    {
                        if (Input.GetKeyDown((KeyCode)(i + (int)KeyCode.Alpha0)))
                        {
                            _directSwap = i;
                            break;
                        }
                    }

                    break;

                default:
                    _directSwap = -1;
                    break;
            }
        }

        Vector3 ReturnByKeys(KeyCode UP, KeyCode LEFT, KeyCode DOWN, KeyCode RIGHT)
        {
            return new Vector3((Input.GetKey(RIGHT) ? 1f : 0f) - (Input.GetKey(LEFT) ? 1f : 0f), 0f, (Input.GetKey(UP) ? 1f : 0f) - (Input.GetKey(DOWN) ? 1f : 0f)).normalized;
        }
        #endregion

        #region INPUT_GETTERS
        public Vector3 move
        {
            get
            {
                if (!_instance.inputsUpdated)
                    _instance.UpdateInputs();
                return _move;
            }
        }

        public Vector3 rotate
        {
            get
            {
                if (!_instance.inputsUpdated)
                    _instance.UpdateInputs();
                return _rotate;
            }
        }

        public bool jump
        {
            get
            {
                if (!_instance.inputsUpdated)
                    _instance.UpdateInputs();
                return _jump;
            }
        }

        public bool shoot
        {
            get
            {
                if (!_instance.inputsUpdated)
                    _instance.UpdateInputs();
                return _shoot;
            }
        }

        public bool reload
        {
            get
            {
                if (!_instance.inputsUpdated)
                    _instance.UpdateInputs();
                return _reload;
            }
        }

        public bool aim
        {
            get
            {
                if (!_instance.inputsUpdated)
                    _instance.UpdateInputs();
                return _aim;
            }
        }

        public int swap
        {
            get
            {
                if (!_instance.inputsUpdated)
                    _instance.UpdateInputs();
                return _cycle;
            }
        }

        public int directSwap
        {
            get
            {
                if (!_instance.inputsUpdated)
                    _instance.UpdateInputs();
                return _directSwap;
            }
        }
        #endregion

        #region END_OF_FRAME_UPDATE
        WaitForEndOfFrame WFEOF;

        private IEnumerator FrameUpdate()
        {
            while (true)
            {
                inputsUpdated = false;
                yield return WFEOF;
            }
        }
        #endregion
    }
}