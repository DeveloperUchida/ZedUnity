#if UNITY_EDITOR

using System;
using System.Diagnostics;
using System.IO;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DeveloperUchida.ZedIntegration
{
    [InitializeOnLoad]
    public sealed class ZedCodeEditor : IExternalCodeEditor
    {
        private const string EditorName = "Zed";

#if UNITY_EDITOR_OSX
        private const string DefaultZedPath = "/Applications/Zed.app";
#elif UNITY_EDITOR_WIN
        private const string DefaultZedPath =
            @"C:\Program Files\Zed\Zed.exe";
#else
        private const string DefaultZedPath = "/usr/bin/zed";
#endif

        private string _installationPath = DefaultZedPath;

        static ZedCodeEditor()
        {
            CodeEditor.Register(new ZedCodeEditor());
        }

        public CodeEditor.Installation[] Installations
        {
            get
            {
                if (!ZedExists(DefaultZedPath))
                    return Array.Empty<CodeEditor.Installation>();

                return new[]
                {
                    new CodeEditor.Installation
                    {
                        Name = EditorName,
                        Path = DefaultZedPath
                    }
                };
            }
        }

        public void Initialize(string editorInstallationPath)
        {
            _installationPath = editorInstallationPath;

            Debug.Log(
                $"[Zed Integration] Initialized: {_installationPath}"
            );
        }

        public bool TryGetInstallationForPath(
            string editorPath,
            out CodeEditor.Installation installation)
        {
            if (ZedExists(editorPath) &&
                IsZedInstallation(editorPath))
            {
                installation = new CodeEditor.Installation
                {
                    Name = EditorName,
                    Path = editorPath
                };

                return true;
            }

            installation = default;
            return false;
        }

        public bool OpenProject(
            string filePath,
            int line,
            int column)
        {
            try
            {
                string projectRoot =
                    Directory.GetParent(Application.dataPath)?.FullName;

                if (string.IsNullOrEmpty(projectRoot))
                {
                    Debug.LogError(
                        "[Zed Integration] " +
                        "Could not determine Unity project root."
                    );

                    return false;
                }

                // Assets/Open C# Project など、
                // ファイル指定なしの場合はProject Rootを開く。
                if (string.IsNullOrEmpty(filePath))
                {
                    return LaunchZed(projectRoot);
                }

                string absoluteFilePath = Path.GetFullPath(filePath);

                // Zed CLI:
                // file.cs:line:column
                string target = absoluteFilePath;

                if (line > 0)
                {
                    target += $":{line}";

                    if (column > 0)
                        target += $":{column}";
                }

                return LaunchZed(target);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Zed Integration] Failed to open Zed:\n{exception}"
                );

                return false;
            }
        }

        public void SyncAll()
        {
            Debug.Log("[Zed Integration] SyncAll requested.");

            ZedProjectGenerator.SyncAll();
        }

        public void SyncIfNeeded(
            string[] addedFiles,
            string[] deletedFiles,
            string[] movedFiles,
            string[] movedFromFiles,
            string[] importedFiles)
        {
            ZedProjectGenerator.SyncIfNeeded(
                addedFiles,
                deletedFiles,
                movedFiles,
                movedFromFiles,
                importedFiles
            );
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Zed Integration",
                EditorStyles.boldLabel
            );

            EditorGUILayout.HelpBox(
                "Uses Zed as the external C# editor.",
                MessageType.Info
            );

            EditorGUILayout.LabelField(
                "Installation",
                _installationPath
            );
        }

        private static bool LaunchZed(string target)
        {
#if UNITY_EDITOR_OSX
            /*
             * macOSではZed.app内部のCLIを直接呼ぶ。
             *
             * Zed.app/Contents/MacOS/zed
             */
            string executable =
                Path.Combine(
                    DefaultZedPath,
                    "Contents",
                    "MacOS",
                    "zed"
                );

            if (!File.Exists(executable))
            {
                Debug.LogError(
                    $"[Zed Integration] " +
                    $"Zed executable not found:\n{executable}"
                );

                return false;
            }
#else
            string executable = DefaultZedPath;
#endif

            ProcessStartInfo startInfo =
                new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            // ArgumentListなら空白を含むPathでも安全
            startInfo.ArgumentList.Add(target);

            Process.Start(startInfo);

            Debug.Log(
                $"[Zed Integration] Opening: {target}"
            );

            return true;
        }

        private static bool ZedExists(string path)
        {
#if UNITY_EDITOR_OSX
            return Directory.Exists(path);
#else
            return File.Exists(path);
#endif
        }

        private static bool IsZedInstallation(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

#if UNITY_EDITOR_OSX
            return path.EndsWith(
                "Zed.app",
                StringComparison.OrdinalIgnoreCase
            );
#else
            return Path.GetFileNameWithoutExtension(path)
                .Equals(
                    "zed",
                    StringComparison.OrdinalIgnoreCase
                );
#endif
        }
    }
}

#endif
