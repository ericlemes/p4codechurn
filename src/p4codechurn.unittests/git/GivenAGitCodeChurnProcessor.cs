﻿using Moq;
using p4codechurn.core;
using p4codechurn.core.git;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace p4codechurn.unittests.git
{
    public class GivenAGitCodeChurnProcessor
    {
        private GitCodeChurnProcessor processor;

        private GitExtractCommandLineArgs args;        

        private MemoryStream memoryStream;

        private Mock<IGitLogParser> gitLogParserMock;

        private Mock<IOutputProcessor> outputProcessorMock;

        private Dictionary<DateTime, Dictionary<string, DailyCodeChurn>> processedOutput;

        private Mock<ICommandLineParser> commandLineParserMock;

        private Mock<IProcessWrapper> processWrapperMock;

        private Mock<ILogger> logger;

        public GivenAGitCodeChurnProcessor()
        {
            args = new GitExtractCommandLineArgs();
            args.GitLogCommand = "git log blah";
            args.OutputType = OutputType.SingleFile;
            args.OutputFile = "outputfile";

            memoryStream = new MemoryStream();

            gitLogParserMock = new Mock<IGitLogParser>();

            outputProcessorMock = new Mock<IOutputProcessor>();
            outputProcessorMock.Setup(m => m.ProcessOutput(args.OutputType, args.OutputFile, It.IsAny<Dictionary<DateTime, Dictionary<string, DailyCodeChurn>>>())).Callback<OutputType, string, Dictionary<DateTime, Dictionary<string, DailyCodeChurn>>>(
                (outputType, outputFile, dict) =>
                {
                    this.processedOutput = dict;
                });

            gitLogParserMock.Setup(m => m.Parse(this.memoryStream)).Returns(new List<GitCommit>());

            commandLineParserMock = new Mock<ICommandLineParser>();
            commandLineParserMock.Setup(m => m.ParseCommandLine("git log blah")).Returns(new Tuple<string, string>("git", "log blah"));

            processWrapperMock = new Mock<IProcessWrapper>();
            processWrapperMock.Setup(m => m.Invoke("git", "log blah")).Returns(this.memoryStream);

            this.logger = new Mock<ILogger>();

            processor = new GitCodeChurnProcessor(this.commandLineParserMock.Object, this.processWrapperMock.Object, gitLogParserMock.Object, outputProcessorMock.Object, logger.Object);            
        }

        [Fact]
        public void WhenExtractingShouldInvokeCommandLine()
        {
            processor.Extract(args);
            
            processWrapperMock.Verify(m => m.Invoke("git", "log blah"), Times.Once());            
        }

        [Fact]
        public void WhenExtractingShouldParseFile()
        {
            processor.Extract(args);
            gitLogParserMock.Verify(m => m.Parse(this.memoryStream), Times.Once());
        }

        [Fact]
        public void WhenExtractingShouldProcessOutput()
        {
            var changesets = new List<GitCommit>()
            {
                new GitCommit()
                {
                    AuthorDate = new DateTime(2018, 10, 2, 15, 30, 00),
                    FileChanges = new List<FileChanges>()
                    {
                        new FileChanges()
                        {
                            Added = 10,
                            Deleted = 5,
                            FileName = "File1.cs"
                        },
                        new FileChanges(){
                            Added = 5,
                            Deleted = 1,
                            FileName = "File2.cs"
                        }
                    }
                },
                new GitCommit()
                {
                    AuthorDate = new DateTime(2018, 10, 1, 12, 00, 00),
                    FileChanges = new List<FileChanges>()
                    {
                        new FileChanges()
                        {
                            Added = 3,
                            Deleted = 2,
                            FileName = "File1.cs"
                        }
                    }
                }
            };

            gitLogParserMock.Setup(m => m.Parse(this.memoryStream)).Returns(changesets);
            
            processor.Extract(args);
            Assert.Equal(2, processedOutput.Count);
        }
    }
}