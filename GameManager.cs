using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public int score;
    Text scoreUI;
    Text recUI;
    Text trashUI;
    Text healthUI;
    public GameObject[] globeUI;
    AudioSource noise;
    public AudioClip loseHealthSound;
    public bool gameOver = false;
    public int health;
    int lastHealth;
    playerMovement pmScript;

    void updateScore(int sc)
    {
        scoreUI.text = "Score: " + sc;
    }
    // Use this for initialization
    void Start()
    {
        scoreUI = GameObject.FindGameObjectWithTag("ScoreUI").GetComponent<Text>();
        trashUI = GameObject.FindGameObjectWithTag("TrashInventoryUI").GetComponent<Text>();
        healthUI = GameObject.FindGameObjectWithTag("HealthUI").GetComponent<Text>();
        pmScript = GameObject.FindGameObjectWithTag("Player").GetComponent<playerMovement>();
        lastHealth = health;
        noise = GetComponent<AudioSource>();
    }

    void Updatehealth()
    {
        for (int i = 0; i < 5; i++)
        {
            globeUI[i].GetComponent<Image>().enabled = false;
        }
        for (int i = 0; i < health; i++)
        {
            globeUI[i].GetComponent<Image>().enabled = true;
        }


    }

    // Update is called once per frame
    void Update()
    {
        updateScore(score);
        Updatehealth();
        if (health <= 0)
        {
            gameOver = true;
            
            if(!pmScript.ai_testing)
            {
                Time.timeScale = .001f;
                SceneManager.LoadScene("gameOver");
            }
            
            Debug.Log("Game Over!");
        }

        if (lastHealth != health)
        {
            lastHealth = health;
            noise.PlayOneShot(loseHealthSound, .5f);

        }


    }


}
