using UnityEngine;
using System.Collections;

public class batteryScript : MonoBehaviour {
    GameManager gm;
    void OnCollisionEnter2D(Collision2D col)
    {
        if(col.transform.tag == "Player")
        {
            if(gm.health < 5)
            {
                gm.health++;
            }
            Destroy(gameObject);
        }
    }
	// Use this for initialization
	void Start () {
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
