using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MemoryFileSystem.Types;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO;

namespace MemoryFileSystem;

partial class MemoryFileSystem 
{
    public IFileStatic File { get; }

    private readonly object _MoveLock = new();
    private readonly object _CreateLock = new();
    
    private IFileStatic CreateFileStatic() {
        var file = Substitute.For<IFileStatic>();
        
        file.Exists(Arg.Any<string>())
            .Returns(o => Items.TryGetValue(NormalizePath(o.Arg<string>()), out var entry) && entry is { IsDirectory: false });
        
        file.ReadAllText(Arg.Any<string>()).Returns(o => {
            var path = NormalizePath(o.Arg<string>());
            if (!Items.TryGetValue(path, out var entry) || entry is not { IsDirectory: false }) {
                throw new FileNotFoundException($"File not found: {path}");
            }

            if (entry.ReadException != null) {
                throw entry.ReadException;
            }

            return Encoding.UTF8.GetString(entry.Content!);
        });
        
        file.GetLastWriteTime(Arg.Any<string>()).Returns(o => {
            var path = NormalizePath(o.Arg<string>());
            if (Items.TryGetValue(path, out var entry) && entry is { IsDirectory: false }) {
                return entry.LastWriteTime;
            }

            throw new FileNotFoundException($"File not found: {path}");
        });
        
        file.When(o => o.Delete(Arg.Any<string>()))
            .Do(o => {
                var path = NormalizePath(o.Arg<string>());
                if (!Items.TryGetValue(path, out var entry)) {
                    return;
                }

                if (entry.IsDirectory) {
                    throw new InvalidOperationException($"Entry at {path} is directory.");
                }

                entry.CheckLock();

                Items.TryRemove(path, out _);
            });
        
        file.When(o => o.Move(Arg.Any<string>(), Arg.Any<string>()))
            .Do(o => {
                var sourceFileName = NormalizePath(o.ArgAt<string>(0));
                var destFileName   = NormalizePath(o.ArgAt<string>(1));

                lock (_MoveLock) {
                    if (!Items.TryGetValue(sourceFileName, out var sourceFile) || sourceFile is not { IsDirectory: false }) {
                        throw new FileNotFoundException($"Source file not found: '{sourceFileName}'.");
                    }

                    if (Items.ContainsKey(destFileName)) {
                        throw new InvalidOperationException($"Destination path already exists: '{destFileName}'.");
                    }

                    sourceFile.CheckLock();

                    var removed = Items.TryRemove(sourceFileName, out _);
                    // Stryker disable once statement, string
                    Debug.Assert(removed, $"Failed to remove source file '{sourceFileName}'.");

                    var added = Items.TryAdd(destFileName, sourceFile with { Path = destFileName });
                    // Stryker disable once statement, string
                    Debug.Assert(added, $"Failed to add destination file '{destFileName}'.");
                }
            });
        
        file.Create(Arg.Any<string>()).Returns(o => {
            var path = NormalizePath(o.Arg<string>());

            lock (_CreateLock) {
                Add(path, Array.Empty<byte>());

                var data = new List<byte>();
                return new MemoryFileStream((buffer, offset, count) => data.AddRange(buffer.Skip(offset).Take(count)), () => { Items[path] = Items[path]! with { Content = data.ToArray() }; });
            }
        });
        
        return file;
    }
}
