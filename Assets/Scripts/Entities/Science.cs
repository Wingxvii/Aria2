using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Science : Building
{
    public Slider researchProgress;
    private Canvas canvas;


    protected override void BaseStart()
    {
        currentHealth = 1000;
        maxHealth = 1000;

        //add upgrade
        if (ResourceManager.Instance.buildingHealth)
        {
            IncreaseBuildingHealth();
        }

        canvas = GetComponentInChildren<Canvas>();
        canvas.transform.LookAt(canvas.transform.position + Camera.main.transform.rotation * Vector3.back, Camera.main.transform.rotation * Vector3.up);

        researchProgress = canvas.transform.Find("Research Progress").GetComponent<Slider>();
        researchProgress.gameObject.SetActive(false);

    }


}
