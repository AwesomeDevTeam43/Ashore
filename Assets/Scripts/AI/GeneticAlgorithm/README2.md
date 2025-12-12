# Sistema de Evolução Genética Adversária (GA)

## Resumo
Este repositório contém uma implementação aplicada de um Algoritmo Genético (AG) integrado num protótipo de jogo Metroidvania. O objectivo é adaptar automaticamente atributos de inimigos (por espécie) em resposta ao desempenho do jogador, mantendo controlos de equilíbrio que previnem escalonamento indevido.

O documento descreve a arquitectura, as representações genéticas, a função de aptidão, os operadores evolutivos, mecanismos de controlo, e procedimentos experimentais recomendados.

---

## Arquitectura e componentes principais

- `GlobalGeneticEvolver` — singleton responsável por populações por espécie, selecção, crossover, mutação, evolução por gerações, persistência em ficheiro e mecânicas de dificuldade adaptativa.
- `EnemyGenome` — representação do genoma com genes normalizados em [0,1] e utilitários para conversão para valores de jogo (HP, dano, velocidade, resistências).
- `EnemyFitnessTracker` — componente por-instância que aplica o genoma ao inimigo, recolhe métricas de combate (dano, tempo de vida) e reporta ao evolutor.
- `GeneticDamageIntegration` — utilitário que liga eventos de dano do jogador ao sistema genético para atribuição precisa de crédito.
- UI de diagnóstico: `EnemyGenomeUI`, `GlobalGeneticDebugUI`, `GeneticDebugController`.

Referências de ficheiros: `Assets/Scripts/AI/GeneticAlgorithm/`.

---

## Métodos e design

### Representação do genoma
Genes são floats normalizados. Esta escolha facilita aplicação uniforme entre espécies com diferentes escalas de estatísticas base. As transformações para valores de jogo são determinísticas (ver secção "Mapeamento Gene → Valores").

Excerto (estrutura de genes):

```csharp
[Range(0f,1f)] float healthGene, damageGene, attackSpeedGene;
[Range(0f,1f)] float movementSpeedGene, aggressionRangeGene;
[Range(0f,1f)] float meleeResistanceGene, rangedResistanceGene, aggressivenessGene;
```

### Função de aptidão — definição e motivação
O sistema define a aptidão (fitness) de uma instância como uma combinação ponderada de métricas observáveis durante um encontro: dano causado e tempo de sobrevivência são os sinais base. O código actual calcula:

$$\text{fitness} = 10 \cdot D + 0.5 \cdot T + B( D, T )$$

onde:
- $D$ é o dano total causado pelo inimigo ao jogador;
- $T$ é o tempo de sobrevivência (seconds);
- $B(D,T)$ é um bónus para eliminações rápidas definido por código (ex.: $B = 2(10 - T)$ quando $D>0$ e $T<10$).

Racional: dano mede eficácia ofensiva directa; tempo mede sustentabilidade; o bónus rápido favorece ataques que capitalizam a velocidade de ataque.

Adicionalmente, existe um ajuste adaptativo que penaliza genomas agressivos se o jogador morrer frequentemente — isto implementa uma forma de regulação baseada no estado do jogador.

### Seleção e operadores evolucionários

- Seleção: torneio de tamanho `tournamentSize` com escolha por aptidão média.
- Elitismo: os `eliteCount` melhores genomas passam para a geração seguinte com portabilidade parcial de aptidão.
- Crossover: uniform crossover (cada gene tem 50% probabilidade de vir de um dos pais).
- Mutação: por-gene com probabilidade `mutationRate` e amplitude `mutationStrength`.

Parâmetros chave encontram-se editáveis como campos serializáveis em `GlobalGeneticEvolver`.

#### Implementação detalhada dos operadores genéticos

**Função de aptidão (CalculateFitness):**

