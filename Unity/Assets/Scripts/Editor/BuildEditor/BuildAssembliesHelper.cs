using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using HybridCLR.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace ET
{
    public static class BuildAssembliesHelper
    {
        public const string CodeDir = "Assets/Bundles/Code/";

        public static void BuildModel(CodeOptimization codeOptimization, GlobalConfig globalConfig)
        {
            if (Directory.Exists(Define.BuildOutputDir))
            {
                string[] logicFiles = Directory.GetFiles(Define.BuildOutputDir, "Model_*");
                foreach (string file in logicFiles)
                {
                    File.Delete(file);
                }
            }

            if (Directory.Exists(CodeDir))
            {
                string[] logicFiles = Directory.GetFiles(CodeDir, "Model_*");
                foreach (string modelFile in logicFiles)
                {
                    File.Delete(modelFile);
                }
            }

            List<string> codes;
            switch (globalConfig.CodeMode)
            {
                case CodeMode.Client:
                    codes = new List<string>()
                    {
                        "Assets/Scripts/Codes/Model/Generate/Client/",
                        "Assets/Scripts/Codes/Model/Share/",
                        "Assets/Scripts/Codes/Model/Client/",
                        "Assets/Scripts/Codes/ModelView/Client/",
                    };
                    break;
                case CodeMode.Server:
                    codes = new List<string>()
                    {
                        "Assets/Scripts/Codes/Model/Generate/Server/",
                        "Assets/Scripts/Codes/Model/Share/",
                        "Assets/Scripts/Codes/Model/Server/",
                        "Assets/Scripts/Codes/Model/Client/",
                    };
                    break;
                case CodeMode.ClientServer:
                    codes = new List<string>()
                    {
                        "Assets/Scripts/Codes/Model/Share/",
                        "Assets/Scripts/Codes/Model/Client/",
                        "Assets/Scripts/Codes/ModelView/Client/",
                        "Assets/Scripts/Codes/Model/Generate/ClientServer/",
                        "Assets/Scripts/Codes/Model/Server/",
                    };
                    break;
                default:
                    throw new Exception("not found enum");
            }

            string assemblyName = $"Model_{++globalConfig.ModelVersion}";
            EditorUtility.SetDirty(globalConfig);

            // 修改 HybridCLR 设置里的热更程序集名
            for (int index = 0; index < SettingsUtil.HybridCLRSettings.hotUpdateAssemblies.Length; index++)
            {
                string assembly = SettingsUtil.HybridCLRSettings.hotUpdateAssemblies[index];
                if (assembly.StartsWith("Model"))
                {
                    SettingsUtil.HybridCLRSettings.hotUpdateAssemblies[index] = assemblyName;
                }
            }

            AssetDatabase.SaveAssets();

            BuildAssembliesHelper.BuildMuteAssembly(assemblyName, codes, Array.Empty<string>(), codeOptimization, globalConfig.CodeMode);

            File.Copy(Path.Combine(Define.BuildOutputDir, $"{assemblyName}.dll"), Path.Combine(CodeDir, $"{assemblyName}.dll.bytes"), true);
            File.Copy(Path.Combine(Define.BuildOutputDir, $"{assemblyName}.pdb"), Path.Combine(CodeDir, $"{assemblyName}.pdb.bytes"), true);
            Debug.Log("copy Model.dll to Bundles/Code success!");
        }

        public static void BuildHotfix(CodeOptimization codeOptimization, GlobalConfig globalConfig)
        {
            if (Directory.Exists(Define.BuildOutputDir))
            {
                string[] logicFiles = Directory.GetFiles(Define.BuildOutputDir, "Hotfix_*");
                foreach (string file in logicFiles)
                {
                    File.Delete(file);
                }
            }

            if (Directory.Exists(CodeDir))
            {
                string[] logicFiles = Directory.GetFiles(CodeDir, "Hotfix_*");
                foreach (string modelFile in logicFiles)
                {
                    File.Delete(modelFile);
                }
            }


            List<string> codes;
            switch (globalConfig.CodeMode)
            {
                case CodeMode.Client:
                    codes = new List<string>()
                    {
                        "Assets/Scripts/Codes/Hotfix/Share/",
                        "Assets/Scripts/Codes/Hotfix/Client/",
                        "Assets/Scripts/Codes/HotfixView/Client/",
                    };
                    break;
                case CodeMode.Server:
                    codes = new List<string>()
                    {
                        "Assets/Scripts/Codes/Hotfix/Share/", "Assets/Scripts/Codes/Hotfix/Server/", "Assets/Scripts/Codes/Hotfix/Client/",
                    };
                    break;
                case CodeMode.ClientServer:
                    codes = new List<string>()
                    {
                        "Assets/Scripts/Codes/Hotfix/Share/",
                        "Assets/Scripts/Codes/Hotfix/Client/",
                        "Assets/Scripts/Codes/HotfixView/Client/",
                        "Assets/Scripts/Codes/Hotfix/Server/",
                    };
                    break;
                default:
                    throw new Exception("not found enum");
            }

            string assemblyName = $"Hotfix_{++globalConfig.HotFixVersion}";
            EditorUtility.SetDirty(globalConfig);

            // 修改 HybridCLR 设置里的热更程序集名
            for (int index = 0; index < SettingsUtil.HybridCLRSettings.hotUpdateAssemblies.Length; index++)
            {
                string assembly = SettingsUtil.HybridCLRSettings.hotUpdateAssemblies[index];
                if (assembly.StartsWith("Hotfix"))
                {
                    SettingsUtil.HybridCLRSettings.hotUpdateAssemblies[index] = assemblyName;
                }
            }

            AssetDatabase.SaveAssets();

            BuildAssembliesHelper.BuildMuteAssembly(assemblyName, codes, new[] { Path.Combine(CodeDir, $"Model_{globalConfig.ModelVersion}.dll.bytes") }, codeOptimization,
                globalConfig.CodeMode);

            File.Copy(Path.Combine(Define.BuildOutputDir, $"{assemblyName}.dll"), Path.Combine(CodeDir, $"{assemblyName}.dll.bytes"), true);
            File.Copy(Path.Combine(Define.BuildOutputDir, $"{assemblyName}.pdb"), Path.Combine(CodeDir, $"{assemblyName}.pdb.bytes"), true);
            Debug.Log("copy Hotfix.dll to Bundles/Code success!");
        }

        private static void BuildMuteAssembly(
            string assemblyName, List<string> CodeDirectorys,
            string[] additionalReferences, CodeOptimization codeOptimization, CodeMode codeMode = CodeMode.Client)
        {
            if (!Directory.Exists(Define.BuildOutputDir))
            {
                Directory.CreateDirectory(Define.BuildOutputDir);
            }

            List<string> scripts = new List<string>();
            for (int i = 0; i < CodeDirectorys.Count; i++)
            {
                DirectoryInfo dti = new DirectoryInfo(CodeDirectorys[i]);
                FileInfo[] fileInfos = dti.GetFiles("*.cs", System.IO.SearchOption.AllDirectories);
                for (int j = 0; j < fileInfos.Length; j++)
                {
                    scripts.Add(fileInfos[j].FullName);
                }
            }

            string dllPath = Path.Combine(Define.BuildOutputDir, $"{assemblyName}.dll");
            string pdbPath = Path.Combine(Define.BuildOutputDir, $"{assemblyName}.pdb");
            File.Delete(dllPath);
            File.Delete(pdbPath);

            Directory.CreateDirectory(Define.BuildOutputDir);

            AssemblyBuilder assemblyBuilder = new AssemblyBuilder(dllPath, scripts.ToArray());

            if (codeMode == CodeMode.Client)
            {
                assemblyBuilder.excludeReferences = new string[]
                {
                    "DnsClient.dll",
                    "MongoDB.Driver.Core.dll",
                    "MongoDB.Driver.dll",
                    "MongoDB.Driver.Legacy.dll",
                    "MongoDB.Libmongocrypt.dll",
                    "SharpCompress.dll",
                    "System.Buffers.dll",
                    "System.Runtime.CompilerServices.Unsafe.dll",
                    "System.Text.Encoding.CodePages.dll"
                };
            }

            //启用UnSafe
            assemblyBuilder.compilerOptions.AllowUnsafeCode = true;

            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            assemblyBuilder.compilerOptions.CodeOptimization = codeOptimization;
            assemblyBuilder.compilerOptions.ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);
            // assemblyBuilder.compilerOptions.ApiCompatibilityLevel = ApiCompatibilityLevel.NET_4_6;

            assemblyBuilder.additionalReferences = additionalReferences;

            assemblyBuilder.flags = AssemblyBuilderFlags.None;
            //AssemblyBuilderFlags.None                 正常发布
            //AssemblyBuilderFlags.DevelopmentBuild     开发模式打包
            //AssemblyBuilderFlags.EditorAssembly       编辑器状态
            assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;

            assemblyBuilder.buildTarget = EditorUserBuildSettings.activeBuildTarget;

            assemblyBuilder.buildTargetGroup = buildTargetGroup;

            assemblyBuilder.buildStarted += assemblyPath => Debug.LogFormat("build start：" + assemblyPath);

            assemblyBuilder.buildFinished += (assemblyPath, compilerMessages) =>
            {
                int errorCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                int warningCount = compilerMessages.Count(m => m.type == CompilerMessageType.Warning);

                Debug.LogFormat("Warnings: {0} - Errors: {1}", warningCount, errorCount);

                if (warningCount > 0)
                {
                    Debug.LogFormat("有{0}个Warning!!!", warningCount);
                }

                if (errorCount > 0)
                {
                    for (int i = 0; i < compilerMessages.Length; i++)
                    {
                        if (compilerMessages[i].type == CompilerMessageType.Error)
                        {
                            string filename = Path.GetFullPath(compilerMessages[i].file);
                            Debug.LogError(
                                $"{compilerMessages[i].message} (at <a href=\"file:///{filename}/\" line=\"{compilerMessages[i].line}\">{Path.GetFileName(filename)}</a>)");
                        }
                    }
                }
            };

            //开始构建
            if (!assemblyBuilder.Build())
            {
                Debug.LogErrorFormat("build fail：" + assemblyBuilder.assemblyPath);
                return;
            }

            while (EditorApplication.isCompiling)
            {
                // 主线程sleep并不影响编译线程
                Thread.Sleep(1);
            }
        }
    }
}