using System.IO;
using System.Linq;
using System.Windows.Input;
using Newtonsoft.Json;
using StrykerReportViewer.Commands;
using StrykerReportViewer.Models;

namespace StrykerReportViewer.ViewModels;

public class MainWindowViewModel
{
    public ICommand LoadReport { get; } = new DelegateCommand(LoadReportHandler);

    private static void LoadReportHandler(object parameter) {
        var strykerOutput = Path.Combine(Directory.GetCurrentDirectory(), "StrykerOutput");
        var newest        = new DirectoryInfo(strykerOutput).EnumerateDirectories("*.*").OrderByDescending(o => o.LastWriteTime).FirstOrDefault();
        if (newest == null) {
            return;
        }

        var reportFile = Path.Combine(newest.FullName, "reports", "mutation-report.json");

        var json   = System.IO.File.ReadAllText(reportFile);
        var report = JsonConvert.DeserializeObject<Report>(json);

    }
}
