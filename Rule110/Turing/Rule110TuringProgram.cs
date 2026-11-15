namespace Rule110.Turing;

/// <summary>
/// Construye la tabla de transiciones δ de la Máquina de Turing que simula
/// el autómata celular unidimensional de la Regla 110 con frontera cíclica.
///
/// Disposición de la cinta (índice entero, cinta doblemente infinita):
///
///   ... U U U | ⊢ | _ _ _ | # | g₀ | # | g₁ | # | g₂ | # ...
///        ↑     ↑     ↑     ↑    ↑                   ↑      ↑
///   contador  left  campo sep  bloque              bloque  bloque
///   unario  end   binario    gen0                  gen1    gen2
///              (K)
/// </summary>
public static class Rule110TuringProgram
{
    public const string InitialState = "q_init";
    public const string AcceptState = Rule110States.Accept;
    public const string RejectState = Rule110States.Reject;

    public const char Separator = '#';
    public const char UnaryMark = 'U';
    public const char LeftEnd = '⊢';
    public const char ConsumedDigit = '_';
    public const char ConsumedZero = 'x';
    public const char ConsumedOne = 'y';

    private const string ModuleInit = "M0 · Inicialización y validación";
    private const string ModuleConvert = "M1 · Conversión binario → unario";
    private const string ModuleClean = "M2 · Limpieza del campo binario";
    private const string ModuleCopy = "M3 · Copia de la generación al historial";
    private const string ModuleRestore = "M4 · Restauración del bloque copiado";
    private const string ModuleCalc = "M5 · Lectura de vecindad y escritura de t+1";
    private const string ModuleCounter = "M6 · Contador unario y control de iteraciones";

    public static TransitionTable BuildTable()
    {
        var builder = new TransitionTableBuilder();

        BuildInitialization(builder);
        BuildConversion(builder);
        BuildCleanup(builder);
        BuildCopy(builder);
        BuildRestore(builder);
        BuildCalculation(builder);
        BuildCounter(builder);

        return builder.Build();
    }

    /// <summary>
    /// Función de transición local de la Regla 110 evaluada sobre
    /// (izquierda, centro, derecha).
    /// </summary>
    public static char ApplyRule(char left, char center, char right) =>
        (left, center, right) switch
        {
            ('1', '1', '1') => '0',
            ('1', '1', '0') => '1',
            ('1', '0', '1') => '1',
            ('1', '0', '0') => '0',
            ('0', '1', '1') => '1',
            ('0', '1', '0') => '1',
            ('0', '0', '1') => '1',
            ('0', '0', '0') => '0',
            _ => throw new InvalidOperationException("Vecindad inválida.")
        };

    private static void BuildInitialization(TransitionTableBuilder builder)
    {
        builder.Add("q_init", '0', "q_bin_scan", '0', HeadMove.Right, ModuleInit);
        builder.Add("q_init", '1', "q_bin_scan", '1', HeadMove.Right, ModuleInit);
        builder.Add("q_init", '#', RejectState, '#', HeadMove.Right, ModuleInit);
        builder.Add("q_init", LeftEnd, RejectState, LeftEnd, HeadMove.Right, ModuleInit);
        builder.Add("q_init", '·', RejectState, Tape.Blank, HeadMove.Right, ModuleInit);

        builder.Add("q_bin_scan", '0', "q_bin_scan", '0', HeadMove.Right, ModuleInit);
        builder.Add("q_bin_scan", '1', "q_bin_scan", '1', HeadMove.Right, ModuleInit);
        builder.Add("q_bin_scan", '#', "q_cells_scan", '#', HeadMove.Right, ModuleInit);
        builder.Add("q_bin_scan", '·', RejectState, Tape.Blank, HeadMove.Right, ModuleInit);
        builder.Add("q_bin_scan", LeftEnd, RejectState, LeftEnd, HeadMove.Right, ModuleInit);

        builder.Add("q_cells_scan", '0', "q_cells_scan", '0', HeadMove.Right, ModuleInit);
        builder.Add("q_cells_scan", '1', "q_cells_scan", '1', HeadMove.Right, ModuleInit);
        builder.Add("q_cells_scan", '#', "q_end_check", '#', HeadMove.Right, ModuleInit);
        builder.Add("q_cells_scan", '·', RejectState, Tape.Blank, HeadMove.Right, ModuleInit);
        builder.Add("q_cells_scan", LeftEnd, RejectState, LeftEnd, HeadMove.Right, ModuleInit);

        builder.Add("q_end_check", '·', "q_conv_goto0", Tape.Blank, HeadMove.Right, ModuleInit);
        builder.Add("q_end_check", '#', RejectState, '#', HeadMove.Right, ModuleInit);
        builder.Add("q_end_check", '0', RejectState, '0', HeadMove.Right, ModuleInit);
        builder.Add("q_end_check", '1', RejectState, '1', HeadMove.Right, ModuleInit);
    }

