using System;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

class Program
{
    static void Main(string[] args)
    {
        string assemblyPath = @"C:\Program Files (x86)\Steam\steamapps\common\Knights of Honor II\Sovereign_Data\Managed\Assembly-CSharp-firstpass.dll";
        string outputDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Knights of Honor II\AIOverhaul\Sources";
        string targetNamespace = "Logic";

        Console.WriteLine($"Decompiling assembly: {assemblyPath}");
        Console.WriteLine($"Target namespace: {targetNamespace}");
        Console.WriteLine($"Output directory: {outputDirectory}");
        Console.WriteLine();

        if (!File.Exists(assemblyPath))
        {
            Console.WriteLine($"ERROR: Assembly file not found at {assemblyPath}");
            return;
        }

        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputDirectory);

        // Create decompiler
        var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false
        });

        // Get all types in the Logic namespace
        var types = decompiler.TypeSystem.MainModule.TypeDefinitions
            .Where(t => t.Namespace == targetNamespace)
            .ToList();

        if (types.Count == 0)
        {
            Console.WriteLine($"WARNING: No types found in namespace '{targetNamespace}'");
            return;
        }

        Console.WriteLine($"Found {types.Count} types in namespace '{targetNamespace}'");
        Console.WriteLine();

        int successCount = 0;
        int errorCount = 0;

        foreach (var type in types)
        {
            try
            {
                string typeName = type.Name;
                Console.Write($"Decompiling {typeName}...");

                // Decompile the type
                string code = decompiler.DecompileTypeAsString(type.FullTypeName);

                // Create namespace folder
                string namespaceFolder = Path.Combine(outputDirectory, type.Namespace);
                Directory.CreateDirectory(namespaceFolder);

                // Save to file in namespace folder
                string fileName = $"{typeName}.cs";
                string filePath = Path.Combine(namespaceFolder, fileName);
                File.WriteAllText(filePath, code);

                Console.WriteLine($" ✓ Saved to {type.Namespace}/{fileName}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ✗ ERROR: {ex.Message}");
                errorCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Decompilation complete!");
        Console.WriteLine($"Success: {successCount} | Errors: {errorCount}");
        Console.WriteLine($"Files saved to: {outputDirectory}");
    }
}