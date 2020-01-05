using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    int damage = 10;
    int distance = 200;
    bool shot = false;

    float maxLinger = 0.5f;
    float linger = 0f;

    List<int> previousLayers = new List<int>();

    Collider[] ignoreThese;

    public void SetStats(GunVector shooter, Collider[] ignore)
    {
        transform.position = shooter.transform.position;
        transform.rotation = shooter.transform.rotation;

        ignoreThese = ignore;

        RaycastHit[] rhit = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward, distance);

        Quaternion whereTo = Quaternion.identity;

        if (ignoreThese != null)
        {
            foreach (Collider c in ignoreThese)
            {
                previousLayers.Add(c.gameObject.layer);
                c.gameObject.layer = 2;
            }
        }

        if (rhit.Length > 0)
        {
            RaycastHit closest = rhit[0];

            for (int i = 1; i < rhit.Length; ++i)
            {
                if (closest.distance > rhit[i].distance)
                {
                    closest = rhit[i];
                }
            }

            whereTo = Quaternion.FromToRotation(transform.up, (closest.point - transform.position).normalized);
        }

        if (ignoreThese != null)
        {
            for (int i = 0; i < ignoreThese.Length; ++i)
            {
                ignoreThese[i].gameObject.layer = previousLayers[i];
            }
        }

        Quaternion newDir = whereTo * transform.rotation;
        transform.rotation = newDir;
    }

    private void FixedUpdate()
    {
        if (!shot)
        {
            shot = true;

            

            transform.position += transform.rotation * Vector3.forward;

            if (ignoreThese != null)
            {
                foreach(Collider c in ignoreThese)
                {
                    previousLayers.Add(c.gameObject.layer);
                    c.gameObject.layer = 2;
                }
            }

            float place = distance;
            RaycastHit[] rhit = Physics.RaycastAll(transform.position, 
                transform.rotation * Vector3.forward, place);

            if (rhit.Length > 0)
            {

                RaycastHit closest = rhit[0];

                for (int i = 1; i < rhit.Length; ++i)
                {
                    if (closest.distance > rhit[i].distance)
                    {
                        closest = rhit[i];
                    }
                }

                place = closest.distance;
            }

            if (ignoreThese != null)
            {
                for (int i = 0; i < ignoreThese.Length; ++i)
                {
                    ignoreThese[i].gameObject.layer = previousLayers[i];
                }
            }
        }

        linger += Time.fixedDeltaTime;

        if (linger >= maxLinger)
        {
            Destroy(gameObject);
        }
    }
}