    private static void BuildConversion(TransitionTableBuilder builder)
    {
        // Regreso al inicio del campo binario (justo a la derecha de ⊢).
        builder.Add("q_conv_goto0", '0', "q_conv_goto0", '0', HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_goto0", '1', "q_conv_goto0", '1', HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_goto0", '#', "q_conv_goto0", '#', HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_goto0", '·', "q_conv_goto0", Tape.Blank, HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_goto0", LeftEnd, "q_conv_scan0", LeftEnd, HeadMove.Right, ModuleConvert);

        // Barrido del campo binario: se busca un dígito 1 (valor no nulo).
        builder.Add("q_conv_scan0", '0', "q_conv_scan0", '0', HeadMove.Right, ModuleConvert);
        builder.Add("q_conv_scan0", '1', "q_conv_scan1", '1', HeadMove.Right, ModuleConvert);
        builder.Add("q_conv_scan0", '#', "q_clean_goto0", '#', HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_scan1", '0', "q_conv_scan1", '0', HeadMove.Right, ModuleConvert);
        builder.Add("q_conv_scan1", '1', "q_conv_scan1", '1', HeadMove.Right, ModuleConvert);
        builder.Add("q_conv_scan1", '#', "q_conv_dec", '#', HeadMove.Left, ModuleConvert);

        // Decremento binario con préstamo (sobre el dígito menos significativo).
        builder.Add("q_conv_dec", '1', "q_conv_append", '0', HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_dec", '0', "q_conv_dec", '1', HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_dec", LeftEnd, RejectState, LeftEnd, HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_dec", '·', RejectState, Tape.Blank, HeadMove.Left, ModuleConvert);

        // Append de una marca unaria al contador (crece hacia la izquierda).
        builder.Add("q_conv_append", '0', "q_conv_append", '0', HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_append", '1', "q_conv_append", '1', HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_append", LeftEnd, "q_conv_at_counter", LeftEnd, HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_at_counter", UnaryMark, "q_conv_append_u", UnaryMark, HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_at_counter", '·', "q_conv_back", UnaryMark, HeadMove.Right, ModuleConvert);
        builder.Add("q_conv_append_u", UnaryMark, "q_conv_append_u", UnaryMark, HeadMove.Left, ModuleConvert);
        builder.Add("q_conv_append_u", '·', "q_conv_back", UnaryMark, HeadMove.Right, ModuleConvert);

        // Regreso al inicio del campo binario por la izquierda del contador.
        builder.Add("q_conv_back", UnaryMark, "q_conv_back", UnaryMark, HeadMove.Right, ModuleConvert);
        builder.Add("q_conv_back", LeftEnd, "q_conv_scan0", LeftEnd, HeadMove.Right, ModuleConvert);
    }

    private static void BuildCleanup(TransitionTableBuilder builder)
    {
        builder.Add("q_clean_goto0", '0', "q_clean_goto0", '0', HeadMove.Left, ModuleClean);
        builder.Add("q_clean_goto0", '1', "q_clean_goto0", '1', HeadMove.Left, ModuleClean);
        builder.Add("q_clean_goto0", LeftEnd, "q_clean_erase", LeftEnd, HeadMove.Right, ModuleClean);

        builder.Add("q_clean_erase", '0', "q_clean_erase", ConsumedDigit, HeadMove.Right, ModuleClean);
        builder.Add("q_clean_erase", '1', "q_clean_erase", ConsumedDigit, HeadMove.Right, ModuleClean);
        builder.Add("q_clean_erase", '#', "q_gen_check_start", '#', HeadMove.Right, ModuleClean);
    }

    private static void BuildCopy(TransitionTableBuilder builder)
    {
        // Lectura y marcado de la celda origen.
        builder.Add("q_copy_next", '0', "q_copy_mark0", ConsumedZero, HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_next", '1', "q_copy_mark1", ConsumedOne, HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_next", '#', "q_copy_tail", '#', HeadMove.Right, ModuleCopy);

        // Recorrido del bloque origen hacia el delimitador '#'.
        builder.Add("q_copy_mark0", '0', "q_copy_mark0", '0', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_mark0", '1', "q_copy_mark0", '1', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_mark0", '#', "q_copy_slot0", '#', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_mark1", '0', "q_copy_mark1", '0', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_mark1", '1', "q_copy_mark1", '1', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_mark1", '#', "q_copy_slot1", '#', HeadMove.Right, ModuleCopy);

        // Búsqueda del próximo hueco del bloque destino (primer blanco).
        builder.Add("q_copy_slot0", '0', "q_copy_slot0", '0', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_slot0", '1', "q_copy_slot0", '1', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_slot0", '·', "q_copy_back0", '0', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_slot1", '0', "q_copy_slot1", '0', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_slot1", '1', "q_copy_slot1", '1', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_slot1", '·', "q_copy_back1", '1', HeadMove.Left, ModuleCopy);

        // Regreso al bloque origen atravesando el delimitador '#'.
        builder.Add("q_copy_back0", '0', "q_copy_back0", '0', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_back0", '1', "q_copy_back0", '1', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_back0", '#', "q_copy_find0", '#', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_back1", '0', "q_copy_back1", '0', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_back1", '1', "q_copy_back1", '1', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_back1", '#', "q_copy_find1", '#', HeadMove.Left, ModuleCopy);

        // Regreso hasta la celda marcada que delimita el avance del origen.
        builder.Add("q_copy_find0", '0', "q_copy_find0", '0', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_find0", '1', "q_copy_find0", '1', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_find0", ConsumedZero, "q_copy_next", ConsumedZero, HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_find0", ConsumedOne, "q_copy_next", ConsumedOne, HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_find1", '0', "q_copy_find1", '0', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_find1", '1', "q_copy_find1", '1', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_find1", ConsumedZero, "q_copy_next", ConsumedZero, HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_find1", ConsumedOne, "q_copy_next", ConsumedOne, HeadMove.Right, ModuleCopy);

        // Cierre del bloque destino con su delimitador '#'.
        builder.Add("q_copy_tail", '0', "q_copy_tail", '0', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_tail", '1', "q_copy_tail", '1', HeadMove.Right, ModuleCopy);
        builder.Add("q_copy_tail", '·', "q_copy_restore", '#', HeadMove.Left, ModuleCopy);

        // Regreso al bloque origen para restaurarlo.
        builder.Add("q_copy_restore", '0', "q_copy_restore", '0', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_restore", '1', "q_copy_restore", '1', HeadMove.Left, ModuleCopy);
        builder.Add("q_copy_restore", '#', "q_restore", '#', HeadMove.Left, ModuleCopy);
    }

    private static void BuildRestore(TransitionTableBuilder builder)
    {
        builder.Add("q_restore", ConsumedZero, "q_restore", '0', HeadMove.Left, ModuleRestore);
        builder.Add("q_restore", ConsumedOne, "q_restore", '1', HeadMove.Left, ModuleRestore);
        builder.Add("q_restore", '#', "q_restore_return", '#', HeadMove.Right, ModuleRestore);

        builder.Add("q_restore_return", '0', "q_restore_return", '0', HeadMove.Right, ModuleRestore);
        builder.Add("q_restore_return", '1', "q_restore_return", '1', HeadMove.Right, ModuleRestore);
        builder.Add("q_restore_return", '#', "q_calc_prep_start", '#', HeadMove.Right, ModuleRestore);
    }

    private static void BuildCalculation(TransitionTableBuilder builder)
    {
        // Módulo de cálculo: barrido in situ sobre el bloque destino.
        //
        // Ciclo de tres pasos por celda:
        //   q_calc_s[l,c,b0]   : cabeza en la celda i; el control conoce l = b(i-1) y c = b(i).
        //                         Se avanza a i+1.
        //   q_calc_r[l,c,b0]   : cabeza en i+1; se lee n = b(i+1) y se vuelve a i.
        //   q_calc_w[l,c,n,b0] : cabeza en i; se escribe f(l,c,n) y se avanza a i+1, donde
        //                         el control queda l' = c y c' = n para la celda siguiente.
        //
        // El delimitador '#' que cierra el bloque sustituye al vecino derecho por b0
        // (borde cíclico derecho). El vecino izquierdo de la primera celda es b(N-1),
        // memorizado durante la preparación.

        foreach (var left in Bits)
        {
            foreach (var current in Bits)
            {
                foreach (var b0 in Bits)
                {
                    foreach (var symbol in Bits)
                    {
                        builder.Add(
                            SweepStart(left, current, b0),
                            symbol,
                            SweepRead(left, current, b0),
                            symbol,
                            HeadMove.Right,
                            ModuleCalc);
                    }
                }
            }
        }

        foreach (var left in Bits)
        {
            foreach (var current in Bits)
            {
                foreach (var b0 in Bits)
                {
                    foreach (var next in Bits)
                    {
                        builder.Add(
                            SweepRead(left, current, b0),
                            next,
                            SweepWrite(left, current, next, b0),
                            next,
                            HeadMove.Left,
                            ModuleCalc);
                    }

                    // Cierre cíclico: el delimitador '#' reemplaza al vecino derecho por b0.
                    builder.Add(
                        SweepRead(left, current, b0),
                        '#',
                        SweepWrite(left, current, b0, b0),
                        '#',
                        HeadMove.Left,
                        ModuleCalc);
                }
            }
        }

        foreach (var left in Bits)
        {
            foreach (var current in Bits)
            {
                foreach (var next in Bits)
                {
                    foreach (var b0 in Bits)
                    {
                        builder.Add(
                            SweepWrite(left, current, next, b0),
                            current,
                            SweepStart(current, next, b0),
                            ApplyRule(left, current, next),
                            HeadMove.Right,
                            ModuleCalc);
                    }
                }
            }
        }

        // El barrido termina al alcanzar el delimitador que cierra el bloque destino.
        foreach (var left in Bits)
        {
            foreach (var current in Bits)
            {
                foreach (var b0 in Bits)
                {
                    builder.Add(
                        SweepStart(left, current, b0),
                        '#',
                        "q_gen_check_start",
                        '#',
                        HeadMove.Right,
                        ModuleCalc);
                }
            }
        }

        BuildCalculationPreparation(builder);
    }
    private static void BuildCalculationPreparation(TransitionTableBuilder builder)
    {
        // q_calc_prep_start: lectura de b0 en la primera celda del bloque destino.
        foreach (var current in Bits)
        {
            builder.Add(
                "q_calc_prep_start",
                current,
                PreparationRead(current),
                current,
                HeadMove.Right,
                ModuleCalc);
        }

        // q_calc_prep_a[b0]: recorrido hasta el delimitador del bloque destino.
        foreach (var b0 in Bits)
        {
            foreach (var current in Bits)
            {
                builder.Add(
                    PreparationRead(b0),
                    current,
                    PreparationRead(b0),
                    current,
                    HeadMove.Right,
                    ModuleCalc);
            }

            builder.Add(PreparationRead(b0), '#', PreparationLast(b0), '#', HeadMove.Left, ModuleCalc);
        }

        // q_calc_prep_c[b0,blast]: lectura de blast y retorno al inicio del bloque.
        foreach (var b0 in Bits)
        {
            foreach (var last in Bits)
            {
                builder.Add(
                    PreparationLast(b0),
                    last,
                    PreparationReturn(b0, last),
                    last,
                    HeadMove.Left,
                    ModuleCalc);

                builder.Add(
                    PreparationReturn(b0, last),
                    '0',
                    PreparationReturn(b0, last),
                    '0',
                    HeadMove.Left,
                    ModuleCalc);
                builder.Add(
                    PreparationReturn(b0, last),
                    '1',
                    PreparationReturn(b0, last),
                    '1',
                    HeadMove.Left,
                    ModuleCalc);
                builder.Add(
                    PreparationReturn(b0, last),
                    '#',
                    SweepStart(last, b0, b0),
                    '#',
                    HeadMove.Right,
                    ModuleCalc);
            }
        }
    }

    private static void BuildCounter(TransitionTableBuilder builder)
    {
        builder.Add("q_gen_check_start", '0', "q_gen_check_walk", '0', HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_start", '1', "q_gen_check_walk", '1', HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_start", '·', "q_gen_check_walk", Tape.Blank, HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_start", ConsumedDigit, "q_gen_check_walk", ConsumedDigit, HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_start", '#', "q_gen_check_walk", '#', HeadMove.Left, ModuleCounter);

        builder.Add("q_gen_check_walk", '0', "q_gen_check_walk", '0', HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_walk", '1', "q_gen_check_walk", '1', HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_walk", ConsumedDigit, "q_gen_check_walk", ConsumedDigit, HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_walk", '#', "q_gen_check_walk", '#', HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_walk", UnaryMark, "q_gen_counter_left", UnaryMark, HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_check_walk", LeftEnd, "q_gen_counter_right", LeftEnd, HeadMove.Left, ModuleCounter);

        builder.Add("q_gen_counter_right", UnaryMark, "q_gen_counter_left", UnaryMark, HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_counter_right", '·', AcceptState, Tape.Blank, HeadMove.Right, ModuleCounter);

        builder.Add("q_gen_counter_left", UnaryMark, "q_gen_counter_left", UnaryMark, HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_counter_left", '·', "q_gen_counter_erase", Tape.Blank, HeadMove.Right, ModuleCounter);

        builder.Add("q_gen_counter_erase", UnaryMark, "q_gen_advance", '·', HeadMove.Right, ModuleCounter);
        builder.Add("q_gen_advance", UnaryMark, "q_gen_advance", UnaryMark, HeadMove.Right, ModuleCounter);
        builder.Add("q_gen_advance", '0', "q_gen_advance", '0', HeadMove.Right, ModuleCounter);
        builder.Add("q_gen_advance", '1', "q_gen_advance", '1', HeadMove.Right, ModuleCounter);
        builder.Add("q_gen_advance", ConsumedDigit, "q_gen_advance", ConsumedDigit, HeadMove.Right, ModuleCounter);
        builder.Add("q_gen_advance", '#', "q_gen_advance", '#', HeadMove.Right, ModuleCounter);
        builder.Add("q_gen_advance", LeftEnd, "q_gen_advance", LeftEnd, HeadMove.Right, ModuleCounter);
        builder.Add("q_gen_advance", '·', "q_gen_source", Tape.Blank, HeadMove.Left, ModuleCounter);

        builder.Add("q_gen_source", '#', "q_gen_source_cells", '#', HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_source_cells", '0', "q_gen_source_cells", '0', HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_source_cells", '1', "q_gen_source_cells", '1', HeadMove.Left, ModuleCounter);
        builder.Add("q_gen_source_cells", '#', "q_copy_next", '#', HeadMove.Right, ModuleCounter);
    }

    private static readonly char[] Bits = { '0', '1' };

    private static string SweepStart(char left, char current, char b0) =>
        $"q_calc_s[l={left},c={current},b0={b0}]";

    private static string SweepRead(char left, char current, char b0) =>
        $"q_calc_r[l={left},c={current},b0={b0}]";

    private static string SweepWrite(char left, char current, char next, char b0) =>
        $"q_calc_w[l={left},c={current},n={next},b0={b0}]";

    private static string PreparationRead(char b0) =>
        $"q_calc_prep_a[b0={b0}]";

    private static string PreparationLast(char b0) =>
        $"q_calc_prep_c[b0={b0}]";

    private static string PreparationReturn(char b0, char last) =>
        $"q_calc_prep_d[b0={b0},blast={last}]";
}

internal sealed class TransitionTableBuilder
{
    private readonly List<TransitionRow> _rows = new();

    public void Add(
        string state,
        char read,
        string nextState,
        char write,
        HeadMove move,
        string module)
    {
        _rows.Add(new TransitionRow(state, read, nextState, write, move, module));
    }

    public TransitionTable Build() => new(_rows);
}
