#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Compilation;
using UnityEngine;

namespace DeveloperUchida.ZedIntegration
{
    internal static class ZedProjectGenerator
    {
        public static void SyncAll()
        {
            Debug.Log("[Zed Integration] Generating C# project files...");

            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName;

            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError(
                    "[Zed Integration] Could not determine project root."
                );
                return;
            }

            Assembly[] assemblies =
                CompilationPipeline.GetAssemblies(
                    AssembliesType.Editor
                );

            foreach (Assembly assembly in assemblies)
            {
                GenerateProject(projectRoot, assembly);
            }

            Debug.Log(
                $"[Zed Integration] Generated {assemblies.Length} project file(s)."
            );
        }

        public static void SyncIfNeeded(
            string[] addedFiles,
            string[] deletedFiles,
            string[] movedFiles,
            string[] movedFromFiles,
            string[] importedFiles)
        {
            bool requiresSync =
                ContainsRelevantFile(addedFiles) ||
                ContainsRelevantFile(deletedFiles) ||
                ContainsRelevantFile(movedFiles) ||
                ContainsRelevantFile(movedFromFiles) ||
                ContainsRelevantFile(importedFiles);

            if (requiresSync)
                SyncAll();
        }

        private static void GenerateProject(
            string projectRoot,
            Assembly assembly)
        {
            string projectPath =
                Path.Combine(
                    projectRoot,
                    $"{assembly.name}.csproj"
                );

            StringBuilder project = new StringBuilder();

            project.AppendLine(
                "<Project Sdk=\"Microsoft.NET.Sdk\">"
            );

            project.AppendLine("  <PropertyGroup>");
            project.AppendLine(
                "    <TargetFramework>netstandard2.1</TargetFramework>"
            );
            project.AppendLine(
                "    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>"
            );
            project.AppendLine(
                "    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>"
            );

            if (assembly.defines != null &&
                assembly.defines.Length > 0)
            {
                string defines =
                    string.Join(";", assembly.defines);

                project.AppendLine(
                    $"    <DefineConstants>{Escape(defines)}</DefineConstants>"
                );
            }

            project.AppendLine("  </PropertyGroup>");

            // -------------------------
            // Source files
            // -------------------------

            project.AppendLine("  <ItemGroup>");

            foreach (string sourceFile in assembly.sourceFiles)
            {
                string absolutePath =
                    Path.GetFullPath(sourceFile);

                project.AppendLine(
                    $"    <Compile Include=\"{Escape(absolutePath)}\" />"
                );
            }

            project.AppendLine("  </ItemGroup>");

            // -------------------------
            // Assembly references
            // -------------------------

            project.AppendLine("  <ItemGroup>");

            foreach (string reference in assembly.allReferences)
            {
                if (string.IsNullOrWhiteSpace(reference))
                    continue;

                string absoluteReference =
                    Path.GetFullPath(reference);

                string referenceName =
                    Path.GetFileNameWithoutExtension(
                        absoluteReference
                    );

                project.AppendLine(
                    $"    <Reference Include=\"{Escape(referenceName)}\">"
                );

                project.AppendLine(
                    $"      <HintPath>{Escape(absoluteReference)}</HintPath>"
                );

                project.AppendLine(
                    "    </Reference>"
                );
            }

            project.AppendLine("  </ItemGroup>");

            project.AppendLine("</Project>");

            File.WriteAllText(
                projectPath,
                project.ToString(),
                new UTF8Encoding(false)
            );

            Debug.Log(
                $"[Zed Integration] Generated: {assembly.name}.csproj"
            );
        }

        private static bool ContainsRelevantFile(
            string[] files)
        {
            if (files == null)
                return false;

            return files.Any(file =>
            {
                if (string.IsNullOrWhiteSpace(file))
                    return false;

                string extension =
                    Path.GetExtension(file);

                return
                    extension.Equals(
                        ".cs",
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    extension.Equals(
                        ".asmdef",
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    extension.Equals(
                        ".asmref",
                        StringComparison.OrdinalIgnoreCase
                    );
            });
        }

        private static string Escape(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}

#endif
