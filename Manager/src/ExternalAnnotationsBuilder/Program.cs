﻿namespace ExternalAnnotationsBuilder;

using System.Xml.Linq;


// converts simple txt with Xml DOC id into valid ExternalAnnotations.xml for ReSharper to use ...

internal static class Program {

    private const string NotNull = "M:JetBrains.Annotations.NotNullAttribute.#ctor";
    private const string CanBeNull = "M:JetBrains.Annotations.CanBeNullAttribute.#ctor";

    public static void Main() {
        var currentDirectory = Directory.GetCurrentDirectory();
        var sources = Directory.EnumerateFiles(currentDirectory, "*.txt", SearchOption.TopDirectoryOnly);
        foreach (var source in sources) {
            Console.WriteLine("Processing file " + source + " ...");
            ProcessSource(source);
        }
    }

    private static void ProcessSource(string source) {
        var name = Path.GetFileNameWithoutExtension(source);
        var target = Path.ChangeExtension(source, ".xml");
        var lines = File.ReadAllLines(source).Where(o => !string.IsNullOrWhiteSpace(o)).OrderBy(o => o).Distinct().ToArray();
        File.WriteAllLines(source, lines);

        var xAssembly = new XElement("assembly", new XAttribute("name", name));

        foreach (var line in lines) {
            var parts = line.Split(' ');
            var xMember = new XElement("member", new XAttribute("name", parts[0]), new XElement("attribute", new XAttribute("ctor", NotNull)));

            foreach (var part in parts.Skip(1)) {
                xMember.Add(new XElement("parameter", new XAttribute("name", part), new XElement("attribute", new XAttribute("ctor", CanBeNull))));
                
            }

            xAssembly.Add(xMember);
        }
        
        var custom = Path.ChangeExtension(source, ".custom");
        if (File.Exists(custom)) {
            var xCustom = XElement.Parse(File.ReadAllText(custom));
            xAssembly.Add(xCustom.Elements("member"));
        }

        xAssembly = Normalize(xAssembly);

        xAssembly.Save(target);
    }

    private static XElement Normalize(XElement xElement) {
        var query =
            from o in xElement.Elements("member")
            let name = o.Attribute("name")
            where name != null
            orderby name.Value
            select o;

        return new XElement(xElement.Name, xElement.Attributes(), query.ToArray());
    }

}