﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using Netcode;
using UnityEngine;

public enum TurretState
{
    Idle,
    IdleShooting,
    PositionalShooting,
    TargetedShooting,
    Recoil,
    Reloading,
}

public class Turret : Entity
{
    public TurretState state = TurretState.Idle;
    public float visionRange = 30.0f;
    public float maxRange = 50.0f;
    private Entity attackPoint;
    public float shortestDist;
    public ParticleSystem muzzle;

    //stats
    public float reloadRate = 5.0f;
    public float recoilRate = 0.5f;
    public int attackAmno = 10;
    public int attackDamage = 5;

    public float reloadTimer = 0.0f;
    public int currentAmno = 10;

    //rotation
    public float rotateSpeed;
    public Vector3 faceingPoint = new Vector3(0, 0, 0);

    //hit ray
    private RaycastHit hit;
    private LayerMask turretLayerMask;

    //models
    public GameObject head;
    public GameObject body;

    //position update dirty flag
    public bool positionUpdated = false;

    public bool changedToIdle = false;

    public int fixedTimeStep;


    protected override void BaseStart()
    {
        fixedTimeStep = (int)(1f / Time.fixedDeltaTime);

        type = EntityType.Turret;

        if (!muzzle){muzzle = GetComponentInChildren<ParticleSystem>();}
        currentHealth = 500;
        maxHealth = 500;

        positionUpdated = false;
        changedToIdle = false;

        turretLayerMask = LayerMask.GetMask("Player");
        turretLayerMask += LayerMask.GetMask("Wall");


    }

    void TickUpdate()
    {
        if (positionUpdated)
        {
            //NetworkManager.AddDataToStack(id, head.transform.rotation.eulerAngles, (int)state);
            changedToIdle = true;
        }
        else if (changedToIdle)
        {
            //NetworkManager.AddDataToStack(id, head.transform.rotation.eulerAngles, (int)state);
            changedToIdle = false;
        }

        positionUpdated = false;


    }

    protected override void BaseFixedUpdate()
    {

        if (GameSceneController.Instance.type == PlayerType.FPS)
        {
        }
        else if (GameSceneController.Instance.type == PlayerType.RTS)
        {


            shortestDist = float.MaxValue;
            float dist = 0.0f;

            #region Fixed Tick
            //count down
            --fixedTimeStep;


            //tick is called 10 times per 50 updates
            if (fixedTimeStep % ResourceManager.ResourceConstants.FRAMETICK == 0)
            {
                TickUpdate();
            }

            //reset the clock
            if (fixedTimeStep <= 0)
            {
                //updates 50Hz
                fixedTimeStep = (int)(1f / Time.fixedDeltaTime);
            }
            #endregion

            switch (state)
            {
                case TurretState.Idle:
                    //search for shortest player
                    foreach (PlayerFPS player in EntityManager.Instance.ActivePlayers())
                    {
                        dist = Vector3.Distance(player.transform.position, this.transform.position);

                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            attackPoint = player;
                        }
                    }
                    if (shortestDist < maxRange)
                    {
                        state = TurretState.IdleShooting;
                    }
                    break;
                case TurretState.IdleShooting:
                    //search for shortest player
                    foreach (PlayerFPS player in EntityManager.Instance.ActivePlayers())
                    {
                        dist = Vector3.Distance(player.transform.position, this.transform.position);
                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            attackPoint = player;
                        }
                    }
                    if (shortestDist < maxRange)
                    {

                        //tell networking to send updated data
                        positionUpdated = true;

                        state = TurretState.IdleShooting;
                        faceingPoint = attackPoint.transform.position;
                        if (currentAmno > 0)
                        {
                            muzzle.Play();
                            if (HitPlayer())
                            {
                                NetworkManager.SendDamage(attackDamage, this.id, attackPoint.id);
                                //attackPoint.OnDamage(attackDamage, this);
                            }
                            currentAmno--;
                            state = TurretState.Recoil;
                            reloadTimer += recoilRate;

                        }
                        else
                        {
                            reloadTimer += reloadRate;
                            state = TurretState.Reloading;
                        }
                    }

                    break;
                case TurretState.TargetedShooting:

                    if (attackPoint)
                    {
                        //search for shortest player
                        dist = Vector3.Distance(attackPoint.transform.position, this.transform.position);
                        shortestDist = dist;
                    //look at
                    if (shortestDist < maxRange)
                    {
                        faceingPoint = attackPoint.transform.position;

                        //tell networking to send updated data
                        positionUpdated = true;

                        if (currentAmno > 0)
                        {
                            muzzle.Play();

                            if (HitPlayer())
                            {
                                    NetworkManager.SendDamage(attackDamage, this.id, attackPoint.id);
                                    //attackPoint.OnDamage(attackDamage, this);
                            }
                            currentAmno--;
                            state = TurretState.Recoil;
                            reloadTimer += recoilRate;

                        }
                        else
                        {
                            reloadTimer += reloadRate;
                            state = TurretState.Reloading;
                        }
                    }
                    else
                    {
                        state = TurretState.Idle;
                    }
                    }

                    break;
                case TurretState.Recoil:
                    //search for shortest player
                    foreach (PlayerFPS player in EntityManager.Instance.ActivePlayers())
                    {
                        dist = Vector3.Distance(player.transform.position, this.transform.position);
                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            attackPoint = player;
                        }
                    }

