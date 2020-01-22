using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GunStatsFPS
{
    public float fireRate = 0.5f;
    public float firePause { get; set; } = 0f;
    public float accuracyAngle = 5f;
    public float reloadSpeed = 2f;
    public float cooldown { get; set; } = 0f;
    public bool autofire = true;
    public GunType gunType = GunType.PISTOL;
    public bool snapToCameraTarget = true;
}

public enum GunType
{
    PISTOL,
    RIFLE,
    RAILGUN,

    TOTAL
}

public class GunFPS : MonoBehaviour
{
    GunVectorFPS[] bulletSpawns;
    public AmmoClipFPS clip { get; private set; }
    public GunStatsFPS specs;
    bool dtUpdated = false;
    Vector3 cameraSight;

    private void Awake()
    {
        clip = GetComponent<AmmoClipFPS>();
        bulletSpawns = GetComponentsInChildren<GunVectorFPS>();
    }

    public void Fire(Transform camTransform, PlayerFPS sender)
    {
        dtUpdated = true;

        specs.firePause += Time.deltaTime;

        while (clip.ammo.currentBullets > 0 && specs.firePause >= specs.fireRate)
        {
            specs.firePause -= specs.fireRate;

            bool rayHit = false;
            Vector3 distanceNormal = Vector3.forward;
            RaycastHit closest;
            if (specs.snapToCameraTarget)
            {
                RaycastHit[] rch = Physics.RaycastAll(new Ray(camTransform.position, camTransform.rotation * Vector3.forward), float.PositiveInfinity, ~PlayerFPS.playerLayer);

                if (rch.Length > 0)
                {
                    rayHit = true;
                    closest = rch[0];

                    for (int i = 1; i < rch.Length; ++i)
                    {
                        if (rch[i].distance < closest.distance)
                            closest = rch[i];
                    }

                    distanceNormal = (closest.point - camTransform.position).normalized;
                }
            }

            foreach(GunVectorFPS gv in bulletSpawns)
            {
                BulletFPS bullet = Instantiate<BulletFPS>(clip.bullet);

                if (!rayHit)
                    bullet.transform.rotation = Quaternion.Euler(new Vector3(specs.accuracyAngle, 0, 360)) * gv.transform.rotation;
                else
                {
                    bullet.transform.rotation = Quaternion.Euler(new Vector3(specs.accuracyAngle, 0, 360)) *
                        Quaternion.FromToRotation(gv.transform.forward, distanceNormal) * gv.transform.rotation;
                }

                bullet.transform.position = gv.transform.position;

                if (clip.ammo.currentBullets > 0)
                    --clip.ammo.currentBullets;

                bullet.Fire(sender);
            }
        }
    }

    public void Reload(int maxAvailable)
    {
        specs.cooldown = specs.reloadSpeed;
        clip.ammo.currentBullets = Mathf.Max(maxAvailable, clip.ammo.maxBulletCount);
    }

    private void LateUpdate()
    {
        if (!dtUpdated)
            specs.firePause = Mathf.Min(specs.firePause + Time.deltaTime, specs.fireRate);
        specs.cooldown = Mathf.Min(specs.cooldown - Time.deltaTime, 0f);
        dtUpdated = false;
    }
}
