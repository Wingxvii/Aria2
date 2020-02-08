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
            switch (num) {
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

            Debug.Log("Research Called: " + num);
        }

        IEnumerator ResearchCoroutine(float timer, Slider progress)
        {
            float startTime = 0.0f;

            while (startTime < timer)
            {
                startTime += Time.deltaTime;
                progress.value = startTime / timer;



                yield return 0;
            }
            progress.value = 1;
        }

    }
}