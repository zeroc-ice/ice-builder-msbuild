// **********************************************************************
//
// Copyright (c) 2009-2018 ZeroC, Inc. All rights reserved.
//
// **********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Xml;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IceBuilder.MSBuild
{
    public class TaskUtil
    {
#if NETSTANDARD2_0
        public static readonly bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
        public static readonly bool isWindows = true;
#endif
        public static string MakeRelative(string from, string to)
        {
            if(!Path.IsPathRooted(from))
            {
                throw new ArgumentException(string.Format("from: `{0}' must be an absolute path", from));
            }
            else if(!Path.IsPathRooted(to))
            {
                return to;
            }

            string[] firstPathParts =
                Path.GetFullPath(from).Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            string[] secondPathParts =
                Path.GetFullPath(to).Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);

            int sameCounter = 0;
            while(sameCounter < Math.Min(firstPathParts.Length, secondPathParts.Length) &&
                  string.Equals(firstPathParts[sameCounter], secondPathParts[sameCounter],
                                StringComparison.CurrentCultureIgnoreCase))
            {
                ++sameCounter;
            }

            // Different volumes, relative path not possible.
            if(sameCounter == 0)
            {
                return to;
            }

            // Pop back up to the common point.
            string newPath = "";
            for(int i = sameCounter; i < firstPathParts.Length; ++i)
            {
                newPath += ".." + Path.DirectorySeparatorChar;
            }
            // Descend to the target.
            for(int i = sameCounter; i < secondPathParts.Length; ++i)
            {
                newPath += secondPathParts[i] + Path.DirectorySeparatorChar;
            }
            return newPath.TrimEnd(Path.DirectorySeparatorChar);
        }

        public class StreamReader
        {
            public string Output
            {
                get
                {
                    return _output;
                }
            }

            public string Error
            {
                get
                {
                    return _error;
                }
            }
            public void ouput(object sendingProcess, DataReceivedEventArgs outLine)
            {
                if(outLine.Data != null)
                {
                    _output += outLine.Data;
                }
            }

            public void error(object sendingProcess, DataReceivedEventArgs outLine)
            {
                if(outLine.Data != null)
                {
                    _error += outLine.Data;
                }
            }

            private string _output;
            private string _error;
        }

        public static int RunCommand(string workingDir, string command, string args, ref string output, ref string error)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = args;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WorkingDirectory = workingDir;

            var streamReader = new StreamReader();
            process.OutputDataReceived += new DataReceivedEventHandler(streamReader.ouput);
            process.ErrorDataReceived += new DataReceivedEventHandler(streamReader.error);

            try
            {
                process.Start();

                //
                // When StandardError and StandardOutput are redirected, at least one
                // should use asynchronous reads to prevent deadlocks when calling
                // process.WaitForExit; the other can be read synchronously using ReadToEnd.
                //
                // See the Remarks section in the below link:
                //
                // http://msdn.microsoft.com/en-us/library/system.diagnostics.process.standarderror.aspx
                //

                // Start the asynchronous read of the standard output stream.
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                // Read Standard error.
                process.WaitForExit();
                error = streamReader.Error;
                output = streamReader.Output;
                return process.ExitCode;
            }
            catch(Exception ex)
            {
                error = ex.ToString();
                return 1;
            }
        }
    }

    public abstract class SliceCompilerTask : ToolTask
    {
        [Required]
        public string WorkingDirectory
        {
            get;
            set;
        }

        [Required]
        public string IceHome
        {
            get;
            set;
        }

        [Required]
        public string IceToolsPath
        {
            get;
            set;
        }

        [Required]
        public string OutputDir
        {
            get;
            set;
        }

        [Required]
        public ITaskItem[] Sources
        {
            get;
            set;
        }

        public string[] IncludeDirectories
        {
            get;
            set;
        }

        public string[] AdditionalOptions
        {
            get;
            set;
        }

        [Output]
        public ITaskItem[] ComputedSources
        {
            get;
            private set;
        }

        protected override string GetWorkingDirectory()
        {
            return WorkingDirectory;
        }

        protected virtual string GetGeneratedPath(ITaskItem item, string outputDir, string ext)
        {
            return Path.Combine(outputDir,
                                Path.GetFileName(Path.ChangeExtension(item.GetMetadata("Identity"), ext)));
        }

        protected abstract string GeneratedExtensions
        {
            get;
        }

        protected virtual Dictionary<string, string> GetOptions()
        {
            var options = new Dictionary<string, string>();
            options["IceHome"] = IceHome;
            options["IceToolsPath"] = IceToolsPath;
            options["OutputDir"] = OutputDir.TrimEnd('\\');
            if(IncludeDirectories != null && IncludeDirectories.Length > 0)
            {
                options["IncludeDirectories"] = string.Join(";", IncludeDirectories);
            }
            if(AdditionalOptions != null)
            {
                options["AdditionalOptions"] = string.Join(";", AdditionalOptions);
            }
            return options;
        }

        protected abstract void TraceGenerated();

        protected override string GenerateCommandLineCommands()
        {
            UsageError = false;
            CommandLineBuilder builder = new CommandLineBuilder(false);

            if(!string.IsNullOrEmpty(OutputDir))
            {
                builder.AppendSwitch("--output-dir");
                builder.AppendFileNameIfNotNull(OutputDir);
            }

            if(IncludeDirectories != null)
            {
                foreach(string path in IncludeDirectories)
                {
                    builder.AppendSwitchIfNotNull("-I", path);
                }
                builder.AppendSwitchIfNotNull("-I", Path.Combine(IceHome, "slice"));
            }

            if(AdditionalOptions != null)
            {
                foreach(var option in AdditionalOptions)
                {
                    builder.AppendTextUnquoted(" ");
                    builder.AppendTextUnquoted(option);
                }
            }

            builder.AppendFileNamesIfNotNull(Sources, " ");

            return builder.ToString();
        }

        protected abstract ITaskItem[] GeneratedItems(ITaskItem source);

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            int status = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

            if(status == 0)
            {
                TraceGenerated();

                //
                // Recompue dependencies
                //
                string output = "";
                string error = "";
                status = TaskUtil.RunCommand(WorkingDirectory, pathToTool, commandLineCommands + " --depend-xml",
                                             ref output, ref error);
                if(status == 0)
                {
                    List<ITaskItem> computed = new List<ITaskItem>();
                    XmlDocument dependsDoc = new XmlDocument();
                    dependsDoc.LoadXml(output);

                    foreach(ITaskItem source in Sources)
                    {
                        var inputs = new List<string>();
                        var depends = dependsDoc.DocumentElement.SelectNodes(
                            string.Format("/dependencies/source[@name='{0}']/dependsOn",
                                          source.GetMetadata("Identity")));
                        if(depends != null)
                        {
                            foreach(XmlNode depend in depends)
                            {
                                inputs.Add(depend.Attributes["name"].Value);
                            }
                        }

                        //
                        // Save the dependencies for each source to a dependency file
                        //
                        // Foo.ice -> $(OutputDir)/SliceCompile.Foo.d
                        //
                        var doc = new XDocument(
                            new XDeclaration("1.0", "utf-8", "yes"),
                            new XElement("dependencies",
                                new XElement("source", new XAttribute("name", source.GetMetadata("Identity")),
                                    inputs.Select(path => new XElement(path)),
                                    new XElement("options",
                                        GetOptions().Select(e => new XElement(e.Key, e.Value))))));

                        doc.Save(Path.Combine(OutputDir,
                                              string.Format("SliceCompile.{0}.d", source.GetMetadata("Filename"))));
                        //
                        // Update the Inputs and Outputs metadata of the output sources,
                        // these info will be use to write the TLog files
                        //
                        inputs = inputs.Select(path => Path.GetFullPath(path)).ToList();
                        inputs.Add(source.GetMetadata("FullPath").ToUpper());
                        inputs.Add(Path.GetFullPath(pathToTool).ToUpper());

                        ITaskItem computedSource = new TaskItem(source.ItemSpec);
                        source.CopyMetadataTo(computedSource);
                        var outputs = GeneratedItems(source).Select((item) => item.GetMetadata("FullPath").ToUpper());
                        computedSource.SetMetadata("Outputs", string.Join(";", outputs));
                        computedSource.SetMetadata("Inputs", string.Join(";", inputs));
                        computed.Add(computedSource);
                    }
                    ComputedSources = computed.ToArray();
                }
            }
            return status;
        }

        protected override string GenerateFullPathToTool()
        {
            string path = Path.Combine(IceToolsPath, ToolName);
            if(!File.Exists(path))
            {
                Log.LogError(string.Format("Slice compiler `{0}' not found. Review Ice Home setting", path));
            }
            return path;
        }

        protected override void LogToolCommand(string message)
        {
            Log.LogMessage(MessageImportance.Low, message);
        }

        private bool UsageError
        {
            get;
            set;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if(UsageError)
            {
                return;
            }

            int i = singleLine.IndexOf(string.Format("{0}:", ToolName));
            if(i != -1)
            {
                i += (ToolName.Length + 1);
                Log.LogError("", "", "", "", 0, 0, 0, 0,
                             string.Format("{0}: {1}", Path.GetFileName(ToolName), singleLine.Substring(i)));
                UsageError = true;
            }
            else
            {
                string s = singleLine.Trim();
                if(s.StartsWith(WorkingDirectory))
                {
                    s = s.Substring(WorkingDirectory.Length);
                }

                string file = "";
                int line = 0;
                string description = "";

                //
                // Skip the drive letter
                //
                i = s.IndexOf(":");
                if(i <= 1 && s.Length > i + 1)
                {
                    i = s.IndexOf(":", i + 1);
                }

                if(i != -1)
                {
                    file = Path.GetFullPath(s.Substring(0, i).Trim().Trim('"'));
                    if(file.IndexOf(WorkingDirectory) != -1)
                    {
                        file = file.Substring(WorkingDirectory.Length)
                            .Trim(Path.DirectorySeparatorChar);
                    }

                    if(s.Length > i + 1)
                    {
                        s = s.Substring(i + 1);

                        i = s.IndexOf(":");
                        if(i != -1)
                        {
                            if(int.TryParse(s.Substring(0, i), out line))
                            {
                                if(s.Length > i + 1)
                                {
                                    s = s.Substring(i + 1);
                                }
                            }
                            else
                            {
                                s = s.Substring(i);
                            }
                        }

                        description = s.Trim();
                        description += Environment.NewLine;
                    }
                }

                if(description.IndexOf("warning:") == 0)
                {
                    Log.LogWarning("", "", "", file, line - 1, 0, 0, 0, description.Substring("warning:".Length));
                }
                else if(description.IndexOf("error:") == 0)
                {
                    Log.LogError("", "", "", file, line - 1, 0, 0, 0, description.Substring("error:".Length));
                }
                else if(!string.IsNullOrEmpty(description))
                {
                    Log.LogError("", "", "", file, line - 1, 0, 0, 0, description);
                }
            }
        }
    }

    public class Slice2CppTask : SliceCompilerTask
    {
        public Slice2CppTask()
        {
            HeaderExt = "h";
            SourceExt = "cpp";
        }

        protected override string ToolName
        {
            get
            {
                return "slice2cpp.exe";
            }
        }

        public string HeaderOutputDir
        {
            get;
            set;
        }
        [Required]
        public string HeaderExt
        {
            get;
            set;
        }
        [Required]
        public string SourceExt
        {
            get;
            set;
        }

        public string BaseDirectoryForGeneratedInclude
        {
            get;
            set;
        }

        protected override string GeneratedExtensions
        {
            get
            {
                return string.Format("{0},{1}", HeaderExt, SourceExt);
            }
        }

        protected override ITaskItem[] GeneratedItems(ITaskItem source)
        {
            return new ITaskItem[]
            {
                new TaskItem(GetGeneratedPath(source, OutputDir, SourceExt)),
                new TaskItem(GetGeneratedPath(source,
                                              string.IsNullOrEmpty(HeaderOutputDir) ? OutputDir : HeaderOutputDir,
                                              HeaderExt)),
            };
        }

        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder builder = new CommandLineBuilder(false);
            if(!HeaderExt.Equals("h"))
            {
                builder.AppendSwitch("--header-ext");
                builder.AppendFileNameIfNotNull(HeaderExt);
            }

            if(!SourceExt.Equals("cpp"))
            {
                builder.AppendSwitch("--source-ext");
                builder.AppendFileNameIfNotNull(SourceExt);
            }

            if(!string.IsNullOrEmpty(BaseDirectoryForGeneratedInclude))
            {
                builder.AppendSwitch("--include-dir");
                builder.AppendFileNameIfNotNull(BaseDirectoryForGeneratedInclude);
            }
            builder.AppendTextUnquoted(" ");
            builder.AppendTextUnquoted(base.GenerateCommandLineCommands());

            return builder.ToString();
        }

        protected override Dictionary<string, string> GetOptions()
        {
            var options = base.GetOptions();
            if(!string.IsNullOrEmpty(HeaderOutputDir))
            {
                options["HeaderOutputDir"] = HeaderOutputDir;
            }
            if(!string.IsNullOrEmpty(BaseDirectoryForGeneratedInclude))
            {
                options["BaseDirectoryForGeneratedInclude"] = BaseDirectoryForGeneratedInclude;
            }
            options["SourceExt"] = SourceExt;
            options["HeaderExt"] = HeaderExt;
            return options;
        }

        protected override void TraceGenerated()
        {
            var headerOutputDir = string.IsNullOrEmpty(HeaderOutputDir) ? OutputDir : HeaderOutputDir;
            foreach(ITaskItem source in Sources)
            {
                var cppSource = GetGeneratedPath(source, OutputDir, SourceExt);
                var cppHeader = GetGeneratedPath(source, headerOutputDir, HeaderExt);
                Log.LogMessage(MessageImportance.High,
                               string.Format("Compiling {0} Generating -> {1} and {2}",
                                             source.GetMetadata("Identity"),
                                             TaskUtil.MakeRelative(WorkingDirectory, cppSource),
                                             TaskUtil.MakeRelative(WorkingDirectory, cppHeader)));
            }
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            int status = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            if(status == 0)
            {
                //
                // If HeaderOutputDir is set move the generated header to its final location
                //
                if(!string.IsNullOrEmpty(HeaderOutputDir))
                {
                    foreach(ITaskItem source in Sources)
                    {
                        string sourceH = GetGeneratedPath(source, OutputDir, HeaderExt);
                        string targetH = GetGeneratedPath(source, HeaderOutputDir, HeaderExt);
                        if(File.Exists(targetH))
                        {
                            File.Delete(targetH);
                        }
                        File.Move(sourceH, targetH);
                    }
                }
            }
            return status;
        }
    }

    public class Slice2CSharpTask : SliceCompilerTask
    {
        protected override string ToolName
        {
            get
            {
                return TaskUtil.isWindows ? "slice2cs.exe" : "slice2cs";
            }
        }

        protected override string GeneratedExtensions
        {
            get
            {
                return "cs";
            }
        }

        protected override void TraceGenerated()
        {
            foreach(ITaskItem source in Sources)
            {
                string message = string.Format("Compiling {0} Generating -> ", source.GetMetadata("Identity"));
                message += TaskUtil.MakeRelative(WorkingDirectory, GetGeneratedPath(source, OutputDir, ".cs"));
                Log.LogMessage(MessageImportance.High, message);
            }
        }

        protected override ITaskItem[] GeneratedItems(ITaskItem source)
        {
            return new ITaskItem[]
            {
                new TaskItem(GetGeneratedPath(source, OutputDir, ".cs"))
            };
        }
    }

    public abstract class SliceDependTask : Task
    {
        [Required]
        public ITaskItem[] Sources
        {
            get;
            set;
        }

        [Required]
        public string IceHome
        {
            get;
            set;
        }

        [Required]
        public string IceToolsPath
        {
            get;
            set;
        }

        [Required]
        public string WorkingDirectory
        {
            get;
            set;
        }
        [Output]
        public ITaskItem[] ComputedSources
        {
            get;
            private set;
        }

        protected abstract string ToolName
        {
            get;
        }

        abstract protected ITaskItem[] GeneratedItems(ITaskItem source);

        protected virtual string GetGeneratedPath(ITaskItem item, string outputDir, string ext)
        {
            return Path.Combine(outputDir,
                                Path.GetFileName(Path.ChangeExtension(item.GetMetadata("Identity"), ext)));
        }

        public virtual Dictionary<string, string> GetOptions(ITaskItem item)
        {
            var options = new Dictionary<string, string>();
            options["IceHome"] = IceHome;
            options["IceToolsPath"] = IceToolsPath;
            options["OutputDir"] = item.GetMetadata("OutputDir").TrimEnd('\\');
            var value = item.GetMetadata("IncludeDirectories");
            if(!string.IsNullOrEmpty(value))
            {
                options["IncludeDirectories"] = value;
            }
            value = item.GetMetadata("AdditionalOptions");
            if(!string.IsNullOrEmpty(value))
            {
                options["AdditionalOptions"] = value;
            }
            return options;
        }

        public override bool Execute()
        {
            List<ITaskItem> computed = new List<ITaskItem>();
            foreach(ITaskItem source in Sources)
            {
                bool skip = true;
                Log.LogMessage(MessageImportance.Low,
                               string.Format("Computing dependencies for {0}", source.GetMetadata("Identity")));
                var sourceInfo = new FileInfo(source.GetMetadata("FullPath"));
                if(!sourceInfo.Exists)
                {
                    Log.LogMessage(MessageImportance.Low,
                                   string.Format("Build required because source: {0} doesn't exists",
                                                 source.GetMetadata("Identity")));
                    skip = false;
                }

                var generatedItems = GeneratedItems(source);
                FileInfo generatedInfo = null;
                FileInfo dependInfo = null;
                //
                // Check if the Slice compiler is older than the source file
                //
                var sliceCompiler = new FileInfo(Path.Combine(IceToolsPath, ToolName));
                if(skip)
                {
                    foreach(ITaskItem item in generatedItems)
                    {
                        generatedInfo = new FileInfo(item.GetMetadata("FullPath"));

                        if(generatedInfo.Exists &&
                            sliceCompiler.LastWriteTime.ToFileTime() > generatedInfo.LastWriteTime.ToFileTime())
                        {
                            Log.LogMessage(MessageImportance.Low,
                                string.Format("Build required because target: {0} is older than Slice compiler: {1}",
                                              TaskUtil.MakeRelative(WorkingDirectory, generatedInfo.FullName),
                                              ToolName));
                            skip = false;
                            break;
                        }
                    }
                }

                XmlDocument dependsDoc = new XmlDocument();
                if(skip)
                {
                    dependInfo = new FileInfo(Path.Combine(WorkingDirectory, source.GetMetadata("OutputDir"),
                        string.Format("SliceCompile.{0}.d", Path.GetFileNameWithoutExtension(sourceInfo.Name))));
                    //
                    // Check that the depdend file exists
                    //
                    if(!dependInfo.Exists)
                    {
                        Log.LogMessage(MessageImportance.Low,
                            string.Format("Build required because depend file: {0} doesn't exists",
                                          TaskUtil.MakeRelative(WorkingDirectory, dependInfo.FullName)));
                        skip = false;
                    }
                    //
                    // Check that the depend file is older than the corresponding Slice source
                    //
                    else if(sourceInfo.LastWriteTime.ToFileTime() > dependInfo.LastWriteTime.ToFileTime())
                    {
                        Log.LogMessage(MessageImportance.Low,
                            string.Format("Build required because source: {0} is older than depend file {1}",
                                          source.GetMetadata("Identity"),
                                          TaskUtil.MakeRelative(WorkingDirectory, dependInfo.FullName)));
                        skip = false;
                    }
                    else
                    {
                        try
                        {
                            dependsDoc.Load(dependInfo.FullName);
                        }
                        catch(XmlException)
                        {
                            try
                            {
                                File.Delete(dependInfo.FullName);
                            }
                            catch(IOException)
                            {
                            }
                            Log.LogMessage(MessageImportance.Low,
                                string.Format("Build required because depend file: {0} has some invalid data",
                                              TaskUtil.MakeRelative(WorkingDirectory, dependInfo.FullName)));
                            skip = false;
                        }
                    }
                }

                if(skip)
                {
                    foreach(ITaskItem item in generatedItems)
                    {
                        generatedInfo = new FileInfo(item.GetMetadata("FullPath"));
                        //
                        // Check that the generated file exists
                        //
                        if(!generatedInfo.Exists)
                        {
                            Log.LogMessage(MessageImportance.Low,
                                string.Format("Build required because generated: {0} doesn't exists",
                                              TaskUtil.MakeRelative(WorkingDirectory, generatedInfo.FullName)));
                            skip = false;
                            break;
                        }
                        //
                        // Check that the generated file is older than the corresponding Slice source
                        //
                        else if(sourceInfo.LastWriteTime.ToFileTime() > generatedInfo.LastWriteTime.ToFileTime())
                        {
                            Log.LogMessage(MessageImportance.Low,
                                string.Format("Build required because source: {0} is older than target {1}",
                                              source.GetMetadata("Identity"),
                                              TaskUtil.MakeRelative(WorkingDirectory, generatedInfo.FullName)));
                            skip = false;
                            break;
                        }
                    }
                }

                if(skip)
                {
                    XmlNodeList options = dependsDoc.DocumentElement.SelectNodes(
                        string.Format("/dependencies/source[@name='{0}']/options/child::node()",
                                      source.GetMetadata("Identity")));
                    if(options != null)
                    {
                        var newOptions = GetOptions(source);
                        var oldOptions = options.Cast<XmlNode>().Select(node => new
                        {
                            node.Name,
                            node.InnerXml
                        }).ToDictionary(t => t.Name, t => t.InnerXml);

                        if(newOptions.Except(oldOptions).Any() || oldOptions.Except(newOptions).Any())
                        {
                            Log.LogMessage(MessageImportance.Low,
                                           string.Format("Build required because source: {0} build options change",
                                                         source.GetMetadata("Identity")));
                            skip = false;
                        }
                    }
                }

                if(skip)
                {
                    XmlNodeList depends = dependsDoc.DocumentElement.SelectNodes(
                        string.Format("/dependencies/source[@name='{0}']/dependsOn", source.GetMetadata("Identity")));

                    if(depends != null)
                    {
                        var inputs = new List<string>();
                        foreach(XmlNode depend in depends)
                        {
                            string path = depend.Attributes["name"].Value;
                            FileInfo dependencyInfo = new FileInfo(path);
                            if(!dependencyInfo.Exists)
                            {
                                Log.LogMessage(MessageImportance.Low,
                                    string.Format("Build required because dependency: {0} doesn't exists",
                                                  TaskUtil.MakeRelative(WorkingDirectory, dependencyInfo.FullName)));
                                skip = false;
                                break;
                            }
                            else if(dependencyInfo.LastWriteTime > generatedInfo.LastWriteTime)
                            {
                                Log.LogMessage(MessageImportance.Low,
                                    string.Format("Build required because source: {0} is older than target: {1}",
                                                  source.GetMetadata("Identity"),
                                                  TaskUtil.MakeRelative(WorkingDirectory, dependencyInfo.FullName)));
                                skip = false;
                                break;
                            }

                            inputs.Add(Path.GetFullPath(depend.Attributes["name"].Value).ToUpper());
                        }
                        inputs.Add(source.GetMetadata("FullPath").ToUpper());
                        inputs.Add(sliceCompiler.FullName.ToUpper());

                        var outputs = GeneratedItems(source).Select(item => item.GetMetadata("FullPath").ToUpper());
                        source.SetMetadata("Outputs", string.Join(";", outputs));
                        source.SetMetadata("Inputs", string.Join(";", inputs));
                    }
                }

                if(skip)
                {
                    string message = string.Format("Skipping {0} -> ", source.GetMetadata("Identity"));
                    message += generatedItems[0].GetMetadata("Identity");
                    if(generatedItems.Length > 1)
                    {
                        message += " and ";
                        message += generatedItems[1].GetMetadata("Identity");
                        message += " are ";
                    }
                    else
                    {
                        message += " is ";
                    }
                    message += "up to date";

                    Log.LogMessage(MessageImportance.Normal, message);
                }

                    ITaskItem computedSource = new TaskItem(source.ItemSpec);
                    source.CopyMetadataTo(computedSource);
                    computedSource.SetMetadata("BuildRequired", skip ? "False" : "True");
                    computedSource.SetMetadata("OutputDir", computedSource.GetMetadata("OutputDir").TrimEnd('\\'));
                    computed.Add(computedSource);
            }
            ComputedSources = computed.ToArray();
            return true;
        }
    }

    public class Slice2CppDependTask : SliceDependTask
    {
        protected override string ToolName
        {
            get
            {
                return "slice2cpp.exe";
            }
        }

        public override Dictionary<string, string> GetOptions(ITaskItem item)
        {
            var options = base.GetOptions(item);
            var value = item.GetMetadata("HeaderOutputDir");
            if(!string.IsNullOrEmpty(value))
            {
                options["HeaderOutputDir"] = value;
            }
            value = item.GetMetadata("BaseDirectoryForGeneratedInclude");
            if(!string.IsNullOrEmpty(value))
            {
                options["BaseDirectoryForGeneratedInclude"] = value;
            }
            value = item.GetMetadata("HeaderExt");
            if(!string.IsNullOrEmpty(value))
            {
                options["HeaderExt"] = value;
            }
            value = item.GetMetadata("SourceExt");
            if(!string.IsNullOrEmpty(value))
            {
                options["SourceExt"] = value;
            }
            return options;
        }

        protected override ITaskItem[] GeneratedItems(ITaskItem source)
        {
            var outputDir = source.GetMetadata("OutputDir");
            var headerOutputDir = source.GetMetadata("HeaderOutputDir");
            if(string.IsNullOrEmpty(headerOutputDir))
            {
                headerOutputDir = outputDir;
            }
            var sourceExt = source.GetMetadata("SourceExt");
            var headerExt = source.GetMetadata("HeaderExt");

            return new ITaskItem[]
            {
                new TaskItem(GetGeneratedPath(source, outputDir, sourceExt)),
                new TaskItem(GetGeneratedPath(source, headerOutputDir, headerExt))
            };
        }
    }

    public class Slice2CSharpDependTask : SliceDependTask
    {
        protected override ITaskItem[] GeneratedItems(ITaskItem source)
        {
            return new ITaskItem[]
            {
                new TaskItem(GetGeneratedPath(source, source.GetMetadata("OutputDir"), ".cs")),
            };
        }

        protected override string ToolName
        {
            get
            {
                return TaskUtil.isWindows ? "slice2cs.exe" : "slice2cs";
            }
        }
    }
}
