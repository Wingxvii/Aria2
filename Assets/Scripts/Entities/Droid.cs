using System.Collections;
using System.Collections.Generic;
using System.Text;
using Networking;
using UnityEngine;
using UnityEngine.AI;


public enum DroidState {
    Standing = 0,
    Moving = 1,
    AttackMoving = 2,
    TetherAttacking = 3,
    TargetAttacking = 4,
    Dancing = 5,
    Death = 6,

}

public class Droid : Entity
{

    public float maxSpeed = 5.0f;
    public float minSpeed = 2.0f;
    private Rigidbody selfRigid;

    private Vector3 journeyPoint;
    private Entity attackPoint;
    public DroidState state = DroidState.Standing;

    public float journeyAccuracy = 2.0f;

    public int attackDamage = 5;
    public int strongAttack = 8;
    public float coolDown = 1.0f;
    public float lowCoolDown = 0.8f;
    public float currentCoolDown = 0.0f;

    public float visualRange = 20.0f;
    NavMeshAgent m_Agent;


    public Animator anim;
    public Transform mesh;
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

        anim = this.GetComponent<Animator>();
        m_Agent = this.GetComponent<NavMeshAgent>();
        maxHealth = 100;
        currentHealth = 100;

        //add upgrade
        if (ResourceManager.Instance.droidStronger)
        {
            IncreaseBuildingHealth();
        }
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
                    foreach (FPSPlayer.Player player in EntityManager.Instance.ActivePlayers())
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
                    foreach (FPSPlayer.Player player in EntityManager.Instance.ActivePlayers())
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

                    anim.SetFloat("Walk", 0);
                    anim.SetFloat("Turn", 0);

                    foreach (FPSPlayer.Player player in EntityManager.Instance.ActivePlayers())
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
                case DroidState.Death:

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
        if (attackee.type == EntityType.Player || attackee.type == EntityType.Dummy)
        {
            state = DroidState.TargetAttacking;
            attackPoint = attackee;
        }
    }

    private void MoveTo(Vector2 pos)
    {

        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        //This will fire when you get the error you're describing.
        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            NavMesh.FindClosestEdge(transform.position, out hit, NavMesh.AllAreas);

            agent.transform.position = hit.position;
            agent.enabled = false;
            agent.enabled = true;
        }

        m_Agent.destination = journeyPoint;

        /*
        faceingPoint = journeyPoint;

        Vector2 dir = new Vector2(pos.x - this.transform.position.x, pos.y - this.transform.position.z).normalized * maxSpeed;

        selfRigid.velocity = new Vector3(dir.x, selfRigid.velocity.y, dir.y);

        Vector3 targetDir = new Vector3(faceingPoint.x - transform.position.x, 0, faceingPoint.z - transform.position.z);
        */
        // Move our position a step closer to the target.



        anim.SetFloat("Walk", Mathf.Clamp(Vector3.Dot(m_Agent.velocity / m_Agent.speed, transform.forward), -1, 1));
        anim.SetFloat("Turn", Mathf.Clamp(Vector3.Dot(m_Agent.velocity / m_Agent.speed, transform.right), -1, 1));

    }
    private void OnAttack()
    {
        if (currentCoolDown <= 0.0f)
        {
            if (ResourceManager.Instance.droidStronger)
            {
                NetworkManager.SendPacketDamage(this.id, attackPoint.id, attackDamage, attackPoint.deaths, 1 << (attackPoint.id + 1));
                currentCoolDown = coolDown;
                anim.Play("Attack");
            }
            else {
                NetworkManager.SendPacketDamage(this.id, attackPoint.id, strongAttack, attackPoint.deaths, 1 << (attackPoint.id + 1));
                currentCoolDown = lowCoolDown;
                anim.Play("Attack");

            }
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (GameSceneController.Instance.type == PlayerType.RTS && other.tag == "Player" && (other.gameObject.GetComponent<Entity>().type == EntityType.Player || other.gameObject.GetComponent<Entity>().type == EntityType.Dummy))
        {
            if (state != DroidState.AttackMoving && state != DroidState.Moving && state != DroidState.TargetAttacking)
            {
                state = DroidState.TetherAttacking;
            }
            if (!(state == DroidState.TetherAttacking && attackPoint != other.gameObject.GetComponent<FPSPlayer.Player>()) && state != DroidState.TargetAttacking)
            {
                attackPoint = other.gameObject.GetComponent<FPSPlayer.Player>();
            }
            OnAttack();
        }
    }

    public override void IncreaseBuildingHealth() {
        currentHealth += 100;
        maxHealth += 100;
        maxSpeed += 2.0f;
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
        dataToSend.Append(transform.rotation.eulerAngles.x);
        dataToSend.Append(",");
        dataToSend.Append(transform.rotation.eulerAngles.y);
        dataToSend.Append(",");
        dataToSend.Append(transform.rotation.eulerAngles.z);
        dataToSend.Append(",");
    }
    public override void UpdateEntityStats(EntityData ed)
    {
        //Debug.Log("UPDATED STATS OF " + id);
        transform.position = ed.position;
        transform.rotation = Quaternion.Euler(ed.rotation);
    }

    public override void OnDeath(bool networkData)
    {
		if (networkData)
			++deaths;
		if (networkData)
			NetworkManager.SendPacketDeath(this.id, killerID);

        Debug.Log("IT DIED " + id);
		StartCoroutine(PlayDeath());
        state = DroidState.Death;
    }

    public void OnDance()
    {
        anim.Play("Dance");
    }

    IEnumerator PlayDeath()
    {
        anim.Play("Death");

        yield return new WaitForSeconds(3.2f);

        OnDeActivate();
    }


}
