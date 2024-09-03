using System.Text;

namespace Jitter2Process
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var path = Environment.CurrentDirectory;
            Console.WriteLine(path);
            path += @"\..\..\..\..\Jitter2\";
            Console.WriteLine(Path.GetFullPath(path));
            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var codeText = File.ReadAllText(file);
                var namespaceIdx = codeText.IndexOf("namespace");
                if (namespaceIdx == -1)
                {
                    Console.WriteLine($"文件{file}没有命名空间!");
                    continue;
                }
                codeText = codeText.Replace("float", "Fix64");
                codeText = codeText.Replace("double", "Fix64");
                codeText = codeText.Replace("MathF", "MathFix");
                codeText = codeText.Replace("Math.", "MathFix.");
                codeText = codeText.Replace("public const ", "public static ");
                codeText = codeText.Replace("private const ", "private static ");
                codeText = codeText.Replace("   const ", "   ");
                codeText = codeText.Replace("public CapsuleShape(Fix64 radius = 0.5f, Fix64 length = 1.0f)", "public CapsuleShape(Fix64 radius /*= 0.5f*/, Fix64 length /*= 1.0f*/)");
                codeText = codeText.Replace("[assembly: InternalsVisibleTo(\"JitterTests\")]", "//[assembly: InternalsVisibleTo(\"JitterTests\")]");
                codeText = codeText.Replace("Fix64 dt = 0.0f", "Fix64 /*dt = 0.0f*/");
                
                
                
                codeText = codeText.Insert(namespaceIdx, "using Fix64 = FixedMath.Fix64;\r\nusing MathFix = FixedMath.MathFix;\r\n\r\n");
                File.WriteAllText(file, codeText);
            }
        }
    }
}
