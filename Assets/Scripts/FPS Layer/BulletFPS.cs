using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BulletStatsFPS
{
    public int damage = 40;
    public float distance = 50f;
    public float persistance = 0.3f;
}

public class BulletFPS : MonoBehaviour
{
    public BulletStatsFPS bulletStats;

    public virtual void Fire(PlayerFPS sender)
    {
        RaycastHit[] rch = Physics.RaycastAll(new Ray(transform.position, transform.rotation * Vector3.forward), bulletStats.distance, ~PlayerFPS.playerLayer);
        
        if (rch.Length > 0)
        {
            RaycastHit closest = rch[0];
        
            for (int i = 1; i < rch.Length; ++i)
            {
                if (closest.distance > rch[i].distance)
                    closest = rch[i];
            }
        
            transform.position = closest.point;
        
            Entity ent = closest.collider.GetComponentInParent<Entity>();
            if (ent != null)
                sender.SendDamage(bulletStats.damage, ent);
        }
        else
        {
            transform.position = transform.position + transform.rotation * Vector3.forward * bulletStats.distance;
        }

        StartCoroutine(Disappear());
    }

    IEnumerator Disappear()
    {
        yield return new WaitForSeconds(bulletStats.persistance);
        Destroy(gameObject);
    }
}
