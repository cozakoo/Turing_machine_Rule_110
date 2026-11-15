# Simulación de un Autómata Celular Unidimensional (Regla 110) mediante Máquina de Turing

**Trabajo Práctico Final — Teoría de Autómatas y Máquinas de Turing**

| | |
|---|---|
| **Integrantes** | Arcos Martín · Cardenas Mauricio |
| **Asignatura** | Teoría de Autómatas y Máquinas de Turing |
| **Tema** | Simulación de un AC 1D con la Regla 110 mediante una Máquina de Turing formal |
| **Tecnologías** | C# 12 · .NET 8 · ASP.NET Core Minimal API · HTML/CSS/JS sin dependencias |
| **Entregables** | Código fuente · Informe técnico · `resultados-regla110.txt` · tabla δ · `.zip` |

---

## 1. Resumen

El trabajo implementa, de forma **formal y ejecutable**, una Máquina de Turing que simula la
evolución temporal de un autómata celular unidimensional gobernado por la **Regla 110** con
**condiciones de frontera cíclicas**, iterando un número prefijado `K` de generaciones.

La máquina se define por la tupla clásica `M = (Q, Σ, Γ, δ, q₀, q_acc, q_rej)` y su tabla de
transiciones `δ : Q × Γ → Q × Γ × {L, R}` —**205 transiciones sobre 76 estados**, agrupadas en
siete módulos funcionales— se construye de manera declarativa en código y se **exporta como
documentación de la entrega** (`tabla-transiciones-maquina-turing.txt`).

El verificador contrastó la Máquina de Turing contra una implementación de referencia directa de
la Regla 110 sobre **27 casos de prueba** (incluidos los tres casos del enunciado, casos borde,
configuraciones periódicas y caóticas, entradas inválidas y **100 configuraciones aleatorias** con
semilla fija): **27/27 casos correctos**, con coincidencia exacta generation a generation.

La aplicación web expone la simulación paso a paso: el usuario puede inspeccionar la **tabla δ**
filtrable por módulo, la **traza de ejecución** (estado, cabeza, símbolo leído/escrito, movimiento)
y la **cinta final** con todas las generaciones registradas.

**Palabras clave:** autómata celular · Regla 110 · Máquina de Turing · tabla de transiciones ·
frontera cíclica · contador unario · complejidad.

---

## 2. Introducción

### 2.1. Planteo del problema

Un autómata celular unidimensional (AC 1D) es una estructura de estados locales que se actualizan en
paralelo: cada celda del sistema calcula su próximo estado a partir de su propia vecindad. La
**Regla 110** es el autómata celular elemental más conocido: su tabla local es

| Vecindad | 111 | 110 | 101 | 100 | 011 | 010 | 001 | 000 |
|---|---|---|---|---|---|---|---|---|
| Nuevo estado | 0 | 1 | 1 | 0 | 1 | 1 | 1 | 0 |

El enunciado del trabajo pide **implementar una Máquina de Turing** capaz de calcular esa
evolución, contando las generaciones mediante un contador `K` expresado en binario sobre la misma
cinta, y **dejando registrada cada generación calculada** hasta detenerse en un estado de
aceptación con la configuración final visible.

### 2.2. Objetivos

1. Definir formalmente la MT (alfabetos, estados por módulos lógicos, tabla `δ`) y su
   configuración inicial.
2. Implementar un motor de MT genérico (cinta, cabeza, tabla `δ`, traza) y la tabla `δ` concreta
   de la Regla 110.
3. Resolver explícitamente la **frontera cíclica** y el **control iterativo con contador unario**.
4. Verificar la implementación con casos de prueba rigurosos y documentar resultados y conclusiones.

### 2.3. Aportes principales

- Una **tabla `δ` explícita de 205 filas**, organizada en siete módulos, auditable desde la
  interfaz web y exportable como archivo de texto.
- La **frontera cíclica resuelta dentro del control finito** de la máquina (no con artificios del
  lenguaje de programación): el valor `b₀` y el valor `b_{N-1}` se memorizan en estados
  parametricados, de modo que la última celda usa `b₀` como vecino derecho y la primera usa
  `b_{N-1}` como vecino izquierdo.
- El **contador unario** exigido por el enunciado, alimentado por una conversión
  binario → unario implementada como submódulo de la propia máquina.
