
# Pathfinding (A*) — Explicação Geral

O pathfinding, de forma geral, é o problema de **encontrar um trajecto “bom” entre um ponto A e um ponto B** num ambiente com obstáculos. “Bom” costuma significar **menor custo** (menor distância, menor tempo, menor risco), respeitando restrições do agente (por exemplo, não atravessar paredes, não passar em zonas demasiado estreitas, etc.).

Quase todos os sistemas de pathfinding assentam em duas ideias principais:

- **Modelação do mundo**: transformar o cenário num **grafo** (nós + ligações), onde cada nó representa um estado/posição possível.
- **Algoritmo de procura**: explorar esse grafo de forma eficiente até encontrar o objectivo.

Em jogos 2D (e em muitos projectos Unity), a modelação mais comum é uma **grelha (grid)**: o mundo é amostrado em células; cada célula torna-se um nó; e o agente pode deslocar-se para células vizinhas (4 ou 8 direcções). As colisões/obstáculos marcam células como **não transitáveis**.

---

## A* (A-star): lógica e referência teórica

O A* é um algoritmo de procura em grafos que pode ser visto como um equilíbrio entre:

- **Dijkstra**: encontra o caminho óptimo usando apenas custo real, mas tende a explorar muito espaço.
- **Greedy Best-First Search**: segue a “direcção do alvo” com base numa estimativa, é rápido mas não garante caminho óptimo.

O A* combina ambos através da função de avaliação:

$$
f(n) = g(n) + h(n)
$$

onde:

- **$g(n)$** é o custo real acumulado do início até ao nó $n$ (o que já foi “caminhado”).
- **$h(n)$** é a **heurística**: uma estimativa do custo de $n$ até ao objectivo (um “palpite” informado).
- **$f(n)$** é a prioridade usada para decidir qual o próximo nó a expandir.

Intuição:

- $g$ impede o algoritmo de “se iludir” (mantém o custo real sob controlo).
- $h$ dá direcção (evita explorar zonas pouco relevantes).

### Como o A* “funciona” (sem entrar em linhas de código)

O A* mantém, conceptualmente, dois conjuntos:

- **Open set** (aberto): nós descobertos, ainda por expandir (a fronteira).
- **Closed set** (fechado): nós já expandidos/“finalizados”.

O ciclo é:

1. Colocar o nó inicial no **open set**.
2. Escolher, do open set, o nó com menor **$f(n)$**.
3. Se esse nó for o objectivo, termina.
4. Caso contrário, **expandir** esse nó: olhar para os vizinhos e calcular custos.
5. Para cada vizinho, se o novo caminho for melhor, actualizar:
	- o melhor custo $g$ conhecido,
	- o valor heurístico $h$,
	- e o **parent** (de onde veio), para depois reconstruir o trajecto.
6. Mover o nó actual para o **closed set** e repetir.

No fim, o trajecto é reconstruído **do objectivo para o início**, seguindo os apontadores `parent` (isto forma uma árvore de predecessores criada durante a procura).

---

## Heurística: a parte mais “teórica” do A*

A heurística $h(n)$ é crucial porque determina o quão eficiente (e correcto) o A* é.

### Heurística admissível

Uma heurística é **admissível** se **nunca sobrestima** o custo real mínimo até ao objectivo:

$$
h(n) \le h^*(n)
$$

onde $h^*(n)$ é o custo real óptimo de $n$ até ao objectivo.

Resultado clássico:

- Se $h$ for admissível e os custos forem não-negativos, o A* **encontra um caminho óptimo**.

### Heurística consistente (ou monótona)

Uma heurística é **consistente** se respeita uma desigualdade do tipo “triângulo”:

$$
h(n) \le c(n,n') + h(n')
$$

