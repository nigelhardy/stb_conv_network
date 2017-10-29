using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public struct infoStruct
{

    public string info;
    public bool shown;
    public bool foundInThisLevel;
    public infoStruct(string inf, bool sh, bool inLevel)
    {
        info = inf;
        shown = sh;
        foundInThisLevel = inLevel;
    }
    public void changeBool(bool inputBool)
    {
        shown = inputBool;
    }
    public void found(bool foundBool)
    {
        foundInThisLevel = foundBool;
    }
}
public class PopupManager : MonoBehaviour
{
    public Dictionary<string, infoStruct> factInfo = new Dictionary<string, infoStruct>();
    // Use this for initialization
    public string[] info;
    bool popupOn = false;
    public Image progressBar;
    public Text popupText;
    public GameObject popupBG;
    public trashSpawn tSpawnScript;
    float OGprogressBarWidth = 0;
    float progressBarWidth = 0;
	float progressBarFull = Screen.width - 20f;
    float progressBarHeight = 21;
    void Start()
    {
        for (int i = 0; i < info.Length - 1; i = i + 2)
        {
            factInfo.Add(info[i], new infoStruct(info[i + 1], false, false));
        }
        popupText.enabled = false;
        popupBG.SetActive(false);

    }
    IEnumerator LevelUp()
    {

        yield return new WaitForSecondsRealtime(2.0f);
        progressBar.rectTransform.sizeDelta = new Vector2(progressBarWidth, progressBarHeight);

    }
    // Update is called once per frame
    void Update()
    {

    }
    public void showPopup(string name)
    {
        infoStruct fact;

        if (factInfo.TryGetValue(name, out fact))
        {
            if (!factInfo[name].shown)
            {
                popupBG.SetActive(true);
                popupText.text = fact.info;
                popupText.enabled = true;
                Time.timeScale = .001f;
            }

            popupOn = true;
        }
    }
    public void depositItem(string name)
    {
        infoStruct fact;

        if (factInfo.TryGetValue(name, out fact))
        {
            if (!factInfo[name].shown)
            {
                factInfo[name] = new infoStruct(factInfo[name].info, true, true);
                updateProgressBar();
            }
            else if (!factInfo[name].foundInThisLevel)
            {
                factInfo[name] = new infoStruct(factInfo[name].info, true, true);
                updateProgressBar();
            }
        }
    }
    public void closePopup()
    {
        popupBG.SetActive(false);
        popupText.enabled = false;
        Time.timeScale = 1f;
        popupOn = false;
    }
    void updateProgressBar()
    {
        progressBarWidth += ((progressBarFull - OGprogressBarWidth) / (tSpawnScript.itemsUnlocked - 1));
        progressBar.rectTransform.sizeDelta = new Vector2(progressBarWidth, progressBarHeight);
        if (progressBarWidth >= progressBarFull)
        {
            resetProgressBar();
        }

    }
    void resetProgressBar()
    {

        SceneManager.LoadScene("winnerScreen");
        Debug.Log("WINNER");
        /*
        if (tSpawnScript.fallingItems.Length <= tSpawnScript.itemsUnlocked)
        {
            tSpawnScript.itemsUnlocked = tSpawnScript.fallingItems.Length;
        }
        
        List<string> names = new List<string>();
        foreach (KeyValuePair<string, infoStruct> ins in factInfo)
        {
            names.Add(ins.Key);
        }
        foreach (string s in names)
        {
            infoStruct tempStruct = new infoStruct(factInfo[s].info, factInfo[s].shown, false);
            factInfo[s] = tempStruct;
        }
        StartCoroutine(LevelUp());
        tSpawnScript.refreshItems(3);
        progressBar.rectTransform.sizeDelta = new Vector2(progressBarFull, progressBarHeight);
        progressBarWidth = OGprogressBarWidth;
        */
    }
}