- Una **verificación cruzada** entre la MT y una implementación de referencia, que permite
  detectar desvíos en cualquier caso no contemplado explícitamente.

---

## 3. Marco teórico

### 3.1. Autómatas celulares elementales

Un AC 1D de radio 1 sobre el alfabeto `{0,1}` se define por una función local
`f : {0,1}³ → {0,1}` y una condición de frontera. Con frontera **cíclica** (anillo), la
configuración `x = (x₀, …, x_{N-1})` evoluciona según

```
xᵢ(t+1) = f( xᵢ₋₁(t), xᵢ(t), xᵢ₊₁(t) )     con índices módulo N
```

La Regla 110 es el elemento 110 de la clasificación de Wolfram. Su comportamiento es
**irreversible y caótico**: a partir de configuraciones aparentemente simples genera estructuras
periódicas, Glue-like y, en Bandung, patrones de comportamiento Turing-completo.

### 3.2. La Regla 110 es Turing-completa

El resultado central (Cook, 2004) es que la Regla 110 es **Turing-completa**: existen
configuraciones iniciales finitas de la Regla 110 cuyas evoluciones simulan una Máquina de Turing
universal y su haltarla. En la práctica, Cook reduce la Regla 110 a un autómata de tags cíclico
(cyclic tag system), que es equivalente en poder de cómputo a una MT.

Este resultado fundamenta el interés académico del ejercicio: la MT que se implementa aquí es una
**simulación formal de un autómata que, en cierto modo, también es universal**. Simular la Regla 110
con una MT es simular —dos veces— un proceso Turing-completo.

### 3.3. Máquina de Turing

Una MT se define por `M = (Q, Σ, Γ, δ, q₀, q_acc, q_rej)` donde:

- `Q` es el conjunto finito de estados de control;
- `Σ` es el alfabeto de entrada;
- `Γ` es el alfabeto de cinta, con `Σ ⊆ Γ`;
- `δ : Q × Γ → Q × Γ × {L, R}` es la función de transición;
- `q₀` es el estado inicial, `q_acc` y `q_rej` los estados de parada.

Una configuración es `(q, w, h)` con `w` el contenido de la cinta y `h` la posición de la cabeza.
La computación consiste en aplicar `δ` hasta alcanzar `q_acc` o `q_rej`.

El diseño de este trabajo utiliza la convención de **cinta doblemente infinita** (índices en `ℤ`),
que simplifica la construcción del contador unario: las marcas `U` del contador crecen hacia la
izquierda del marcador inicial `⊢`, sin colisionar con la palabra de entrada.

### 3.4. Equivalencia con implementaciones directas

Una simulación de AC 1D escrita directamente con código imperativo (la *implementación de
referencia* de este trabajo) es Turing-completa en el mismo sentido que la MT, pero **no exhibe la
estructura formal** que el enunciado evalúa: no hay estados, ni tabla de transiciones, ni cinta
manipulada por una cabeza. La MT permite, en cambio:

- exhibir explícitamente la estructura de control y su tabla `δ`;
- verificar el comportamiento **paso a paso** (traza);
- razonar sobre el número de pasos y la complejidad de la simulación.

Por eso la implementación de referencia no se descarta: se conserva como **oráculo** para la
verificación cruzada descrita en la sección 7.

---

## 4. Especificación del problema

### 4.1. Entrada

```
<K_binario>#<estado_inicial>#
```

- `K_binario`: entero binario no vacío (el número de generaciones a calcular).
- `estado_inicial`: hilera binaria de longitud `N ≥ 8`.
- Se admiten exactamente tres segmentos separados por `#`, con el último vacío.

Ejemplos del enunciado: `100#000000010000000#` (K = 4) y `1110#01101001#` (K = 14).

### 4.2. Salida

- La cinta registra **cada generación calculada** como bloques binarios delimitados por `#`.
- Al agotarse el contador unario `K`, la máquina alcanza `q_acc` dejando visible la configuración
  final.
- Las respuestas inválidas se detectan en la capa de aplicación (HTTP 200 con `isValid = false`),
  conforme al contrato de la API.

### 4.3. Condiciones de borde

La vecindad es **cíclica**: la celda `0` tiene como vecino izquierdo a la celda `N-1`, y la celda
`N-1` tiene como vecino derecho a la celda `0`.

---

## 5. Diseño formal de la Máquina de Turing

### 5.1. La tupla

