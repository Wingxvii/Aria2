using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
public class ReadyButton : MonoBehaviour
{
	public GameObject textField;
	public GameObject canvas;
	public GameObject canvas2;
	private bool isReady = true;
	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
	public void ready()
	{
		if (isReady)
		{
			canvas2.GetComponent<CanvasGroup>().enabled = false;
			canvas.GetComponent<CanvasGroup>().enabled = false;
			textField.GetComponent<TMP_Text>().text = "UNREADY";
			isReady = false;
		}
		else
		{
			canvas2.GetComponent<CanvasGroup>().enabled = true;
			canvas.GetComponent<CanvasGroup>().enabled = true;
			textField.GetComponent<TMP_Text>().text = "READY";
			isReady = true;
		}
	}
	
}
