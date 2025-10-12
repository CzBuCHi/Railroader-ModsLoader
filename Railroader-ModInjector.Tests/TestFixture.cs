using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Messages;
using JetBrains.Annotations;
using Railroader.ModInjector;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Track;
using UnityEngine;

namespace Railroader_ModInterfaces.Tests;

[PublicAPI]
[CollectionDefinition("TestFixture")]
public sealed class TestFixture : IDisposable, ICollectionFixture<TestFixture>
{
    public string GameDir => @"c:\Program Files (x86)\Steam\steamapps\common\Railroader\";

    public IEnumerable<LogEvent> Events => TestLogManager.Events;

    public string LogMessages => string.Join("\r\n", TestLogManager.Events.Select(o => {
        var sb = new StringBuilder();
        using (TextWriter output = new StringWriter(sb)) {
            o.MessageTemplate.Render(o.Properties, output);
        }

        return sb.ToString();
    }));

    public TestFixture()
    {
        Directory.SetCurrentDirectory(GameDir);
        TestLogManager.Awake();
    }

    public void Dispose() {
        Log.CloseAndFlush();
    }

    // modified version of LogManager from Assembly-CSharp
    private static class TestLogManager
    {
        private static readonly SimpleSink _Sink = new();

        public  static IEnumerable<LogEvent> Events => _Sink.Events;

        public static void Awake() {
            Log.Logger = Injector.CreateLogger(
                MakeConfiguration()
                    .WriteTo.Sink(_Sink, LogEventLevel.Debug)
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Model.AI.AutoEngineer", LogEventLevel.Warning)
                    .MinimumLevel.Override("Model.AI.AutoEngineerPlanner", LogEventLevel.Warning)
                    .MinimumLevel.Override("Effects.Decals.CanvasDecalRenderer", LogEventLevel.Warning)
            );
            // do not call mod main here
            //Injector.ModInjectorMain();
        }

        private static LoggerConfiguration MakeConfiguration() {
            return new LoggerConfiguration()
                   .Destructure.ByTransforming<Vector3>((Func<Vector3, object>)(v => (object)new {
                       X = v.x,
                       Y = v.y,
                       Z = v.z
                   }))
                   .Destructure.ByTransforming<Quaternion>((Func<Quaternion, object>)(q => {
                       Vector3 eulerAngles = q.eulerAngles;
                       return (object)new {
                           EulerX = eulerAngles.x,
                           EulerY = eulerAngles.y,
                           EulerZ = eulerAngles.z
                       };
                   }))
                   .Destructure.ByTransforming<Location>((Func<Location, object>)(l => (object)new {
                       Id = l.segment.id,
                       Distance = l.distance,
                       EndIsA = (l.end == TrackSegment.End.A)
                   }))
                   .Destructure.ByTransforming<Snapshot.TrackLocation>((Func<Snapshot.TrackLocation, object>)(l => (object)new {
                       Id = l.segmentId,
                       Distance = l.distance,
                       EndIsA = l.endIsA
                   }))
                   .Destructure.ByTransforming<Model.Car>((Func<Model.Car, object>)(c => (object)new {
                       Id = c.id,
                       Name = c.DisplayName
                   }));
            //.WriteTo.UnityConsole("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}");
        }
    }

    private sealed class SimpleSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