```csharp
private float CalculateFitness(float damageDealt, float survivalTime)
{
    // Dano causado é o fator mais importante
    float fitness = (damageDealt * 10f);

    // Recompensa pela sobrevivência (encoraja comportamentos defensivos)
    fitness += survivalTime * 0.5f;

    // Bónus para eliminações rápidas (beneficia attackSpeedGene)
    if (damageDealt > 0 && survivalTime < 10f) 
    {
        fitness += (10f - survivalTime) * 2.0f; 
    }

    return fitness;
}
```

**Ciclo de evolução (Evolve):**

```csharp
private void Evolve(SpeciesPopulation pop)
{
    pop.generation++;
    pop.killsSinceEvolution = 0;
    
    List<EnemyGenome> newPopulation = new List<EnemyGenome>();
    
    // Elitismo: preserva os melhores
    var elites = pop.population
        .OrderByDescending(g => g.AverageFitness)
        .Take(eliteCount)
        .ToList();
    
    foreach (var elite in elites)
    {
        var clone = elite.Clone();
        clone.Fitness = elite.AverageFitness * 0.3f; // Carryover reduzido
        newPopulation.Add(clone);
    }
    
    // Preenchimento com crossover e mutação
    while (newPopulation.Count < populationSize)
    {
        EnemyGenome child;
        
        if (Random.value < crossoverRate && pop.population.Count >= 2)
        {
            var p1 = TournamentSelect(pop);
            var p2 = TournamentSelectExcluding(pop, p1);
            child = EnemyGenome.Crossover(p1, p2);
        }
        else
        {
            child = TournamentSelect(pop).Clone();
        }
        
        child.Mutate(mutationRate, mutationStrength);
        child.species = pop.species;
        child.ClampGenes(maxGeneValue);
        
        newPopulation.Add(child);
    }
    
    pop.population = newPopulation;
}
```

**Crossover uniforme (EnemyGenome.Crossover):**

```csharp
public static EnemyGenome Crossover(EnemyGenome parent1, EnemyGenome parent2)
{
    EnemyGenome child = new EnemyGenome { species = parent1.species };
    
    // Cada gene tem 50% de chance de vir de cada pai
    child.healthGene = Random.value > 0.5f ? parent1.healthGene : parent2.healthGene;
    child.damageGene = Random.value > 0.5f ? parent1.damageGene : parent2.damageGene;
    child.attackSpeedGene = Random.value > 0.5f ? parent1.attackSpeedGene : parent2.attackSpeedGene;
    child.movementSpeedGene = Random.value > 0.5f ? parent1.movementSpeedGene : parent2.movementSpeedGene;
    child.aggressionRangeGene = Random.value > 0.5f ? parent1.aggressionRangeGene : parent2.aggressionRangeGene;
    child.meleeResistanceGene = Random.value > 0.5f ? parent1.meleeResistanceGene : parent2.meleeResistanceGene;
    child.rangedResistanceGene = Random.value > 0.5f ? parent1.rangedResistanceGene : parent2.rangedResistanceGene;
    child.aggressivenessGene = Random.value > 0.5f ? parent1.aggressivenessGene : parent2.aggressivenessGene;
    
    return child;
}
```

**Mutação (EnemyGenome.Mutate):**

```csharp
public void Mutate(float mutationRate = 0.1f, float mutationStrength = 0.2f)
{
    if (Random.value < mutationRate)
        healthGene = MutateGene(healthGene, mutationStrength);
    
    if (Random.value < mutationRate)
        damageGene = MutateGene(damageGene, mutationStrength);
    
    if (Random.value < mutationRate)
        attackSpeedGene = MutateGene(attackSpeedGene, mutationStrength);
    
    if (Random.value < mutationRate)
        movementSpeedGene = MutateGene(movementSpeedGene, mutationStrength);
    
    if (Random.value < mutationRate)
        aggressionRangeGene = MutateGene(aggressionRangeGene, mutationStrength);
    
    if (Random.value < mutationRate)
        meleeResistanceGene = MutateGene(meleeResistanceGene, mutationStrength);
    
    if (Random.value < mutationRate)
        rangedResistanceGene = MutateGene(rangedResistanceGene, mutationStrength);
    
    if (Random.value < mutationRate)
        aggressivenessGene = MutateGene(aggressivenessGene, mutationStrength);
}

private float MutateGene(float gene, float strength)
{
    float mutation = Random.Range(-strength, strength);
    return Mathf.Clamp01(gene + mutation);
}
```

