// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif.Driver;

using Moq;

using Xunit;

namespace Microsoft.CodeAnalysis.Sarif.Multitool
{
    public class ValidateCommandTests
    {
        private static readonly ResourceExtractor Extractor = new ResourceExtractor(typeof(ValidateCommandTests));

        // A simple schema against which a SARIF log file successfully validates.
        // This way, we don't have to read the SARIF schema from disk to run these tests.
        private const string SchemaFileContents =
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""object""
}";

        private const string SchemaFilePath = @"c:\schemas\SimpleSchemaForTest.json";
        private const string LogFileDirectory = @"C:\Users\John\logs";
        private const string LogFileName = "example.sarif";
        private const string OutputFilePath = @"C:\Users\John\output\example-validation.sarif";

        [Fact]
        [Trait(TestTraits.Bug, "https://github.com/microsoft/sarif-sdk/issues/1064")]
        public void ValidateCommand_AcceptsTargetFileWithSpaceInName()
        {
            // Here's the space:
            const string LogFileDirectoryWithSpace = @"c:\Users\John Smith\logs";

            string logFilePath = Path.Combine(LogFileDirectoryWithSpace, LogFileName);

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(x => x.DirectoryExists(LogFileDirectoryWithSpace)).Returns(true);
            mockFileSystem.Setup(x => x.DirectoryEnumerateFiles(It.IsAny<string>())).Returns(new string[0]);
            mockFileSystem.Setup(x => x.DirectoryGetFiles(LogFileDirectoryWithSpace, LogFileName)).Returns(new string[] { logFilePath });
            mockFileSystem.Setup(x => x.FileReadAllText(logFilePath)).Returns(RewriteCommandTests.MinimalCurrentV2Text);
            mockFileSystem.Setup(x => x.FileReadAllText(SchemaFilePath)).Returns(SchemaFileContents);

            var validateCommand = new ValidateCommand(mockFileSystem.Object);

            var options = new ValidateOptions
            {
                SchemaFilePath = SchemaFilePath,
                TargetFileSpecifiers = new string[] { logFilePath },
                Kind = new List<ResultKind> { ResultKind.Fail },
                Level = new List<FailureLevel> { FailureLevel.Warning, FailureLevel.Error }
            };

            int returnCode = validateCommand.Run(options);
            returnCode.Should().Be(0);
        }

        [Fact]
        [Trait(TestTraits.Bug, "https://github.com/microsoft/sarif-sdk/issues/1340")]
        public void ValidateCommand_WhenOutputFileIsPresentAndForceOptionIsAbsent_DoesNotOverwriteOutputFile()
        {
            string logFilePath = Path.Combine(LogFileDirectory, LogFileName);

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(x => x.FileExists(OutputFilePath)).Returns(true);
            mockFileSystem.Setup(x => x.DirectoryExists(LogFileDirectory)).Returns(true);
            mockFileSystem.Setup(x => x.DirectoryGetDirectories(It.IsAny<string>())).Returns(new string[0]);
            mockFileSystem.Setup(x => x.DirectoryGetFiles(LogFileDirectory, LogFileName)).Returns(new string[] { logFilePath });
            mockFileSystem.Setup(x => x.FileReadAllText(logFilePath)).Returns(RewriteCommandTests.MinimalCurrentV2Text);
            mockFileSystem.Setup(x => x.FileReadAllText(SchemaFilePath)).Returns(SchemaFileContents);

            var validateCommand = new ValidateCommand(mockFileSystem.Object);

            var options = new ValidateOptions
            {
                SchemaFilePath = SchemaFilePath,
                TargetFileSpecifiers = new string[] { logFilePath },
                OutputFilePath = OutputFilePath,
                Kind = new List<ResultKind> { ResultKind.Fail },
                Level = new List<FailureLevel> { FailureLevel.Warning, FailureLevel.Error }
            };

            int returnCode = validateCommand.Run(options);
            returnCode.Should().Be(1);
            validateCommand.ExecutionException.Should().BeOfType<ExitApplicationException<ExitReason>>();
        }

        [Fact]
        public void WhenWeDoNotHaveResultsWithoutVerbose()
        {
            string path = "ValidateSarif.sarif";
            string outputPath = "ValidateSarifOutput.sarif";
            File.WriteAllText(path, Extractor.GetResourceText($"ValidateCommand.{path}"));

            SarifLog sarifLog = ExecuteTest(path, outputPath);
            sarifLog.Runs.Count.Should().Be(1);
            sarifLog.Runs[0].Results.Should().BeEmpty();
        }

        [Fact]
        public void WhenWeDoHaveConfigurationChangingFailureLevelXml()
        {
            string path = "ValidateSarif.sarif";
            string configuration = "Configuration.xml";
            string outputPath = "ValidateSarifOutput.sarif";
            File.WriteAllText(path, Extractor.GetResourceText($"ValidateCommand.{path}"));
            File.WriteAllText(configuration, Extractor.GetResourceText($"ValidateCommand.{configuration}"));

            SarifLog sarifLog = ExecuteTest(path, outputPath, configuration);
            sarifLog.Runs.Count.Should().Be(1);
            sarifLog.Runs[0].Results.Count.Should().Be(1);
        }

        [Fact]
        public void WhenWeDoHaveConfigurationChangingFailureLevelJson()
        {
            string path = "ValidateSarif.sarif";
            string configuration = "Configuration.json";
            string outputPath = "ValidateSarifOutput.sarif";
            File.WriteAllText(path, Extractor.GetResourceText($"ValidateCommand.{path}"));
            File.WriteAllText(configuration, Extractor.GetResourceText($"ValidateCommand.{configuration}"));

            SarifLog sarifLog = ExecuteTest(path, outputPath, configuration);
            sarifLog.Runs.Count.Should().Be(1);
            sarifLog.Runs[0].Results.Count.Should().Be(1);
        }

        private static SarifLog ExecuteTest(string path, string outputPath, string configuration = null)
        {
            var options = new ValidateOptions
            {
                TargetFileSpecifiers = new string[] { path },
                OutputFilePath = outputPath,
                Force = true,
                ConfigurationFilePath = configuration,
                Kind = new List<ResultKind> { ResultKind.Fail },
                Level = new List<FailureLevel> { FailureLevel.Warning, FailureLevel.Error }
            };

            // Verify command returned success
            int returnCode = new ValidateCommand().Run(options);
            returnCode.Should().Be(0);

            return SarifLog.Load(outputPath);
        }

        // Note: I would have liked to provide tests for two other conditions:
        //
        // 1. When the output file is present and the force option is present.
        // 2. When the output file is absent (regardless of force option).
        //
        // The problem is that in both cases, AnalyzeCommandBase creates a SarifLogger that
        // creates the output file by way of a FileStream (see the SarifLogger ctor).
        // The FileStream ctor uses the real file system under the covers; we don't get a
        // chance to inject our mock file system.
        //
        // I could probably work around that by changing the way the SarifLogger creates
        // its output file, but that seemed a bridge too far for this PR.
    }
}
