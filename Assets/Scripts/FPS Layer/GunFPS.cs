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

    public ParticleSystem shotFlash;
    public bool playing = false;

    private void Awake()
    {
        clip = GetComponent<AmmoClipFPS>();
        bulletSpawns = GetComponentsInChildren<GunVectorFPS>();

        shotFlash = GetComponentInChildren<ParticleSystem>();
    }

    public void StartPlaying()
    {
        playing = true;
        shotFlash.Play();
    }

    public void StopPlaying()
    {
        playing = false;
        shotFlash.Stop();
    }

    public void Fire(Transform camTransform, PlayerFPS sender)
    {
        dtUpdated = true;

        specs.firePause += Time.deltaTime;

        while (clip.ammo.currentBullets > 0 && specs.firePause >= specs.fireRate)
        {
            if (!playing)
                StartPlaying();
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

            StartPlaying();

            foreach(GunVectorFPS gv in bulletSpawns)
            {
                BulletFPS bullet = Instantiate<BulletFPS>(clip.bullet);

                if (!rayHit)
                    bullet.transform.rotation = gv.transform.rotation * Quaternion.Euler(Vector3.forward * Random.Range(0, 360)) * Quaternion.Euler(Vector3.right * Random.Range(0, specs.accuracyAngle));
                else
                {
                    bullet.transform.rotation = Quaternion.FromToRotation(gv.transform.forward, distanceNormal) * gv.transform.rotation *
                        Quaternion.Euler(Vector3.forward * Random.Range(0, 360)) * Quaternion.Euler(Vector3.right * Random.Range(0, specs.accuracyAngle));
                }

                bullet.transform.position = gv.transform.position;

                if (clip.ammo.currentBullets > 0)
                    --clip.ammo.currentBullets;

                bullet.Fire(sender);
            }
        }
    }

    private void OnEnable()
    {
        if (playing)
            StopPlaying();
    }

    

    public void Reload(int maxAvailable)
    {
        specs.cooldown = specs.reloadSpeed;
        clip.ammo.currentBullets = Mathf.Min(maxAvailable, clip.ammo.maxBulletCount);
    }

    private void LateUpdate()
    {
        if (playing)
            StopPlaying();
        if (!dtUpdated)
        {
            specs.firePause = Mathf.Min(specs.firePause + Time.deltaTime, specs.fireRate);
        }
        specs.cooldown = Mathf.Max(specs.cooldown - Time.deltaTime, 0f);
        dtUpdated = false;
    }
}
