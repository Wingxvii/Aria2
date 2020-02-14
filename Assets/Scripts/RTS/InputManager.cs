using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RTSInput
{
    public enum MouseEvent { 
        None,
        Turret,
        Barracks,
        Wall,
        Science,
        MovementCursor,
        AttackCursor,
        RallyCursor,
    }

    public enum StaticTag {
        Ground,
        Buildable,
        Other,
    }

    public class InputManager : MonoBehaviour { 
        #region SingletonCode
        private static InputManager _instance;
        public static InputManager Instance { get { return _instance; } }
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }
        }
        //single pattern ends here
        #endregion

        #region SelectionBox
        [Header("Selection Box")]

        public Vector2 boxStart;
        public Vector2 boxEnd;
        public bool boxActive = false;
        public Texture selectionBox;
        public int BoxThreshold = 15;
        #endregion

        #region SelectedAttributes

        [Header("Selected Attributes")]
        //primary selected object
        public Entity PrimaryEntity;
        public List<Entity> SelectedEntities;
        //selected type cycling
        public bool selectedTypeFlag = false;
        //dirty flag for selection changes
        public bool selectionChanged = false;
        #endregion

        #region Raycasting
        [Header("Raycasting")]
        //raycasting

        //cast mouse position on static game map
        public Vector3 staticPosition;
        public StaticTag staticTag;

        //currently hit object, if any
        public Entity HitObject;

        private Ray ray;
        private RaycastHit hit;
        #endregion

        #region Blueprints
        public GameObject activeBlueprint;

        public GameObject turretBlueprint;
        public GameObject barracksBlueprint;   
        public GameObject wallBlueprint;
        public GameObject scienceBlueprint;
        public GameObject moveCursorBlueprint;
        public GameObject attackCursorBlueprint;
        public GameObject rallyBlueprint;
        #endregion

        #region DoubleClick
        //double click
        [Header("Double Click")]
        private float doubleClickTimeLimit = 0.2f;
        #endregion

        MouseEvent currentEvent = MouseEvent.None;

        // Start is called before the first frame update
        void Start()
        {
            //SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(2));

            SelectedEntities = new List<Entity>();
            StartCoroutine(DoubleClickListener());

            //prefab instanitation
            turretBlueprint = Instantiate(turretBlueprint);
            turretBlueprint.SetActive(false);
            barracksBlueprint = Instantiate(barracksBlueprint);
            barracksBlueprint.SetActive(false);
            wallBlueprint = Instantiate(wallBlueprint);
            wallBlueprint.SetActive(false);
            scienceBlueprint = Instantiate(scienceBlueprint);
            scienceBlueprint.SetActive(false);
            moveCursorBlueprint = Instantiate(moveCursorBlueprint);
            moveCursorBlueprint.SetActive(false);
            attackCursorBlueprint = Instantiate(attackCursorBlueprint);
            attackCursorBlueprint.SetActive(false);
            rallyBlueprint = Instantiate(rallyBlueprint);
            rallyBlueprint.SetActive(false);
            activeBlueprint = turretBlueprint;
            activeBlueprint.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            //[FPS]update input only if game is currently running
            if (ResourceManager.Instance.gameState == ResourceManager.GameState.Running)
            {
                #region Raycast
                //raycast the mouse
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 500, EntityManager.Instance.staticsMask))
                {
                    staticPosition = hit.point;
                    PlacementGrid grid;

                    if (hit.collider.CompareTag("Ground"))
                    {
                        staticTag = StaticTag.Ground;
                    }
                    
                    if (hit.collider.gameObject.TryGetComponent<PlacementGrid>(out grid))
                    {
                        staticPosition = grid.Snap(hit.point) - grid.m_offset;
                        staticTag = StaticTag.Buildable;
                    }
                    else {
                        staticTag = StaticTag.Other;
                    }

                }
                if (Physics.Raycast(ray, out hit, 500, EntityManager.Instance.entitysMask))
                {
                    HitObject = hit.collider.gameObject.GetComponent<Entity>();
                }
                else
                {
                    HitObject = null;
                }
                //check to see if anything gets hit

                #endregion

                //handle selection box first
                HandleSelectionBox();

                //handle left mouse click events
                HandleLeftMouseClicks();

                //handle right mouse click events
                HandleRightMouseClicks();

                //handle key press
                HandleKeys();

                //check activity
                CheckActivity();

                //handle selection changes #depricated in commit e27ca50f5208e5b2c054ff258cac52ef5b323323
                //HandleSelectionChanges();

                //check validity of current blueprint
                CheckValidity();



                //bind prefab object to mouse
                if (activeBlueprint != null && activeBlueprint.activeSelf)
                {
                    activeBlueprint.transform.position = new Vector3(InputManager.Instance.staticPosition.x, InputManager.Instance.staticPosition.y, InputManager.Instance.staticPosition.z);
                    ShellPlacement bp = activeBlueprint.GetComponent<ShellPlacement>();
                    if (bp.offset)
                    {
                        activeBlueprint.transform.position += bp.offset.localPosition;
                    }
                }
            }
        }
        //handles selection box
        private void HandleSelectionBox()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                //handle box init behaviour
                if (Input.GetMouseButtonDown(0) && boxActive == false && currentEvent == MouseEvent.None)
                {
                    boxStart = Input.mousePosition;
                    boxActive = true;
                }
                //handle box drag updates
                else if (Input.GetMouseButton(0) && boxActive)
                {
                    if (Mathf.Abs(boxStart.x - Input.mousePosition.x) > BoxThreshold || Mathf.Abs(boxStart.y - Input.mousePosition.y) > BoxThreshold)
                    {
                        boxEnd = Input.mousePosition;
                    }
                    else
                    {
                        boxEnd = Vector2.zero;
                    }
                }
            }

            //handle box release
            if (Input.GetMouseButtonUp(0) && boxActive)
                {
                    foreach (Entity obj in EntityManager.Instance.AllEntities)
                    {
                        if (obj == null) continue;

                        Vector3 screenPoint = Camera.main.WorldToScreenPoint(obj.transform.position);

                        if (screenPoint.x >= Mathf.Min(boxStart.x, Input.mousePosition.x) &&
                            screenPoint.x <= Mathf.Max(boxStart.x, Input.mousePosition.x) &&
                            screenPoint.y >= Mathf.Min(boxStart.y, Input.mousePosition.y) &&
                            screenPoint.y <= Mathf.Max(boxStart.y, Input.mousePosition.y) && !SelectedEntities.Contains(obj) &&
                            obj.gameObject.activeSelf)
                        {
                            SelectedEntities.Add(obj);
                            obj.GetComponent<Entity>().OnSelect();
                            selectionChanged = true;
                        }
                    }
                    SwitchPrimarySelected();

                    boxStart = Vector2.zero;
                    boxEnd = Vector2.zero;
                    boxActive = false;

                }
        }

        //handles left mouse input
        private void HandleLeftMouseClicks()
        {
            //check if anything events need to be handled
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                #region Prefab Logic
                //check for prefab placeable
                if (currentEvent == MouseEvent.Turret || currentEvent == MouseEvent.Wall || currentEvent == MouseEvent.Barracks || currentEvent == MouseEvent.Science)
                {
                    if (activeBlueprint != null && activeBlueprint.GetComponent<ShellPlacement>().placeable)
                    {
                        ShellPlacement shell = activeBlueprint.GetComponent<ShellPlacement>();

                        //check for continous placement
                        if (!Input.GetKey(KeyCode.LeftShift))
                        {
                            activeBlueprint.SetActive(false);
                            ClearSelection();
                        }
                        if (ResourceManager.Instance.Purchase(shell.type))
                        {
                            CommandManager.Instance.Build(activeBlueprint.transform.position, shell.type);
                        }
                        //not purchaseable
                        else
                        {
                            NotificationManager.Instance.HitNotification(NotificationType.INSUFFICIENT_CREDITS);
                        }

                    }
                    //handle prefab not placeable exception
                    else if (activeBlueprint != null && !activeBlueprint.GetComponent<ShellPlacement>().placeable)
                    {
                        NotificationManager.Instance.HitNotification(NotificationType.INVALID_PLACEMENT);
                    }
                }
                #endregion

                #region Unit Command Logic
                //handle unit movement commands
                else if (currentEvent == MouseEvent.MovementCursor)
                {
                    //send the mouse location of all objects with the same type as the primary type
                    foreach (Entity obj in SelectedEntities)
                    {
                        if (obj.type == PrimaryEntity.type)
                        {
                            CommandManager.Instance.IssueLocation(obj, staticPosition);
                        }
                    }

                    activeBlueprint.SetActive(false);
                    currentEvent = MouseEvent.None;
                }
                //handle unit/building attack commands
                else if (currentEvent == MouseEvent.AttackCursor)
                {
                    if (HitObject != null && HitObject.type == EntityType.Player)
                    {
                        foreach (Entity obj in SelectedEntities)
                        {
                            if (obj.type == EntityType.Droid || obj.type == EntityType.Turret)
                            {
                                CommandManager.Instance.IssueAttack(obj, HitObject);
                            }
                        }
                    }
                    else {
                        foreach (Entity obj in SelectedEntities)
                        {
                            if (obj.type == EntityType.Droid || obj.type == EntityType.Turret)
                            {
                                CommandManager.Instance.IssueAttack(obj, staticPosition);
                            }
                        }
                    }
                    activeBlueprint.SetActive(false);
                    currentEvent = MouseEvent.None;
                }
                //handle barracks rally point
                else if (currentEvent == MouseEvent.RallyCursor)
                {
                    //send the mouse location of all objects with the same type as the primary type
                    foreach (Entity obj in SelectedEntities)
                    {
                        if (obj.type == PrimaryEntity.type)
                        {
                            CommandManager.Instance.IssueLocation(obj, staticPosition);
                        }
                    }
                    activeBlueprint.SetActive(false);
                    currentEvent = MouseEvent.None;
                }
                #endregion

                #region Selection Logic
                //selection logic
                else
                {
                    //selection checking
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        //handle deselection
                        if (HitObject == null)
                        {
                            foreach (Entity obj in SelectedEntities)
                            {
                                obj.OnDeselect();
                            }
                            SelectedEntities.Clear();
                        }
                        //check if object is active
                        else if (HitObject.gameObject.activeSelf)
                        {
                            //check to see if already selected
                            if (SelectedEntities.Contains(HitObject))
                            {
                                //Selective Deselection
                                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
                                {
                                    ClearSelection();
                                    SelectedEntities.Add(HitObject);
                                    SwitchPrimarySelected(HitObject);

                                    currentEvent = MouseEvent.None;
                                    HitObject.OnSelect();
                                }
                                //refocus to one selection
                                else
                                {
                                    DeselectItem(HitObject);
                                }
                            }
                            //if not already selected
                            else
                            {
                                //Selective Deselection
                                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
                                {
                                    ClearSelection();
                                }

                                SelectedEntities.Add(HitObject);
                                SwitchPrimarySelected(HitObject);

                                currentEvent = MouseEvent.None;
                                HitObject.OnSelect();
                            }
                        }
                        //deselect on ground selection, with selection exceptions
                        else if (HitObject.gameObject.tag == "Ground" && !(currentEvent == MouseEvent.None && boxActive) && Input.GetKey(KeyCode.LeftShift))
                        {
                            foreach (Entity obj in SelectedEntities)
                            {
                                obj.OnDeselect();
                            }
                            currentEvent = MouseEvent.None;
                            SelectedEntities.Clear();
                        }
                        selectionChanged = true;
                    }
                }
                #endregion
            }
        }

        //handles right mouse input
        private void HandleRightMouseClicks()
        {
            if (Input.GetKeyUp(KeyCode.Mouse1))
            {
                //disable current command and revert to selection 
                if (currentEvent != MouseEvent.None)
                {
                    activeBlueprint.SetActive(false);
                    currentEvent = MouseEvent.None;
                }
                //if current event is selection
                if (currentEvent == MouseEvent.None)
                {
                    //check if enemy selected
                    if (HitObject != null && HitObject.type == EntityType.Player)
                    {
                        foreach (Entity obj in SelectedEntities)
                        {
                            if (obj.type == EntityType.Droid || obj.type == EntityType.Turret)
                            {
                                CommandManager.Instance.IssueAttack(obj, HitObject);
                            }
                        }
                    }
                    else if(SelectedEntities.Count > 0 && staticTag == StaticTag.Ground)
                    {
                        //give every object their default commands
                        foreach (Entity obj in SelectedEntities)
                        {
                            if (obj.type == EntityType.Turret)
                            {
                                CommandManager.Instance.IssueAttack(obj, HitObject);
                            }
                            else if (obj.type == EntityType.Droid)
                            {
                                CommandManager.Instance.IssueLocation(obj, staticPosition);
                            }
                            else if (obj.type == EntityType.Barracks) {
                                CommandManager.Instance.IssueLocation(obj, staticPosition);
                            }
                        }
                    }
                }
            }
        }

        //handles all key input
        private void HandleKeys()
        {

            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Break();
            }
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Delete))
            {
                OnDeleteAll();
            }
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                OnDelete();
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                OnBuildPrefabs(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                OnBuildPrefabs(2);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                OnBuildPrefabs(3);
            }
            if (Input.GetKeyDown("escape"))
            {
                Application.Quit();
            }


            //deactivate preset on shift hold up
            if (Input.GetKeyUp(KeyCode.LeftShift) && currentEvent != MouseEvent.None)
            {
                ShiftUp();
            }



            //handles primary selectable cycling
            if (Input.GetKeyDown(KeyCode.Tab) && PrimaryEntity != null && currentEvent == MouseEvent.None)
            {
                //allows the program to do a full search starting from the flag
                do
                {
                    foreach (Entity obj in SelectedEntities)
                    {
                        //this checks for the first item in the next selectable type, then switches the primary to it
                        if (PrimaryEntity == obj)
                        {
                            //checks if this went full circle
                            if (selectedTypeFlag)
                            {
                                selectedTypeFlag = false;
                                break;
                            }
                            //else, set flag to true
                            selectedTypeFlag = true;
                        }
                        //if a different type is found, and flag is active
                        if (obj.type != PrimaryEntity.type && selectedTypeFlag)
                        {
                            selectedTypeFlag = false;
                            PrimaryEntity = obj;
                        }
                    }
                } while (selectedTypeFlag);                 //my first practical use of do-while =D
            }

        }

        //check the activity of all objects
        private void CheckActivity() {
            for (int counter = InputManager.Instance.SelectedEntities.Count-1; counter >= 0; counter--) {
                Entity obj = InputManager.Instance.SelectedEntities[counter];

                if (!obj.gameObject.activeSelf) {
                    //remove if not active
                    InputManager.Instance.SelectedEntities.Remove(obj);

                    //update primary selected activity
                    if (PrimaryEntity == obj) {
                        SwitchPrimarySelected();
                    }
                    selectionChanged = true;
                }
            }
        }

        //handles changes in selection
        private void HandleSelectionChanges()
        {

            //if flag is activated do operations
            if (selectionChanged)
            {
                //Debug.Log("Selection Changed");
                if (SelectedEntities.Count > 1)
                {
                    //here is where the selection UI happens
                    SelectionUI.Instance.ProcessUI(true);
                }

                selectionChanged = false;
            }
        }


        #region Helpers

        //checks for if current mouse blueprints' activities
        public void CheckValidity()
        {

            if (turretBlueprint.activeSelf)
            {
                //check for activity
                if (activeBlueprint != turretBlueprint) { turretBlueprint.SetActive(false); }
                //check for mouse status validity
                if (InputManager.Instance.currentEvent != MouseEvent.Turret) { turretBlueprint.SetActive(false); }
            }
            if (barracksBlueprint.activeSelf)
            {
                //check for activity
                if (activeBlueprint != barracksBlueprint) { barracksBlueprint.SetActive(false); }
                //check for mouse status validity
                if (InputManager.Instance.currentEvent != MouseEvent.Barracks) { barracksBlueprint.SetActive(false); }
            }
            if (wallBlueprint.activeSelf)
            {
                //check for activity
                if (activeBlueprint != wallBlueprint) { wallBlueprint.SetActive(false); }
                //check for mouse status validity
                if (InputManager.Instance.currentEvent != MouseEvent.Wall) { barracksBlueprint.SetActive(false); }

            }
            if (scienceBlueprint.activeSelf)
            {
                //check for activity
                if (activeBlueprint != wallBlueprint) { wallBlueprint.SetActive(false); }
                //check for mouse status validity
                if (InputManager.Instance.currentEvent != MouseEvent.Science) { barracksBlueprint.SetActive(false); }

            }
            if (moveCursorBlueprint.activeSelf)
            {
                //check for activity
                if (activeBlueprint != moveCursorBlueprint) { moveCursorBlueprint.SetActive(false); }
                //check for mouse status validity
                if (InputManager.Instance.currentEvent != MouseEvent.MovementCursor) { moveCursorBlueprint.SetActive(false); }

            }
            if (attackCursorBlueprint.activeSelf)
            {
                //check for activity
                if (activeBlueprint != attackCursorBlueprint) { attackCursorBlueprint.SetActive(false); }
                //check for mouse status validity
                if (InputManager.Instance.currentEvent != MouseEvent.AttackCursor) { attackCursorBlueprint.SetActive(false); }
            }
            if (rallyBlueprint.activeSelf)
            {
                //check for activity
                if (activeBlueprint != rallyBlueprint) { rallyBlueprint.SetActive(false); }
                //check for mouse status validity
                if (InputManager.Instance.currentEvent != MouseEvent.RallyCursor) { rallyBlueprint.SetActive(false); }

            }

        }
        public void OnBuildPrefabs(int prefab)
        {
            ClearSelection();
            activeBlueprint.SetActive(false);

            switch (prefab)
            {
                case 1:
                    currentEvent = MouseEvent.Turret;
                    turretBlueprint.SetActive(true);
                    activeBlueprint = turretBlueprint;
                    break;
                case 2:
                    currentEvent = MouseEvent.Barracks;
                    barracksBlueprint.SetActive(true);
                    activeBlueprint = barracksBlueprint;
                    break;
                case 3:
                    currentEvent = MouseEvent.Wall;
                    wallBlueprint.SetActive(true);
                    activeBlueprint = wallBlueprint;
                    break;
                case 4:
                    currentEvent = MouseEvent.Science;
                    scienceBlueprint.SetActive(true);
                    activeBlueprint = scienceBlueprint;
                    break;
                default:
                    Debug.LogError("Error: Invalid Type for building blueprint");
                    break;
            }
        }

        //deselects an object
        public void DeselectItem(Entity obj)
        {
            obj.OnDeselect();

            //remove obj
            if (SelectedEntities.Contains(obj))
            {
                SelectedEntities.Remove(obj);
            }

            //exception checking
            if (SelectedEntities.Count > 0)
            {
                if (PrimaryEntity == obj)
                {
                    PrimaryEntity = SelectedEntities[SelectedEntities.Count - 1];
                }
                if (SelectedEntities.Count > 1)
                {
                    SelectionUI.Instance.ProcessUI(false);
                }
            }
            else
            {
                ClearSelection();
            }

        }

        //if shift is released at any time
        public void ShiftUp()
        {
            activeBlueprint.SetActive(false);
            InputManager.Instance.ClearSelection();
        }


        //clears selection (deselects everything)
        public void ClearSelection()
        {
            if (!SelectedEntities.Count.Equals(0))
            {
                foreach (Entity obj in SelectedEntities)
                {
                    obj.OnDeselect();
                }
                SelectedEntities.Clear();
            }

            selectionChanged = true;
            currentEvent = MouseEvent.None;
            boxStart = Vector2.zero;
            boxEnd = Vector2.zero;
            boxActive = false;

            SwitchPrimarySelected();
        }

        //focus on single entity
        public void OnFocusSelected(Entity obj)
        {
            ClearSelection();
            SelectedEntities.Add(obj);
            SwitchPrimarySelected(obj);

            currentEvent = MouseEvent.None;
            obj.OnSelect();

        }
        //switches currently active entity
        public void SwitchPrimarySelected(Entity primary = null)
        {
            if (primary == null && SelectedEntities.Count > 0)
            {
                PrimaryEntity = SelectedEntities[SelectedEntities.Count - 1];
            }
            else
            {
                PrimaryEntity = primary;
            }
        }

        //deactivate selected entities
        public void OnDelete()
        {
            foreach (Entity obj in InputManager.Instance.SelectedEntities)
            {
                obj.OnDeActivate();
            }
        }

        //deactivate all entities
        public void OnDeleteAll()
        {
            InputManager.Instance.ClearSelection();

            foreach (Entity obj in EntityManager.Instance.AllEntities)
            {
                if (obj.gameObject.activeSelf)
                {
                    obj.OnDeActivate();
                }
            }
        }


        #endregion

        #region UIInputFunctions

        //train unit in barracks
        public void OnTrainBarracks()
        {
            foreach (Entity entity in SelectedEntities) {
                if (entity.type == PrimaryEntity.type) {
                    CommandManager.Instance.CallAction(entity, 1);
                }
            }
        }

        //switch current mouse action to Movement indicator
        public void OnSelectMove()
        {
            activeBlueprint.SetActive(false);
            activeBlueprint = moveCursorBlueprint;
            activeBlueprint.SetActive(true);

            currentEvent = MouseEvent.MovementCursor;
        }

        //switch current mouse action to Attack indicator
        public void OnSelectAttack()
        {
            activeBlueprint.SetActive(false);
            activeBlueprint = attackCursorBlueprint;
            activeBlueprint.SetActive(true);

            currentEvent = MouseEvent.AttackCursor;

        }
        //reloads all turrets
        public void OnReload()
        {
            foreach (Entity entity in SelectedEntities)
            {
                if (entity.type == PrimaryEntity.type)
                {
                    entity.CallAction(1);
                }
            }
        }

        //switch current mouse action to Rally point indicator
        public void OnRally()
        {
            activeBlueprint.SetActive(false);
            activeBlueprint = rallyBlueprint;
            activeBlueprint.SetActive(true);

            currentEvent = MouseEvent.RallyCursor;
        }
        #endregion

        #region Functional Input
        private void OnGUI()
        {
            //used to draw selection box

            if (boxStart != Vector2.zero && boxEnd != Vector2.zero)
            {
                GUI.DrawTexture(new Rect(boxStart.x, Screen.height - boxStart.y, boxEnd.x - boxStart.x, -1 * ((Screen.height - boxStart.y) - (Screen.height - boxEnd.y))), selectionBox);
            }
        }

        private IEnumerator DoubleClickListener()
        {
            while (enabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    yield return ClickEvent();
                }
                yield return null;
            }
        }

        private IEnumerator ClickEvent()
        {
            //pause a frame so you don't pick up the same mouse down event.
            yield return new WaitForEndOfFrame();

            float count = 0f;
            while (count < doubleClickTimeLimit)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    DoubleClick();
                    yield break;
                }
                count += Time.deltaTime;
                yield return null;
            }
        }

        private void DoubleClick()
        {
            //Debug.Log("Double Clicked");
            if (currentEvent == MouseEvent.None)
            {
                if (HitObject != null && Input.GetKey(KeyCode.LeftShift))
                {
                    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

                    //screenspace selection
                    foreach (Entity entity in EntityManager.Instance.AllEntities)
                    {
                        if (entity.type == HitObject.type &&
                            GeometryUtility.TestPlanesAABB(planes, entity.GetComponent<Renderer>().bounds) &&
                            !SelectedEntities.Contains(entity) && entity.gameObject.activeSelf)
                        {
                            SelectedEntities.Add(entity);
                            entity.GetComponent<Entity>().OnSelect();
                            selectionChanged = true;
                        }
                    }
                    //update primary selecteable
                    SwitchPrimarySelected();
                }
            }
        }
        #endregion
    }
}