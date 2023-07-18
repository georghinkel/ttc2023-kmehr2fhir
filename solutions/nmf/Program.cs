using Hsrm.TTC23;
using Hsrm.TTC23.Fhir;
using Hsrm.TTC23.Kmehr;
using NMF.Synchronizations;
using NMF.Transformations;
using System.Diagnostics;
using System.Xml.Serialization;

var stopwatch = new Stopwatch();

stopwatch.Start();

var tool = Environment.GetEnvironmentVariable("Tool");
var input = Path.GetFileName(args[0]);
var output = Path.GetFileName(args[1]);
var runIndex = Environment.GetEnvironmentVariable("RunIndex");

var kmehrSerializer = new XmlSerializer(typeof(kmehrmessageType));
var fhirSerializer = new XmlSerializer(typeof(Bundle));

var kmehr = (kmehrmessageType)kmehrSerializer.Deserialize(File.OpenRead(args[0]))!;
var fhir = null as Bundle;

stopwatch.Stop();

void Report(TimeSpan time, string phase)
{
    if (fhir != null) Console.WriteLine($"{tool};{input};{output};{runIndex};{phase};Entries;{fhir.entry.Count}");
    Console.WriteLine($"{tool};{input};{output};{runIndex};{phase};Runtime (ns);{time.Ticks * 100}");
}

Report(stopwatch.Elapsed, "Load");

stopwatch.Restart();
var transformation = new KmehrToFhir();
transformation.Initialize();
stopwatch.Stop();

Report(stopwatch.Elapsed, "Init");

stopwatch.Restart();
transformation.Synchronize(ref kmehr, ref fhir, SynchronizationDirection.LeftToRight, ChangePropagationMode.None);
stopwatch.Stop();

Report(stopwatch.Elapsed, "Transform");

fhirSerializer.Serialize(File.Create(output), fhir);
