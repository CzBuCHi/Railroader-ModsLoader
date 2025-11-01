using System;
using Mono.Cecil;

namespace Railroader.ModManager.Features.CodePatchers;

public sealed record TypePatcherInfo(Type MarkerType, Func<TypePatcherDelegate> Factory);

public delegate bool TypePatcherDelegate(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition);
