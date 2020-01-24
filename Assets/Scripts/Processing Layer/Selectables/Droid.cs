using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum DroidState {
    Standing = 0,
    Moving = 1,
    AttackMoving = 2,
    TetherAttacking = 3,
    TargetAttacking = 4,
    Dancing = 5,
}

public class Droid : Entity
{

    public float maxSpeed = 5.0f;
    public float minSpeed = 2.0f;
    private Rigidbody selfRigid;

    private Vector3 journeyPoint;
    private Entity attackPoint;
    public DroidState state = DroidState.Standing;

    public float journeyAccuracy = 5.0f;

    public int attackDamage = 5;
    public float coolDown = 1.0f;
    public float currentCoolDown = 0.0f;

    public float visualRange = 20.0f;

    //rotation
    public float rotateSpeed;
    public Vector3 faceingPoint = new Vector3(0, 0, 0);
    //network updates

    // Start is called before the first frame update
    protected override void BaseStart()
    {
        type = EntityType.Droid;

        //setup rigidbody
        if (!selfRigid){selfRigid = this.GetComponent<Rigidbody>();}

        maxHealth = 100;
        currentHealth = 100;
    }

    // Update is called once per frame
    protected override void BaseUpdate()
    {
        if (GameSceneController.Instance.type == PlayerType.FPS)
        {
        }
        else if (GameSceneController.Instance.type == PlayerType.RTS) {
            if (currentCoolDown > 0.0f)
            {
                currentCoolDown -= Time.deltaTime;
            }
            if (state != DroidState.Standing)
            {
                Vector3 targetDir = new Vector3(faceingPoint.x - transform.position.x, 0, faceingPoint.z - transform.position.z);

                // The step size is equal to speed times frame time.
                float step = rotateSpeed * Time.deltaTime;

                Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0f);

                // Move our position a step closer to the target.
                transform.rotation = Quaternion.LookRotation(newDir);
            }

        }
    }

    protected override void BaseFixedUpdate() {
        if (selfRigid.velocity.magnitude > maxSpeed)
        {
            selfRigid.velocity = selfRigid.velocity.normalized * maxSpeed;
        }


        if (GameSceneController.Instance.type == PlayerType.FPS)
        {

        }
        else if (GameSceneController.Instance.type == PlayerType.RTS) {
            float shortestDist;

            //AI STATE MACHINE
            switch (state)
            {
                case DroidState.Moving:
                    if (Vector3.Distance(this.transform.position, journeyPoint) < journeyAccuracy)
                    {
                        state = DroidState.Standing;
                    }
                    else
                    {

                        MoveTo(new Vector2(journeyPoint.x, journeyPoint.z));

                    }
                    break;
                case DroidState.TargetAttacking:
                    //check if gameobject is seeable
                    journeyPoint = attackPoint.transform.position;
                    MoveTo(new Vector2(journeyPoint.x, journeyPoint.z));
                    break;

                case DroidState.TetherAttacking:
                    shortestDist = float.MaxValue;

                    //check shortest in range for each player
                    foreach (PlayerFPS player in EntityManager.Instance.ActivePlayers())
                    {
                        float dist = Vector3.Distance(player.transform.position, this.transform.position);
                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            attackPoint = player;
                        }
                    }
                    if (shortestDist < visualRange)
                    {
                        state = DroidState.TetherAttacking;
                    }


                    //check if gameobject is seeable
                    journeyPoint = attackPoint.transform.position;
                    MoveTo(new Vector2(journeyPoint.x, journeyPoint.z));
                    break;
                case DroidState.AttackMoving:
                    //check if gameobject is seeable
                    MoveTo(new Vector2(journeyPoint.x, journeyPoint.z));
                    shortestDist = float.MaxValue;

                    //check shortest in range for each player
                    foreach (PlayerFPS player in EntityManager.Instance.ActivePlayers())
                    {
                        float dist = Vector3.Distance(player.transform.position, this.transform.position);
                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            attackPoint = player;
                        }
                    }
                    if (shortestDist < visualRange)
                    {
                        state = DroidState.TetherAttacking;
                    }

                    break;
                case DroidState.Standing:
                    shortestDist = float.MaxValue;

                    foreach (PlayerFPS player in EntityManager.Instance.ActivePlayers())
                    {
                        float dist = Vector3.Distance(player.transform.position, this.transform.position);
                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            attackPoint = player;
                        }
                    }
                    if (shortestDist < visualRange)
                    {
                        state = DroidState.TetherAttacking;
                    }


                    break;

            }
        }
    }

    public override void IssueLocation(Vector3 location)
    {
        state = DroidState.Moving;
        journeyPoint = location;
    }

    public override void IssueAttack(Vector3 location)
    {
        state = DroidState.AttackMoving;
        journeyPoint = location;
    }
    public override void IssueAttack(Entity attackee)
    {
        if (attackee.type == EntityType.Player)
        {
            state = DroidState.TargetAttacking;
            attackPoint = attackee;
        }
    }

    private void MoveTo(Vector2 pos)
    {
        faceingPoint = journeyPoint;

        Vector2 dir = new Vector2(pos.x - this.transform.position.x, pos.y - this.transform.position.z).normalized * maxSpeed;
        selfRigid.velocity = new Vector3(dir.x, selfRigid.velocity.y, dir.y);
    }
    private void OnAttack()
    {
        if (currentCoolDown <= 0.0f)
        {
            attackPoint.OnDamage(attackDamage, this);
            currentCoolDown = coolDown;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (GameSceneController.Instance.type == PlayerType.RTS && other.tag == "Entity" && other.gameObject.GetComponent<Entity>().type == EntityType.Player)
        {
            if (state != DroidState.AttackMoving && state != DroidState.Moving && state != DroidState.TargetAttacking)
            {
                state = DroidState.TetherAttacking;
            }
            if (!(state == DroidState.TetherAttacking && attackPoint != other.gameObject.GetComponent<PlayerFPS>()) && state != DroidState.TargetAttacking)
            {
                attackPoint = other.gameObject.GetComponent<PlayerFPS>();
            }
            OnAttack();
        }
    }


}
