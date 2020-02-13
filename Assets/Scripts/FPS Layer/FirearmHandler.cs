using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class FirearmHandler : MonoBehaviour
{
	public GunStats gunStats;
	//Scriptable Object required variables
	public Firearms guns;
	public Firearms slot1, slot2, slot3;

	public Camera PV;
	public Transform ammoHUD;

	//Firearm Universal
	public GameObject bulletHole;
	public GameObject bulletImpact;

	public Transform barrel;

	//Method usage
	private int remainingClip;
	private float currentAcc;
	private float timeToNextShot;

	// Start is called before the first frame update
	void Start()
    {
		updateWeapon();

		reload();


    }

  
    void Update()
    {
		if (Input.GetKeyDown(KeyCode.R))
		{
			reload();
		}
		if (Input.GetKey(KeyCode.Mouse0) && Time.time >= timeToNextShot)
			{
			timeToNextShot = Time.time + 1f /gunStats.RoF;
			if (remainingClip > 0)
			{
				fire();
			}
			else
			{
				dryFire();
			}
			
		}
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			guns = slot1;
			updateWeapon();
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			guns = slot2;
			updateWeapon();
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			guns = slot3;
			updateWeapon();
		}

	}
	void reload()
	{
		remainingClip = gunStats.clip;
		currentAcc = gunStats.accuracy;
	}
	void updateWeapon()
	{
		gunStats = guns.gunStats;
		reload();
	}
	void fire()
	{
		Cursor.visible = false;
		remainingClip--;
		for (int i = 0; i < gunStats.projNum; i++)
		{
			float variance = (200 - currentAcc) *0.001f;
			Vector3 projectileDirection = PV.transform.forward;
			projectileDirection.x += Random.Range(-variance, variance);
			projectileDirection.y += Random.Range(-variance, variance);
			projectileDirection.z += Random.Range(-variance, variance);

			currentAcc = currentAcc - gunStats.accReduc;

			if (currentAcc <= 0.0f)
				currentAcc = 0.0f;

			Ray bulletTrajectory = new Ray(PV.transform.position, projectileDirection);
			RaycastHit hit;
			if (Physics.Raycast(bulletTrajectory, out hit))
			{
				Debug.DrawRay(PV.transform.position,projectileDirection*10,Color.green,10,false);
				GameObject impact= Instantiate(bulletImpact, hit.point, Quaternion.LookRotation(hit.normal));
				Destroy(impact,1f);
			}
		}
	}
	void dryFire()
	{
	}
}
