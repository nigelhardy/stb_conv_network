using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class trashSpawn : MonoBehaviour {
	float fallType;
	public GameObject[] fallingItems;
	public float spawnTime = 2f;
    public int itemsUnlocked;
    public PopupManager pm;
    public GameObject battery;
    public GameObject gator;
    public GameObject lightPowerup;
    List<GameObject> itemDropsList = new List<GameObject>();
    // Use this for initialization
    List<int> trashIndices = new List<int>{ 0, 2, 3, 4, 12, 13, 15, 16, 17, 19 };
	playerMovement plr;

    public List<Transform> gators;
    public List<Transform> recycleable;
    public List<Transform> trash;
    public List<Transform> powerups;

	void Start () {
		// Begin the spawning of trash
		InvokeRepeating("Spawn", spawnTime, 2.0f);
        StartCoroutine(spawnGator(Random.Range(15f, 45f)));
        StartCoroutine(spawnBattery(Random.Range(10f, 35f)));
        StartCoroutine(spawnLight(Random.Range(8f, 20f)));
        itemsUnlocked = fallingItems.Length;
        refreshItems(3);

		GameObject player = GameObject.FindGameObjectWithTag ("Player");
		plr = player.GetComponent<playerMovement> ();
    }
    public void refreshItems(int numberOfEach)
    {
        itemDropsList.Clear();
        for (int i = 0; i < numberOfEach; i++)
        {
            for (int j = 0; j < itemsUnlocked; j++)
            {
                itemDropsList.Add(fallingItems[j]);
            }
        }
    }
    IEnumerator spawnGator(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        float rotAmount = (Random.Range(-1f, 1f) * 20f);
        GameObject init;
        float xSpawn = Random.Range(0f, 8f);
        Vector3 pos = new Vector3(transform.position.x + xSpawn, transform.position.y, 0);
        init = Instantiate(gator, pos, transform.rotation) as GameObject;
        gators.Add(init.GetComponent<Transform>());
        StartCoroutine(spawnGator(Random.Range(10f, 30f)));
    }
    IEnumerator spawnBattery(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        float rotAmount = (Random.Range(-1f, 1f) * 20f);
        GameObject init;
        float xSpawn = Random.Range(0f, 8f);
        Vector3 pos = new Vector3(transform.position.x + xSpawn, transform.position.y, 0);
        init = Instantiate(battery, pos, transform.rotation) as GameObject;
        powerups.Add(init.GetComponent<Transform>());
        StartCoroutine(spawnBattery(Random.Range(10f, 35f)));
    }
    IEnumerator spawnLight(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        float rotAmount = (Random.Range(-1f, 1f) * 20f);
        GameObject init;
        float xSpawn = Random.Range(0f, 8f);
        Vector3 pos = new Vector3(transform.position.x + xSpawn, transform.position.y, 0);
        init = Instantiate(lightPowerup, pos, transform.rotation) as GameObject;
        powerups.Add(init.GetComponent<Transform>());
        StartCoroutine(spawnLight(Random.Range(8f, 20f)));
    }
    // Update is called once per frame
	void Update () {
		if (Input.touchCount == 1) {
			Touch touch = Input.touches [0];
			if (touch.position.x < Screen.width / 2) {
				Left_Click ();
			} else if (touch.position.x > Screen.width / 2) {
				Right_Click ();
			}
		} else {
			plr.leftRight = Mathf.Lerp (plr.leftRight, 0f, 0.45f);
		}
	}

	void FixedUpdate() {
		
	}

	void Spawn() {
		float rotAmount = (Random.Range(-1f, 1f) * 20f);
        GameObject init;
		float xSpawn = Random.Range(0f, 8f);
		Vector3 pos = new Vector3 (transform.position.x + xSpawn, transform.position.y, 0);
        int tItem = Random.Range(0, itemDropsList.Count);
        init = Instantiate(itemDropsList[tItem], pos, transform.rotation) as GameObject;

        bool isTrash = trashIndices.IndexOf(tItem) != -1;
        if(isTrash)
        {
            trash.Add(init.GetComponent<Transform>());
        }
        else
        {
            recycleable.Add(init.GetComponent<Transform>());
        }
        itemDropsList.Remove(itemDropsList[tItem]);
        Rigidbody2D rb = init.GetComponent<Rigidbody2D>();
        rb.AddTorque(rotAmount);
    }

	public void Left_Click() {
		plr.leftRight = Mathf.Lerp (0f, -1.0f, 0.35f);
		Debug.Log ("Testing Left");
	}

	public void Right_Click() {
		plr.leftRight = Mathf.Lerp (0f, 1.0f, 0.35f);
		Debug.Log ("Testing Right");
	}
}
