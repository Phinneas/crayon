﻿using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Crayon
{
    internal class Program
    {
#if DEBUG
#else
        private static readonly string USAGE = Util.JoinLines(
            "Usage:",
            "  crayon BUILD-FILE -target BUILD-TARGET-NAME [OPTIONS...]",
            "",
            "Flags:",
            "",
            "  -target            When a build file is specified, selects the",
            "                     target within that build file to build.",
            "",
            "  -vm                Output a standalone VM for a platform.",
            "",
            "  -vmdir             Directory to output the VM to (when -vm is",
            "                     specified).",
            "");
#endif

        static void Main(string[] args)
        {
            using (new PerformanceSection("Crayon"))
            {
#if DEBUG
                args = GetEffectiveArgs(args);

                // First chance exceptions should crash in debug builds.
                Program.Compile(args);

                // Crash if there were any graphics contexts that weren't cleaned up.
                // This is okay on Windows, but on OSX this is a problem, so ensure that a
                // regressions are quickly noticed.
                SystemBitmap.Graphics.EnsureCleanedUp();

#else
                if (args.Length == 0)
                {
                    System.Console.WriteLine(USAGE);
                }
                else
                {
                    try
                    {
                        Program.Compile(args);
                    }
                    catch (InvalidOperationException e)
                    {
                        System.Console.Error.WriteLine(e.Message);
                    }
                    catch (ParserException e)
                    {
                        System.Console.Error.WriteLine(e.Message);
                    }
                }
#endif
            }
#if DEBUG
            string summary = PerformanceTimer.GetSummary();
            Console.WriteLine(summary);
#endif
        }

        private static string GetCommandLineFlagValue(string previousFlag, string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == previousFlag && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static string[] GetEffectiveArgs(string[] actualArgs)
        {
#if DEBUG
            if (actualArgs.Length == 0)
            {
                string crayonHome = System.Environment.GetEnvironmentVariable("CRAYON_HOME");
                if (crayonHome != null)
                {
                    string debugArgsFile = System.IO.Path.Combine(crayonHome, "DEBUG_ARGS.txt");
                    if (System.IO.File.Exists(debugArgsFile))
                    {
                        string[] debugArgs = System.IO.File.ReadAllText(debugArgsFile).Trim().Split('\n');
                        string lastArgSet = debugArgs[debugArgs.Length - 1].Trim();
                        if (lastArgSet.Length > 0)
                        {
                            return lastArgSet.Split(' ');
                        }
                    }
                }
            }
#endif
            return actualArgs;
        }

        private enum ExecutionType
        {
            EXPORT_VM_BUNDLE,
            EXPORT_VM_STANDALONE,
            EXPORT_CBX,
            RUN_CBX,
            SHOW_USAGE,
        }

        private static ExecutionType IdentifyUseCase(string[] args)
        {
            if (args.Length == 0) return ExecutionType.SHOW_USAGE;

            foreach (string arg in args)
            {
                if (arg == "-vm" || arg == "-vmdir") return ExecutionType.EXPORT_VM_STANDALONE;
                if (arg == "-target") return ExecutionType.EXPORT_VM_BUNDLE;
                if (arg == "-cbx") return ExecutionType.EXPORT_CBX;
            }
            return ExecutionType.RUN_CBX;
        }

        private static void Compile(string[] args)
        {
            switch (IdentifyUseCase(args))
            {
                case ExecutionType.EXPORT_CBX:
                    new CbxExporter(args[0]).Export();
                    return;

                case ExecutionType.EXPORT_VM_BUNDLE:
                    Program.ExportVmBundle(args);
                    return;

                case ExecutionType.EXPORT_VM_STANDALONE:
                    Program.ExportStandaloneVm(args);
                    return;

                case ExecutionType.RUN_CBX:
                    string cbxFile = new CbxExporter(args[0]).Export().GetCbxPath();

                    string crayonRuntimePath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("CRAYON_HOME"), "vm", "CrayonRuntime.exe");
                    cbxFile = FileUtil.GetPlatformPath(cbxFile);
                    System.Diagnostics.Process appProcess = new System.Diagnostics.Process();

                    int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                    string flags = cbxFile + " vmpid:" + processId;

                    appProcess.StartInfo = new System.Diagnostics.ProcessStartInfo(crayonRuntimePath, flags)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    appProcess.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                    appProcess.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
                    appProcess.Start();
                    appProcess.BeginOutputReadLine();
                    appProcess.BeginErrorReadLine();
                    appProcess.WaitForExit();
                    return;

                case ExecutionType.SHOW_USAGE:
#if RELEASE
                    Console.WriteLine(USAGE);
#endif
                    return;

                default:
                    throw new Exception(); // unknown use case.
            }
        }
        
        private static void ExportStandaloneVm(string[] args)
        {
            using (new PerformanceSection("ExportStandaloneVm"))
            {
                Platform.AbstractPlatform standaloneVmPlatform = platformProvider.GetPlatform(GetCommandLineFlagValue("-vm", args));
                string targetDirectory = GetCommandLineFlagValue("-vmdir", args);
                if (targetDirectory == null || standaloneVmPlatform == null) throw new InvalidOperationException("-vm and -vmdir flags must both have correct values.");
                targetDirectory = FileUtil.FinalizeTilde(targetDirectory);
                VmGenerator vmGenerator = new VmGenerator();
                List<Library> allLibraries = new LibraryManager(platformProvider).GetAllAvailableLibraries(standaloneVmPlatform);
                Dictionary<string, FileOutput> result = vmGenerator.GenerateVmSourceCodeForPlatform(
                    standaloneVmPlatform,
                    null,
                    null,
                    allLibraries,
                    VmGenerationMode.EXPORT_VM_AND_LIBRARIES);
                FileOutputExporter exporter = new FileOutputExporter(targetDirectory);
                exporter.ExportFiles(result);
            }
        }

        public static ResourceDatabase PrepareResources(
            BuildContext buildContext,
            ByteBuffer nullableByteCode) // CBX files will not have this in the resources
        {
            using (new PerformanceSection("Program.PrepareResources"))
            {
                // This really needs to go in a separate helper file.
                ResourceDatabase resourceDatabase = ResourceDatabaseBuilder.CreateResourceDatabase(buildContext);
                if (nullableByteCode != null)
                {
                    resourceDatabase.ByteCodeFile = new FileOutput()
                    {
                        Type = FileOutputType.Text,
                        TextContent = ByteCodeEncoder.Encode(nullableByteCode),
                    };
                }

                using (new PerformanceSection("Program.PrepareResources/ImageSheetStuff"))
                {
                    Common.ImageSheets.ImageSheetBuilder imageSheetBuilder = new Common.ImageSheets.ImageSheetBuilder();
                    if (buildContext.ImageSheetIds != null)
                    {
                        foreach (string imageSheetId in buildContext.ImageSheetIds)
                        {
                            imageSheetBuilder.PrefixMatcher.RegisterId(imageSheetId);

                            foreach (string fileMatcher in buildContext.ImageSheetPrefixesById[imageSheetId])
                            {
                                imageSheetBuilder.PrefixMatcher.RegisterPrefix(imageSheetId, fileMatcher);
                            }
                        }
                    }
                    Common.ImageSheets.Sheet[] imageSheets = imageSheetBuilder.Generate(resourceDatabase);

                    resourceDatabase.AddImageSheets(imageSheets);
                }

                resourceDatabase.GenerateResourceMapping();

                return resourceDatabase;
            }
        }

        private static void ExportVmBundle(string[] args)
        {
            using (new PerformanceSection("ExportVmBundle"))
            {
                BuildContext buildContext = GetBuildContext(args);
                Platform.AbstractPlatform platform = GetPlatformInstance(buildContext);
                if (platform == null) throw new InvalidOperationException("Unrecognized platform. See usage.");

                CompilationBundle compilationResult = CompilationBundle.Compile(buildContext);

                // Need to re-instantiate the libraries. The libraries are instantiated in a platform-context-free
                // for the purpose of compiling the byte code. For the VM bundle, they need to know about the platform.
                Library[] libraries;
                using (new PerformanceSection("Program.ExportVmBundle.CloneLibraries"))
                {
                    libraries = compilationResult.LibrariesUsed
                        .Select(lib => lib.CloneWithNewPlatform(platform))
                        .ToArray();
                }

                ResourceDatabase resourceDatabase = PrepareResources(buildContext, compilationResult.ByteCode);

                VmGenerator vmGenerator = new VmGenerator();
                Dictionary<string, FileOutput> result = vmGenerator.GenerateVmSourceCodeForPlatform(
                    platform,
                    compilationResult,
                    resourceDatabase,
                    libraries,
                    VmGenerationMode.EXPORT_SELF_CONTAINED_PROJECT_SOURCE);

                string outputDirectory = buildContext.OutputFolder;
                if (!FileUtil.IsAbsolutePath(outputDirectory))
                {
                    outputDirectory = FileUtil.JoinPath(buildContext.ProjectDirectory, outputDirectory);
                }
                outputDirectory = FileUtil.GetCanonicalizeUniversalPath(outputDirectory);
                FileOutputExporter exporter = new FileOutputExporter(outputDirectory);

                exporter.ExportFiles(result);
            }
        }

        private static Platform.AbstractPlatform GetPlatformInstance(BuildContext buildContext)
        {
            string platformId = buildContext.Platform.ToLowerInvariant();
            return platformProvider.GetPlatform(platformId);
        }

        private static PlatformProvider platformProvider = new PlatformProvider();

        public static string GetValidatedCanonicalBuildFilePath(string originalBuildFilePath)
        {
            string buildFilePath = originalBuildFilePath;
            buildFilePath = FileUtil.FinalizeTilde(buildFilePath);
            if (!buildFilePath.StartsWith("/") &&
                !(buildFilePath.Length > 1 && buildFilePath[1] == ':'))
            {
                // Build file will always be absolute. So make it absolute if it isn't already.
                buildFilePath = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(), buildFilePath));

            }

            if (!System.IO.File.Exists(buildFilePath))
            {
                throw new InvalidOperationException("Build file does not exist: " + originalBuildFilePath);
            }

            return buildFilePath;
        }

        private static BuildContext GetBuildContext(string[] args)
        {
            using (new PerformanceSection("GetBuildContext"))
            {
                Dictionary<string, string> argLookup = Program.ParseArgs(args);

                string buildFile = argLookup.ContainsKey("buildfile") ? argLookup["buildfile"] : null;
                string target = argLookup.ContainsKey("target") ? argLookup["target"] : null;

                if (buildFile == null || target == null)
                {
                    throw new InvalidOperationException("Build file and target must be specified together.");
                }

                buildFile = GetValidatedCanonicalBuildFilePath(buildFile);

                string projectDirectory = System.IO.Path.GetDirectoryName(buildFile);

                BuildContext buildContext = null;

                argLookup.Remove("buildfile");
                argLookup.Remove("target");
                projectDirectory = System.IO.Path.GetDirectoryName(buildFile);

                buildContext = BuildContext.Parse(projectDirectory, System.IO.File.ReadAllText(buildFile), target);


                buildContext = buildContext ?? new BuildContext();

                // command line arguments override build file values if present.

                if (buildContext.Platform == null)
                    throw new InvalidOperationException("No platform specified in build file.");

                if (buildContext.SourceFolders.Length == 0)
                    throw new InvalidOperationException("No source folder specified in build file.");

                if (buildContext.OutputFolder == null)
                    throw new InvalidOperationException("No output folder specified in build file.");

                buildContext.OutputFolder = FileUtil.JoinAndCanonicalizePath(projectDirectory, buildContext.OutputFolder);
                if (buildContext.IconFilePath != null)
                {
                    buildContext.IconFilePath = FileUtil.JoinAndCanonicalizePath(projectDirectory, buildContext.IconFilePath);
                }

                foreach (FilePath sourceFolder in buildContext.SourceFolders)
                {
                    if (!FileUtil.DirectoryExists(sourceFolder.AbsolutePath))
                    {
                        throw new InvalidOperationException("Source folder does not exist.");
                    }
                }

                buildContext.ProjectID = buildContext.ProjectID ?? "Untitled";

                return buildContext;
            }
        }

        // TODO: remove this and just use the simple extractor O(n^2) is worth getting rid of extra code when there's only a handful of args.
        private static readonly HashSet<string> ATOMIC_FLAGS = new HashSet<string>("min readablebytecode".Split(' '));
        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; ++i)
            {
                if (!args[i].StartsWith("-"))
                {
                    output["buildfile"] = args[i];
                }
                else
                {
                    string flagName = args[i].Substring(1);
                    if (flagName.Length == 0)
                    {
                        continue;
                    }

                    if (ATOMIC_FLAGS.Contains(flagName.ToLowerInvariant()))
                    {
                        output[flagName] = "true";
                    }
                    else if (i + 1 < args.Length)
                    {
                        output[flagName] = args[++i];
                    }
                    else
                    {
                        output[flagName] = "true";
                    }
                }
            }

            return output;
        }
    }
}
