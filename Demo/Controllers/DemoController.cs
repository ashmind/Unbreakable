using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using AppDomainToolkit;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.IO;
using Unbreakable.Demo.Models;

namespace Unbreakable.Demo.Controllers {
    [RoutePrefix("")]
    public class DemoController : Controller {
        private readonly string DefaultCode = @"
            using System;
            static class Program {
                static int Run() {
                    return 0;
                }
            }
        ".Replace("            ", "").Trim();

        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        [Route("")]
        public ActionResult Index() {
            return View(new DemoViewModel {
                Code = DefaultCode
            });
        }

        [HttpPost]
        [Route("Test")]
        [ValidateInput(false)]
        public ActionResult Test(string code) {
            var stopwatch = Stopwatch.StartNew();
            return View("Index", new DemoViewModel {
                Code = code,
                Result = Run(code),
                Duration = stopwatch.Elapsed
            });
        }

        private string Run(string code) {
            try {
                var compilation = CSharpCompilation.Create(
                    "_",
                    new[] { CSharpSyntaxTree.ParseText(code) },
                    new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );
                using (var assemblyStream = MemoryStreamManager.GetStream())
                using (var rewrittenStream = MemoryStreamManager.GetStream()) {
                    var compilationResult = compilation.Emit(assemblyStream);
                    if (!compilationResult.Success)
                        return string.Join("\r\n", compilationResult.Diagnostics);
                    assemblyStream.Seek(0, SeekOrigin.Begin);
                    var guardToken = AssemblyGuard.Rewrite(assemblyStream, rewrittenStream);
                    var currentSetup = AppDomain.CurrentDomain.SetupInformation;
                    using (var context = AppDomainContext.Create(new AppDomainSetup {
                        ApplicationBase = currentSetup.ApplicationBase,
                        PrivateBinPath = currentSetup.PrivateBinPath
                    })) {
                        context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                        var result = RemoteFunc.Invoke(context.Domain, rewrittenStream, guardToken, RemoteRun);
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex) {
                return ex.ToString();
            }
        }

        private static object RemoteRun(MemoryStream assemblyStream, RuntimeGuardToken token) {
            using (token.Scope()) {
                var assembly = Assembly.Load(assemblyStream.ToArray());
                var type = assembly.GetType("Program", true);
                var method = type.GetMethod("Run", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                    throw new NotSupportedException("Static method 'Run' not found on type 'Program'.");

                var result = method.Invoke(null, null);
                if (result?.GetType().Assembly == assembly || (result is MemberInfo m && m.Module.Assembly == assembly))
                    throw new Exception("Result returned by Program.Run must not belong to the user assembly.");
                return result;
            }
        }
    }
}