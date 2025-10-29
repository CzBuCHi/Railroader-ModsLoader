//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using NSubstitute;
//using Railroader.ModManager.Services.Wrappers.FileSystem;

//namespace MemoryFileSystem.Internal;

//public sealed class MemoryZipArchive(MemoryZip memoryZip) : IZipArchive
//{
//    public IReadOnlyCollection<IZipArchiveEntry> Entries { get; } =
//        memoryZip
//            .Where(e => !e.IsDirectory)
//            .Select(o => new MemoryZipArchiveEntry(o).Mock())
//            .ToArray();

//    public IZipArchiveEntry? GetEntry(string entryName) =>  Entries.FirstOrDefault(e => e.FullName == entryName);

//    [ExcludeFromCodeCoverage]
//    public void Dispose() {
//    }

//    public IZipArchive Mock() {
//        var mock = Substitute.For<IZipArchive>();
//        mock.Entries.Returns(_ => Entries);
//        mock.GetEntry(Arg.Any<string>()).Returns(o => GetEntry(o.Arg<string>()));
//        return mock;
//    }
//}
