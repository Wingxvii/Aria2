using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terminal : MonoBehaviour
{
	public GameObject gate;
	public bool final;
	private bool launch = false;
	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if (launch)
			gate.GetComponent<Transform>().position = gate.GetComponent<Transform>().position + Vector3.up*0.1f;
	}
	public void openGate(GameObject gate)
	{
		if (!final)
			Destroy(gate);
		else
		{
			launch = true;
			
		}
	}
}
