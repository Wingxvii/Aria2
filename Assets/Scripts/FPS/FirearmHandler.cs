using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class FirearmHandler : MonoBehaviour
{
	public GunStats gunStats;
	//Scriptable Object required variables
	public Firearms activeGun;
	public Firearms[] slots;

	public Camera PV;
	public Transform ammoHUD;

	//Firearm Universal
	public GameObject bulletHole;
	public GameObject bulletImpact;
	public GameObject muzzleFlash;

	public Transform barrel;

	//Method usage
	private int remainingClip;
	private float currentAcc;
	private float timeToNextShot;

    int parentPlayer = -1;

    // Start is called before the first frame update
    void Start()
    {
		updateWeapon();

		reload();

        if (GetComponentInParent<FPSPlayer.Player>())
        {
            parentPlayer = GetComponentInParent<FPSPlayer.Player>().id;

            Debug.Log(parentPlayer - 1);
            Netcode.NetworkManager.firearms[parentPlayer - 1] = this;
        }
    }

  
    void Update()
    {
        if (PV != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                reload();
            }
            if (Input.GetKey(KeyCode.Mouse0) && Time.time >= timeToNextShot)
            {
                timeToNextShot = Time.time + 1f / gunStats.RoF;
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
                activeGun = slots[0];
                updateWeapon();
                Netcode.NetworkManager.SendPacketWeapon(0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                activeGun = slots[1];
                updateWeapon();
                Netcode.NetworkManager.SendPacketWeapon(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                activeGun = slots[2];
                updateWeapon();
                Netcode.NetworkManager.SendPacketWeapon(2);
            }
        }
	}

    public void NetworkingUpdate(int weapon)
    {
        if (activeGun != slots[weapon])
        {
            //Debug.Log(weapon);
            activeGun = slots[weapon];
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
		gunStats = activeGun.gunStats;
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
				GameObject flash = Instantiate(muzzleFlash,barrel.position, Quaternion.LookRotation(PV.transform.forward));
				flash.transform.SetParent(barrel);
				Destroy(impact,1f);
				Destroy(flash, 1f);

                Entity ET = hit.collider.GetComponentInParent<Entity>();
                if (ET != null)
                {
                    if (ET.type == EntityType.Player || ET.type == EntityType.Dummy)
                    {
                        Netcode.NetworkManager.SendPacketDamage(parentPlayer, ET.id, gunStats.dmg);
                    }
                }
			}
		}
	}
	void dryFire()
	{
	}
}