```
M = (Q, Σ, Γ, δ, q₀, q_acc, q_rej)
```

| Componente | Definición |
|---|---|
| `Σ` | `{0, 1, #}` — la palabra de entrada `K#celdas#` |
| `Γ` | `{0, 1, #, U, ⊢, _, x, y, ·}` — cinta |
| `q₀` | `q_init` |
| `q_acc` | `q_acc` (contador unario agotado) |
| `q_rej` | `q_rej` (entrada malformada) |

**Significado de los símbolos de cinta**

| Símbolo | Significado |
|---|---|
| `0`, `1` | estado de una celda del autómata |
| `#` | delimitador de bloques (campo binario / bloque de generación) |
| `U` | marca unaria del contador `K` |
| `⊢` | marcador del inicio de la palabra de entrada (borde izquierdo del contador) |
| `_` | dígito binario ya consumido (limpieza del campo `K`) |
| `x`, `y` | celda consumida que preserva su bit (`x` = 0, `y` = 1) durante la copia |
| `·` | blanco |

### 5.2. Disposición de la cinta

La cinta es doblemente infinita; el contenido inicial es `⊢` seguido de la palabra de entrada, y
el resto en blanco. La cabeza comienza en la posición 1 (primer dígito de `K`).

```
        contador unario        campo binario K        bloque gen0        historial
      ┌───────────────┐   ┌──────────────┐   ┌──────────────────┐   ┌──────────────────┐
... U U U │ ⊢ │ _ _ _ │ # │ b₀ b₁ … b_{N-1} │ # │ g₁ │ # │ g₂ │ # │ g₃ │ # ...
      └───────────────┘   └──────────────┘   └──────────────────┘   └──────────────────┘
          −K … −1          0   1 … m        m+1        m+N           m+N+1
```

- El **contador unario** ocupa las posiciones `-K … -1` y se consume desde la derecha.
- El **campo binario** ocupa `1 … m`; tras la conversión queda marcado con `_`.
- Cada **generación** es un bloque de `N` celdas binarias precedido por `#`; el bloque de la
  generación `t` sirve de origen y el siguiente bloque (a su derecha) de destino.
- El delimitador `#` que cierra cada bloque es el **landmark** que permite a la máquina localizar
  los extremos del bloque sin necesidad de contar: es la razón por la que el conjunto de estados
  no depende de `N` ni de `K`.

### 5.3. Conjunto de estados `Q` por módulos

76 estados repartidos en siete módulos lógicos:

| Módulo | Estados | Transiciones | Función |
|---|---|---|---|
| **M0 · Inicialización y validación** | 4 | 19 | Recorre `K#celdas#`, valida los símbolos y la presencia de los delimitadores. |
| **M1 · Conversión binario → unario** | 8 | 24 | Decremento binario con préstamo y append de una marca `U` por unidad. |
| **M2 · Limpieza del campo binario** | 2 | 6 | Sustituye los dígitos consumidos por `_`. |
| **M3 · Copia de la generación al historial** | 11 | 35 | Copia el bloque origen al bloque destino, marcando las celdas origen como consumidas. |
| **M4 · Restauración del bloque copiado** | 2 | 6 | Devuelve `x`/`y` a `0`/`1`, dejando intacto el bloque del historial. |
| **M5 · Lectura de vecindad y escritura de `t+1`** | 41 | 88 | Aplica `f` de la Regla 110 sobre cada vecindad y escribe la nueva generación. |
| **M6 · Contador unario y control de iteraciones** | 8 | 27 | Decrementa el contador, decide si hay otra generación y reencuadra la cabeza. |

### 5.4. La función de transición `δ`

La tabla completa se exporta en `tabla-transiciones-maquina-turing.txt` y se consulta en la
aplicación web. Se describe a continuación la estructura de las familias de estados.

#### 5.4.1. Ciclo de cálculo (M5)

El barrido se realiza **in situ** sobre el bloque destino, de izquierda a derecha. El ciclo de
trabajo de cada celda tiene **tres pasos**:

| Paso | Estado | Posición de la cabeza | Lee | Escribe | Se mueve a | Observación |
|---|---|---|---|---|---|---|
| 1 | `q_calc_s[l,c,b0]` | celda `i` | `c = bᵢ` | `c` | `i+1` | el control conoce `l = bᵢ₋₁` y `b0` |
| 2 | `q_calc_r[l,c,b0]` | celda `i+1` | `n = bᵢ₊₁` | `n` | `i` | se lee el vecino derecho |
| 3 | `q_calc_w[l,c,n,b0]` | celda `i` | `c` | **`f(l,c,n)`** | `i+1` | el control queda `l' = c`, `c' = n` |

