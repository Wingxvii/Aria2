using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTSInput;

public class Barracks : Building
{
    public Slider buildProcess;
    public GameObject processObject;

    public Queue<float> buildTimes;
    public float currentBuildTime = 0;

    public static float droidTrainTime = 5.0f;
    public static int maxTrainingCap = 6;

    private Canvas canvas;

    public GameObject flagObj;
    public bool flagActive = false;

    private Transform spawnPoint;

    //inherited function realizations
    protected override void BaseStart()
    {
        currentHealth = 1000;
        maxHealth = 1000;

        //add upgrade
        if (ResourceManager.Instance.buildingHealth) {
            IncreaseBuildingHealth();
        }
        

        type = EntityType.Barracks;

        canvas = GetComponentInChildren<Canvas>();
        canvas.transform.LookAt(canvas.transform.position + Camera.main.transform.rotation * Vector3.back, Camera.main.transform.rotation * Vector3.up);

        spawnPoint = this.transform.Find("SpawnPoint");
        
        buildProcess.gameObject.SetActive(false);

        buildProcess = canvas.transform.Find("Building Progress").GetComponent<Slider>();
        buildProcess.gameObject.SetActive(false);

        processObject.SetActive(false);

        buildTimes = new Queue<float>();

        //create a flag from the prefab
        flagObj = Instantiate(flagObj, Vector3.zero, Quaternion.identity);
        flagObj.SetActive(false);
        flagActive = false;

        BaseActivation();

    }

    protected override void BaseUpdate()
    {
        if (GameSceneController.Instance.type == PlayerType.RTS)
        {
            //add to queue
            if (currentBuildTime <= 0 && buildTimes.Count > 0)
            {
                // buildProcess.gameObject.SetActive(true);
                processObject.SetActive(true);
                currentBuildTime += buildTimes.Dequeue();
            }
            //tick queue
            else if (currentBuildTime > 0)
            {
                buildProcess.value = currentBuildTime / droidTrainTime;
                currentBuildTime -= Time.deltaTime;
                if (currentBuildTime <= 0)
                {
                    if (flagActive)
                    {
                        ResourceManager.Instance.QueueFinished(spawnPoint, EntityType.Droid, flagObj.transform.position);
                    }
                    else
                    {
                        ResourceManager.Instance.QueueFinished(spawnPoint, EntityType.Droid);
                    }
                }
            }
            //queue ended
            else if (currentBuildTime <= 0)
            {
                // buildProcess.gameObject.SetActive(false);
                processObject.SetActive(false);
            }
        }
    }

    public override void BaseSelected()
    {
        if (flagActive)
        {
            flagObj.SetActive(true);
        }
    }

    public override void BaseDeselected()
    {
        flagObj.SetActive(false);
    }


    public override void IssueLocation(Vector3 location)
    {
        flagObj.transform.position = new Vector3(location.x, location.y + 2.5f, location.z);
        flagActive = true;
        flagObj.SetActive(true);
    }
    public override void BaseActivation()
    {
        ResourceManager.Instance.numBarracksActive++;
        ResourceManager.Instance.UpdateSupply();
    }

    public override void BaseDeactivation()
    {
        ResourceManager.Instance.numBarracksActive--;
        ResourceManager.Instance.UpdateSupply();

        while (buildTimes.Count > 0)
        {
            buildTimes.Dequeue();
            ResourceManager.Instance.Refund(EntityType.Droid);
        }

        currentBuildTime = 0;
        flagActive = false;
        buildTimes.Clear();
    }

    //child-sepific functions
    public void OnTrainRequest()
    {
        if (buildTimes.Count < maxTrainingCap && ResourceManager.Instance.supplyCurrent < ResourceManager.Instance.totalSupply && ResourceManager.Instance.Purchase(EntityType.Droid))
        {
            buildTimes.Enqueue(ResourceManager.Instance.RequestQueue(EntityType.Droid));
        }
        else if (ResourceManager.Instance.supplyCurrent >= ResourceManager.Instance.totalSupply) {
            NotificationManager.Instance.HitNotification(NotificationType.SUPPLY_BLOCKED);
        }
        else if (buildTimes.Count >= maxTrainingCap)
        {
            NotificationManager.Instance.HitNotification(NotificationType.QUEUE_FULL);
        }
        else
        {
            NotificationManager.Instance.HitNotification(NotificationType.INSUFFICIENT_CREDITS);
        }
    }

    public override void CallAction(int action)
    {
        if (action == 1) {
            OnTrainRequest();
        }
    }
}