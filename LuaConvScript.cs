using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonSharp.Interpreter;
using System.Linq;

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
    pool poolGlobal = null;
    // values from game that can be used
    //gmScript.health
    //gmScript.gameOver
    //gmScript.score
    //pmScript.holdingRecycle
    //pmScript.holdingTrash

    // classes for Conv network
    class gene
    {
        public int into;
        // out is capitalized because out is keyword
        public int Out;
        public double weight;
        public bool enabled;
        public int innovation;

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
        public int maxneuron = 0;
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
        public int currentSpecies = 0;
        public int currentGenome = 0;
        public int currentFrame = 0;
        public double maxFitness = 0;
        public pool(int Outputs)
        {
            innovation = Outputs;
            species = new List<species>();
        }
    }
    class neuron
    {
        public List<gene> incoming;
        public double value;

        public neuron()
        {
            incoming = new List<gene>();
            value = 0.0;
        }
    }
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    void initStuff()
    {
        //Time.timeScale = 100f;
        pmScript = GameObject.FindGameObjectWithTag("Player").GetComponent<playerMovement>();
        gmScript = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        tsScript = GameObject.Find("trashLocation").GetComponent<trashSpawn>();
        // init params
        Outputs = ButtonNames.Count;
        InputSize = (BoxRadius * 2 + 1) * (BoxRadius * 2 + 1) + 3;
        Inputs = InputSize + 1;
        timeout = TimeoutConstant;
    }
    // Use this for initialization
    void Start()
    {
        initStuff();


    }
    // Update is called once per frame
    void Update()
    {
        if(pmScript == null)
        {
            initStuff();
            if (pmScript == null)
            {
                return;
            }
        }
        
        removeNull();
        getPositions();
        if(poolGlobal == null)
        {
            poolGlobal = initializePool();
        }
        

        species species = poolGlobal.species[poolGlobal.currentSpecies];
        if(poolGlobal.currentGenome >= species.genomes.Count)
        {
            int x = 0;
            //stop
        }
        genome genome = species.genomes[poolGlobal.currentGenome];

        if (poolGlobal.currentFrame % 5 == 0)
        {
            evaluateCurrent(poolGlobal);
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

        double timeoutBonus = poolGlobal.currentFrame / 4;
        if (timeout + timeoutBonus <= 0)
        {
            double fitness = rightmost - poolGlobal.currentFrame / 2;
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

            if (fitness > poolGlobal.maxFitness)
            {
                poolGlobal.maxFitness = fitness;
            }

            poolGlobal.currentSpecies = 0;
            poolGlobal.currentGenome = 0;
            while (fitnessAlreadyMeasured(poolGlobal))
            {
                nextGenome(poolGlobal);
            }
            initializeRun(poolGlobal);
        }

        double measured = 0;
        double total = 0;
        for (int i = 0; i < poolGlobal.species.Count; i++)
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

        poolGlobal.currentFrame += 1;

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

        int n = Random.Range(0, count);
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
                geneLocal.weight = Random.Range(0.0f, 1.0f) * 4f - 2f;
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

        var gene = candidates[Random.Range(0, candidates.Count)];
        gene.enabled = !gene.enabled;
    }
    void nodeMutate(genome g)
    {
        if (g.genes.Count == 0)
            return;

        g.maxneuron = g.maxneuron + 1;

        int rand = Random.Range(0, g.genes.Count);
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
        List<string> keys = new List<string>(g.mutationRates.Keys);
        foreach (string key in keys)
        {
            if (Mathf.RoundToInt(Random.Range(0, 2)) == 1)
            {
                g.mutationRates[key] = .95 * g.mutationRates[key];
            }
            else
            {
                g.mutationRates[key] = 1.05263 * g.mutationRates[key];
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
        if(x - pmScript.transform.position.x < 1.0f)
        {
            return 1;
        }
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
                //for (int i = 0; i < extSprites.Count; i++)
                //{
                //    double distx = Mathf.Abs(extSprites[i].position.x) - (marioX + (double)dx);
                //    double disty = Mathf.Abs(extSprites[i].position.y) - (marioY + (double)dy);
                //    if (distx <= 8 && disty <= 8)
                //    {
                //        inputs.Add(-1);
                //    }
                //}
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
        return new species();
    }
    gene newGene()
    {
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
        double maxFitness = 0f;
        int maxs, maxg;
        maxs = maxg = 0;
        for (int i = 0; i < poolGlobal.species.Count; i++)
        {
            for (int j = 0; j < poolGlobal.species[i].genomes.Count; j++)
            {
                if (poolGlobal.species[i].genomes[j].fitness > maxFitness)
                {
                    maxFitness = poolGlobal.species[i].genomes[j].fitness;
                    maxs = i;
                    maxg = j;
                }
            }

            poolGlobal.currentSpecies = maxs;
            poolGlobal.currentGenome = maxg;
            poolGlobal.maxFitness = maxFitness;

            initializeRun(poolGlobal); // This may not make sense, are we going to restart the game each run?

        }
    }

    void generateNetwork(genome gen)
    {
        Dictionary<int, neuron> network = new Dictionary<int, neuron>();
        for (int i = 0; i < Inputs; i++)
        {
            network[i] = newNeuron();
        }

        for (int i = 0; i < Outputs; i++)
        {
            network[MaxNodes + i] = newNeuron();
        }

        gen.genes.Sort((x, y) => x.Out.CompareTo(y.Out));

        for (int i = 0; i < gen.genes.Count; i++)
        {
            var gene = gen.genes[i];

            if (gene.enabled)
            {
                if (network[gene.Out] == null)
                {
                    network[gene.Out] = new neuron();
                }

                var neuron = network[gene.Out];
                neuron.incoming.Add(gene);

                if (network[gene.into] == null)
                {
                    network[gene.into] = new neuron();
                }

            }

            gen.network = network;
        }


        gen.network = network;
    }

    Dictionary<string, bool> evaluateNetwork(Dictionary<int, neuron> network, List<int> inputs)
    {
        inputs.Add(1);
        Debug.Log(inputs.Count);
        //if (inputs.Count != Inputs)
        //{
        //    Debug.Log("Incorrect number of neural network inputs");
        //    return null;
        //}

        // originally when to Inputs (Inputs is supposed to equal inputs.Count
        for (int i = 0; i < Inputs; i++)
        {
            network[i].value = inputs[i];
        }
        List<int> keys = new List<int>(network.Keys);
        foreach (int key in keys)
        {
            double sum = 0;

            for (int j = 0; j < network[key].incoming.Count; j++)
            {
                gene incoming = network[key].incoming[j];
                neuron other = network[incoming.into];
                sum = sum + incoming.weight * other.value;
            }

            if (network.Count > 0)
            {
                network[key].value = sigmoid(sum);
            }
        }

        Dictionary<string, bool> outputs = new Dictionary<string, bool>();

        for (int o = 0; o < Outputs; o++)
        {
            string button = ButtonNames[o];
            if (network[MaxNodes + o].value > 0)
            {
                outputs[button] = true;
            }
            else
            {
                outputs[button] = false;
            }
        }

        return outputs;
    }

    genome crossover(genome g1, genome g2)
    {
        if (g2.fitness > g1.fitness)
        {
            genome tempg = g1;
            g1 = g2;
            g2 = tempg;
        }

        genome child = newGenome();

        List<gene> innovations2 = new List<gene>();

        for (int i = 0; i < g2.genes.Count; i++)
        {
            gene gene = g2.genes[i];
            innovations2[gene.innovation] = gene;
        }

        for (int i = 0; i < g1.genes.Count; i++)
        {
            gene gene1 = g1.genes[i];
            gene gene2 = innovations2[gene1.innovation];
            if (gene2 != null && Random.Range(1, 3) == 1 && gene2.enabled)
            {
                child.genes.Add(copyGene(gene2));
            }
            else
            {
                child.genes.Add(copyGene(gene1));
            }
        }

        child.maxneuron = Mathf.Max(g1.maxneuron, g2.maxneuron);
        List<string> keys = new List<string>(g1.mutationRates.Keys);
        foreach (string key in keys)
        {
            child.mutationRates[key] = g1.mutationRates[key];
        }

        return child;
    }

    double weights(List<gene> genes1, List<gene> genes2)
    {
        List<gene> i2 = new List<gene>();

        for (int i = 0; i < genes2.Count; i++)
        {
            gene gene = genes2[i];
            i2[gene.innovation] = gene;
        }

        float sum = 0;
        float coincident = 0;

        for (int i = 0; i < genes1.Count; i++)
        {
            gene gene = genes1[i];
            if (i2[gene.innovation] != null)
            {
                gene gene2 = i2[gene.innovation];
                sum = sum + Mathf.Abs((float)gene.weight - (float)gene2.weight);
                coincident = coincident + 1;
            }
        }

        return sum / coincident;
    }

    bool sameSpecies(genome gen1, genome gen2)
    {
        double dd = DeltaDisjoint * disjoint(gen1.genes, gen2.genes);
        double dw = DeltaWeights * weights(gen1.genes, gen2.genes);
        return (dd + dw) < DeltaThreshold;
    }

    void calculateAverageFitness(species species)
    {
        double total = 0;

        for (int g = 0; g < species.genomes.Count; g++)
        {
            genome genome = species.genomes[g];
            total = total + genome.globalRank;
        }
    }

    double totalAverageFitness()
    {
        double total = 0;
        for (int i = 0; i < poolGlobal.species.Count; i++)
        {
            species species = poolGlobal.species[i];
            total = total + species.averageFitness;
        }

        return total;
    }
    // END Daniel's Work


    void rankGlobally()
    {
        List<genome> global = new List<genome>(); //This called for a local arraylist..
        for (int s = 0; s < poolGlobal.species.Count; s++)
        {
            species species = poolGlobal.species[s];
            for (int g = 0; g < species.genomes.Count; g++)
            {
                global.Add(species.genomes[g]);
            }
        }
        List<genome> sortedGlobal = global.OrderBy(o => o.fitness).ToList();
        for(int i = 0; i < sortedGlobal.Count;i++)
        {
            sortedGlobal[i].globalRank = i;
        }
    }
    void cullSpecies(bool cutToOne)
    {
        pool pool = poolGlobal;
        for (int s = 0; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];

            /* LUA
			global.sort(species.genomes, function (a,b)
				return (a.fitness > b.fitness)
				end)
			*/
            List<genome> sortedGenomes = sp.genomes.OrderBy(o => o.fitness).ToList();
            sp.genomes = sortedGenomes;
            double remaining = Mathf.Ceil(sp.genomes.Count / 2);
            if (cutToOne)
            {
                remaining = 1;
            }
            while (sp.genomes.Count > remaining)
            {
                sp.genomes.RemoveAt(sp.genomes.Count-1);
            }
        }
    }
    genome breedChild(species species)
    {
        genome child;
        if (Random.Range(0f, 1f) < CrossoverChance)
        {
            genome g1 = species.genomes[Random.Range(0, species.genomes.Count)];
            genome g2 = species.genomes[Random.Range(0, species.genomes.Count)];
            child = crossover(g1, g2);
        }
        else
        {
            genome g = species.genomes[Random.Range(0, species.genomes.Count)];
            child = copyGenome(g);
        }
        mutate(child);
        return child;
    }
    void removeStaleSpecies()
    {
        pool pool = poolGlobal;
        List<species> survived = new List<species>();
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
                survived.Add(sp);
            }
        }
        pool.species = survived;
    }
    void removeWeakSpecies()
    {
        pool pool = poolGlobal;

        List<species> survived = new List<species>();
        float sum = (float)totalAverageFitness();
        for (int s = 0; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];
            double breed = Mathf.Floor((float)sp.averageFitness / sum * Population);

            if (breed >= 1)
            {
                survived.Add(sp);
            }
        }
        pool.species = survived;
    }
    void addToSpecies(genome child)
    {
        pool pool = poolGlobal;
        bool foundSpecies = false;
        for (int s = 0; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];
            if (!foundSpecies && sameSpecies(child, sp.genomes[0]))
            {
                sp.genomes.Add(child);
                foundSpecies = true;
            }
        }
        if (!foundSpecies)
        {
            species childSpecies = newSpecies();
            childSpecies.genomes.Add(child);
            pool.species.Add(childSpecies);
        }
    }
    void newGeneration()
    {
        pool pool = poolGlobal;

        cullSpecies(false);
        rankGlobally();
        removeStaleSpecies();
        rankGlobally();
        for (int s = 0; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];
            calculateAverageFitness(sp);
        }
        removeWeakSpecies();
        double sum = totalAverageFitness();
        List<genome> children = new List<genome>();
        for (int s = 0; s < pool.species.Count; s++)
        {
            species sp = pool.species[s];
            double breed = Mathf.Floor((float)sp.averageFitness / (float)sum * Population) - 1;
            for (int i = 0; i < breed; i++)
            {
                children.Add(breedChild(sp));
            }
        }
        cullSpecies(true);
        while (children.Count + pool.species.Count < Population)
        {
            species sp = pool.species[Random.Range(0, pool.species.Count)];
            children.Add(breedChild(sp));
        }
        for (int c = 0; c < children.Count; c++)
        {
            genome child = children[c];
            addToSpecies(child);
        }
        pool.generation = pool.generation + 1;
    }
    void clearJoypad()
    {
        state = 0;
    }

    // End 8=Dustin's Work

    // BEING Scott's Work
    pool initializePool()
    {
        poolGlobal = new pool(Outputs);

        for (int i = 0; i < Population; i++)
        {
            genome basic = basicGenome();
            addToSpecies(basic);
        }

        initializeRun(poolGlobal);
        return poolGlobal;
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
        generateNetwork(genome);
        evaluateCurrent(pool);
    }

    void evaluateCurrent(pool pool)
    {
        species species = pool.species[pool.currentSpecies];
        genome genome = species.genomes[pool.currentGenome];

        List<int> inputs = getInputs();
        Dictionary<string, bool> controller = evaluateNetwork(genome.network, inputs);

        //if (controller != null && controller["Left"] && !controller["Right"])
        //{
        //    state = 1;
        //}
        //else if (controller != null && !controller["Left"] && controller["Right"])
        //{
        //    state = 2;
        //}
        //else
        //{
        //    state = 0;
        //}

        //Is there a joypad class? Where is this in our current stuff??
        //joypad.set(controller);

    }

    void nextGenome(pool pool)
    {
        pool.currentGenome = pool.currentGenome + 1;
        if (pool.currentGenome >= pool.species[pool.currentSpecies].genomes.Count)
        {
            pool.currentGenome = 0;
            pool.currentSpecies = pool.currentSpecies + 1;
            if (pool.currentSpecies > pool.species.Count)
            {
                newGeneration();
                pool.currentSpecies = 0;
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