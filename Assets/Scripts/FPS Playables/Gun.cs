using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public float cooldown = 1f;
    float remainingCooldown = 0f;
    GunVector[] shootingPoints;
    public AmmoClip ammo;

    private void Awake()
    {
        shootingPoints = GetComponentsInChildren<GunVector>();
        ammo = GetComponent<AmmoClip>();
    }

    public void Shoot(Collider[] ignore)
    {
        float DT = Time.deltaTime;
        while (remainingCooldown < DT && ammo.CurrentBulletCount > 0 && ammo.remainingReload <= 0f)
        {
            DT -= remainingCooldown;
            for (int i = 0; i < shootingPoints.Length; ++i)
            {
                Bullet b = Instantiate<Bullet>(ammo.ammunition);
                b.SetStats(shootingPoints[i], ignore);

                if (ammo.CurrentBulletCount > 0)
                    --ammo.CurrentBulletCount;
            }

            remainingCooldown = cooldown;
        }

        remainingCooldown -= DT;
    }
}
