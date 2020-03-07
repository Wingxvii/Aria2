using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropOnTen : MonoBehaviour
{
    Animation anim;

    public int counter = 505; //buffer

    void Start()
    {
        anim = this.GetComponent<Animation>();


    }

    private void FixedUpdate()
    {
        if (counter > 0)
        {
            counter--;
        }
        if(counter == 0) {
            anim.Play("doordrop");
            counter--;
        }
    }
}