                    //look at
                    faceingPoint = attackPoint.transform.position;
                    //tell networking to send updated data
                    positionUpdated = true;

                    if (reloadTimer <= 0.0f)
                    {
                        ///BUG: If recoiling from TargettedShooting, will go to IdleShooting
                        state = TurretState.IdleShooting;
                    }
                    break;

                case TurretState.Reloading:
                    if (reloadTimer <= 0.0f)
                    {
                        currentAmno = 10;
                        state = TurretState.Idle;
                    }
                    break;
            }
        }
    }

    public override void CallAction(int action)
    {
        if (action == 1) { //reload
            Reload();
        }

    }
    protected override void BaseUpdate()
    {
        if (GameSceneController.Instance.type == PlayerType.FPS)
        {
        }
        else if (GameSceneController.Instance.type == PlayerType.RTS)
        {


            if (state != TurretState.Idle)
            {
                Vector3 targetDir = new Vector3(faceingPoint.x - head.transform.position.x, faceingPoint.y - head.transform.position.y, faceingPoint.z - head.transform.position.z);

                // The step size is equal to speed times frame time.
                float step = rotateSpeed * Time.deltaTime;

                Vector3 newDir = Vector3.RotateTowards(head.transform.forward, targetDir, step, 0.0f);

                // Move our position a step closer to the target.
                body.transform.rotation = Quaternion.LookRotation(new Vector3(newDir.x, 0, newDir.z).normalized);
                head.transform.rotation = Quaternion.LookRotation(newDir);

            }

            if (reloadTimer >= 0.0f)
            {
                reloadTimer -= Time.deltaTime;
            }
        }
    }
    public override void IssueAttack(Entity attackee)
    {
        if (state != TurretState.Reloading)
        {
            state = TurretState.TargetedShooting;
        }
        attackPoint = attackee;
    }
    public void Reload()
    {
        reloadTimer += reloadRate;
        state = TurretState.Reloading;
    }

    private bool HitPlayer()
    {
        if (Physics.Raycast(head.transform.position, head.transform.forward, out hit, maxRange, turretLayerMask))
        {
            Entity hitEntity = hit.transform.GetComponent<Entity>();

            if (hit.transform.gameObject.tag == "Entity" && hitEntity.type == EntityType.Wall)
            {
                Wall hitWall = (Wall)hitEntity;

                hitWall.WallIsHit(hit.point);
                hitWall.OnDamage(attackDamage, this);
                return false;
            }
            else if (hit.transform.gameObject.tag == "Entity" && hitEntity.type == EntityType.Player)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        return false;

    }


    public override void GetEntityString(ref StringBuilder dataToSend)
    {
        dataToSend.Append(id);
        dataToSend.Append(",");

        //send object positions
        dataToSend.Append(transform.position.x);
        dataToSend.Append(",");
        dataToSend.Append(transform.position.y);
        dataToSend.Append(",");
        dataToSend.Append(transform.position.z);
        dataToSend.Append(",");

        dataToSend.Append(head.transform.rotation.eulerAngles.x);
        dataToSend.Append(",");
        dataToSend.Append(head.transform.rotation.eulerAngles.y);
        dataToSend.Append(",");
        dataToSend.Append(head.transform.rotation.eulerAngles.z);
        dataToSend.Append(",");
    }

    public override void UpdateEntityStats(EntityData ed)
    {
        Vector3 localRot = head.transform.localRotation.eulerAngles;
        localRot.x = ed.rotation.x;
        head.transform.localRotation = Quaternion.Euler(localRot);
        localRot = body.transform.localRotation.eulerAngles;
        localRot.y = ed.rotation.y;
        body.transform.localRotation = Quaternion.Euler(localRot);

        Debug.Log("BEAUTIFUL! " + id);
    }
}
