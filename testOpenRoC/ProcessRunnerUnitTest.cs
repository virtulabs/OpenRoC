﻿namespace testOpenRoC
{
    using liboroc;
    using System.IO;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProcessRunnerUnitTest
    {
        internal static string AssemblyPath
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        internal static string TestProcessesPath
        {
            get { return Path.Combine(AssemblyPath, "..", "..", "processes"); }
        }

        internal static string TestProcessWindowedPath
        {
            get { return Path.Combine(TestProcessesPath, "testProcessWindowed.exe"); }
        }

        [TestMethod]
        public void TestPrcoessWindowedExists()
        {
            Assert.IsTrue(Directory.Exists(TestProcessesPath));
            Assert.IsTrue(File.Exists(TestProcessWindowedPath));
        }

        [TestMethod]
        public void ProcessStartupStateAssumeCrashIfNotRunning()
        {
            ProcessOptions options = new ProcessOptions
            {
                CrashedIfNotRunning = true,
                Path = TestProcessWindowedPath,
                WorkingDirectory = TestProcessesPath
            };

            options.InitialStateEnumValue = ProcessRunner.Status.Invalid;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                Assert.AreEqual(runner.State, ProcessRunner.Status.Stopped);
                Assert.IsNull(runner.Process);

                runner.Monitor();
                Assert.IsNull(runner.Process);
            }

            options.InitialStateEnumValue = ProcessRunner.Status.Disabled;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                Assert.AreEqual(runner.State, ProcessRunner.Status.Disabled);
                Assert.IsNull(runner.Process);

                runner.Monitor();
                Assert.IsNull(runner.Process);
            }

            options.InitialStateEnumValue = ProcessRunner.Status.Stopped;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                Assert.AreEqual(runner.State, ProcessRunner.Status.Stopped);
                Assert.IsNull(runner.Process);

                runner.Monitor();
                Assert.IsNull(runner.Process);
            }

            options.InitialStateEnumValue = ProcessRunner.Status.Running;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                Assert.AreEqual(runner.State, ProcessRunner.Status.Running);
                Assert.IsNull(runner.Process);

                runner.Monitor();
                Assert.IsNotNull(runner.Process);
            }
        }

        [TestMethod]
        public void ProcessStartupStateDoNotAssumeCrashIfNotRunning()
        {
            ProcessOptions options = new ProcessOptions
            {
                CrashedIfNotRunning = false,
                Path = TestProcessWindowedPath,
                WorkingDirectory = TestProcessesPath
            };

            options.InitialStateEnumValue = ProcessRunner.Status.Invalid;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                Assert.AreEqual(runner.State, ProcessRunner.Status.Stopped);
                Assert.IsNull(runner.Process);

                runner.Monitor();
                Assert.IsNull(runner.Process);
            }

            options.InitialStateEnumValue = ProcessRunner.Status.Disabled;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                Assert.AreEqual(runner.State, ProcessRunner.Status.Disabled);
                Assert.IsNull(runner.Process);

                runner.Monitor();
                Assert.IsNull(runner.Process);
            }

            options.InitialStateEnumValue = ProcessRunner.Status.Stopped;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                Assert.AreEqual(runner.State, ProcessRunner.Status.Stopped);
                Assert.IsNull(runner.Process);

                runner.Monitor();
                Assert.IsNull(runner.Process);
            }

            options.InitialStateEnumValue = ProcessRunner.Status.Running;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                Assert.AreEqual(runner.State, ProcessRunner.Status.Running);
                Assert.IsNull(runner.Process);

                runner.Monitor();
                Assert.IsNotNull(runner.Process);
            }
        }

        [TestMethod]
        public void PrcoessStateChanges()
        {
            ProcessOptions options = new ProcessOptions
            {
                CrashedIfNotRunning = true,
                Path = TestProcessWindowedPath,
                WorkingDirectory = TestProcessesPath
            };

            // running to stopped
            options.InitialStateEnumValue = ProcessRunner.Status.Running;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                runner.Monitor();
                Assert.IsNotNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Running);

                runner.State = ProcessRunner.Status.Stopped;
                runner.Monitor();
                Assert.IsNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Stopped);
            }

            // running to disabled
            options.InitialStateEnumValue = ProcessRunner.Status.Running;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                runner.Monitor();
                Assert.IsNotNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Running);

                runner.State = ProcessRunner.Status.Disabled;
                runner.Monitor();
                Assert.IsNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Disabled);
            }

            // stopped to running
            options.InitialStateEnumValue = ProcessRunner.Status.Stopped;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                runner.Monitor();
                Assert.IsNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Stopped);

                runner.State = ProcessRunner.Status.Running;
                runner.Monitor();
                Assert.IsNotNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Running);
            }

            // stopped to disabled
            options.InitialStateEnumValue = ProcessRunner.Status.Stopped;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                runner.Monitor();
                Assert.IsNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Stopped);

                runner.State = ProcessRunner.Status.Disabled;
                runner.Monitor();
                Assert.IsNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Disabled);
            }

            // disabled to running
            options.InitialStateEnumValue = ProcessRunner.Status.Disabled;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                runner.Monitor();
                Assert.IsNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Disabled);

                runner.State = ProcessRunner.Status.Running;
                runner.Monitor();
                Assert.IsNotNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Running);
            }

            // disabled to stopped
            options.InitialStateEnumValue = ProcessRunner.Status.Disabled;
            using (ProcessRunner runner = new ProcessRunner(options))
            {
                runner.Monitor();
                Assert.IsNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Disabled);

                runner.State = ProcessRunner.Status.Stopped;
                runner.Monitor();
                Assert.IsNull(runner.Process);
                Assert.AreEqual(runner.State, ProcessRunner.Status.Stopped);
            }
        }
    }
}
