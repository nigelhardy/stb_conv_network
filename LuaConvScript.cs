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

    // originally lua parameters
    public int BoxRadius = 6;
    public int InputSize;

    List<string> ButtonNames = new List<string> { "Left", "Right" };
    int Inputs; 
    int Outputs;

    public int Population = 300;
    public double DeltaDisjoint = 2.0;
    public double DeltaWeights = 0.4;
    public double DeltaThreshold = 1.0;

    public int StaleSpecies = 15;

    public double MutateConnectionsChance = 0.25;
    public double PerturbChance = 0.90;
    public double CrossoverChance = 0.75;
    public double LinkMutationChance = 2.0;
    public double NodeMutationChance = 0.50;
    public double BiasMutationChance = 0.40;
    public double StepSize = 0.1;
    public double DisableMutationChance = 0.4;
    public double EnableMutationChance = 0.2;

    public double TimeoutConstant = 20;
    private double timeout;

    public int MaxNodes = 1000000;

    private double marioX, marioY;
    //gmScript.health
    //gmScript.gameOver
    //gmScript.score
    //pmScript.holdingRecycle
    //pmScript.holdingTrash
    class gene
    {
        public double into = 0;
        // out is capitalized because out is keyword
        public double Out = 0;
        public double weight = 0;
        public bool enabled = true;
        public double innovation = 0;
    }
    class genome
    {
        public double fitness = 0;
        public double adjustedFitness = 0;
        //network
        public double maxneuron = 0;
        public double globalRank = 0;
        public Dictionary<string, double> mutationRates = new Dictionary<string, double>();
        public genome(double MutateConnectionsChance, double LinkMutationChance, double BiasMutationChance,
            double NodeMutationChance, double EnableMutationChance, double DisableMutationChance, double StepSize)
        {
            mutationRates["connections"] = MutateConnectionsChance;
            mutationRates["link"] = LinkMutationChance;
            mutationRates["bias"] = BiasMutationChance;
            mutationRates["node"] = NodeMutationChance;
            mutationRates["enable"] = EnableMutationChance;
            mutationRates["disable"] = DisableMutationChance;
            mutationRates["step"] = StepSize;
        }
        
    }
    class species
    {
        public double topFitness = 0;
        public double staleness = 0;
        public double averageFitness;
    }
    class pool
    {
        public double species;
    }
    // Use this for initialization
    void Start () {
        pmScript = GameObject.FindGameObjectWithTag("Player").GetComponent<playerMovement>();
        gmScript = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        tsScript = GameObject.Find("trashLocation").GetComponent<trashSpawn>();
        // init params
        Outputs = ButtonNames.Count;
        InputSize = (BoxRadius * 2 + 1) * (BoxRadius * 2 + 1);
        Inputs = InputSize + 1;
        timeout = TimeoutConstant;


    }
    // Update is called once per frame
    void Update()
    {
        removeNull();
        getPositions();
        



        if (!pmScript.ai_testing || true)
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


        
        if (gmScript.gameOver)
        {
            restartLevel();
        }

    }

    genome newGenome()
    {
        genome gen = new genome(MutateConnectionsChance, LinkMutationChance, BiasMutationChance,
            NodeMutationChance, EnableMutationChance, DisableMutationChance, StepSize);

        return gen;
    }
    genome copyGenome(genome g1)
    {
        genome gen2 = new genome(g1.mutationRates["connections"], g1.mutationRates["link"], g1.mutationRates["bias"],
            g1.mutationRates["node"], g1.mutationRates["enable"], g1.mutationRates["disable"], g1.mutationRates["connections"]);
        gen2.maxneuron = g1.maxneuron;

        return gen2;
    }
    genome basicGenome()
    {
        // to do
        return newGenome();
    }
    gene copyGene(gene g1)
    {
        gene g2 = new gene();
        g2.into = g1.into;
        g2.Out = g1.Out;
        g2.weight = g1.weight;
        g2.enabled = g1.enabled;
        g2.innovation = g1.innovation;
        return g2;
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
    void removeNull()
    {
        // positions of gators (enemy)
        for (int i = 0; i < tsScript.gators.Count; i++)
        {
            if (tsScript.gators[i] == null)
            {
                tsScript.gators.RemoveAt(i);
            }
        }
        // positions of trash
        for (int i = 0; i < tsScript.trash.Count; i++)
        {
            if (tsScript.trash[i] == null)
            {
                tsScript.trash.RemoveAt(i);
            }
        }
        // positions of recycleables
        for (int i = 0; i < tsScript.recycleable.Count; i++)
        {
            if (tsScript.recycleable[i] == null)
            {
                tsScript.recycleable.RemoveAt(i);
            }

        }
        // powerups
        for (int i = 0; i < tsScript.powerups.Count; i++)
        {
            if (tsScript.powerups[i] == null)
            {
                tsScript.powerups.RemoveAt(i);
            }
        }
    }


    void getPositions()
    {
        marioX = pmScript.player.position.x;
        marioY = pmScript.player.position.y;
    }
    int getTile(int dx, int dy)
    {
        int x = (int)Mathf.Floor((float)(marioX + dx + 8) / 16.0f);

        int y = (int)Mathf.Floor((float)(marioY + dy) / 16.0f);
        return 0;
    }
    // Could probably add sprites for powerups and 
    // put trash and recycling in different categories
    // but this will do for now
    List<Transform> getSprites()
    {
        List<Transform> sprites = new List<Transform>();
        for (int i = 0; i < tsScript.trash.Count; i++)
        {
            
        }
        // positions of recycleables
        for (int i = 0; i < tsScript.recycleable.Count; i++)
        {
            sprites.Add(tsScript.recycleable[i]);
        }
        return sprites;
    }
    List<Transform> getExtendedSprites()
    {
        List<Transform> sprites = new List<Transform>();
        // positions of recycleables
        for (int i = 0; i < tsScript.gators.Count; i++)
        {
            sprites.Add(tsScript.gators[i]);
        }
        return sprites;
    }

    List<int> getInputs()
    {
        getPositions();
        List<Transform> sprites = getSprites();
        List<Transform> extSprites = getExtendedSprites();

        //inputs = {}
        List<int> inputs = new List<int>();
        for (int dy = -BoxRadius*16; dy < BoxRadius*16; dy=dy+16)
        {
            for (int dx = -BoxRadius*16; dx < BoxRadius*16; dx = dx + 16)
            {
                inputs.Add(0);
                int tile = getTile(dx, dy);
                if(tile == 1 && marioY+dy < 0x1B0)
                {
                    inputs.Add(1);
                }
                for (int i = 0; i < sprites.Count; i++)
                {
                    double distx = Mathf.Abs(sprites[i].position.x) - (marioX + (double) dx);
                    double disty = Mathf.Abs(sprites[i].position.y) - (marioY + (double)dy);
                    if(distx <= 8 && disty <= 8)
                    {
                        inputs.Add(-1);
                    }
                }
                for (int i = 0; i < sprites.Count; i++)
                {
                    double distx = Mathf.Abs(extSprites[i].position.x) - (marioX + (double)dx);
                    double disty = Mathf.Abs(extSprites[i].position.y) - (marioY + (double)dy);
                    if (distx <= 8 && disty <= 8)
                    {
                        inputs.Add(-1);
                    }
                }
            }
        }
        return inputs;
    }


}
