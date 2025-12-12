# Sistema de Evolução Genética Adversária (GA)

## 📋 Visão Geral
Este projeto implementa um Algoritmo Genético (AG) integrado num jogo estilo Metroidvania, concebido para adaptar dinamicamente a dificuldade e o comportamento dos inimigos com base no desempenho do jogador.

Ao contrário do nivelamento de dificuldade tradicional (que apenas aumenta a vida/dano linearmente), este sistema permite que cada espécie de inimigo "evolua" gerações sucessivas, otimizando os seus atributos (genes) para se tornarem desafios mais eficazes contra o estilo de jogo específico do jogador.

## 🧬 Arquitetura do Sistema
O sistema está dividido em quatro componentes fundamentais para garantir modularidade e separation of concerns:

- **O Cérebro (GlobalGeneticEvolver):** Gestor central singleton que controla as populações, executa a seleção natural, crossover e mutação.
- **O ADN (EnemyGenome):** Estrutura de dados que contém os genes normalizados (0.0 a 1.0) de cada indivíduo.
- **O Sensor (EnemyFitnessTracker):** Componente local em cada inimigo que recolhe dados em tempo real (dano causado, tempo de sobrevivência).
- **A Taxonomia (EnemySpecies):** Garante que a evolução ocorre dentro de linhas de espécies independentes (ex: Moscas não evoluem baseadas no sucesso de Golens).

## ⚙️ Implementação e Teoria

### 1. Representação Genética (O Genoma)
Optámos por utilizar valores normalizados (float 0-1) para os genes em vez de valores absolutos. Isto permite que o mesmo sistema genético seja aplicável a qualquer inimigo, independentemente das suas estatísticas base.

**Excerto de EnemyGenome.cs:**
```csharp
[Header("Combat Genes")]
[Range(0f, 1f)] public float healthGene = 0.5f;     // Escala a Vida Base
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

---

## Limitações e trabalho futuro (academic framing)

- Atribuição de aptidão por similaridade introduz ruído na ligação entre instância em cena e exemplar da população; uma referência por-ID permitiria créditos exactos.
- Sinais de telemetria são actualmente limitados (predominância de dano e tempo). Para favorecer genes comportamentais (movimento, agressividade, velocidade de ataque), é recomendada a recolha de métricas adicionais: tempo em alcance, contagem de ataques, hits por segundo, dano mitigado.
- O esquema de pesos na função de aptidão é heurístico; uma análise sensibilidade / grid search sobre `mutationRate`, `crossoverRate`, `eliteCount` e os pesos da função de aptidão é necessária para validar robustez.
- A persistência em ficheiro é útil para iteração, mas requer controlos experimentais (seed RNG, logs de configuração) para reprodutibilidade científica.

---

## Apêndice — quickstart e pseudocódigo

Quickstart mínimo:

1. Colocar `GenericTrainingArena` na cena.
2. Confirmar prefabs e componentes (`Enemy_Health`, `HealthSystem`, `EnemyFitnessTracker`).
3. Pressionar Play e abrir `GlobalGeneticDebugUI` (`G`).

Pseudocódigo (fluxo essencial):

```
spawn enemy -> GetGenome(instanceId)
enemy fights -> calls RegisterDamageDealt when hits
on enemy death -> RegisterKill(instanceId, survivalTime, killedByPlayer)
    fitness = CalculateFitness(damage, survival)
    fitness = ApplyAdaptiveFitnessAdjustment(fitness, genome)
    UpdatePopulationFitness(pop, genome, fitness)
    if killsSinceEvolution >= evolveTriggerCount: Evolve(pop)
```

---


