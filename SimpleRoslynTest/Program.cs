using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace SimpleRoslynTest {
    class Program {
        const string TestCode = @"
using System;

namespace MyNamespace {
    public class MyTestClass {
        public string Hello() {
            return ""Hello!"";
        }

        public string Foo<T>() {
            return ""Foo!"";
        }
    }
}
";

        static void Main(string[] args) {
            MeasureCompilationPerf(CompileCodeIntoAssemblyUsingRoslyn);
            //MeasureCompilationPerf(CompileCodeIntoAssemblyUsingCodeDom);
        }

        private static void MeasureCompilationPerf(Func<string, string, Assembly> compilingMethod) {
            compilingMethod(TestCode, "Warmup");

            var start = DateTime.Now;
            for (int i = 0; i < 5000; i++) {
                var assembly = compilingMethod(TestCode, "TestAssembly" + i);

                var testClass = assembly.CreateInstance("MyNamespace.MyTestClass");
            }
            Console.WriteLine(DateTime.Now - start);

            GC.Collect();

            Console.WriteLine("Press enter to end");
            Console.ReadLine();
        }

        private static Assembly CompileCodeIntoAssemblyUsingRoslyn(string code, string assemblyName) {
            var syntaxTree = SyntaxTree.ParseCompilationUnit(code);

            var mscorlib = new AssemblyFileReference(typeof(object).Assembly.Location);

            var compilationOptions = new CompilationOptions(assemblyKind: AssemblyKind.DynamicallyLinkedLibrary);

            var compilation = Compilation.Create(assemblyName, compilationOptions, new[] { syntaxTree }, new[] { mscorlib });

            var memStream = new MemoryStream();
            var emitResult = compilation.Emit(memStream);

            if (!emitResult.Success) {
                foreach (Diagnostic diagnostic in emitResult.Diagnostics) {
                    Console.WriteLine(diagnostic.Info.ToString());
                }
            }

            return Assembly.Load(memStream.GetBuffer());
        }

        private static Assembly CompileCodeIntoAssemblyUsingCodeDom(string code, string assemblyName) {
            var codeDomProvider = new CSharpCodeProvider();

            CompilerResults results = codeDomProvider.CompileAssemblyFromSource(new CompilerParameters(), code);
            if (results.Errors.HasErrors) {
                foreach (CompilerError error in results.Errors) {
                    Console.WriteLine(error.ErrorText);
                }
            }

            return results.CompiledAssembly;
        }
    }
}
