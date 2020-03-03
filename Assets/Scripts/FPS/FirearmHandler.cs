using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public enum AnimationState
{
	NONE,
	WALK,
	RUN,
	RELOAD,
	FIRE,
}

public class FirearmHandler : MonoBehaviour
{
	public AnimationState currState = AnimationState.NONE;

	public GunStats gunStats;
	//Scriptable Object required variables
	public Firearms activeGun;
	public Firearms[] slots;

	public Camera PV;
	public Transform DummyPV;
	public Transform ammoHUD;

	//Firearm Universal
	public GameObject bulletHole;
	public GameObject bulletImpact;
	public GameObject muzzleFlash;

	public Transform barrel;

	public Animation anim;
	public FPSPlayer.Player parentController;

	//Method usage
	private int remainingClip;
	private float currentAcc;
	public float timeToNextShot;

	int parentPlayer = -1;

	// Start is called before the first frame update
	void Start()
	{
		updateWeapon();
		anim = GetComponent<Animation>();
		parentController = GetComponentInParent<FPSPlayer.Player>();

		reload();

		if (GetComponentInParent<FPSPlayer.Player>())
		{
			parentPlayer = GetComponentInParent<FPSPlayer.Player>().id;

			Debug.Log(parentPlayer - 1);
			Networking.NetworkManager.firearms[parentPlayer - 1] = this;
		}
	}


	void Update()
	{
		if (PV != null)
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				reload();
				StartCoroutine(ReloadGun());
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
			else
			{
				//update to walk or run animations
				if ((currState == AnimationState.NONE || currState == AnimationState.WALK) && parentController.m_state == FPSPlayer.Player.PlayerState.RUNNING)
				{
					currState = AnimationState.RUN;
					anim.Play("run");
				}
				else if ((currState == AnimationState.NONE || currState == AnimationState.RUN) && parentController.m_state == FPSPlayer.Player.PlayerState.WALKING)
				{
					currState = AnimationState.WALK;
					anim.Play("move");
				}


				if (currState == AnimationState.RUN && parentController.m_state != FPSPlayer.Player.PlayerState.RUNNING)
				{
					Debug.Log("Stop Running");
					currState = AnimationState.NONE;
					anim.Stop();
				}
				if (currState == AnimationState.WALK && parentController.m_state != FPSPlayer.Player.PlayerState.WALKING)
				{
					Debug.Log("Stop Walking");
					currState = AnimationState.NONE;
					anim.Stop();
				}
				if (currState == AnimationState.NONE && parentController.m_state == FPSPlayer.Player.PlayerState.IDLE && Time.time <= timeToNextShot)
				{
					Debug.Log("Nothing");
					currState = AnimationState.NONE;
					anim.Stop();
				}
			}

			//check movement
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				activeGun = slots[0];
				updateWeapon();
				Networking.NetworkManager.SendPacketWeapon(0);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				activeGun = slots[1];
				updateWeapon();
				Networking.NetworkManager.SendPacketWeapon(1);
			}
			if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				activeGun = slots[2];
				updateWeapon();
				Networking.NetworkManager.SendPacketWeapon(2);
			}

			if (!anim.isPlaying) { currState = AnimationState.NONE; }

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
			float variance = (200 - currentAcc) * 0.001f;
			Vector3 projectileDirection = PV.transform.forward;
			projectileDirection.x += Random.Range(-variance, variance);
			projectileDirection.y += Random.Range(-variance, variance);
			projectileDirection.z += Random.Range(-variance, variance);

			currentAcc = currentAcc - gunStats.accReduc;

			if (currentAcc <= 0.0f)
				currentAcc = 0.0f;

			//GameObject flash = Instantiate(muzzleFlash, barrel.position, Quaternion.LookRotation(PV.transform.forward));
			Vector3 impactPoint = new Vector3(float.NaN, float.NaN, float.NaN);
			Vector3 impactNormal = Vector3.zero;

			Ray bulletTrajectory = new Ray(PV.transform.position, projectileDirection);
			RaycastHit hit;
			if (Physics.Raycast(bulletTrajectory, out hit))
			{
				Debug.DrawRay(PV.transform.position, projectileDirection * 10, Color.green, 10, false);
				//GameObject impact= Instantiate(bulletImpact, hit.point, Quaternion.LookRotation(hit.normal));
				impactPoint = hit.point;
				impactNormal = hit.normal;

				Entity ET = hit.collider.GetComponentInParent<Entity>();
				if (ET != null)
				{
					Debug.Log(ET.name);
					if (ET.type != EntityType.Player && ET.type != EntityType.Dummy)
					{
						Networking.NetworkManager.SendPacketDamage(parentPlayer, ET.id, gunStats.dmg, ET.life);
					}
				}
			}

			PlayShootEffect(impactPoint, impactNormal);

            Networking.NetworkManager.SendPacketFiring(parentPlayer, impactPoint, impactNormal);
		}
		StartCoroutine(FireGun());
	}

	public void PlayShootEffect(Vector3 impactPoint, Vector3 impactNormal)
	{
		GameObject flash = Instantiate(muzzleFlash, barrel.position, Quaternion.LookRotation(PV.transform.forward));
		Destroy(flash, 1f);
		flash.transform.SetParent(barrel);
		if (!float.IsNaN(impactPoint.x))
		{
			GameObject impact = Instantiate(bulletImpact, impactPoint, Quaternion.LookRotation(impactNormal));

			Destroy(impact, 1f);
		}

	}

	void dryFire()
	{
		if (currState != AnimationState.RELOAD)
		{
			StartCoroutine(FireGun());
		}
	}

	IEnumerator ReloadGun()
	{
		anim.Play("reload");
		currState = AnimationState.RELOAD;

		yield return new WaitForSeconds(anim.GetClip("reload").length);
		if (!anim.isPlaying) { currState = AnimationState.NONE; }

	}
	IEnumerator FireGun()
	{
		anim.Play("fire");
		currState = AnimationState.FIRE;

		yield return new WaitForSeconds(anim.GetClip("fire").length);
		if (!anim.isPlaying) { currState = AnimationState.NONE; }
	}

}
