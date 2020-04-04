using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terminal : MonoBehaviour
{
    public int gateNumber { get; private set; } = -1;
    public GameObject gate;
    public bool final;
    private bool launch = false;
    public bool opened { get; private set; } = false;
	// Start is called before the first frame update
	void Start()
	{
        gateNumber = Networking.NetworkManager.gates.Count;
        Networking.NetworkManager.gates.Add(this);
	}

	// Update is called once per frame
	void Update()
	{
        if (launch)
        {
            gate.GetComponent<Transform>().position = gate.GetComponent<Transform>().position + Vector3.up * 0.1f;
            Networking.NetworkManager.EndGame();
        }
	}
	public void openGate(GameObject gate)
	{
        if (!opened)
        {
            if (final)
            {
				launch = true;
				

            }
            else
            {
				Destroy(gate);

			}

            opened = true;
        }
	}
}