Los 32 estados de estas tres familias se generan paramétricamente
(`l`, `c`, `n`, `b0 ∈ {0,1}`), de modo que la tabla refleja literalmente las 8 reglas locales de la
Regla 110.

#### 5.4.2. Frontera cíclica

La condición de borde se resuelve **dentro del control finito**:

- **Vecino derecho de la última celda:** al alcanzar el delimitador `#` que cierra el bloque, la
  transición `q_calc_r[l,c,b0] —#→ q_calc_w[l,c,b0,b0]` usa el valor `b0` (la primera celda de la
  generación) en lugar del delimitador. Ese `b0` viaja memorizado en el sufijo `b0` de cada estado
  paramétrico.
- **Vecino izquierdo de la primera celda:** la subrutina de preparación
  (`q_calc_prep_*`) recorre el bloque destino, lee `b0` y `b_{N-1}` y arranca el barrido en el
  estado `q_calc_s[l=b_{N-1}, c=b0, b0=b0]`.

De este modo, **ningún estado depende de `N`**: la topología de anillo se implementa con dos bits
de control, no con una convención del lenguaje de programación.

#### 5.4.3. Copia al historial y preservación de estados

Para registrar cada generación sin destruir la anterior, la máquina:

1. **copia** el bloque origen al bloque destino; en el trayecto marca cada celda origen como
   consumida (`0 → x`, `1 → y`), lo que preserva el bit y marca el avance;
2. **restaura** las celdas origen (`x → 0`, `y → 1`) dejándolas intactas en el historial;
3. **calcula** la nueva generación in situ sobre la copia.

El mecanismo de marcado cumple una segunda función: durante el recorrido de regreso, la máquina
localiza la próxima celda origen leyendo las marcas `x`/`y`, sin necesidad de contar.

#### 5.4.4. Conversión binario → unario y contador

El módulo M1 implementa el algoritmo clásico de conversión por decremento repetido:

```
mientras K ≠ 0:
    K ← K − 1              (decremento binario con préstamo: q_conv_dec)
    contador ← contador + 1 marca U   (q_conv_append / q_conv_append_u)
```

La conversión es *O(K · (m + K))* pasos. El contador unario resultante se consume en el módulo M6:
en cada iteración se borra la marca `U` más a la derecha; cuando ya no queda ninguna, la máquina
transiciona a `q_acc` con la configuración final visible, tal como pide el enunciado.

### 5.5. Configuración inicial

```
cinta :  ⊢ K₁ K₂ … K_m # c₁ c₂ … c_N # · · · …
cabeza: 1
estado : q_init
```

donde `K₁…K_m` es la escritura binaria de `K` y `c₁…c_N` la configuración inicial del autómata.

---

## 6. Implementación

### 6.1. Arquitectura del software

```
Rule110/                          aplicación web (ASP.NET Core Minimal API)
  Program.cs                      orquestación HTTP; no contiene lógica de simulación
  Generation.cs                   DTOs compartidos (Generation, SimulationResult)
  TapeInput.cs                    parseo y validación del formato de entrada
  Rule110Oracle.cs                implementación de referencia de la Regla 110 (oráculo)
  Turing/
    Tape.cs                       cinta de índice entero, doblemente infinita
    TransitionTable.cs            tabla δ como dato + volcado a texto
    TuringMachine.cs              motor genérico (estado, cabeza, δ, traza, tope de pasos)
    Rule110TuringProgram.cs       construcción declarativa de la tabla δ
    Rule110TuringMachine.cs       fachada: prepara la cinta, ejecuta y extrae generaciones
  Verification/
    Rule110CrossValidator.cs      verificación cruzada MT ↔ oráculo
    Rule110TestCatalog.cs         catálogo de casos y generación del reporte
    Rule110Diagnostics.cs         utilidades de inspección de la traza
  wwwroot/                        interfaz estática (HTML, CSS, JS sin dependencias)

Rule110.TestRunner/               runner de consola (genera resultados-regla110.txt)
docs/                             informe y tabla de transiciones
scripts/                          empaquetado y verificación
```