**Limitação de genes (EnemyGenome.ClampGenes):**

```csharp
public void ClampGenes(float maxValue)
{
    healthGene = Mathf.Min(healthGene, maxValue);
    damageGene = Mathf.Min(damageGene, maxValue);
    attackSpeedGene = Mathf.Min(attackSpeedGene, maxValue);
    movementSpeedGene = Mathf.Min(movementSpeedGene, maxValue);
    aggressionRangeGene = Mathf.Min(aggressionRangeGene, maxValue);
    meleeResistanceGene = Mathf.Min(meleeResistanceGene, maxValue);
    rangedResistanceGene = Mathf.Min(rangedResistanceGene, maxValue);
    aggressivenessGene = Mathf.Min(aggressivenessGene, maxValue);
}
```

### Atribuição de aptidão à população
Em vez de actualizar directamente o genoma que foi instanciado (que pode ter sido modificado pelos multiplicadores de geração), o sistema procura o exemplar mais semelhante dentro da população e acumula o fitness nesse exemplar. A similaridade é calculada pela soma das diferenças absolutas num subconjunto de genes.

Esta estratégia mantém resiliência a pequenas variações e permite acumular estatísticas na população persistente, ao custo de alguma imprecisão na atribuição directa.

---

## Mapeamento Gene → Valores de Jogo (implementação)

- `GetScaledHealth(baseHealth, minMult=0.5f, maxMult=2.0f)`
  - HP = round(baseHealth * lerp(minMult, maxMult, healthGene))
- `GetScaledDamage(baseDamage, minMult=0.5f, maxMult=2.0f)`
  - Dano = baseDamage * lerp(minMult, maxMult, damageGene)
- `GetScaledAttackInterval(baseInterval, minMult=0.5f, maxMult=1.5f)`
  - Intervalo = baseInterval * lerp(maxMult, minMult, attackSpeedGene) (gene alto → ataque mais rápido)
- `GetScaledMovementSpeed(baseSpeed, minMult=0.7f, maxMult=1.5f)`
- Resistências: `GetScaledMeleeResistance(maxResistance=0.5f)` retorna a fração mitigada de dano.

Exemplo numérico: `healthGene = 0.8`, `baseHealth = 100` → multiplicador ≈ 1.7 → HP ≈ 170.

---

## Instrumentação, diagnóstico e persistência

- UI por-inimigo: `EnemyGenomeUI` mostra genes e indicadores em tempo real.
- Painel global: `GlobalGeneticDebugUI` (toggle `G`) apresenta estatísticas por espécie e curva de evolução.
- Controles: `GeneticDebugController` (H, Ctrl+R, F5, Tab) para testes e respawn.
- Integração de dano: `GeneticDamageIntegration` liga eventos `HealthSystem` do jogador à contabilização de dano para garantir atribuição correcta.
- Persistência: o estado do evolutor é serializado em JSON para `Application.persistentDataPath` (campo `saveFileName`).

---

## Procedimento experimental recomendado

Configuração típica para observação rápida:

- `populationSize` = 20
- `evolveTriggerCount` = 5
- `rounds` (no `GenericTrainingArena`) = 200

Experimentos sugeridos:

1. Ablação de genes: fixe `damageGene` e observe se comportamento/movimento evolui.
2. Telemetria adicional: instrumentar `EnemyFitnessTracker` para registar tempo em alcance e número de ataques, de modo a aferir impacto de `movementSpeed` e `attackSpeed`.
3. Comparar com/sem `adaptiveDifficulty` para apreciar o efeito do regulador no equilíbrio.

