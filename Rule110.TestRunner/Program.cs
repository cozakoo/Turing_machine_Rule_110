using Rule110.Turing;
using Rule110.Verification;

var report = Rule110TestCatalog.Run();

var reportPath = Path.GetFullPath("resultados-regla110.txt");
File.WriteAllLines(reportPath, report.Lines);

var tablePath = Path.GetFullPath("tabla-transiciones-maquina-turing.txt");
File.WriteAllText(tablePath, Rule110TuringMachine.Table.Describe());

Console.WriteLine($"Reporte exportado: {reportPath}");
Console.WriteLine($"Tabla de transiciones exportada: {tablePath}");
Console.WriteLine($"Casos correctos: {report.Total - report.Failed}/{report.Total}");

return report.Failed == 0 ? 0 : 1;
