using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MemoryFileSystem2.Types;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO.File;

namespace MemoryFileSystem2;

partial class MemoryFileSystem : MemoryFileSystem.IFile
{
    public interface IFile
    {
        Exists           Exists           { get; }
        ReadAllText      ReadAllText      { get; }
        GetLastWriteTime GetLastWriteTime { get; }
        Delete           Delete           { get; }
        Move             Move             { get; }
        Create           Create           { get; }
    }

    public IFile File => this;

    private Exists?           _Exists;
    private ReadAllText?      _ReadAllText;
    private GetLastWriteTime? _GetLastWriteTime;
    private Delete?           _Delete;
    private Move?             _Move;
    private Create?           _Create;

    Exists IFile.Exists {
        [DebuggerStepThrough]
        get => _Exists ??= CreateExists();
    }

    ReadAllText IFile.ReadAllText {
        [DebuggerStepThrough]
        get { return _ReadAllText ??= CreateReadAllText(); }
    }

    GetLastWriteTime IFile.GetLastWriteTime {
        [DebuggerStepThrough]
        get { return _GetLastWriteTime ??= CreateGetLastWriteTime(); }
    }

    Delete IFile.Delete {
        [DebuggerStepThrough]
        get { return _Delete ??= CreateDelete(); }
    }

    Move IFile.Move {
        [DebuggerStepThrough]
        get { return _Move ??= CreateMove(); }
    }

    Create IFile.Create {
        [DebuggerStepThrough]
        get { return _Create ??= CreateCreate(); }
    }

    private Exists CreateExists() {
        var mock = Substitute.For<Exists>();
        mock.Invoke(Arg.Any<string>())
            .Returns([DebuggerStepThrough](o) => Items.TryGetValue(NormalizePath(o.Arg<string>()), out var entry) && entry is { IsDirectory: false });
        return mock;
    }

    private ReadAllText CreateReadAllText() {
        var mock = Substitute.For<ReadAllText>();
        mock.Invoke(Arg.Any<string>()).Returns(o => {
            var path = NormalizePath(o.Arg<string>());
            if (!Items.TryGetValue(path, out var entry) || entry is not { IsDirectory: false }) {
                throw new FileNotFoundException($"File not found: {path}");
            }

            if (entry.ReadException != null) {
                throw entry.ReadException;
            }

            return Encoding.UTF8.GetString(entry.ExistingContent);
        });
        return mock;
    }

    private GetLastWriteTime CreateGetLastWriteTime() {
        var mock = Substitute.For<GetLastWriteTime>();
        mock.Invoke(Arg.Any<string>()).Returns(o => {
            var path = NormalizePath(o.Arg<string>());
            if (Items.TryGetValue(path, out var entry) && entry is { IsDirectory: false }) {
                return entry.LastWriteTime;
            }

            throw new FileNotFoundException($"File not found: {path}");
        });

        return mock;
    }

    private Delete CreateDelete() {
        var mock = Substitute.For<Delete>();
        mock.When(o => o.Invoke(Arg.Any<string>()))
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

        return mock;
    }

    private readonly object _MoveLock = new();

    private Move CreateMove() {
        var mock = Substitute.For<Move>();
        mock.When(o => o.Invoke(Arg.Any<string>(), Arg.Any<string>()))
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
        return mock;
    }

    private readonly object _CreateLock = new();

    private Create CreateCreate() {
        var mock = Substitute.For<Create>();
        mock.Invoke(Arg.Any<string>()).Returns(o => {
            var path = NormalizePath(o.Arg<string>());

            lock (_CreateLock) {
                Add(path, Array.Empty<byte>());

                var data = new List<byte>();
                return new MemoryFileStream((buffer, offset, count) => data.AddRange(buffer.Skip(offset).Take(count)), () => { Items[path] = Items[path]! with { Content = data.ToArray() }; });
            }
        });

        return mock;
    }
}
