using UnityEngine;
using System.Collections;

public class plasticScript : MonoBehaviour {
    GameManager gm;
    
	// Use this for initialization
	void Start () {
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.transform.tag == "FallenTrash")
        {
            Destroy(gameObject);
            if(gm.health > 0)
            {
                gm.health-=2;
            }
          
        }
    }
}
