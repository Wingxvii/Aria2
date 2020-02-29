using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSInput
{
    public class ScienceManager : MonoBehaviour
    {
        #region SingletonCode
        private static ScienceManager _instance;
        public static ScienceManager Instance { get { return _instance; } }
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

        public Sprite[] researchImages;
        
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        #region upgradeCalls
        public void DoubleBounties(){
            ResourceManager.Instance.doubleBounties = true;
        }

        public void ImprovedBuildingHealth() {
            if (ResourceManager.Instance.buildingHealth == false)
            {
                ResourceManager.Instance.buildingHealth = true;

                foreach (Entity entity in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Barracks])
                {
                    entity.IncreaseBuildingHealth();
                }
                foreach (Entity entity in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Science])
                {
                    entity.IncreaseBuildingHealth();
                }
                foreach (Entity entity in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Turret])
                {
                    entity.IncreaseBuildingHealth();
                }
                foreach (Entity entity in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Wall])
                {
                    entity.IncreaseBuildingHealth();
                }
            }
        }

        public void ImprovedDroid()
        {
            if (ResourceManager.Instance.buildingHealth == false)
            {

                ResourceManager.Instance.droidStronger = true;

                foreach (Entity entity in EntityManager.Instance.ActiveEntitiesByType[(int)EntityType.Droid])
                {
                    entity.IncreaseBuildingHealth();
                }
            }
        }



        public void IncreaseTrickle() {
            ResourceManager.Instance.resourceTrickle = true;
        }

        public void FasterTraining() {
            ResourceManager.Instance.fasterTraining = true;
            Barracks.droidTrainTime = 3.0f;
        }

        #endregion

        public void CallResearch(int num)
        {
            ResearchProgress slider = InputManager.Instance.PrimaryEntity.GetComponent<Science>().researchProgress;
            if (!slider.gameObject.activeSelf && !CheckResearched(num) && ResourceManager.Instance.RequestResearch(num))
            {
                //No need for the old slider to be visible
                slider.gameObject.SetActive(true);
                if (num < researchImages.Length) {
                    slider.SetImage(researchImages[num]);
                }
                switch (num)
                {
                    case 0:
                        StartCoroutine(ResearchCoroutine(10.0f, slider, num));
                        break;
                    case 1:
                        StartCoroutine(ResearchCoroutine(10.0f, slider, num));
                        break;
                    case 2:
                        StartCoroutine(ResearchCoroutine(20.0f, slider, num));
                        break;
                    case 3:
                        StartCoroutine(ResearchCoroutine(20.0f, slider, num));
                        break;
                    case 4:
                        StartCoroutine(ResearchCoroutine(25.0f, slider, num));
                        break;
                    case 5:
                        Debug.Log("FOG NOT YET IMPLEMENTED");

                        break;
                    default:
                        Debug.LogWarning("Unrecognized Research");
                        break;
                }
            }
            else if (slider.gameObject.activeSelf)
            {
                Debug.Log("Current Research Center is Busy");
            }
            else if (CheckResearched(num)) {
                Debug.Log("Already Researched");
            }
        }

        public void FinishResearch(int num)
        {
            switch (num)
            {
                case 0:
                    DoubleBounties();
                    break;
                case 1:
                    ImprovedBuildingHealth();
                    break;
                case 2:
                    IncreaseTrickle();
                    break;
                case 3:
                    FasterTraining();
                    break;
                case 4:
                    ImprovedDroid();
                    break;
                case 5:
                    Debug.Log("FOG NOT YET IMPLEMENTED");

                    break;
                default:
                    Debug.LogWarning("Unrecognized Research");
                    break;
            }
        }

        public bool CheckResearched(int num) {
            switch (num)
            {
                case 0:
                    if (ResourceManager.Instance.doubleBounties) { return true; } else { return false; }
                case 1:
                    if (ResourceManager.Instance.buildingHealth) { return true; } else { return false; }
                case 2:
                    if (ResourceManager.Instance.resourceTrickle) { return true; } else { return false; }
                case 3:
                    if (ResourceManager.Instance.fasterTraining) { return true; } else { return false; }
                case 4:
                    if (ResourceManager.Instance.droidStronger) { return true; } else { return false; }
                case 5:
                    Debug.Log("FOG NOT YET IMPLEMENTED");
                    return false;
                    break;
                default:
                    Debug.LogWarning("Unrecognized Research Check");
                    return false;
            }
        }


        IEnumerator ResearchCoroutine(float timer, ResearchProgress progress, int researchNum)
        {
            float startTime = 0.0f;

            while (startTime < timer)
            {
                startTime += Time.deltaTime;
                progress.SetValue(1-(startTime / timer));

                yield return 0;
            }
            FinishResearch(researchNum);
            progress.gameObject.SetActive(false);
        }

    }
}