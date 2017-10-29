using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class playerMovement : MonoBehaviour
{
    // Use this for initialization
    Transform player;
    Rigidbody2D rb;
    GameManager gm;
    AudioSource audSource;
    public AudioClip blip;
    public AudioClip blipLower;
    public AudioClip lifeSound;
    bool ground = true;
    bool holdingTrash = false;
    bool holdingRecycle = false;
    string inventoryName = "Inventory";
    public float runSpeed = 20f;
    public float jumpHeight = 10f;
    public float blipVolume = 0f;
    PopupManager pm;
    Animator anim;

    GameObject currentItem;

	public float leftRight = 0;
    public bool ai_testing = false;

    void Start()
    {
        player = GetComponent<Transform>();
        rb = GetComponent<Rigidbody2D>();
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        audSource = GetComponent<AudioSource>();
        pm = GameObject.FindGameObjectWithTag("PopupManager").GetComponent<PopupManager>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(ai_testing)
        {

        }
        else
        {
            leftRight = Input.GetAxis("Horizontal");
        }
        
        transform.position = new Vector3(player.position.x + leftRight * Time.deltaTime * runSpeed, player.position.y, player.position.z);
        if (leftRight > 0.1)
        {
            anim.SetBool("Right", true);
            anim.SetBool("Left", false);
        }
        else if (leftRight < -0.1)
        {
            anim.SetBool("Left", true);
            anim.SetBool("Right", false);
        }
        else
        {
            anim.SetBool("Left", false);
            anim.SetBool("Right", false);
        }

    }
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.transform.tag == "Ground")
        {
            ground = true;
        }
        else if (col.transform.tag == "Trash" && !holdingTrash && !holdingRecycle)
        {
            inventoryName = col.transform.name.Substring(0, col.transform.name.Length - 7);
            if (!ai_testing)
            {
                pm.showPopup(inventoryName);
            }
            audSource.PlayOneShot(blip, blipVolume);
            Destroy(col.transform.gameObject);

            currentItem = col.gameObject;
            holdingTrash = true;

            GameObject test = GameObject.FindGameObjectWithTag("Inventory");
            Image img = test.GetComponent<Image>();
            img.enabled = true;
            img.sprite = currentItem.GetComponent<SpriteRenderer>().sprite;

        }
        else if (col.transform.tag == "Recycle" && !holdingTrash && !holdingRecycle)
        {
            inventoryName = col.transform.name.Substring(0, col.transform.name.Length - 7);
            if (!ai_testing)
            {
                pm.showPopup(inventoryName);
            }

            audSource.PlayOneShot(blip, blipVolume);
            Destroy(col.transform.gameObject);

            currentItem = col.gameObject;
            holdingRecycle = true;

            GameObject test = GameObject.FindGameObjectWithTag("Inventory");
            Image img = test.GetComponent<Image>();
            img.enabled = true;
            img.sprite = currentItem.GetComponent<SpriteRenderer>().sprite;
        }
        else if (col.transform.tag == "Life")
        {
            audSource.PlayOneShot(lifeSound, .17f);
        }
        else
        {
            Rigidbody2D rb = col.gameObject.GetComponent<Rigidbody2D>();
            rb.AddForce(new Vector2(100, 100));
        }
    }
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform.tag == "TrashBin")
        {
            if (holdingTrash)
            {
                currentItem = null;
                pm.depositItem(inventoryName);
                GameObject test = GameObject.FindGameObjectWithTag("Inventory");
                Image img = test.GetComponent<Image>();
                img.enabled = false;
                holdingTrash = false;
                gm.score++;
                audSource.PlayOneShot(blipLower, blipVolume);
            }
        }
        else if (col.transform.tag == "RecycleBin")
        {
            if (holdingRecycle)
            {
                currentItem = null;
                pm.depositItem(inventoryName);
                GameObject test = GameObject.FindGameObjectWithTag("Inventory");
                Image img = test.GetComponent<Image>();
                img.enabled = false;
                holdingRecycle = false;
                gm.score++;
                audSource.PlayOneShot(blipLower, blipVolume);

            }
        }
        
    }
}