El motor de MT (`TuringMachine`) es **agnóstico del problema**: recibe una `TransitionTable` y
ejecuta pasos hasta `q_acc`/`q_rej` o hasta el tope configurado, registrando la traza. La tabla δ
de la Regla 110 se genera en `Rule110TuringProgram` mediante familias paramétricas, de modo que
cambiar una regla local o agregar un módulo es una edición localizada.

### 6.2. Contrato de la API

| Endpoint | Descripción |
|---|---|
| `POST /api/turing/simulate` | Ejecuta la MT. Acepta `?trace=true` para incluir la traza. Devuelve `isValid`, `errorMessage`, `steps` (K+1 generaciones) y `machine` (estado final, pasos, traza, cinta). |
| `GET /api/turing/table` | Devuelve la tabla `δ` completa y la descripción formal de la máquina. |
| `GET /api/turing/tests` | Ejecuta el catálogo de pruebas y devuelve el reporte (mismo contenido que el `.txt` de entrega). |

Las entradas inválidas se responden con **HTTP 200** e `isValid = false`, conforme al contrato
original del proyecto.

### 6.3. Interfaz web

La interfaz editorial existente se amplía con un cuarto bloque, **“Máquina de Turing”**, que incluye:

- el resumen de la corrida (estado final, pasos, cantidad de generaciones, tamaño de `δ`);
- la **tabla `δ`** con filtro por módulo y buscador de texto;
- la **traza de ejecución** (paso, estado, cabeza, símbolo leído, símbolo escrito, movimiento,
  estado siguiente);
- la **cinta final**, donde se observan los bloques de cada generación y el contador unario.

La aplicación admite parámetros de URL `?tape=…&auto=1&trace=1` para reproducir un caso
concreto desde un enlace.

---

## 7. Metodología de verificación

La verificación se estructura en tres niveles complementarios:

### 7.1. Casos con resultado esperado (nivel 1)

Cada caso fija la **entrada**, el número de iteraciones, la longitud de la cinta, el **estado final
esperado** y una descripción del fenómeno que ejercita. El resultado esperado proviene del
enunciado (casos 1 a 3), de la consigna extendida a `N = 8` (caso 10) o de la implementación de
referencia, verificada contra la MT.

### 7.2. Verificación cruzada (nivel 2)

Para **100 configuraciones aleatorias** (semilla fija `20260916`, `N ∈ [8, 32]`, `K ∈ [0, 5]`) se
compara, generación a generación, el resultado de la Máquina de Turing con el de la implementación
de referencia. Este nivel detecta desvíos en comportamientos no contemplados por los casos
explícitos (combinaciones poco frecuentes de vecindades y tamaños de cinta).

### 7.3. Entradas inválidas (nivel 3)

Se comprueba que el parser rechaza, con el mensaje correspondiente: `N < 8`, celdas no binarias,
`K` no binario, segmentos vacíos, delimitador final ausente y `K` fuera del rango de `Int32`.

### 7.4. Herramientas

| Componente | Uso |
|---|---|
| `Rule110.TestRunner` | Ejecuta el catálogo, escribe `resultados-regla110.txt` y `tabla-transiciones-maquina-turing.txt`, devuelve código de salida `0` si todo pasa. |
| `GET /api/turing/tests` | El mismo catálogo ejecutado desde la web, útil para demostración. |
| Inspección de traza | `Rule110Diagnostics.Describe(entrada)` permite volcar la traza completa de una corrida. |

---

## 8. Resultados

### 8.1. Casos de prueba

`resultados-regla110.txt` contiene **un caso por línea**. Extracto:

| Caso | Entrada | K | N | Estado final | Pasos MT |
|---|---|---|---|---|---|
| 1 | `11#000100#` | 3 | 6 | `110100` | 699 |
| 2 | `100#000000010000000#` | 4 | 15 | `000111110000000` | 3 191 |
| 3 | `1110#01101001#` | 14 | 8 | `01101110` | 6 229 |
| 4 | `0#00000000#` | 0 | 8 | `00000000` | 37 |
| 5 | `1#00000000#` | 1 | 8 | `00000000` | 323 |
| 8 | `1#00000001#` | 1 | 8 | `00000011` | 323 |
| 9 | `1000#01010101#` | 8 | 8 | `00000000` | 3 055 |
| 11 | `1#10101010#` | 1 | 8 | `11111111` | 323 |
| 14 | `1111#000100010001000100010001#` | 15 | 24 | `110111011101110111011101` | 29 427 |
| 15 | `100000#000…0001#` | 32 | 32 | `01111110110111110101101000011111` | 118 355 |

