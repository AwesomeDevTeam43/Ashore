# Ashore — Documentação da Implementação de Algoritmos de Inteligencia Artificial

## Índice

- [1) Pathfinding](#1-pathfinding)
- [2) State Machine (BeeEnemy)](#2-state-machine-beeenemy)
- [3) Evolução Genética Adversária (GA)](#3-ga)

---

<a id="1-pathfinding"></a>

## 1) Pathfinding

### Pathfinding — Processo (Grelhas, A*, Movimento)

Este documento descreve o pathfinding tal como está implementado no **Ashore (Unity 2D)**, com foco nos scripts `NavGrid2D`, `GridPathfinder2D`, `NavAgent2D` e no inimigo `BeeEnemy`.

Fluxo real, em runtime:

1. **Escolha de grelha**: usa `NavGrid2D.Instance` (baked) quando existe; caso contrário usa `GridPathfinder2D` (local/dinâmico).
2. **Planeamento**: `FindPath(start, target)` corre A* e devolve uma lista de `Vector2` (waypoints).
3. **Execução**: o `BeeEnemy` segue os waypoints e transforma-os em `desiredVelocity`, com validação local (`CircleCast`/`OverlapCircle`) e recuperação quando fica bloqueado.

---

### 1) Grelhas (grids): como se representa o mundo para o pathfinding

O A* (e algoritmos semelhantes) precisam de um grafo. A grelha é uma forma prática de construir esse grafo a partir do mundo:

No teu projecto existem **dois modos de grelha**:

- **NavGrid2D (baked)**: uma grelha de nível que pode ser “baked” (`Bake()`), e depois usada em tempo real.
- **GridPathfinder2D (local/dinâmico)**: constrói uma grelha temporária à volta de um centro (por omissão, o ponto médio entre start e target) e pode usar cache por “chunks” com TTL.


- **Resolução (nodeRadius/nodeDiameter)**: no teu projecto, `nodeRadius` define o “passo” da grelha e afecta o compromisso custo vs precisão (mais nós = A* mais pesado; menos nós = trajectos mais aproximados).

- **Transitável vs não transitável**: cada célula é marcada com base em testes de colisão (se ali cabe o agente sem intersectar obstáculos).

Exemplo (marcar uma célula como transitável, já a considerar folga/clearance):

```csharp
Vector2 worldPoint = origin + new Vector2(x * nodeDiameter + nodeRadius, y * nodeDiameter + nodeRadius);
float r = nodeRadius + clearance;
bool walkable = !Physics2D.OverlapCircle(worldPoint, r, obstacleMask);
grid[x, y] = new Node(walkable, worldPoint, x, y);
```

- **Folga (clearance)**: o teu código aumenta o raio do teste de colisão (ou ajusta `clearance` na grelha) com base no tamanho do agente, para evitar trajectos apertados.

Exemplo (no `NavAgent2D`, aplicar folga com base no raio do agente ao pedir caminho numa `NavGrid2D`):

```csharp
float originalClearance = targetGrid.clearance;
try
{
	targetGrid.clearance = Mathf.Max(targetGrid.clearance, agentRadius);
	lastPath = targetGrid.FindPath(start, target);
}
finally
{
	targetGrid.clearance = originalClearance;
}
```


- **Vizinhança (8 direcções + diagonais)**: o teu `GetNeighbours(...)` considera diagonais e bloqueia “corner-cutting” verificando as duas células ortogonais.

Exemplo (proibir diagonais que “cortam cantos”):

```csharp
if (dx != 0 && dy != 0)
{
	var n1 = grid[node.x + dx, node.y];
	var n2 = grid[node.x, node.y + dy];
	if (!n1.walkable || !n2.walkable) continue;
}
```

- **Grelha baked vs grelha dinâmica (no teu código)**:
	- *Baked* = `NavGrid2D` (pré-calculada via `Bake()`, usada com `NavGrid2D.Instance.FindPath(...)`).
	- *Dinâmica/local* = `GridPathfinder2D` (construída à volta de um centro e usada como fallback quando não existe `NavGrid2D.Instance`).

---


### 2) A* (resumo prático)

O A* é o algoritmo que escolhe “por onde ir” na grelha, equilibrando custo real e direcção ao alvo:

$$f(n)=g(n)+h(n)$$

- **$g$**: custo acumulado desde o início
- **$h$**: estimativa até ao alvo

No teu código, a distância/heurística vem de `GetDistance(...)` e é **Octile** (custos 10/14).

Exemplo (o nó guarda $g$, $h$ e calcula $f$):

```csharp
public int gCost;
public int hCost;
public int fCost => gCost + hCost;
```

O `FindPath(...)` segue este fluxo:

1. Começa no nó inicial (open set).
2. Repete: escolhe o nó com menor $f$, expande vizinhos, actualiza custos e `parent`.
3. Quando chega ao alvo, reconstrói o caminho seguindo `parent` (do alvo para o início) e invertendo.

Exemplo (actualização de vizinho quando se encontra um caminho melhor):

```csharp
int newCost = current.gCost + GetDistance(current, neighbour);
if (!openSet.Contains(neighbour) || newCost < neighbour.gCost)
{
	neighbour.gCost = newCost;
	neighbour.hCost = GetDistance(neighbour, targetNode);
	neighbour.parent = current;
	if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
}
```

Nota do teu código: tanto em `GridPathfinder2D` como em `NavGrid2D`, o open set é uma `List` e o closed set é um `HashSet`, com desempate por `h` quando `f` empata.

---

### 3) Execução do caminho: como o teu inimigo se move

O A* devolve uma lista de pontos (`currentPath`). No teu inimigo (a abelha), a execução funciona assim:

#### Seguimento por waypoints

- O caminho é uma lista de waypoints.
- O agente aponta para o waypoint actual.
- Quando chega “perto o suficiente” (threshold), avança para o próximo.

Exemplo (trocar de waypoint quando chega perto):

```csharp
if (toWp.magnitude <= pathPointThreshold)
{
	pathIndex++;
	if (pathIndex >= currentPath.Count)
	{
		desiredVelocity = Vector2.zero;
		return;
	}
}
```

No teu código, `pathPointThreshold` demasiado pequeno pode causar oscilações; demasiado grande pode fazer avançar waypoints cedo demais.

#### Aplicação do movimento (Rigidbody2D)

No teu código, a abelha calcula uma `desiredVelocity` e aplica-a no `FixedUpdate` através do `Rigidbody2D`:

```csharp
rb.linearVelocity = desiredVelocity;
```

Isto significa que **não estás a usar** um modo “cinemático puro” baseado em `transform.position`/`MovePosition` como estratégia principal; o movimento é feito via `Rigidbody2D`.

#### Validação local (anti-tunneling / bloqueios)

Antes de aplicar a velocidade, o teu `FixedUpdate` faz um `CircleCast` na direcção do movimento para evitar atravessar obstáculos, e se detectar bloqueio força repath ou tenta um pequeno “nudge”:

```csharp
var hit = Physics2D.CircleCast(origin, rad, dir, stepDist, obstacleMask);
if (hit.collider != null)
{
	if (hit.distance <= 0.02f)
	{
		float nudgeSpeed = Mathf.Max(desiredSpeed * 0.6f, 0.5f);
		desiredVelocity = hit.normal.normalized * nudgeSpeed;
	}
	else
		desiredVelocity = Vector2.zero;
	repathTimer = 0f;
}
```

#### Fallback quando não há caminho

Se depois das tentativas o `currentPath` ficar vazio, o teu código não pára: faz um steer local que amostra 16 direcções e escolhe a melhor (equilibra “ir para o alvo” e “estar livre de colisões”), usando `CircleCast` para avaliar espaço livre.

---

### 4) Integração em tempo real (no `BeeEnemy`): repath, fallback e recuperação

A tua integração “em jogo” acontece sobretudo em `BeeEnemy.FollowPathTowards(target, speed)`:

- É chamada em **Roaming** para aproximar ao jogador quando não há lunge/LOS, e para regressar ao `spawnPosition` quando sai do leash.
- É chamada em **Lunging** para ir para `playerAttackPoint` (pés do jogador).
- É chamada em **Retreating** para ir para `retreatTargetPosition`.

#### Quando recalcula o caminho

O caminho é recalculado quando `repathTimer` expira (a cada `repathInterval`) ou quando o agente chega ao fim do caminho (`pathIndex >= currentPath.Count`):

```csharp
repathTimer -= Time.deltaTime;
if (repathTimer <= 0f || pathIndex >= currentPath.Count)
{
	repathTimer = repathInterval;
}
```

#### Escolha de grelha (baked vs local)

- Com `NavGrid2D.Instance`: usa `NavGrid2D.Instance.FindPath(transform.position, target)`.
- Sem `NavGrid2D.Instance`: reconfigura o `GridPathfinder2D` com `gridWorldSize`, `nodeRadius`, `obstacleMask`, aplica `SetClearance(GetClearance())`, e chama `FindPath(transform.position, target)`.

#### Tentativas de recuperação quando `FindPath` falha

Se `path == null`, o `BeeEnemy` tenta recuperar:

- multiplica `gridWorldSize` por `1.5f` e `2.0f`;
- tenta centros base: ponto médio `(start+target)/2`, posição actual (start) e target;
- se continuar a falhar, tenta 4 centros offset a partir do midpoint (ao longo de `dirToTarget` e do perpendicular), com offset `max(1, min(big.x,big.y)*0.25f)`.

#### Recuperação de “stuck” (só em `Retreating`)

No `FixedUpdate`, se a abelha estiver quase parada durante tempo suficiente (`stuckVelocityThreshold` e `stuckTimeThreshold`), força repath e escolhe um `retreatTargetPosition` alternativo numa direcção aleatória (`Random.insideUnitCircle.normalized`), até `maxRecoveryAttempts` (depois volta a `Roaming`).

---

<a id="2-state-machine-beeenemy"></a>

## 2) State Machine (BeeEnemy)

### State Machine — BeeEnemy (Giant Bee)

Estados:

- `Roaming`
- `AttackWindup`
- `Lunging`
- `Retreating`

---

### Loop (o que corre em runtime)

#### Update

O `Update()` reduz `currentCooldown`, faz `FlipSprite()`, calcula `playerDistance` e chama `StateMachine(playerDistance)`.

```csharp
if (currentCooldown > 0f) currentCooldown -= Time.deltaTime;
FlipSprite();
StateMachine(Vector3.Distance(transform.position, player.transform.position));
```

#### FixedUpdate

O movimento é aplicado por física (`rb.linearVelocity = desiredVelocity`). Antes disso, o script faz validação de colisões e pode forçar repath (`repathTimer = 0f`). A recuperação de “stuck” existe apenas em `Retreating`.

---

### Transições (exactas)

| De | Para | Condição no código | Efeito/Acção |
|---|------|---------------------|--------------|
| `Roaming` | `AttackWindup` | `currentCooldown <= 0f` e `playerDistance <= typedStats.lungeRange` e `HasLineOfSightToAttackPoint(GetPlayerFeetPosition())` | `StartAttackWindup()` |
| `AttackWindup` | `Lunging` | `windupTimer >= windupDuration` | `BeginLungeAfterWindup()` |
| `Lunging` | `Retreating` | `lungeTimer >= typedStats.lungeDuration` **ou** `Distance(transform.position, playerAttackPoint) < 0.3f` | `EndLunge()` |
| `Lunging` | `Retreating` | colisão com o jogador (`OnCollisionEnter2D`) | `EndLunge()` |
| `Retreating` | `Roaming` | `Distance(transform.position, retreatTargetPosition) < 0.5f` | `rb.linearVelocity = 0`, `currentCooldown = 0`, `repathTimer = 0`, `desiredVelocity = 0` |

---

### Comportamento por estado (resumo)

#### Roaming

- Calcula `withinLeash` por `typedStats.playerDetect * 1.0f`.
- Se estiver fora do leash, chama `FollowPathTowards(spawnPosition, typedStats.retreatSpeed)`.
- Se estiver dentro do leash e puder atacar:
  - com LOS para os “pés”: entra em `AttackWindup`;
  - sem LOS: aproxima via `FollowPathTowards(player.position, typedStats.roamSpeed)`.
- Se estiver dentro do leash mas o jogador estiver fora de `lungeRange`: aproxima via pathfinding.
- Caso contrário: `desiredVelocity = Vector2.zero`.

#### AttackWindup

- Ao entrar (`StartAttackWindup()`): guarda `lungeStartPosition`, define `playerAttackPoint` como “pés”, zera `windupTimer` e activa flags/animação.
- Enquanto decorre: `desiredVelocity = Vector2.zero`; quando `windupTimer >= windupDuration` passa a `Lunging`.

#### Lunging

- Chama `FollowPathTowards(playerAttackPoint, typedStats.lungingForce)`.
- Termina por tempo (`typedStats.lungeDuration`), por distância ao ponto (`< 0.3f`) ou por colisão com o player → `EndLunge()`.

#### Retreating

- Chama `FollowPathTowards(retreatTargetPosition, typedStats.retreatSpeed)`.
- Se chegar a `retreatTargetPosition` (distância `< 0.5f`), volta a `Roaming` e limpa cooldown.
- Se ficar “stuck” tempo suficiente, força repath e escolhe um `retreatTargetPosition` alternativo.

---

### Alvos e checks usados na decisão

#### Ponto de ataque (pés)

`playerAttackPoint = GetPlayerFeetPosition()`:

```csharp
Bounds bounds = playerCollider.bounds;
return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
```

#### LOS para iniciar AttackWindup

Inicia windup apenas se `Raycast` não acertar em nada no `obstacleMask`:

```csharp
var hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
return hit.collider == null;
```

---

### Recuperação de stuck (só em Retreating)

- Thresholds (Inspector): `stuckVelocityThreshold = 0.05f`, `stuckTimeThreshold = 0.8f`, `maxRecoveryAttempts = 5`.
- Se exceder tentativas: volta a `Roaming`.
- Caso contrário: escolhe direcção aleatória e chama `FollowPathTowards(...)` com fallbacks quando `typedStats` é null.

```csharp
Vector2 altDir = Random.insideUnitCircle.normalized;
retreatTargetPosition = transform.position + (Vector3)(altDir * (typedStats != null ? typedStats.retreatRange : 2f));
FollowPathTowards(retreatTargetPosition, typedStats != null ? typedStats.retreatSpeed : 1f);
```

---

### Onde o pathfinding entra

- `Roaming` → `FollowPathTowards(player.position, typedStats.roamSpeed)` ou `FollowPathTowards(spawnPosition, typedStats.retreatSpeed)`
- `Lunging` → `FollowPathTowards(playerAttackPoint, typedStats.lungingForce)`
- `Retreating` → `FollowPathTowards(retreatTargetPosition, typedStats.retreatSpeed)`

---

<a id="3-ga"></a>

## 3) Evolução Genética Adversária (GA)

### Sistema de Evolução Genética Adversária (GA)

### Resumo
Este repositório contém uma implementação aplicada de um Algoritmo Genético (AG) integrado num protótipo de jogo Metroidvania. O objectivo é adaptar automaticamente atributos de inimigos (por espécie) em resposta ao desempenho do jogador, mantendo controlos de equilíbrio que previnem escalonamento indevido.

O documento descreve a arquitectura, as representações genéticas, a função de aptidão, os operadores evolutivos, mecanismos de controlo, e procedimentos experimentais recomendados.

---

### Arquitectura e componentes principais

- `GlobalGeneticEvolver` — singleton responsável por populações por espécie, selecção, crossover, mutação, evolução por gerações, persistência em ficheiro e mecânicas de dificuldade adaptativa.
- `EnemyGenome` — representação do genoma com genes normalizados em [0,1] e utilitários para conversão para valores de jogo (HP, dano, velocidade, resistências).
- `EnemyFitnessTracker` — componente por-instância que aplica o genoma ao inimigo, recolhe métricas de combate (dano, tempo de vida) e reporta ao evolutor.
- `GeneticDamageIntegration` — utilitário que liga eventos de dano do jogador ao sistema genético para atribuição precisa de crédito.
- UI de diagnóstico: `EnemyGenomeUI`, `GlobalGeneticDebugUI`, `GeneticDebugController`.

Referências de ficheiros: `Assets/Scripts/AI/GeneticAlgorithm/`.

---

### Métodos e design

#### Representação do genoma
Genes são floats normalizados. Esta escolha facilita aplicação uniforme entre espécies com diferentes escalas de estatísticas base. As transformações para valores de jogo são determinísticas (ver secção "Mapeamento Gene → Valores").

Excerto (estrutura de genes):

```csharp
[Range(0f,1f)] float healthGene, damageGene, attackSpeedGene;
[Range(0f,1f)] float movementSpeedGene, aggressionRangeGene;
[Range(0f,1f)] float meleeResistanceGene, rangedResistanceGene, aggressivenessGene;
```

#### Função de aptidão — definição e motivação
O sistema define a aptidão (fitness) de uma instância como uma combinação ponderada de métricas observáveis durante um encontro: dano causado e tempo de sobrevivência são os sinais base. O código actual calcula:

$$\text{fitness} = 10 \cdot D + 0.5 \cdot T + B( D, T )$$

onde:
- $D$ é o dano total causado pelo inimigo ao jogador;
- $T$ é o tempo de sobrevivência (seconds);
- $B(D,T)$ é um bónus para eliminações rápidas definido por código (ex.: $B = 2(10 - T)$ quando $D>0$ e $T<10$).

Racional: dano mede eficácia ofensiva directa; tempo mede sustentabilidade; o bónus rápido favorece ataques que capitalizam a velocidade de ataque.

Adicionalmente, existe um ajuste adaptativo que penaliza genomas agressivos se o jogador morrer frequentemente — isto implementa uma forma de regulação baseada no estado do jogador.

#### Seleção e operadores evolucionários

- Seleção: torneio de tamanho `tournamentSize` com escolha por aptidão média.
- Elitismo: os `eliteCount` melhores genomas passam para a geração seguinte com portabilidade parcial de aptidão.
- Crossover: uniform crossover (cada gene tem 50% probabilidade de vir de um dos pais).
- Mutação: por-gene com probabilidade `mutationRate` e amplitude `mutationStrength`.

Parâmetros chave encontram-se editáveis como campos serializáveis em `GlobalGeneticEvolver`.

##### Implementação detalhada dos operadores genéticos

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

#### Atribuição de aptidão à população
Em vez de actualizar directamente o genoma que foi instanciado (que pode ter sido modificado pelos multiplicadores de geração), o sistema procura o exemplar mais semelhante dentro da população e acumula o fitness nesse exemplar. A similaridade é calculada pela soma das diferenças absolutas num subconjunto de genes.

Esta estratégia mantém resiliência a pequenas variações e permite acumular estatísticas na população persistente, ao custo de alguma imprecisão na atribuição directa.

---

### Mapeamento Gene → Valores de Jogo (implementação)

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

### Instrumentação, diagnóstico e persistência

- UI por-inimigo: `EnemyGenomeUI` mostra genes e indicadores em tempo real.
- Painel global: `GlobalGeneticDebugUI` (toggle `G`) apresenta estatísticas por espécie e curva de evolução.
- Controles: `GeneticDebugController` (H, Ctrl+R, F5, Tab) para testes e respawn.
- Integração de dano: `GeneticDamageIntegration` liga eventos `HealthSystem` do jogador à contabilização de dano para garantir atribuição correcta.
- Persistência: o estado do evolutor é serializado em JSON para `Application.persistentDataPath` (campo `saveFileName`).

---

### Procedimento experimental recomendado

Configuração típica para observação rápida:

- `populationSize` = 20
- `evolveTriggerCount` = 5
- `rounds` (no `GenericTrainingArena`) = 200

Experimentos sugeridos:

1. Ablação de genes: fixe `damageGene` e observe se comportamento/movimento evolui.
2. Telemetria adicional: instrumentar `EnemyFitnessTracker` para registar tempo em alcance e número de ataques, de modo a aferir impacto de `movementSpeed` e `attackSpeed`.
3. Comparar com/sem `adaptiveDifficulty` para apreciar o efeito do regulador no equilíbrio.
