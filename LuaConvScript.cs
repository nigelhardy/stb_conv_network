using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonSharp.Interpreter;

public class LuaConvScript : MonoBehaviour
{
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
    private double rightmost = 0;
    // values from game that can be used
    //gmScript.health
    //gmScript.gameOver
    //gmScript.score
    //pmScript.holdingRecycle
    //pmScript.holdingTrash

    // classes for Conv network
    class gene
    {
        public double into;
        // out is capitalized because out is keyword
        public double Out;
        public double weight;
        public bool enabled;
        public double innovation;

        public gene()
        {
            into = 0;
            Out = 0;
            weight = 0;
            enabled = true;
            innovation = 0;
        }
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
        public double topFitness;
        public double staleness;
        public List<genome> genomes;
        public double averageFitness;

        public species()
        {
            topFitness = 0;
            staleness = 0;
            genomes = new List<genome>();
            averageFitness = 0;
        }
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
        public List<int> incoming;
        public double value;

        public neuron()
        {
            incoming = new List<int>();
            value = 0.0;
        }
    }

    // Use this for initialization
    void Start()
    {
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

        pool pool = initializePool();

        species species = pool.species[pool.currentSpecies];
        genome genome = species.genomes[pool.currentGenome];

        if (pool.currentFrame % 5 == 0)
        {
            evaluateCurrent(pool);
        }

        //Don't know what to do with this
        //joypad.set(controller);

        getPositions();
        if (marioX > rightmost)
        {
            rightmost = marioX;
            timeout = TimeoutConstant;
        }

        timeout = timeout - 1;

        double timeoutBonus = pool.currentFrame / 4;
        if (timeout + timeoutBonus <= 0)
        {
            double fitness = rightmost - pool.currentFrame / 2;
            if (rightmost > 4816)
            {
                fitness += 1000;
            }
            if (rightmost > 3186)
            {
                fitness += 1000;
            }
            if (fitness == 0)
            {
                fitness = -1;
            }

            genome.fitness = fitness;

            if (fitness > pool.maxFitness)
            {
                pool.maxFitness = fitness;
            }

            pool.currentSpecies = 1;
            pool.currentGenome = 1;
            while (fitnessAlreadyMeasured(pool))
            {
                nextGenome(pool);
            }
            initializeRun(pool);
        }

        double measured = 0;
        double total = 0;
        for (int i = 0; i < pool.species.Count; i++)
        {
            for (int j = 0; j < species.genomes.Count; j++)
            {
                total += 1;
                if (genome.fitness != 0)
                {
                    measured += 1;
                }
            }
        }

        pool.currentFrame += 1;

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
            if (n == 0)
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
            if (Random.Range(0.0f, 1.0f) < PerturbChance)
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
        if (g.genes.Count == 0)
            return;

        g.maxneuron = g.maxneuron + 1;

        int rand = Random.Range(1, g.genes.Count);
        var gene = g.genes[rand];
        if (!gene.enabled)
            return;
        gene.enabled = false;

        gene gene1 = copyGene(gene);
        gene1.Out = g.maxneuron;
        gene1.weight = 1.0;
        gene1.innovation += 1;
        gene1.enabled = true;
        g.genes.Add(gene1);

        gene gene2 = copyGene(gene);
        gene2.into = g.maxneuron;
        gene2.innovation += 1;
        gene2.enabled = true;
        g.genes.Add(gene2);
    }
    void mutate(genome g)
    {
        Random random = new Random();
        foreach (KeyValuePair<string, double> entry in g.mutationRates)
        {
            // do something with entry.Value or entry.Key
            if (Mathf.RoundToInt(Random.Range(0, 2)) == 1)
            {
                g.mutationRates[entry.Key] = .95 * entry.Value;
            }
            else
            {
                g.mutationRates[entry.Key] = 1.05263 * entry.Value;
            }
        }
        if (Random.Range(0.0f, 1.0f) < g.mutationRates["connections"])
        {
            pointMutate(g);
        }
        double p = g.mutationRates["link"];
        while (p > 0)
        {
            if (Random.Range(0.0f, 1.0f) < p)
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
        genome bg = newGenome();
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
        for (int dy = -BoxRadius * 16; dy < BoxRadius * 16; dy = dy + 16)
        {
            for (int dx = -BoxRadius * 16; dx < BoxRadius * 16; dx = dx + 16)
            {
                inputs.Add(0);
                int tile = getTile(dx, dy);
                if (tile == 1 && marioY + dy < 0x1B0)
                {
                    inputs.Add(1);
                }
                for (int i = 0; i < sprites.Count; i++)
                {
                    double distx = Mathf.Abs(sprites[i].position.x) - (marioX + (double)dx);
                    double disty = Mathf.Abs(sprites[i].position.y) - (marioY + (double)dy);
                    if (distx <= 8 && disty <= 8)
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
        return new pool(Outputs);
    }
    /*
    * Functions that need to be implemented
    * */

    // Nigel's Work
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
    // END Nigel's work

    // Phucs Work
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
    // End Phucs work


    // Begin Daniel's Work
    void playTop()
    {
        // to do
    }
    void generateNetwork(genome gen)
    {
        // to do
        Dictionary<int, neuron> network = new Dictionary<int, neuron>();

        gen.network = network;
    }
    Dictionary<string, bool> evaluateNetwork(Dictionary<int, neuron> network, List<int> inputs)
    {
        // to do
        Dictionary<string, bool> outputs = new Dictionary<string, bool>();


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
    void calculateAverageFitness(species species)
    {
        // to do
    }
    double totalAverageFitness()
    {
        // to do
        return 0;
    }
    // END Daniel's Work


    // Being Dustin's Work
    void rankGlobally()
    {
        pool pool = initializePool();
        ArrayList global = new ArrayList(); //This called for a local arraylist..
        for (int i=0; i<pool.species.Count; i++)
        {
            species sp = pool.species[i];
            for (int g=0; g<sp.genomes.Count; g++)
            {
                global.Add(sp.genomes[g]);
            }
        }

        /* LUA
		 global.sort(global, function (a,b)
		return (a.fitness < b.fitness)
		end)
		*/

        //global.Sort();
        //return(a.fitness < b.fitness)
    }
    void cullSpecies(bool cutToOne)
    {
        pool pool = initializePool();
        for (int s = 0; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];

            /* LUA
			global.sort(species.genomes, function (a,b)
				return (a.fitness > b.fitness)
				end)
			*/

            double remaining = Mathf.Ceil(sp.genomes / 2);
            if (cutToOne)
            {
                remaining = 1;
            }
            while (sp.genomes > remaining)
            {
                global.Remove(sp.genomes);
            }
        }
    }
    species breedChild(species species)
    {
        ArrayList child = new ArrayList();
        if (Random.Range(0, 1) < CrossoverChance)
        {
            genome g1 = species.genomes[Random.Range(1, species.genomes)];
            genome g2 = species.genomes[Random.Range(1, species.genomes)];
            child = crossover(g1, g2);
        }
        else
        {
            genome g = species.genomes[Random.Range(1, species.genomes)];
            child = copyGenome(g);
        }
        mutate(child);
        return new species();
    }
    void removeStaleSpecies()
    {
        pool pool = initializePool();
        ArrayList survived = new ArrayList();
        for (int s = 0; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];

            /*
			 global.sort(species.genomes, function (a,b)
			return (a.fitness > b.fitness)
			end)
			*/
            if (sp.genomes[1].fitness > sp.topFitness)
            {
                sp.topFitness = sp.genomes[1].fitness;
                sp.staleness = 0;
            }
            else
            {
                sp.staleness = sp.staleness + 1;
            }
            if (sp.staleness < StaleSpecies || sp.topFitness >= pool.maxFitness)
            {
                global.Add(survived, sp);
            }
        }
        pool.species = survived;
    }
    void removeWeakSpecies()
    {
        pool pool = initializePool();

        ArrayList survived = new ArrayList();
        int sum = totalAverageFitness;
        for (int s = 0; s < pool.species; s++)
        {
            species sp = pool.species[s];
            double breed = Mathf.Floor(sp.averageFitness / sum * Population);

            if (breed >= 1)
            {
                global.Add(survived, sp);
            }
        }
        pool.species = survived;
    }
    void addToSpecies(species child)
    {
        pool pool = initializePool();
        bool foundSpecies = false;
        for (int s = 1; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];
            if (!foundSpecies && sameSpecies(child, sp.genomes[1]))
            {
                global.Add(sp.genomes, child);
                foundSpecies = true;
            }
        }
        if (!foundSpecies)
        {
            species childSpecies = newSpecies();
            global.Add(childSpecies.genomes, child);
            global.Add(pool.species, childSpecies);
        }
    }
    void newGeneration()
    {
        pool pool = initializePool();

        cullSpecies(false);
        rankGlobally();
        removeStaleSpecies();
        rankGlobally();
        for (int s = 1; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];
            calculateAverageFitness(sp);
        }
        removeWeakSpecies();
        double sum = totalAverageFitness();
        ArrayList children = new ArrayList();
        for (int s = 0; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];
            double breed = Mathf.Floor(sp.averageFitness / sum * Population) - 1;
            for (int i = 0; i < breed; i++)
            {
                global.Add(children, breedChild(sp));
            }
        }
        cullSpecies(true);
        while (children + pool.species < Population)
        {
            species sp = pool.species[Random.Range(1, pool.species)];
            global.Add(children, breedChild(sp));
        }
        for (int c = 1; c < children.Count; c++)
        {
            species child = children[c];
            addToSpecies(child);
        }
        pool.generation = pool.generation + 1;
    }
    void clearJoypad()
    {
        ArrayList controller = new ArrayList();
        for (int b = 1; b < ButtonNames; b++)
        {
            controller["P1 " + ButtonNames[b]] = false;
        }
        joypad.set(controller);
    }

