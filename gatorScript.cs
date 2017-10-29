using UnityEngine;
using System.Collections;

public class gatorScript : MonoBehaviour {
	GameManager gm;

	// Use this for initialization
	void Start () {
		gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
	}
	
	// Update is called once per frame
	void Update () {

	}

	void OnCollisionEnter2D(Collision2D col)
	{
		if(col.transform.tag == "Player")
		{
			gm.health = 0;
		}
	}
}
