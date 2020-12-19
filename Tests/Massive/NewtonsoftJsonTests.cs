using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AshMind.Extensions;
using Newtonsoft.Json;
using ReflectionMagic;
using Xunit;

namespace Unbreakable.Tests.Massive {
    using Mono.Cecil;
    using static ApiAccess;

    public class NewtonsoftJsonTests {
        [Fact(Skip = "WIP")]
        public void CanRewriteAssembly() {
            // Assert.DoesNotThrow(() => 
            RewriteNewtonsoftJson();
        }

        [Fact(Skip = "WIP")]
        public void RewrittenAssembly_CanParseJson() {
            var rewritten = RewriteNewtonsoftJson();
            var jsonConvert = rewritten.GetType(typeof(JsonConvert).FullName!, true).AsDynamicType();
            var json = jsonConvert.SerializeObject(new TestData { Name = "Test" });
            var result = jsonConvert.DeserializeObject<TestData>(json);
            Assert.Equal(5, (int)result.a);
        }
        
        private static Assembly RewriteNewtonsoftJson() {
            var assemblyPath = typeof(JsonSerializer).Assembly.GetAssemblyFileFromCodeBase().FullName;
            var definition = AssemblyDefinition.ReadAssembly(assemblyPath);
            definition.Name.HasPublicKey = false;
            definition.Name.PublicKey = new byte[0];
            definition.Name.HashAlgorithm = AssemblyHashAlgorithm.None;
            definition.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
            AssemblyGuard.Rewrite(definition, new AssemblyGuardSettings {
                ApiPolicy = ApiRules,
                MethodLocalsSizeLimit = IntPtr.Size * 40,
                MethodStackPushSizeLimit = 120,
                AllowExplicitLayoutInTypesMatchingPattern = new Regex("<PrivateImplementationDetails>")
            });

            var assemblyStream = new MemoryStream();
            definition.Write(assemblyStream);
            definition.Write(Path.Combine(Path.GetDirectoryName(assemblyPath)!, "Newtonsoft.Json.Rewritten.dll"));
            return Assembly.Load(assemblyStream.ToArray());
        }

        private static readonly ApiPolicy ApiRules = ApiPolicy.SafeDefault()
            .Namespace("System", Neutral,
                n => n.Type(typeof(Array), Neutral,
                          t => t.Member(nameof(Array.CreateInstance), Allowed)
                      )
                      .Type(typeof(Buffer), Allowed)
                      .Type(typeof(CLSCompliantAttribute), Allowed)
                      .Type(typeof(Type), Neutral,
                          t => t.Member(nameof(Type.MakeGenericType), Allowed)
                                .Member(nameof(Type.GetConstructors), Allowed)
                                .Member(nameof(Type.GetMethod), Allowed)
                                .Member(nameof(Type.GetType), Allowed)
                      )
            )
            .Namespace("System.Collections", Allowed)
            .Namespace("System.Collections.ObjectModel", Allowed)
            .Namespace("System.Data.SqlTypes", Allowed)
            .Namespace("System.Diagnostics", Neutral,
                n => n.Type(typeof(System.Diagnostics.TraceLevel), Allowed)
            )
            .Namespace("System.IO", Allowed)
            .Namespace("System.Numerics", Allowed)
            .Namespace("System.Reflection", Allowed)
            .Namespace("System.Runtime.CompilerServices", Neutral,
                n => n.Type(typeof(AsyncStateMachineAttribute), Allowed)
                      .Type(typeof(AsyncTaskMethodBuilder), Allowed)
                      .Type(typeof(AsyncTaskMethodBuilder<>), Allowed)
                      .Type(typeof(ConfiguredTaskAwaitable), Allowed)
                      .Type(typeof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter), Allowed)
                      .Type(typeof(ConfiguredTaskAwaitable<>), Allowed)
                      .Type(typeof(ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter), Allowed)
                      .Type(typeof(TaskAwaiter<>), Allowed)
            )
            .Namespace("System.Runtime.Serialization", Allowed)
            .Namespace("System.Text.RegularExpressions", Allowed)
            .Namespace("System.Threading", Allowed)
            .Namespace("System.Threading.Tasks", Allowed)
            .Namespace("System.Xml", Allowed)
            .Namespace("System.Xml.Linq", Allowed);

        private class TestData {
            public string? Name { get; set; }
        }
    }
}
