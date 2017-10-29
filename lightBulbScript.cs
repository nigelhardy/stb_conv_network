using UnityEngine;
using System.Collections;

public class lightBulbScript : MonoBehaviour
{
    GameManager gm;
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.transform.tag == "Player")
        {
            gm.score += 5;
            Destroy(gameObject);
        }
    }
    // Use this for initialization
    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}