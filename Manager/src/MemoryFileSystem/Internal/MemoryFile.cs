//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using MemoryFileSystem.Types;
//using NSubstitute;
//using Railroader.ModManager.Services.Wrappers.FileSystem;

//namespace MemoryFileSystem.Internal;

//public sealed class MemoryFile(IMemoryFileSystem fileSystem) : IFile
//{

    
//    public void Delete(string path) {

//    }

//    private readonly object _MoveLock = new();

//    public void Move(string sourceFileName, string destFileName) {
//        sourceFileName = fileSystem.NormalizePath(sourceFileName);
//        destFileName = fileSystem.NormalizePath(destFileName);

//        lock (_MoveLock) {
//            if (!fileSystem.Items.TryGetValue(sourceFileName, out var sourceFile) || sourceFile is not { IsDirectory: false }) {
//                throw new FileNotFoundException($"Source file not found: '{sourceFileName}'.");
//            }

//            if (fileSystem.Items.ContainsKey(destFileName)) {
//                throw new InvalidOperationException($"Destination path already exists: '{destFileName}'.");
//            }

//            sourceFile.CheckLock();
            
//            var removed = fileSystem.Items.TryRemove(sourceFileName, out _);
//            // Stryker disable once statement, string
//            System.Diagnostics.Debug.Assert(removed, $"Failed to remove source file '{sourceFileName}'.");

//            var added = fileSystem.Items.TryAdd(destFileName, sourceFile with { Path = destFileName });
//            // Stryker disable once statement, string
//            System.Diagnostics.Debug.Assert(added, $"Failed to add destination file '{destFileName}'.");
//        }
//    }

//    private readonly object _CreateLock = new();

//    public Stream Create(string path) {
//        path = fileSystem.NormalizePath(path);

//        lock (_CreateLock) {
//            fileSystem.Add(path, Array.Empty<byte>());

//            var data = new List<byte>();
//            return new MemoryFileStream((buffer, offset, count) => data.AddRange(buffer.Skip(offset).Take(count)), () => {
//                fileSystem.Items[path] = fileSystem.Items[path]! with { Content = data.ToArray() };
//            });
//        }
//    }

//    public IFile Mock() {
//        var mock = Substitute.For<IFile>();
//        mock.Exists(Arg.Any<string>()).Returns(o => Exists(o.Arg<string>()));
//        mock.ReadAllText(Arg.Any<string>()).Returns(o => ReadAllText(o.Arg<string>()));
//        mock.GetLastWriteTime(Arg.Any<string>()).Returns(o => GetLastWriteTime(o.Arg<string>()));
//        mock.When(o => o.Delete(Arg.Any<string>())).Do(o => Delete(o.Arg<string>()));
//        mock.When(o => o.Move(Arg.Any<string>(), Arg.Any<string>())).Do(o => Move(o.ArgAt<string>(0), o.ArgAt<string>(1)));
//        mock.Create(Arg.Any<string>()).Returns(o => Create(o.Arg<string>()));
//        return mock;
//    }


//}
