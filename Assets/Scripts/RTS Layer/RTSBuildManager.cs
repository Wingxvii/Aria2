using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RTSInput
{
    public class RTSBuildManager : MonoBehaviour
    {
        #region SingletonCode
        private static RTSBuildManager _instance;
        public static RTSBuildManager Instance { get { return _instance; } }
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

        public GameObject prefabObject;
        public EntityType prefabType;

        //all prefabs
        public GameObject turretBlueprint;
        public GameObject barracksBlueprint;
        public GameObject wallBlueprint;
        public GameObject moveCursorBlueprint;
        public GameObject attackCursorBlueprint;
        public GameObject rallyBlueprint;

        void Start()
        {
            //set this as active scene
            //SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(2));


            //prefab instanitation
            turretBlueprint = Instantiate(Resources.Load<GameObject>("Prefabs/Blueprints/turretBlueprint"));
            turretBlueprint.SetActive(false);
            barracksBlueprint = Instantiate(Resources.Load<GameObject>("Prefabs/Blueprints/barracksBlueprint"));
            barracksBlueprint.SetActive(false);
            wallBlueprint = Instantiate(Resources.Load<GameObject>("Prefabs/Blueprints/wallBlueprint"));
            wallBlueprint.SetActive(false);
            moveCursorBlueprint = Instantiate(Resources.Load<GameObject>("Prefabs/Blueprints/moveCursorBlueprint"));
            moveCursorBlueprint.SetActive(false);
            attackCursorBlueprint = Instantiate(Resources.Load<GameObject>("Prefabs/Blueprints/attackCursorBlueprint"));
            attackCursorBlueprint.SetActive(false);
            rallyBlueprint = Instantiate(Resources.Load<GameObject>("Prefabs/Blueprints/rallyBlueprint"));
            rallyBlueprint.SetActive(false);
            prefabObject = turretBlueprint;
            prefabObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            #region hotkeys
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
            #endregion

            //bind prefab object to mouse
            if (prefabObject != null && prefabObject.activeSelf)
            {
                prefabObject.GetComponent<Transform>().position = new Vector3(SelectionManager.Instance.mousePosition.x, SelectionManager.Instance.mousePosition.y + prefabObject.GetComponent<Transform>().localScale.y, SelectionManager.Instance.mousePosition.z);
            }

        }

        public void OnBuildPrefabs(int prefab)
        {
            SelectionManager.Instance.OnPrefabCreation();
            prefabObject.SetActive(false);

            switch (prefab)
            {
                case 1:
                    turretBlueprint.SetActive(true);
                    prefabObject = turretBlueprint;
                    prefabType = EntityType.Turret;
                    break;
                case 2:
                    barracksBlueprint.SetActive(true);
                    prefabObject = barracksBlueprint;
                    prefabType = EntityType.Barracks;
                    break;
                case 3:
                    wallBlueprint.SetActive(true);
                    prefabObject = wallBlueprint;
                    prefabType = EntityType.Wall;
                    break;
                default:
                    Debug.LogError("Error: Invalid Type for building blueprint");
                    break;
            }
        }
        public void OnDelete()
        {
            foreach (Entity obj in SelectionManager.Instance.SelectedEntities)
            {
                obj.OnDeActivate();
            }
        }

        public void OnDeleteAll()
        {
            SelectionManager.Instance.ClearSelection();

            foreach (Entity obj in EntityManager.Instance.AllEntities)
            {
                if (obj.gameObject.activeSelf) {
                    obj.OnDeActivate();
                }
            }
        }
    }
}