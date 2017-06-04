using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
                Result = (ResultViewModel)TempData["result"],
            });
        }

        [HttpPost]
        [Route("Test")]
        [ValidateInput(false)]
        public ActionResult Test(string code) {
            TempData["result"] = Run(code);
            return RedirectToAction("Index", new { code = LZString.CompressToBase64(code) });
        }

        private ResultViewModel Run(string code) {
            var totalStopwatch = Stopwatch.StartNew();
            var compilationStopwatch = new Stopwatch();
            var executionStopwatch = new Stopwatch();

            ResultViewModel resultModel(string output) => new ResultViewModel(output, compilationStopwatch.Elapsed, executionStopwatch.Elapsed, totalStopwatch.Elapsed);

            try {
                compilationStopwatch.Start();
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
                        return resultModel(string.Join("\r\n", compilationResult.Diagnostics));
                    assemblyStream.Seek(0, SeekOrigin.Begin);
                    var guardToken = AssemblyGuard.Rewrite(assemblyStream, rewrittenStream);
                    compilationStopwatch.Stop();
                    var currentSetup = AppDomain.CurrentDomain.SetupInformation;
                    using (var context = AppDomainContext.Create(new AppDomainSetup {
                        ApplicationBase = currentSetup.ApplicationBase,
                        PrivateBinPath = currentSetup.PrivateBinPath
                    })) {
                        context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                        executionStopwatch.Start();
                        var result = RemoteFunc.Invoke(context.Domain, rewrittenStream, guardToken, RemoteRun);
                        executionStopwatch.Stop();
                        return resultModel(result?.ToString());
                    }
                }
            }
            catch (Exception ex) {
                return resultModel(ex.ToString());
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