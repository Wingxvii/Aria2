using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : Entity
{
    public ParticleSystem Wallhit;

    protected override void BaseStart()
    {
        Wallhit = GetComponentInChildren<ParticleSystem>();
        type = EntityType.Wall;

        currentHealth = 500;
        maxHealth = 500;
        base.BaseStart();
    }

    public void WallIsHit(Vector3 hitPoint)
    {
        Wallhit.transform.position = hitPoint;
        Wallhit.Play();
    }

    public override void IssueBuild()
    {
        ready = false;
        StartCoroutine(BuildCoroutine());
    }

    IEnumerator BuildCoroutine()
    {
        Animation anim = this.GetComponent<Animation>();

        //play build animation
        anim.Play();

        while (anim.isPlaying)
        {
            yield return 0;
        }
        ready = true;
    }

}
