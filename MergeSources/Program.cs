using System.Text;
using System.Text.Json;

const string modInterfacesPath = @"c:\projects\Railroader\Railroader-ModsLoader\Railroader-ModInterfaces";
const string modInjectorPath   = @"c:\projects\Railroader\Railroader-ModsLoader\Railroader-ModInjector";
const string dummyModPath      = @"c:\projects\Railroader\Railroader-ModsLoader\Railroader-DummyMod";


MergeSources(modInterfacesPath);
MergeSources(modInjectorPath);
MergeSources(dummyModPath);

return;

static void MergeSources(string path) {

    var project = Directory.EnumerateFiles(path, "*.csproj").Single();

    var bin = Path.Combine(path, "bin");
    var obj = Path.Combine(path, "obj");
    var files = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
                         .Where(o => !o.StartsWith(bin) && !o.StartsWith(obj))
                         .Select(o => new SourceCode(o[(path.Length + 1)..], File.ReadAllText(o, Encoding.UTF8)));

    var sources = new List<SourceCode> {
        new(project[(path.Length + 1)..], File.ReadAllText(project, Encoding.UTF8)),
    };
    sources.AddRange(files);

    var json = JsonSerializer.Serialize(sources).Replace("\\u003C", "<").Replace("\\u003E", ">").Replace("\\u0022", "\\\"");
    File.WriteAllText(path + ".json", json, Encoding.UTF8);
}

internal record SourceCode(string Path, string Content);

