using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using AppDomainToolkit;
using AshMind.Extensions;
using LZStringCSharp;
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
        private static readonly IReadOnlyCollection<MetadataReference> MetadataReferences = new MetadataReference[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();
        

        [Route("")]
        public ActionResult Index(string code = null) {
            return View(new DemoViewModel {
                Code = code != null ? LZString.DecompressFromBase64(code) : DefaultCode,
                Result = (string)TempData["result"],
                Duration = (TimeSpan?)TempData["duration"]
            });
        }

        [HttpPost]
        [Route("Test")]
        [ValidateInput(false)]
        public ActionResult Test(string code) {
            var stopwatch = Stopwatch.StartNew();
            TempData["result"] = Run(code);
            TempData["duration"] = stopwatch.Elapsed;

            return RedirectToAction("Index", new { code = LZString.CompressToBase64(code) });
        }

        private string Run(string code) {
            try {
                var compilation = CSharpCompilation.Create(
                    "_",
                    new[] { CSharpSyntaxTree.ParseText(code) },
                    MetadataReferences,
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