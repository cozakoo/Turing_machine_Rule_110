# Regla 110 · Simulador mediante Máquina de Turing

Trabajo Práctico Final: simulación de un autómata celular unidimensional con la **Regla 110** y
frontera cíclica, implementada como una **Máquina de Turing formal** con tabla de transiciones
explícita.

| | |
|---|---|
| **Integrantes** | Arcos Martín · Cardenas Mauricio |
| **Informe técnico** | [`docs/INFORME.md`](docs/INFORME.md) |
| **Stack** | C# 12 · .NET 8 · ASP.NET Core Minimal API · HTML/CSS/JS sin dependencias |

## Qué hace

La aplicación **ejecuta una Máquina de Turing** (cinta única, δ : Q × Γ → Q × Γ × {L,R}) que:

1. lee la entrada `K_binario#celdas#` de la cinta;
2. convierte `K` de binario a **unario** y lo consume como contador de generaciones;
3. calcula `K` generaciones de la Regla 110 con **frontera cíclica**, dejando cada generación
   registrada en la cinta como bloque delimitado por `#`;
4. se detiene en `q_acc` con la configuración final visible.

La tabla `δ` (205 transiciones sobre 76 estados, agrupadas en 7 módulos) se puede consultar y
exportar desde la propia aplicación.

## Requisitos

- **.NET SDK 8.x** (`dotnet --list-sdks`).
- Sin dependencias NuGet externas, sin Node, sin Docker, sin base de datos.

> Si `dotnet` no está en el `PATH` (típico en equipos corporativos), usá
> `& "C:\Program Files\dotnet\dotnet.exe"` con el resto de los argumentos.

## Ejecución

```powershell
# Compilar
dotnet build Rule110/Rule110.csproj
dotnet build Rule110.TestRunner/Rule110.TestRunner.csproj

# Aplicación web -> http://localhost:5000
dotnet run --project Rule110

# Casos de prueba: escribe resultados-regla110.txt y
# tabla-transiciones-maquina-turing.txt en el directorio actual
dotnet run --project Rule110.TestRunner
```

Salida esperada del runner: `Casos correctos: 27/27` y exit code `0`.

### Endpoints

| Endpoint | Descripción |
|---|---|
| `POST /api/turing/simulate` | Ejecuta la MT. `?trace=true` agrega la traza paso a paso. |
| `GET /api/turing/table` | Tabla `δ` completa y descripción formal de la máquina. |
| `GET /api/turing/tests` | Corre el catálogo de pruebas y devuelve el reporte. |

La web admite `?tape=…&auto=1&trace=1` para reproducir un caso concreto desde un enlace.

### Nota de entorno (WDAC)

En equipos con política de Control de aplicaciones activa, un ensamblado recién compilado y sin
firma puede ser bloqueado al cargarlo (`FileLoadException: ... una directiva de Control de
aplicaciones bloqueó este archivo`). No es un defecto del proyecto. Workarounds:
`dotnet run -c Release --project Rule110.TestRunner`, recompilar hasta que el binario sea aceptado,
o ejecutar el catálogo desde la web con `GET /api/turing/tests`.

## Estructura

```
Rule110/                     aplicación web + núcleo de la Máquina de Turing
  Program.cs                 Minimal API (orquesta HTTP; sin lógica de simulación)
  TapeInput.cs               parseo y validación de la entrada
  Rule110Oracle.cs           implementación de referencia (oráculo de la verificación)
  Turing/                     motor genérico de MT + tabla δ de la Regla 110
  Verification/               verificación cruzada, catálogo de casos y diagnósticos
  wwwroot/                    interfaz estática (HTML, CSS, JS)
Rule110.TestRunner/          runner de casos de prueba
docs/                        informe técnico y tabla de transiciones
scripts/empaquetar.ps1       genera dist/tp-integrador-regla110.zip
```

## Entregables

| Archivo | Contenido |
|---|---|
| `docs/INFORME.md` | informe técnico: diseño formal, metodología, resultados, conclusiones y referencias |
| `resultados-regla110.txt` | casos de prueba, uno por línea, con el resultado de cada uno |
| `tabla-transiciones-maquina-turing.txt` | tabla `δ` completa, agrupada por módulo |
| `dist/tp-integrador-regla110.zip` | paquete de entrega generado por `scripts/empaquetar.ps1` |

## Verificación

El catálogo cubre los casos del enunciado, casos borde (`N = 8`, `K = 0`, `K = 1`), configuraciones
uniformes, periódicas y caóticas, entradas inválidas y una **verificación cruzada** de 100
configuraciones aleatorias contra la implementación de referencia.
