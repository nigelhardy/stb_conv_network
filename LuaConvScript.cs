using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonSharp.Interpreter;

public class LuaConvScript : MonoBehaviour {
    int state = 0;
    playerMovement pmScript;
    GameManager gmScript;
    trashSpawn tsScript;
    // Use this for initialization
    void Start () {
        pmScript = GameObject.FindGameObjectWithTag("Player").GetComponent<playerMovement>();
        gmScript = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        tsScript = GameObject.Find("trashLocation").GetComponent<trashSpawn>();
        Debug.Log(runLuaScript());
    }
    double runLuaScript()
    {
        // just an example
        string tempIn = "45";
        string script = @"
                -- defines a factorial function
		        function fact (n)
			        if (n == 0) then
				        return 1
			        else
				        return n*fact(n - 1)
			        end
		        end

		        return fact({0})";
        script = string.Format(script, tempIn);
        DynValue res = Script.RunString(script);
        return res.Number;
    }
    void restartLevel()
    {
        SceneManager.LoadScene("Main");
    }
    void stateToMovement(int state)
    {
        if (state == 1)
        {
            state = 1;
            pmScript.leftRight = -1;
        }
        else if (state == 2)
        {
            pmScript.leftRight = 1;
        }
        else
        {
            // state == 0
            pmScript.leftRight = 0;
        }
    }
	// Update is called once per frame
	void Update () {
        
        if(!pmScript.ai_testing || true)
        {
            // only for testings, otherwise state
            bool l = Input.GetButton("Fire1");
            bool r = Input.GetButton("Fire2");
            if (l)
            {
                state = 1;
            }
            else if (r)
            {
                state = 2;
            }
            else
            {
                state = 0;
            }
            stateToMovement(state);
        }
        // positions of gators (enemy)
        for (int i = 0; i < tsScript.gators.Count; i++)
        {
            if (tsScript.gators[i] == null)
            {
                tsScript.gators.RemoveAt(i);
            }
            else
            {

            }
        }
        // positions of trash
        for (int i = 0; i < tsScript.trash.Count; i++)
        {
            if(tsScript.trash[i] ==  null)
            {
                tsScript.trash.RemoveAt(i);
            }
            else
            {
                
            }
        }
        // positions of recycleables
        for(int i = 0; i < tsScript.recycleable.Count; i++)
        {
            if (tsScript.recycleable[i] == null)
            {
                tsScript.recycleable.RemoveAt(i);
            }
            else
            {

            }
        }
        for (int i = 0; i < tsScript.powerups.Count; i++)
        {
            if (tsScript.recycleable[i] == null)
            {
                tsScript.recycleable.RemoveAt(i);
            }
            else
            {

            }
        }
        //Debug.Log(gmScript.health);
        //Debug.Log(gmScript.gameOver);
        //Debug.Log(gmScript.score);
        if(gmScript.gameOver)
        {
            restartLevel();
        }

    }
}
