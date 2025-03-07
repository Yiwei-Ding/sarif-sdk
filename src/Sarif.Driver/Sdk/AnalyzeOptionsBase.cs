﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using CommandLine;

namespace Microsoft.CodeAnalysis.Sarif.Driver
{
    [Verb("analyze", HelpText = "Analyze one or more binary files for security and correctness issues.")]
    public abstract class AnalyzeOptionsBase : CommonOptionsBase
    {
        [Value(0,
               HelpText = "One or more specifiers to a file, directory, or filter pattern that resolves to one or more binaries to analyze.")]
        public IEnumerable<string> TargetFileSpecifiers { get; set; }

        [Option(
            'o',
            "output",
            HelpText = "File path to which analysis output will be written.")]
        public string OutputFilePath { get; set; }

        [Option(
            'r',
            "recurse",
            HelpText = "Recurse into subdirectories when evaluating file specifier arguments.")]
        public bool Recurse { get; set; }

        [Option(
            'c',
            "config",
            HelpText = "Path to policy file that will be used to configure analysis. This defaults to 'default.configuration.xml' beside the main tool; passing value of 'default' or removing that file will configure the tool to use its built-in settings.")]
        public string ConfigurationFilePath { get; set; }

        [Option(
            'q',
            "quiet",
            HelpText = "Suppress all console output (except for catastrophic tool runtime or configuration errors).")]
        public bool Quiet { get; set; }

        [Option(
            's',
            "statistics",
            HelpText = "Generate timing and other statistics for analysis session.")]
        [Obsolete()]
        public bool Statistics { get; set; }

        [Option(
            'h',
            "hashes",
            HelpText = "Output MD5, SHA1, and SHA-256 hash of analysis targets when emitting SARIF reports.")]
        [Obsolete("Use --insert instead, passing 'Hashes' along with any other references to data to be inserted.")]
        public bool ComputeFileHashes { get; set; }

        [Option(
            'e',
            "environment",
            HelpText = "Log machine environment details of run to output file. WARNING: This option records potentially sensitive information (such as all environment variable values) to any emitted log.")]
        public bool LogEnvironment { get; set; }

        [Option(
            "plugin",
            Separator = ';',
            HelpText = "Path to plugin that will be invoked against all targets in the analysis set.")]
        public IEnumerable<string> PluginFilePaths { get; set; }

        [Option(
            "invocation-properties",
            Separator = ';',
            HelpText = "Properties of the Invocation object to log. NOTE: StartTime and EndTime are always logged.")]
        public IEnumerable<string> InvocationPropertiesToLog { get; set; }

        [Option(
            "rich-return-code",
            HelpText = "Emit a 'rich' return code consisting of a bitfield of conditions (as opposed to 0 or 1 indicating success or failure.")]
        public bool RichReturnCode { get; set; }

        [Option(
            'z',
            "optimize",
            HelpText = "Omit redundant properties, producing a smaller but non-human-readable log.")]
        public bool Optimize { get; set; }

        [Option(
            "trace",
            Separator = ';',
            Default = new string[] { },
            HelpText = "Execution traces, expressed as a semicolon-delimited list, that " +
                       "should be emitted to the console and log file (if appropriate). " +
                       "Valid values: ScanTime.")]
        public virtual IEnumerable<string> Traces { get; set; }

        [Option(
            "level",
            Separator = ';',
            Default = new FailureLevel[] { FailureLevel.Error, FailureLevel.Warning },
            HelpText = "A semicolon delimited list to filter output of scan results to one or more failure levels. Valid values: Error, Warning and Note.")]
        public IEnumerable<FailureLevel> Level { get; set; }

        [Option(
            "kind",
            Separator = ';',
            Default = new ResultKind[] { ResultKind.Fail },
            HelpText = "A semicolon delimited list to filter output to one or more result kinds. Valid values: Fail (for literal scan results), Pass, Review, Open, NotApplicable and Informational.")]
        public IEnumerable<ResultKind> Kind { get; set; }

        [Option(
            "baseline",
            HelpText = "A SARIF file to be used as baseline.")]
        public string BaselineSarifFile { get; set; }

        [Option(
            "postUri",
            HelpText = "A URI to which the SARIF log file will be posted.")]
        public string PostUri { get; set; }
    }
}
