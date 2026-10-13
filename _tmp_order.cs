using System;
using System.Linq;
using System.Reflection;
using mooSQL.Pure.Tests.TestHelpers;
var t = typeof(SysConfig);
Console.WriteLine("GetProperties order:");
foreach (var p in t.GetProperties()) Console.WriteLine("  " + p.Name + " token=" + p.MetadataToken);