**Resultado global: 27/27 casos correctos** (incluidos la verificación cruzada de 100
configuraciones aleatorias y las 6 entradas inválidas rechazadas correctamente).

El caso 1 reproduce exactamente la secuencia del enunciado:

```
Gen 0: 000100
Gen 1: 001100
Gen 2: 011100
Gen 3: 110100
```

### 8.2. Estructura de la tabla de transiciones

| Módulo | Transiciones | Estados |
|---|---|---|
| M0 · Inicialización y validación | 19 | 4 |
| M1 · Conversión binario → unario | 24 | 8 |
| M2 · Limpieza del campo binario | 6 | 2 |
| M3 · Copia al historial | 35 | 11 |
| M4 · Restauración del bloque copiado | 6 | 2 |
| M5 · Lectura de vecindad y escritura de `t+1` | 88 | 41 |
| M6 · Contador unario y control | 27 | 8 |
| **Total** | **205** | **76** |

El 43 % de las transiciones corresponde al núcleo de la Regla 110 (M5), que es parametrizado: las
ocho reglas locales se obtienen de una única familia de estados.

### 8.3. Complejidad

Sea `N` la longitud de la cinta y `K` el número de generaciones.

- **Conversión binario → unario:** `O(K · (m + K))` pasos, con `m = O(log K)`.
- **Copia de una generación al historial:** `O(N²)` pasos (cada celda requiere un recorrido de ida
  y vuelta entre el bloque origen y el destino).
- **Cálculo de una generación:** `O(N)` pasos (tres pasos por celda).
- **Recorridos de control:** `O(N + K)` por generación.
- **Total:** `O(N² · K + K²)` pasos.

En una variante **multicinta** (contador, bloque de celdas e historial en cintas separadas) el
mismo algoritmo se ejecuta en `O(N · K)` pasos, porque las cabezas independientes eliminan el
recorrido de ida y vuelta. La variante de cinta única fue elegida por el enunciado, que evalúa la
estructura de la tabla de transiciones de una MT clásica; el análisis comparado se incluye como
observación de diseño.

---

## 9. Discusión

### 9.1. Por qué cinta única

Una MT multicinta simplifica el diseño algorítmico y mejora la complejidad, pero la tabla `δ`
deja de ser una terna clásica `(estado, símbolo) → (estado, símbolo, dirección)` y pasa a ser una
relación sobre la tupla de símbolos leídos en cada cinta. El enunciado evalúa explícitamente la
**claridad y estructura de la tabla de transiciones de la Máquina de Turing**, por lo que se optó
por la formulación de cinta única, que además reproduce la definición clásica de MT.

### 9.2. El costo de la frontera cíclica

El requisito más difícil de la consigna no es la Regla 110 sino la **topología de anillo**: en una
cinta lineal, la celda `0` y la celda `N-1` no son vecinas. La solución adoptada —memorizar `b0`
y `b_{N-1}` en el control finito— tiene dos consecuencias favorables:

1. el conjunto de estados **no crece con `N`** (la MT sigue siendo finita y sus estados son
   paramétricos en dos bits);
2. el comportamiento cíclico queda **explícito en la tabla `δ`**, auditable desde la interfaz.

La alternativa habitual —usar el módulo aritmético del lenguaje para acceder a
`cells[(i-1+N)%N]`— produce el mismo resultado pero oculta la condición de borde detrás de una
convención, lo que impide asignar al modelo el mérito de resolverla.

### 9.3. Conservación de la generación anterior

Registrar cada generación sin destruir la anterior exige, en cinta única, marcar el bloque origen
durante la copia. El marcado con `x`/`y` (que preservan el bit) cumple dos funciones: permite
restaurar el bloque y permite localizar el avance del recorrido sin contar. Es un ejemplo del
interés de las marcas_auxiliares en la construcción de MT: sin ellas, la máquina necesitaría un
contador adicional en el control o un registro en la cinta.

### 9.4. Limitaciones

- La simulación es cuadrática en `N` por generación; para cintas de miles de celdas conviene la
  variante multicinta.
- El tope de pasos (40 000 000) protege contra entradas patológicas; al alcanzarlo la API informa
  que la máquina no llegó al estado de aceptación.
