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

    // values from game that can be used
    //gmScript.health
    //gmScript.gameOver
    //gmScript.score
    //pmScript.holdingRecycle
    //pmScript.holdingTrash

    // classes for Conv network
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
        public List<gene> genes;
        public double fitness = 0;
        public double adjustedFitness = 0;
        //network
        public double maxneuron = 0;
        public double globalRank = 0;
        public Dictionary<string, double> mutationRates = new Dictionary<string, double>();
        public Dictionary<int, neuron> network = new Dictionary<int, neuron>();
        public genome(double MutateConnectionsChance, double LinkMutationChance, double BiasMutationChance,
            double NodeMutationChance, double EnableMutationChance, double DisableMutationChance, double StepSize)
        {
            genes = new List<gene>();
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
        public List<genome> genomes;
        public double averageFitness = 0;
        
    }
    class pool
    {
        public List<species> species;
        public int generation = 0;
        public int innovation;
        public int currentSpecies = 1;
        public int currentGenome = 1;
        public int currentFrame = 0;
        public double maxFitness = 0;
        public pool(int Outputs)
        {
            innovation = Outputs;
        }
    }
    class neuron
    {
        public Dictionary<int, gene> incoming;
        public double value;
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
        for (int i = 0; i < g1.genes.Count; i++)
        {
            gen2.genes.Add(copyGene(g1.genes[i]));
        }
        return gen2;
    }
    int randomNeuron(genome g, bool nonInput)
    {
        // not done or working
        Dictionary<int, bool> neurons = new Dictionary<int, bool>();

        if (!nonInput)
        {
            for (int i = 0; i < Inputs; i++)
            {
                neurons[i] = true;
            }
        }

        for (int i = 0; i < Outputs; i++)
        {
            neurons[MaxNodes + i] = true;
        }

        for (int i = 0; i < g.genes.Count; i++)
        {
            if (!nonInput || (int)g.genes[i].into > Inputs)
            {
                neurons[(int)g.genes[i].into] = true;
            }
            if (!nonInput || (int)g.genes[i].Out > Inputs)
            {
                neurons[(int)g.genes[i].Out] = true;
            }
        }

        int count = 0;
        for (int i = 0; i < neurons.Count; i++)
        {
            count++;
        }

        int n = Random.Range(1, count);
        foreach (var item in neurons)
        {
            n = n - 1;
            if(n == 0)
            {
                return item.Key;
            }
        }
        return 0;
    }
    void pointMutate(genome g)
    {
        double step = g.mutationRates["step"];
        for (int i = 0; i < g.genes.Count; i++)
        {
            gene geneLocal = g.genes[i];
            if(Random.Range(0.0f, 1.0f) < PerturbChance)
            {
                geneLocal.weight = geneLocal.weight + Random.Range(0.0f, 1.0f) * step * 2 - step;
            }
            else
            {
                geneLocal.weight = Random.Range(0.0f, 1.0f) * 4 - 2;
            }
        }
    }
    void linkMutate(genome g, bool b)
    {
        var neuron1 = randomNeuron(g, false);
        var neuron2 = randomNeuron(g, true);

        var newLink = new gene();

        if (neuron1 <= Inputs && neuron2 <= Inputs)
        {
            return;
        }
        if (neuron2 <= Inputs)
        {
            var temp = neuron1;
            neuron1 = neuron2;
            neuron2 = temp;
        }

        newLink.into = neuron1;
        newLink.Out = neuron2;
        if (b)
            newLink.into = Inputs;

        if (containsLink(g.genes, newLink))
        {
            return;
        }

        newLink.innovation += 1;
        float t = Random.Range(0, 1);
        newLink.weight = (double)t;

    }
    void enableDisableMutate(genome g, bool b)
    {
        // to do
        List<gene> candidates = new List<gene>();

        for (int i = 0; i < g.genes.Count; i++)
        {
            if (g.genes[i].enabled != b)
                candidates.Add(g.genes[i]);
        }

        if (candidates.Count == 0)
            return;

        var gene = candidates[Random.Range(1, candidates.Count)];
        gene.enabled = !gene.enabled;
    }
    void nodeMutate(genome g)
    {
        // to do
    }
    void mutate(genome g)
    {
        Random random = new Random();
        foreach (KeyValuePair<string, double> entry in g.mutationRates)
        {
            // do something with entry.Value or entry.Key
            if(Mathf.RoundToInt(Random.Range(0,2)) == 1)
            {
                g.mutationRates[entry.Key] = .95 * entry.Value;
            }
            else
            {
                g.mutationRates[entry.Key] = 1.05263 * entry.Value;
            }
        }
        if(Random.Range(0.0f, 1.0f) < g.mutationRates["connections"])
        {
            pointMutate(g);
        }
        double p = g.mutationRates["link"];
        while (p > 0)
        {
            if(Random.Range(0.0f, 1.0f) < p)
            {
                linkMutate(g, false);
            }
            p -= 1;
        }

        p = g.mutationRates["bias"];
        while (p > 0)
        {
            if (Random.Range(0.0f, 1.0f) < p)
            {
                linkMutate(g, true);
            }
            p -= 1;
        }

        p = g.mutationRates["node"];
        while (p > 0)
        {
            if (Random.Range(0.0f, 1.0f) < p)
            {
                nodeMutate(g);
            }
            p -= 1;
        }

        p = g.mutationRates["enable"];
        while (p > 0)
        {
            if (Random.Range(0.0f, 1.0f) < p)
            {
                enableDisableMutate(g, true);
            }
            p -= 1;
        }

        p = g.mutationRates["disable"];
        while (p > 0)
        {
            if (Random.Range(0.0f, 1.0f) < p)
            {
                enableDisableMutate(g, false);
            }
            p -= 1;
        }


    }
    genome basicGenome()
    {
        // to do
        genome bg =  newGenome();
        // not sure how this innovation variable is useful??
        double innovation = 1;
        bg.maxneuron = Inputs;
        mutate(bg);
        return bg;
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
    bool containsLink(List<gene> genes, gene l)
    {
        for (int i = 0; i < genes.Count; i++)
        {
            var gene = genes[i];
            if (gene.into == l.into && gene.Out == l.Out)
            {
                return true;
            }
        }
        return false;
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
    pool newPool()
    {
        // to do
        return new pool(Outputs);
    }
    /*
    * Functions that need to be implemented
    * */

    int disjoint(List<gene> genes1, List<gene> genes2)
    {
        return 0;
    }
    double sigmoid(double x)
    {
        // to do
        return 0;
    }
    pool newInovation()
    {
        // to do
        return new pool(Outputs);
    }
    species newSpecies()
    {
        // to do
        return new species();
    }
    gene newGene()
    {
        // to do
        return new gene();
    }
    neuron newNeuron()
    {
        return new neuron();
    } 
    void generateNetwork(genome gen)
    {
        // to do
        Dictionary<int, neuron> network = new Dictionary<int, neuron>();

        gen.network = network;
    }
    Dictionary<int, bool> evaluateNetwork(Dictionary<int, neuron> network, int inputs)
    {
        // to do
        Dictionary<int, bool> outputs = new Dictionary<int, bool>();


        return outputs;
    }
    genome crossover(gene g1, gene g2)
    {
        // to do
        genome child = newGenome();

        return child;
    }
    double weights(List<gene> genes1, List<gene> genes2)
    {
        // to do
        return 0;
    }
    double sameSpecies(genome gen1, genome gen2)
    {
        // to do
        return 0;
    }
    void rankGlobally()
    {
        // to do
    }
    void calculateAverageFitness(species species)
    {
        // to do
    }
    double totalAverageFitness()
    {
        // to do
        return 0;
    }
    void cullSpecies(bool cutToOne)
    {
        // to do
    }
    species breedChild(species species)
    {
        // to do
        return new species();
    }
    void removeStaleSpecies()
    {
        // to do
    }
    void removeWeakSpecies()
    {
        // to do
    }
    void addToSpecies(species child)
    {
        // to do
    }
    void newGeneration()
    {
        // to do
    }
    void initializePool()
    {
        // to do
    }
    void clearJoypad()
    {
        // to do
    }
    void initializeRun()
    {
        // to do
    }
    void evaluateCurrent()
    {
        // to do
    }
    void nextGenome()
    {
        // to do
    }
    bool fitnessAlreadyMeasured()
    {
        // to do
        return false;
    }
    void writeFile()
    {
        // to do
    }
    void savePool()
    {
        // to do
    }
    void loadPool()
    {
        // to do
    }
    void playTop()
    {
        // to do
    }

    /*
     * End of Lua Functions
     * */



    // Functions not originally in Lua

    // example way to exec lua script
    // probably won't be needed since we are translating everything
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
    // states are input that allow for movement of player
    void stateToMovement(int state)
    {
        // moving left
        if (state == 1)
        {
            state = 1;
            pmScript.leftRight = -1;
        }
        else if (state == 2)
        {
            // moving right
            pmScript.leftRight = 1;
        }
        else
        {
            // staying still
            // state == 0
            pmScript.leftRight = 0;
        }
    }
    // removes all objects that are null
    // prevents getting positions of anything that has been destroyed
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

}
