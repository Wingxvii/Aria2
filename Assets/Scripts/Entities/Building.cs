using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : Entity
{

    public int healthUpgradeIncrease;

    public override void IssueBuild()
    {
        ready = false;
        StartCoroutine(BuildCoroutine());
    }

    public override void IncreaseBuildingHealth()
    {
        maxHealth += healthUpgradeIncrease;
        currentHealth += healthUpgradeIncrease;
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