- La validación de `N ≥ 8` se realiza en la capa de aplicación; la MT en sí opera con cualquier
  `N ≥ 1` (de hecho, el caso 1 del enunciado, de `N = 6`, se ejecuta en el runner).

---

## 10. Conclusiones

1. Se implementó una **Máquina de Turing formal** para la Regla 110 con frontera cíclica, con una
   tabla `δ` de 205 transiciones sobre 76 estados, agrupada en siete módulos funcionales y
   exportada como parte de la entrega.
2. El **contador unario** exigido por el enunciado se implementa como submódulo de la propia
   máquina, incluida la conversión binario → unario con decremento y préstamo.
3. La **frontera cíclica** se resuelve en el control finito de la máquina, de modo que la tabla
   `δ` refleja explícitamente el tratamiento del anillo.
4. La verificación arrojó **27/27 casos correctos**, con coincidencia exacta entre la MT y la
   implementación de referencia sobre 100 configuraciones aleatorias adicionales.
5. La formalización aporta beneficios que la versión directa no ofrece: trazabilidad paso a paso,
   tabla de transiciones auditable, complejidad argumentable y una base sólida para extender el
   modelo (por ejemplo, a otras reglas elementales o a fronteras reflectantes).

Como perspectiva, el mismo esqueleto —alfabetos, módulos de estados, tabla `δ` parametrizada y
verificación cruzada— puede reutilizarse para simular cualquier AC 1D elemental, y el trabajo
de Cook (2004) recuerda que la Regla 110 ocupa un lugar particular: es capaz de simular, ella
misma, a una Máquina de Turing.

---

## 11. Referencias bibliográficas

- Cook, M. (2004). *A universal cellular automaton*. Small Machines, 2.
- Hopcroft, J. E., Motwani, R. y Ullman, J. D. (2006). *Introduction to Automata Theory, Languages,
  and Computation* (3.ª ed.). Addison-Wesley. Capítulos 2 y 3.
- Sipser, M. (2012). *Introduction to the Theory of Computation* (3.ª ed.). Cengage Learning.
  Capítulos 3 y 4.
- Turing, A. M. (1936). On computable numbers, with an application to the Entscheidungsproblem.
  *Proceedings of the London Mathematical Society*, s2-22(1), 447–458.
- Wolfram, S. (2002). *A New Kind of Science*. Wolfram Media. Capítulo 4 (reglas elementales).
- Michiel, H. (s. f.). *Rule 110*. Wolfram MathWorld.
- Sutton, J. (2019). *An Introduction to the Theory of Computation*. University of Cambridge.
- Documento de la cátedra: *Trabajo Práctico Final — Simulación de un Autómata Celular
  Unidimensional (Regla 110) con Máquina de Turing* (`TP_integrador_final.pdf`).

---

## Anexo A. Ejecución del proyecto

```powershell
# Compilación
dotnet build Rule110/Rule110.csproj
dotnet build Rule110.TestRunner/Rule110.TestRunner.csproj

# Aplicación web -> http://localhost:5000
dotnet run --project Rule110

# Casos de prueba: escribe resultados-regla110.txt y
# tabla-transiciones-maquina-turing.txt en el directorio actual
dotnet run --project Rule110.TestRunner
```

> **Nota de entorno.** En equipos con política de integridad de código (WDAC) activa, la
> ejecución de ensamblados recién compilados y sin firma puede ser bloqueada por el sistema
> (`FileLoadException` / “Una directiva de Control de aplicaciones bloqueó este archivo”). No es un
> defecto del proyecto: la verificación y el runner siguen siendo válidos y pueden ejecutarse con
> `dotnet run -c Release --project Rule110.TestRunner` o desde la web con `GET /api/turing/tests`.

## Anexo B. Archivos de la entrega

| Archivo | Contenido |
|---|---|
| `resultados-regla110.txt` | un caso de prueba por línea, con el resultado de cada uno |
| `tabla-transiciones-maquina-turing.txt` | tabla `δ` completa, agrupada por módulo |
| `docs/INFORME.md` | este informe |
| `Rule110/` | código fuente de la aplicación web y de la Máquina de Turing |
| `Rule110.TestRunner/` | runner de casos de prueba |
| `scripts/empaquetar.ps1` | genera el `.zip` de entrega sin `bin/` ni `obj/` |
