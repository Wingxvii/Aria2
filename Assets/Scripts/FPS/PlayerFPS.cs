using System.Collections;
using System.Collections.Generic;
using Netcode;
using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    public enum PlayerState
    {
        Alive = (1 << 0),
        Shooting = (1 << 1),
        Jumping = (1 << 2),
    }

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
    public int state = (int)PlayerState.Alive;

    public bool disableManualControl = false;
}

public enum MinorPlayerState
{
    None,
    Reloading
}

public class PlayerFPS : Entity
{

    public PivotFPS[] pivots { get; private set; }
    GunFPS[] guns;
    int selectedGun = 0;
    GunFPS mainGun;
    Transform spawnPoint;
    int spawnPointNum = -1;
    Collider[] colliders;
    Rigidbody rb;
    public Camera mainCam { get; private set; }

    public int[] remainingAmmunition = new int[(int)GunType.TOTAL];


    public PlayerStats stats;
    MinorPlayerState mps = MinorPlayerState.None;

    public static LayerMask playerLayer { get; private set; }

    public FirearmHandler playerGun;

    private void Awake()
    {
        BaseAwake();
    }

    protected override void BaseAwake()
    {
        base.BaseAwake();

        ResetValues();

        if(playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        rb = GetComponentInParent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        guns = GetComponentsInChildren<GunFPS>();
        pivots = GetComponentsInChildren<PivotFPS>();
        mainCam = GetComponentInChildren<Camera>();

        //mainGun = guns[0];

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
                stats.state |= (int)PlayerStats.PlayerState.Jumping;
                //Debug.Log("JOOMP");
            }
            else
            {
                stats.state &= ~(int)PlayerStats.PlayerState.Jumping;
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

            Netcode.NetworkManager.SendPacketEntities();
        }
        else if (type == EntityType.Dummy)
        {
            //Edit things via networking code here

            if (!stats.disableManualControl)
            {
                if (GameSceneController.Instance.type == PlayerType.RTS && Input.GetKey(KeyCode.D) && ResourceManager.ResourceConstants.RTSPLAYERDEBUGMODE)
                {
                    rb.velocity += new Vector3(1 * stats.acceleration * Time.fixedDeltaTime / 3, 0, 0);
                }
                if (GameSceneController.Instance.type == PlayerType.RTS && Input.GetKey(KeyCode.A) && ResourceManager.ResourceConstants.RTSPLAYERDEBUGMODE)
                {
                    rb.velocity += new Vector3(1 * -stats.acceleration * Time.fixedDeltaTime / 3, 0, 0);
                }
                if (GameSceneController.Instance.type == PlayerType.RTS && Input.GetKey(KeyCode.W) && ResourceManager.ResourceConstants.RTSPLAYERDEBUGMODE)
                {
                    rb.velocity += new Vector3(0, 0, 1 * stats.acceleration * Time.fixedDeltaTime / 3);
                }
                if (GameSceneController.Instance.type == PlayerType.RTS && Input.GetKey(KeyCode.S) && ResourceManager.ResourceConstants.RTSPLAYERDEBUGMODE)
                {
                    rb.velocity += new Vector3(0, 0, 1 * -stats.acceleration * Time.fixedDeltaTime / 3);
                }

                rb.velocity = MiscFuncsFPS.ApplyFriction(rb.velocity, in stats);
            }
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
            //depricated
            //@db8525830a6f6aabdd0265844dbee4d7e64cb4f1 (Baseline Firearms Rework)
            /*
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

                        if (Netcode.NetworkManager.isConnected)
                            Netcode.NetworkManager.SendWeaponSwap(selectedGun);
                    }

                    if (FPSLayer.InputManager.Instance.directSwap > 0 && FPSLayer.InputManager.Instance.directSwap <= guns.Length && FPSLayer.InputManager.Instance.directSwap - 1 != selectedGun)
                    {
                        mainGun.gameObject.SetActive(false);
                        selectedGun = FPSLayer.InputManager.Instance.directSwap - 1;
                        mainGun = guns[selectedGun];
                        mainGun.gameObject.SetActive(true);

                        if (Netcode.NetworkManager.isConnected)
                            Netcode.NetworkManager.SendWeaponSwap(selectedGun);
                    }

                    //if (FPSLayer.InputManager.Instance.shoot)
                       // mainGun.Fire(mainCam.transform, this);

                    //if (FPSLayer.InputManager.Instance.reload)
                    //{
                    //    int ammoType = (int)mainGun.specs.gunType;
                   ///     mps = MinorPlayerState.Reloading;
                  //      mainGun.Reload(remainingAmmunition[ammoType]);
                  //      remainingAmmunition[ammoType] = Mathf.Max(0, remainingAmmunition[ammoType] - mainGun.clip.ammo.maxBulletCount);
                  //  }

                    break;

                case MinorPlayerState.Reloading:
                    if (mainGun.specs.cooldown <= 0)
                        mps = MinorPlayerState.None;
                    break;
            }
            */
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
        Debug.Log("U DEAD");
    }

    protected override void BaseOnDestory()
    {
        if (spawnPointNum >= 0)
            SpawnManager.Instance.freeSpawnPoints.Enqueue(spawnPointNum);
    }

    //Use this to network damage being dealt
    public void SendDamage(int damage, Entity receiver)
    {
        Netcode.NetworkManager.SendPacketDamage(this.id, receiver.id, damage, receiver.life);
        receiver.OnDamage(damage, this);
    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("COLLISION");
        if (type == EntityType.Player || (type == EntityType.Dummy && !stats.disableManualControl))
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

    public void SendUpdate(Vector3 pos, Vector3 rot, int state)
    {
        if (GameSceneController.Instance.type == PlayerType.FPS)
        {
            UniversalUpdate(pos, rot, state);

        }
        else if (GameSceneController.Instance.type == PlayerType.RTS)
        {

            UniversalUpdate(pos, rot, state);
        }

    }

    void UniversalUpdate(Vector3 pos, Vector3 rot, int state)
    {
        //Debug.Log(pos + " , " + rot + ", " + state);
        if (!stats.disableManualControl)
        {
            stats.disableManualControl = true;
            rb.useGravity = false;
        }
        //Debug.Log("UPDATED");
        rb.velocity = (pos - this.transform.position) / Time.fixedDeltaTime * 0.1f;
        transform.position = pos;
        //this.transform.rotation = Quaternion.Euler(new Vector3(0f, rot.y, 0f));
        foreach (PivotFPS p in pivots)
        {
            p.StrictRotate(rot);
        }
        stats.state = state;

        //playerGun.NetworkingUpdate(weapon);

        //if (weapon != selectedGun)
        //{
        //    mainGun.gameObject.SetActive(false);
        //    selectedGun = weapon;
        //    mainGun = guns[selectedGun];
        //    mainGun.gameObject.SetActive(true);
        //}

        //if ((state & (int)PlayerStats.PlayerState.Shooting) > 0)
        //{
        //    if (!mainGun.playing)
        //    {
        //        mainGun.StartPlaying();
        //    }
        //}
        //else
        //{
        //    if (mainGun.playing)
        //    {
        //        mainGun.StopPlaying();
        //    }
        //}

        //if ((state & (int)PlayerStats.PlayerState.Jumping) > 0)
        //{
        //    Vector3 vel = rb.velocity;
        //    vel.y = stats.jumpPower;
        //    rb.velocity = vel;
        //}
    }

    public override void OnDamage(float num, int id, int entityLife)
    {
        if (life == entityLife)
        {
            if (destructable)
            {
                currentHealth -= num;
            }
            if (currentHealth <= 0 && GameSceneController.Instance.type == PlayerType.FPS)
            {
                killerID = id;
                OnDeath();
            }
        }
    }

    public override void OnDamage(int num, Entity culprit)
    {
        Debug.Log("DAMAGE: " + num);
        if (destructable)
        {
            currentHealth -= num;
        }
        if (currentHealth <= 0 && GameSceneController.Instance.type == PlayerType.FPS)
        {
            OnDeath();
        }
    }

    public override void UpdateEntityStats(EntityData ed)
    {
        if (type == EntityType.Dummy)
            SendUpdate(ed.position, ed.rotation, ed.state);
    }
}