    // End Dustin's Work

    // BEING Scott's Work
    pool initializePool()
    {
        pool pool = new pool(Outputs);

        for (int i = 0; i < Population; i++)
        {
            genome basic = basicGenome();
            addToSpecies(basic);
        }

        initializeRun(pool);
        return pool;
    }

    void initializeRun(pool pool)
    {
        // Still need?
        //savestate.load(Filename);
        rightmost = 0;
        pool.currentFrame = 0;
        timeout = TimeoutConstant;
        clearJoypad();

        species species = pool.species[pool.currentSpecies];
        genome genome = species.genomes[pool.currentGenome];
        evaluateCurrent(pool);
    }

    void evaluateCurrent(pool pool)
    {
        species species = pool.species[pool.currentSpecies];
        genome genome = species.genomes[pool.currentGenome];

        List<int> inputs = getInputs();
        Dictionary<string, bool> controller = evaluateNetwork(genome.network, inputs);

        if (controller["P1 Left"] && controller["P1 Right"])
        {
            controller["P1 Left"] = false;
            controller["P1 Right"] = false;
        }
        if (controller["P1 Up"] && controller["P1 Down"])
        {
            controller["P1 Up"] = false;
            controller["P1 Down"] = false;
        }

        //Is there a joypad class? Where is this in our current stuff??
        //joypad.set(controller);
    }

    void nextGenome(pool pool)
    {
        pool.currentGenome = pool.currentGenome + 1;
        if (pool.currentGenome > pool.species[pool.currentSpecies].genomes.Count)
        {
            pool.currentGenome = 1;
            pool.currentSpecies = pool.currentSpecies + 1;
            if (pool.currentSpecies > pool.species.Count)
            {
                newGeneration();
                pool.currentSpecies = 1;
            }
        }
    }

    bool fitnessAlreadyMeasured(pool pool)
    {
        species species = pool.species[pool.currentSpecies];
        genome genome = species.genomes[pool.currentGenome];

        return genome.fitness != 0;
    }

    //Might not need these last three functions
    //If we do then we need to find a replacement for 'forms' in lua
    void writeFile(string filename)
    {
        // to do

    }

    void savePool()
    {
        // to do
        string filename = "";
        writeFile(filename);
    }

    void loadPool()
    {
        // to do
        string filename = "";
        //loadFile(filename);
    }
    // End Scott's Work


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