onde $c(n,n')$ é o custo de ir do nó $n$ para um vizinho $n'$.

Na prática, consistência tende a tornar a procura mais estável e a reduzir a necessidade de “reabrir” nós.

### Heurísticas típicas em grelha

- Movimento em 4 direcções: **Manhattan** ($|dx| + |dy|$).
- Movimento em 8 direcções: **Octile** (mistura passos ortogonais e diagonais), porque a diagonal tem custo aproximado de $\sqrt{2}$ vezes o passo recto.

---

## Decisões de modelação comuns (grelha + obstáculos + “raio” do agente)

Mesmo com A* correcto, o comportamento “na prática” depende muito de como modelas o mundo:

- **Transitável vs não transitável**: cada célula é marcada com base em testes de colisão (se ali cabe o agente sem intersectar obstáculos).
- **Folga (clearance)**: em vez de planear para um “ponto”, planeias para um agente com volume. Conceptualmente, é como “engordar” os obstáculos pelo raio do agente (ideia próxima de soma de Minkowski), para evitar trajectos que raspem nas paredes.
- **Diagonais e “cortar cantos”**: se permites diagonais, normalmente proíbes a diagonal quando ela implicaria atravessar o canto entre dois obstáculos (evita trajectos fisicamente impossíveis).

---

## Como o resto da lógica “trabalha com” o A* (integração em jogo)

Na prática, um inimigo/agente não “faz A* e pronto”. O A* é apenas a parte de **planeamento global**. À volta dele existe uma lógica que torna o sistema útil em tempo real:

### 1) Decisão / objectivo (quando e para onde procurar)

Antes de correr o A*, a IA define:

- qual é o **destino** (seguir o jogador, recuar, patrulhar, regressar à origem, etc.);
- qual é o **custo aceitável** (por exemplo, procurar só numa área local vs procurar no mapa todo);
- quando é que vale a pena **recalcular** (repathing), porque o mundo e o alvo podem mudar.

Isto liga-se directamente ao A* porque o algoritmo só responde bem à pergunta correcta: “qual o melhor trajecto de start para target *agora*?”

### 2) Repathing (A* em tempo real)

Como o jogador se move e a situação muda, é comum:

- recalcular o caminho **a intervalos** (ex.: de X em X segundos),
- ou recalcular quando há sinais de falha (agente bloqueado, target mudou muito, etc.).

Ou seja: o A* dá-te um trajecto óptimo no grafo actual, mas o “sistema” decide **com que frequência** volta a planear para se adaptar.

### 3) Seguimento do trajecto (path following)

O A* devolve um caminho discreto (uma sequência de nós/células ou pontos). Depois tens de o transformar em movimento:

- o agente escolhe um waypoint actual;
- move-se na direcção desse waypoint;
- quando chega suficientemente perto (limiar), passa ao próximo.

Este passo é essencial porque:

- o A* trabalha num espaço discretizado,
- mas o movimento no jogo é contínuo (física, velocidade, aceleração).

Sem um bom seguimento, até um caminho óptimo pode parecer “mau” (oscilações, ziguezagues, overshoot).

### 4) Validação local e colisões (a diferença entre “plano” e “execução”)

O A* assume que o grafo representa bem o mundo. Mas no motor de jogo:

- há colisões finas,
- há resolução limitada da grelha,
- há interacções físicas (empurrões, atrito, contactos),
- há obstáculos dinâmicos.

Por isso, muitas implementações incluem verificações locais do tipo:

- “esta direcção está livre no próximo passo?”
- “estou a encostar numa parede?”
- “o caminho planeado continua transitável?”

Isto actua como uma camada de segurança: quando a execução no mundo diverge do plano, o sistema detecta e reage.

### 5) Recuperação / anti-stuck

Mesmo com pathfinding bom, ficar preso acontece. Estratégias gerais incluem:

- forçar repath se não houver progresso durante algum tempo;
- escolher um objectivo alternativo (pequeno desvio, ponto intermédio);
- aumentar a área de pesquisa (grelha maior) quando falha a procura;
- usar um “fallback” de steering local (para não ficar imóvel).

Estas técnicas não substituem o A*; são uma forma prática de lidar com:

- discretização da grelha,
- mudanças inesperadas no ambiente,
- limitações da física.

### 6) Duas camadas: planeamento global + controlo local

Uma maneira útil de resumir a arquitectura é:

- **A*** = planeamento global no grafo (“por onde é melhor ir?”)
- **Controlo local** (seguimento + colisões + recuperação) = execução robusta (“como é que vou lá de facto, no mundo real?”)

É esta combinação que faz o pathfinding funcionar bem em jogo.

---

## Porque é que jogos não correm A* a cada frame

Mesmo sendo eficiente, A* pode ficar pesado se a grelha for grande ou houver muitos agentes. Por isso é comum combinar com:

- recalcular trajecto a cada X segundos,
- usar cache,
- procurar em grelhas locais em vez de mapa inteiro,
- e ter um comportamento de fallback quando não há caminho.

