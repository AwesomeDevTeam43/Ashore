# Sistema de Pathfinding 2D - Documentação Técnica

## Índice
1. [Visão Geral](#visão-geral)
2. [Arquitetura do Sistema](#arquitetura-do-sistema)
3. [Algoritmo A* (A-Star)](#algoritmo-a-a-star)
4. [Componentes Principais](#componentes-principais)
5. [Fluxo de Execução](#fluxo-de-execução)
6. [Integração com Inimigos](#integração-com-inimigos)
7. [Parâmetros Configuráveis](#parâmetros-configuráveis)
8. [Otimizações](#otimizações)
9. [Debugging Visual](#debugging-visual)

---

## Visão Geral

O sistema de pathfinding permite que inimigos (como a `BeeEnemy`) naveguem pelo cenário evitando obstáculos. Utiliza o **algoritmo A*** sobre uma **grid 2D** para encontrar o caminho mais curto entre dois pontos.

### Dois Modos de Operação:
1. **NavGrid2D** (Preferido): Grid pré-calculado ("baked") para toda a sala - mais eficiente
2. **GridPathfinder2D** (Fallback): Grid dinâmico calculado em tempo real - mais flexível

---

## Arquitetura do Sistema

```
┌─────────────────────────────────────────────────────────────────┐
│                        Enemy (BeeEnemy)                         │
│                              │                                  │
│                    FollowPathTowards()                          │
│                              │                                  │
│              ┌───────────────┴───────────────┐                  │
│              ▼                               ▼                  │
│     ┌─────────────────┐           ┌──────────────────┐          │
│     │   NavGrid2D     │           │ GridPathfinder2D │          │
│     │   (Baked)       │           │    (Dynamic)     │          │
│     │                 │           │                  │          │
│     │ • Grid global   │           │ • Grid local     │          │
│     │ • Pré-calculado │           │ • Tempo real     │          │
│     │ • Singleton     │           │ • Com cache      │          │
│     └─────────────────┘           └──────────────────┘          │
│              │                               │                  │
│              └───────────────┬───────────────┘                  │
│                              ▼                                  │
│                    Algoritmo A* (A-Star)                        │
│                              │                                  │
│                              ▼                                  │
│                   Lista de Waypoints                            │
│                      (Vector2[])                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## Algoritmo A* (A-Star)

O A* é um algoritmo de busca que encontra o caminho mais curto combinando:
- **g(n)**: Custo real do início até o nó atual
- **h(n)**: Heurística (estimativa) do nó atual até o destino
- **f(n) = g(n) + h(n)**: Custo total estimado

### Passo a Passo do A*:

```
1. INICIALIZAÇÃO
   ├── Criar openSet (nós a explorar) com nó inicial
   ├── Criar closedSet (nós já explorados) vazio
   └── Definir g=0 para nó inicial

2. LOOP PRINCIPAL (enquanto openSet não está vazio)
   │
   ├── 2.1 Selecionar nó com menor f(n) do openSet
   │        └── Se empate, preferir menor h(n)
   │
   ├── 2.2 Se nó atual = destino → SUCESSO!
   │        └── Reconstruir caminho seguindo parents
   │
   ├── 2.3 Mover nó atual para closedSet
   │
   └── 2.4 Para cada vizinho do nó atual:
            │
            ├── Se vizinho em closedSet → ignorar
            ├── Se vizinho não é walkable → ignorar
            │
            ├── Calcular novo gCost = atual.g + distância
            │
            └── Se novo gCost < vizinho.gCost OU vizinho não está em openSet:
                 ├── Atualizar vizinho.gCost
                 ├── Calcular vizinho.hCost (distância até destino)
                 ├── Definir vizinho.parent = nó atual
                 └── Adicionar vizinho ao openSet (se não estiver)

3. Se openSet ficou vazio → FALHA (sem caminho possível)
```

### Cálculo de Distância (Heurística):

```csharp
// Movimento diagonal custa 14 (√2 ≈ 1.414 × 10)
// Movimento cardinal custa 10

private static int GetDistance(Node a, Node b)
{
    int dstX = Mathf.Abs(a.x - b.x);
    int dstY = Mathf.Abs(a.y - b.y);
    
    // Primeiro move diagonalmente o máximo possível
    // Depois move em linha reta o restante
    if (dstX > dstY)
        return 14 * dstY + 10 * (dstX - dstY);
    return 14 * dstX + 10 * (dstY - dstX);
}
```

**Exemplo Visual:**
```
De A até B (3 células X, 2 células Y):

    A · · ·
    · ╲ · ·
    · · ╲ B

Custo = 14×2 (diagonal) + 10×1 (horizontal) = 38
```

---

## Componentes Principais

### 1. Node (Nó da Grid)

Cada célula da grid é representada por um `Node`:

```csharp
public class Node
{
    public bool walkable;      // Pode-se passar por aqui?
    public Vector2 worldPos;   // Posição no mundo
    public int x, y;           // Coordenadas na grid
    public int gCost;          // Custo do início até aqui
    public int hCost;          // Estimativa até o destino
    public Node parent;        // Nó anterior no caminho
    
    public int fCost => gCost + hCost;  // Custo total
}
```

### 2. Construção da Grid

```csharp
// Para cada célula da grid:
for (int x = 0; x < gridSizeX; x++)
{
    for (int y = 0; y < gridSizeY; y++)
    {
        // Calcular posição no mundo
        Vector2 worldPoint = origin + new Vector2(
            x * nodeDiameter + nodeRadius,
            y * nodeDiameter + nodeRadius
        );
        
        // Verificar se há obstáculo (Physics2D.OverlapCircle)
        float r = nodeRadius + clearance;
        bool walkable = !Physics2D.OverlapCircle(worldPoint, r, obstacleMask);
        
        // Criar nó
        grid[x, y] = new Node(walkable, worldPoint, x, y);
    }
}
```

**Visualização da Grid:**
```
┌───┬───┬───┬───┬───┬───┬───┬───┐
│ · │ · │ · │ · │ · │ · │ · │ · │  · = walkable
├───┼───┼───┼───┼───┼───┼───┼───┤  █ = obstáculo
│ · │ · │ █ │ █ │ █ │ · │ · │ · │
├───┼───┼───┼───┼───┼───┼───┼───┤
│ · │ · │ █ │ █ │ █ │ · │ · │ · │
├───┼───┼───┼───┼───┼───┼───┼───┤
│ A │ · │ · │ · │ · │ · │ · │ B │  A = início
├───┼───┼───┼───┼───┼───┼───┼───┤  B = destino
│ · │ · │ · │ · │ · │ · │ · │ · │
└───┴───┴───┴───┴───┴───┴───┴───┘
```

### 3. Obtenção de Vizinhos

```csharp
private IEnumerable<Node> GetNeighbours(Node node)
{
    // Verificar 8 direções (incluindo diagonais)
    for (int dx = -1; dx <= 1; dx++)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;  // Ignorar o próprio nó
            
            int nx = node.x + dx;
            int ny = node.y + dy;
            
            // Verificar limites da grid
            if (nx < 0 || ny < 0 || nx >= gridSizeX || ny >= gridSizeY) 
                continue;
            
            var neighbour = grid[nx, ny];
            if (!neighbour.walkable) continue;
            
            // IMPORTANTE: Prevenir "corner cutting"
            // Não permitir diagonal se os cardinais adjacentes estão bloqueados
            if (dx != 0 && dy != 0)
            {
                var n1 = grid[node.x + dx, node.y];  // Horizontal
                var n2 = grid[node.x, node.y + dy];  // Vertical
                if (!n1.walkable || !n2.walkable) continue;
            }
            
            yield return neighbour;
        }
    }
}
```

**Corner Cutting - Por que evitar:**
```
Sem proteção:           Com proteção:
    · █ ·                   · █ ·
    · ╲ ·  ← Atravessa      · │ ·  ← Contorna
    · █ ·     a parede!     · └─·     corretamente
```

---

## Fluxo de Execução

### Passo 1: Inimigo decide mover-se

```csharp
// Em RoamBehavior() ou RetreatBehavior():
FollowPathTowards(player.transform.position, typedStats.roamSpeed);
```

### Passo 2: Verificar se precisa recalcular caminho

```csharp
private void FollowPathTowards(Vector3 target, float speed)
{
    repathTimer -= Time.deltaTime;
    
    // Recalcular a cada 0.25 segundos OU se chegou ao fim do caminho atual
    if (repathTimer <= 0f || pathIndex >= currentPath.Count)
    {
        repathTimer = repathInterval;  // Reset timer (0.25s)
        
        // Tentar encontrar caminho...
    }
}
```

### Passo 3: Escolher sistema de pathfinding

```csharp
List<Vector2> path = null;

if (NavGrid2D.Instance != null)
{
    // PREFERIDO: Usar grid pré-calculada da sala
    path = NavGrid2D.Instance.FindPath(transform.position, target);
}
else
{
    // FALLBACK: Criar grid dinâmica local
    pathfinder.Configure(gridWorldSize, nodeRadius, obstacleMask);
    pathfinder.SetClearance(GetClearance());  // Raio do collider
    path = pathfinder.FindPath(transform.position, target);
}
```

### Passo 4: Se falhou, tentar recuperação

```csharp
if (path == null)
{
    // Estratégia 1: Aumentar tamanho da grid (1.5x, 2x)
    // Estratégia 2: Tentar diferentes centros para a grid
    // Estratégia 3: Usar steering local como último recurso
    
    float[] sizeMults = new float[] { 1.5f, 2.0f };
    Vector2[] centers = new Vector2[] {
        (start + target) * 0.5f,  // Meio
        start,                      // Posição atual
        target                      // Destino
    };
    
    // Tentar combinações até encontrar caminho...
}
```

### Passo 5: Seguir waypoints

```csharp
// Obter waypoint atual
Vector2 wp = currentPath[pathIndex];
Vector2 toWp = wp - (Vector2)transform.position;

// Se chegou perto o suficiente, avançar para próximo waypoint
if (toWp.magnitude <= pathPointThreshold)  // 0.15 unidades
{
    pathIndex++;
    if (pathIndex >= currentPath.Count)
    {
        desiredVelocity = Vector2.zero;  // Chegou ao destino!
        return;
    }
    wp = currentPath[pathIndex];
    toWp = wp - (Vector2)transform.position;
}

// Definir velocidade em direção ao waypoint
desiredVelocity = toWp.normalized * speed;
```

### Passo 6: Aplicar movimento (FixedUpdate)

```csharp
private void FixedUpdate()
{
    // Verificação de colisão ANTES de mover (previne tunneling)
    float stepDist = desiredVelocity.magnitude * Time.fixedDeltaTime;
    Vector2 dir = desiredVelocity.normalized;
    
    var hit = Physics2D.CircleCast(origin, clearance, dir, stepDist, obstacleMask);
    
    if (hit.collider != null)
    {
        // Colisão detectada!
        if (hit.distance <= 0.02f)
        {
            // Muito perto: empurrar para fora
            desiredVelocity = hit.normal * nudgeSpeed;
        }
        else
        {
            // Bloqueado à frente: parar e recalcular
            desiredVelocity = Vector2.zero;
            repathTimer = 0f;  // Forçar repath imediato
        }
    }
    
    // Aplicar velocidade final
    rb.linearVelocity = desiredVelocity;
}
```

---

## Integração com Inimigos

### Configuração no Inspector (BeeEnemy)

```
[Header("Pathfinding")]
├── obstacleMask        → LayerMask (Ground | MovingPlatform)
├── gridWorldSize       → Vector2 (12, 8) - Tamanho da grid local
├── nodeRadius          → float (0.2) - Metade do tamanho de célula
├── pathPointThreshold  → float (0.15) - Distância para "chegar" a waypoint
└── repathInterval      → float (0.25) - Segundos entre recálculos
```

### Clearance (Folga do Collider)

O sistema calcula automaticamente o "clearance" baseado no collider do inimigo:

```csharp
private float GetClearance()
{
    if (col2D is CircleCollider2D cc)
        return cc.radius * scale * 0.6f;
    
    if (col2D is CapsuleCollider2D cap)
        return Mathf.Max(cap.size.x, cap.size.y) * 0.3f * scale;
    
    // Fallback: usar bounds do collider
    Bounds b = col2D.bounds;
    float halfDiagonal = 0.5f * Mathf.Sqrt(b.size.x² + b.size.y²);
    return Mathf.Max(clearance, halfDiagonal);
}
```

Isto garante que o pathfinder encontra caminhos onde o inimigo **realmente cabe**.

---

## Parâmetros Configuráveis

### GridPathfinder2D

| Parâmetro | Tipo | Default | Descrição |
|-----------|------|---------|-----------|
| `gridWorldSize` | Vector2 | (12, 8) | Dimensões da grid em unidades do mundo |
| `nodeRadius` | float | 0.2 | Raio de cada célula (metade do tamanho) |
| `obstacleMask` | LayerMask | - | Layers consideradas obstáculos |
| `clearance` | float | 0 | Folga extra além do nodeRadius |
| `useCache` | bool | true | Usar cache de grids |
| `cacheTTL` | float | 0.75 | Tempo de vida do cache (segundos) |

### NavGrid2D

| Parâmetro | Tipo | Default | Descrição |
|-----------|------|---------|-----------|
| `origin` | Vector2 | (-20, -12) | Canto inferior esquerdo da grid |
| `size` | Vector2 | (40, 24) | Dimensões totais da grid |
| `nodeRadius` | float | 0.12 | Raio de cada célula |
| `clearance` | float | 0 | Folga extra para verificação |
| `obstacleMask` | LayerMask | - | Layers bloqueadoras |
| `bakeOnStart` | bool | true | Calcular grid no Start() |

---

## Otimizações

### 1. Cache de Grids (GridPathfinder2D)

```csharp
// Grids são cacheadas por "chunk" para evitar recálculo
string key = $"{size}|r{nodeRadius}|m{mask}|cx{chunkX}|cy{chunkY}";

if (cache.TryGetValue(key, out var entry))
{
    if (Time.time - entry.createdTime <= cacheTTL)
    {
        // Reutilizar grid existente!
        grid = entry.grid;
        return;
    }
}
```

### 2. Repath Interval

Em vez de recalcular o caminho a cada frame, usa-se um intervalo:

```csharp
repathTimer -= Time.deltaTime;
if (repathTimer <= 0f)  // A cada 0.25 segundos
{
    repathTimer = repathInterval;
    // Recalcular caminho...
}
```

### 3. Fallback para Steering Local

Se o A* falhar completamente, usa-se um steering simples:

```csharp
// Amostrar 16 direções e escolher a melhor
for (int i = 0; i < 16; i++)
{
    float angle = (360f / 16) * i;
    Vector2 dir = new Vector2(Cos(angle), Sin(angle));
    
    float align = Vector2.Dot(dir, toTarget.normalized);  // Alinhamento com destino
    float free = CheckClearance(dir);                      // Espaço livre
    
    float score = align * 0.7f + free * 0.6f;
    if (score > bestScore) bestDir = dir;
}
```

### 4. Nudge de Recuperação de Stuck

```csharp
// Se velocidade muito baixa por muito tempo → stuck!
if (speed < stuckVelocityThreshold && stuckTimer >= stuckTimeThreshold)
{
    // Escolher direção aleatória para tentar sair
    Vector2 altDir = Random.insideUnitCircle.normalized;
    retreatTargetPosition = position + altDir * retreatRange;
}
```

---

## Debugging Visual

### Gizmos do NavGrid2D

```csharp
private void OnDrawGizmosSelected()
{
    // Contorno da grid
    Gizmos.color = new Color(0, 1, 0, 0.1f);
    Gizmos.DrawWireCube(origin + size * 0.5f, size);
    
    // Células (verde = walkable, vermelho = bloqueado)
    for (int x = 0; x < gridX; x++)
    for (int y = 0; y < gridY; y++)
    {
        var n = grid[x, y];
        Gizmos.color = n.walkable ? Color.green : Color.red;
        Gizmos.DrawCube(n.worldPos, nodeRadius * 1.6f);
    }
}
```

### Gizmos do BeeEnemy

```csharp
// Caminho atual (cyan)
if (currentPath.Count > 0)
{
    Gizmos.color = Color.cyan;
    for (int i = 0; i < currentPath.Count - 1; i++)
    {
        Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        Gizmos.DrawWireSphere(currentPath[i], 0.06f);
    }
}

// Clearance do agente (azul claro)
Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
Gizmos.DrawWireSphere(transform.position, GetClearance());

// Velocidade desejada (laranja)
Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
Gizmos.DrawLine(position, position + desiredVelocity * 0.1f);
```

---

## Resumo do Fluxo Completo

```
┌────────────────────────────────────────────────────────────────┐
│  1. Update() → StateMachine() → RoamBehavior()                 │
│                                      │                         │
│  2. FollowPathTowards(target, speed) │                         │
│              │                       ▼                         │
│              │         ┌─────────────────────────┐             │
│              │         │ repathTimer expirou?    │             │
│              │         │ OU pathIndex >= count?  │             │
│              │         └───────────┬─────────────┘             │
│              │                     │ SIM                       │
│              ▼                     ▼                           │
│  3. ┌─────────────────────────────────────────────┐            │
│     │ NavGrid2D existe?                           │            │
│     │ SIM → NavGrid2D.FindPath()                  │            │
│     │ NÃO → GridPathfinder2D.FindPath()           │            │
│     └─────────────────────────────────────────────┘            │
│              │                                                 │
│              ▼                                                 │
│  4. Path encontrado?                                           │
│     │ SIM → currentPath = path                                 │
│     │ NÃO → Tentar recovery (grid maior, centros diferentes)   │
│     │       └→ Ainda NÃO? → Steering local                     │
│              │                                                 │
│              ▼                                                 │
│  5. Seguir waypoints:                                          │
│     │ wp = currentPath[pathIndex]                              │
│     │ Se perto (< 0.15) → pathIndex++                          │
│     │ desiredVelocity = (wp - pos).normalized * speed          │
│              │                                                 │
│              ▼                                                 │
│  6. FixedUpdate():                                             │
│     │ CircleCast para verificar colisão                        │
│     │ Se colidir → nudge ou parar + repath                     │
│     │ rb.linearVelocity = desiredVelocity                      │
│              │                                                 │
│              ▼                                                 │
│  7. Inimigo move-se suavemente evitando obstáculos! ✓          │
└────────────────────────────────────────────────────────────────┘
```

---

## Ficheiros Relacionados

- `Assets/Scripts/Pathfinding/GridPathfinder2D.cs` - Pathfinder dinâmico com A*
- `Assets/Scripts/Pathfinding/NavGrid2D.cs` - Grid pré-calculada da sala
- `Assets/Scripts/Enemy/Enemy_GiantBee.cs` - Exemplo de integração com inimigo
