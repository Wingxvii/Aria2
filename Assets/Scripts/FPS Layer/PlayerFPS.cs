using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    public float acceleration = 1000f;
    public float airborneAcceleration = 100f;
    public float maxSpeed = 40f;
    public float jumpPower = 2500f;
    public float maxWalkableAngle = 30f;
    public float friction = 500f;
    public float aerialDrag = 0f;

    public Quaternion groundAngle { get; set; } = Quaternion.identity;
    public float groundAngleFloat { get; set; } = 0f;
    public bool colliding { get; set; } = false;
}

public enum MinorPlayerState
{
    None,
    Reloading
}

public class PlayerFPS : Entity
{
    PivotFPS[] pivots;
    GunFPS[] guns;
    int selectedGun = 0;
    GunFPS mainGun;
    Transform spawnPoint;
    int spawnPointNum = -1;
    Collider[] colliders;
    Rigidbody rb;
    Camera mainCam;

    public int[] remainingAmmunition = new int[(int)GunType.TOTAL];


    public PlayerStats stats;
    MinorPlayerState mps = MinorPlayerState.None;

    public static LayerMask playerLayer { get; private set; }

    private void Awake()
    {
        BaseAwake();
    }

    protected override void BaseAwake()
    {
        base.BaseAwake();

        if(playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        rb = GetComponentInParent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        guns = GetComponentsInChildren<GunFPS>();
        pivots = GetComponentsInChildren<PivotFPS>();
        mainCam = GetComponentInChildren<Camera>();

        mainGun = guns[0];

        foreach (Collider c in colliders)
        {
            c.material.dynamicFriction = 0;
            c.material.staticFriction = 0;
            c.material.bounciness = 0;
            c.material.frictionCombine = PhysicMaterialCombine.Minimum;
            c.material.bounceCombine = PhysicMaterialCombine.Minimum;
        }

    }

    protected override void BaseStart()
    {
        base.BaseStart();

        for (int i = 1; i < guns.Length; ++i)
        {
            guns[i].gameObject.SetActive(false);
        }
    }

    public override void ResetValues()
    {
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            this.gameObject.transform.rotation = Quaternion.identity;
        }
        else
        {
            spawnPointNum = SpawnManager.Instance.freeSpawnPoints.Dequeue();
            spawnPoint = SpawnManager.Instance.FPSspawnpoints[spawnPointNum];
            transform.position = spawnPoint.position;
            this.gameObject.transform.rotation = Quaternion.identity;
        }

        this.currentHealth = maxHealth;
    }

    protected override void BaseFixedUpdate()
    {
        base.BaseFixedUpdate();

        if (type == EntityType.Player)
        {
            rb.velocity = MiscFuncsFPS.ApplyFriction(rb.velocity, in stats);

            if (FPSLayer.InputManager.Instance.jump && stats.groundAngleFloat <= stats.maxWalkableAngle)
            {
                //rb.AddForce(stats.groundAngle * Vector3.up * stats.jumpPower);
                Vector3 vel = Quaternion.Inverse(stats.groundAngle) * rb.velocity;
                vel.y = stats.jumpPower;
                rb.velocity = stats.groundAngle * vel;
                //Debug.Log("JOOMP");
            }

            //Debug.Log(FPSLayer.InputManager.Instance.move);
            rb.velocity = MiscFuncsFPS.ClampVelocity(
                rb.velocity, 
                (stats.colliding ? stats.acceleration : stats.airborneAcceleration) * (transform.rotation * FPSLayer.InputManager.Instance.move) * Time.fixedDeltaTime, 
                in stats
                );

            stats.groundAngle = Quaternion.FromToRotation(Vector3.up, Vector3.down);
            stats.groundAngleFloat = 180f;
            stats.colliding = false;
        }
        else if (type == EntityType.Dummy)
        {
            //Edit things via networking code here
        }

        //Debug.Log("FIXED");
    }

    protected override void BaseUpdate()
    {
        base.BaseUpdate();

        if (type == EntityType.Player) { 

            foreach (PivotFPS p in pivots)
            {
                p.RotateSelf(FPSLayer.InputManager.Instance.rotate);
            }

            switch(mps)
            {
                case MinorPlayerState.None:
                    if (FPSLayer.InputManager.Instance.swap != 0)
                    {
                        mainGun.gameObject.SetActive(false);
                        selectedGun = (FPSLayer.InputManager.Instance.swap + selectedGun) % guns.Length;
                        if (selectedGun < 0)
                            selectedGun += guns.Length;
                        mainGun = guns[selectedGun];
                        mainGun.gameObject.SetActive(true);
                    }

                    if (FPSLayer.InputManager.Instance.directSwap > 0 && FPSLayer.InputManager.Instance.directSwap <= guns.Length && FPSLayer.InputManager.Instance.directSwap - 1 != selectedGun)
                    {
                        mainGun.gameObject.SetActive(false);
                        selectedGun = FPSLayer.InputManager.Instance.directSwap - 1;
                        mainGun = guns[selectedGun];
                        mainGun.gameObject.SetActive(true);
                    }

                    if (FPSLayer.InputManager.Instance.shoot)
                        mainGun.Fire(mainCam.transform, this);

                    if (FPSLayer.InputManager.Instance.reload)
                    {
                        int ammoType = (int)mainGun.specs.gunType;
                        mps = MinorPlayerState.Reloading;
                        mainGun.Reload(remainingAmmunition[ammoType]);
                        remainingAmmunition[ammoType] = Mathf.Max(0, remainingAmmunition[ammoType] - mainGun.clip.ammo.maxBulletCount);
                    }

                    break;

                case MinorPlayerState.Reloading:
                    if (mainGun.specs.cooldown <= 0)
                        mps = MinorPlayerState.None;
                    break;
            }
            //Debug.Log("OOPDATE");
        }
        else if (type == EntityType.Dummy)
        {
            //Edit things via networking code here
        }
    }

    protected override void BaseLateUpdate()
    {
        base.BaseLateUpdate();

        if (type == EntityType.Player)
        {

        }
        else if (type == EntityType.Dummy)
        {
            //Edit things via networking code here
        }
    }

    public override void OnDeath()
    {
        ResetValues();
    }

    protected override void BaseOnDestory()
    {
        if (spawnPointNum >= 0)
            SpawnManager.Instance.freeSpawnPoints.Enqueue(spawnPointNum);
    }

    //Use this to network damage being dealt
    public void SendDamage(int damage, Entity receiver)
    {

    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("COLLISION");
        if (type == EntityType.Player)
        {
            if (collision.contacts.Length > 0)
                stats.colliding = true;
            foreach (ContactPoint cp in collision.contacts)
            {
                Vector3 trueNorm = cp.normal / cp.normal.magnitude;
                float dotProduct = Vector3.Dot(Vector3.up, trueNorm);
                if (dotProduct > Vector3.Dot(Vector3.up, stats.groundAngle * Vector3.up))
                {
                    stats.groundAngle = Quaternion.FromToRotation(Vector3.up, trueNorm);
                    stats.groundAngleFloat = Mathf.Acos(Vector3.Dot(Vector3.up, trueNorm));
                }
            }
        }
        //Debug.Log("STILL HERE");
    }
}
