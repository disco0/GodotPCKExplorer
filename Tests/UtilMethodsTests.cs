﻿using NUnit.Framework;

using System.Reflection;
using System;
using System.Linq;
using System.IO;
using GodotPCKExplorer;
using System.Windows.Forms;
using System.Collections.Generic;
using Ionic.Zip;

namespace Tests
{
    [TestFixture]
#if !UNIX
    [Apartment(System.Threading.ApartmentState.STA)]
#else
    [RequiresSTA]
#endif
    public class UtilMethodsTests
    {
        enum OS
        {
            Windows,
            Linux,
            MacOS,
        }

        static OS Platform
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                        return OS.Linux;
                    case PlatformID.MacOSX:
                        return OS.MacOS;
                    default:
                        return OS.Windows;
                }
            }
        }

        static string ExecutableExtension
        {
            get => Platform == OS.Windows ? ".exe" : "";
        }

        static string ZipFilePath
        {
            get => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Test.zip");
        }

        static string PlatformFolder
        {
            get
            {
                switch (Platform)
                {
                    case OS.Windows:
                        return "win";
                    case OS.Linux:
                        return "linux";
                    case OS.MacOS:
                        return "mac";
                    default:
                        return "";
                }
            }
        }

        static readonly string binaries_base = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestBinaries");
        static readonly string binaries = Path.Combine(binaries_base, PlatformFolder);
        static readonly string pck_error = "Error: Couldn't load project data at path \".\". Is the .pck file missing?";

        List<string> OriginalTestFiles = new List<string>();

        void Title(string name)
        {
            Console.WriteLine($"===================={name}====================");
        }

        string Exe(string name)
        {
            return name + ExecutableExtension;
        }

        [SetUp]
        public void GodotPCKInit()
        {
            Program.CMDMode = true;
            Program.ForceConsoleMode = true;

            if (!Directory.Exists(binaries_base))
                Directory.CreateDirectory(binaries_base);

            using (var z = ZipFile.Read(ZipFilePath))
            {
                OriginalTestFiles.Clear();
                foreach (var e in z.Entries)
                    OriginalTestFiles.Add(Path.Combine(binaries_base, e.FileName));
            }

            var not_all_files = false;
            var files = Directory.GetFiles(binaries_base, "*", SearchOption.AllDirectories);
            foreach (var f in OriginalTestFiles)
                if (!files.Contains(f))
                {
                    not_all_files = true;
                    break;
                }

            if (!Directory.Exists(binaries_base) || files.Length == 0 || not_all_files)
            {
                using (var zip = ZipFile.Read(ZipFilePath))
                {
                    foreach (var e in zip)
                    {
                        if (e.FileName.StartsWith(PlatformFolder))
                            e.Extract(binaries_base, ExtractExistingFileAction.DoNotOverwrite);
                    }
                }

            }
        }

        [TearDown]
        public void ClearBinaries()
        {
            foreach (var d in Directory.GetDirectories(binaries_base))
                Directory.Delete(d, true);
            foreach (var f in Directory.GetFiles(binaries_base, "*", SearchOption.AllDirectories))
                if (!OriginalTestFiles.Contains(f))
                    File.Delete(f);
        }

        [Test]
        public void TestHelpCommand()
        {
            Assert.IsTrue(PCKActions.HelpRun());
        }

        [Test]
        public void TestOpenCommand()
        {
            Application.Idle += (sender, e) => Application.Exit();
            Title("Open");
            Assert.IsTrue(PCKActions.OpenPCKRun(Path.Combine(binaries, "Test.pck")));

            Title("Wrong Path");
            Assert.IsFalse(PCKActions.OpenPCKRun(Path.Combine(binaries, "WrongPath/Test.pck")));
        }

        [Test]
        public void TestInfoCommand()
        {
            Title("Info PCK");
            Assert.IsTrue(PCKActions.InfoPCKRun(Path.Combine(binaries, "Test.pck")));
            Title("Info EXE");
            Assert.IsTrue(PCKActions.InfoPCKRun(Path.Combine(binaries, Exe("TestEmbedded"))));
            Title("Wrong path");
            Assert.IsFalse(PCKActions.InfoPCKRun(Path.Combine(binaries, "WrongPath/Test.pck")));
        }

        [Test]
        public void TestExtractScanPackCommand()
        {
            string exportTestPath = Path.Combine(binaries, "ExportTest");
            string testEXE = Path.Combine(binaries, Exe("Test"));
            string testPCK = Path.Combine(binaries, "Test.pck");
            string testEmbedPack = Path.Combine(binaries, Exe("TestPack"));
            string selectedFilesPck = Path.Combine(binaries, "SelecetedFiles.pck");
            string exportTestSelectedPath = Path.Combine(binaries, "ExportTestSelected");
            string newPckPath = Path.Combine(binaries, "out.pck");
            string exportTestSelectedWrongPath = Path.Combine(binaries, "ExportTestSelectedWrong");
            string overwritePath = exportTestPath + "Overwrite";
            string out_exe = Path.ChangeExtension(newPckPath, ExecutableExtension);

            Title("Extract");
            Assert.IsTrue(PCKActions.ExtractPCKRun(testPCK, exportTestPath, true));

            Title("Extract Wrong Path");
            Assert.IsFalse(PCKActions.ExtractPCKRun(Path.Combine(binaries, "WrongPath/Test.pck"), exportTestPath, true));

            Title("Compare content with folder");
            var list_of_files = Utils.ScanFoldersForFiles(Path.GetFullPath(exportTestPath));
            {
                var pck = new PCKReader();
                Assert.IsTrue(pck.OpenFile(testPCK));
                Assert.AreEqual(pck.Files.Count, list_of_files.Count);

                foreach (var f in list_of_files)
                    Assert.IsTrue(pck.Files.ContainsKey(f.Path));

                pck.Close();
            }

            // select at least one file
            var rnd = new Random();
            var first = false;
            var seleceted_files = list_of_files.Where((f) =>
            {
                if (!first)
                {
                    first = true;
                    return true;
                }
                return rnd.NextDouble() > 0.5;
            }).ToList();

            Title("Extract only seleceted files and compare");
            {
                var export_files = seleceted_files.Select((s) => s.Path);

                Assert.IsTrue(PCKActions.ExtractPCKRun(testPCK, exportTestSelectedPath, true, export_files));

                var exportedSelectedList = Utils.ScanFoldersForFiles(exportTestSelectedPath);
                Assert.AreEqual(exportedSelectedList.Count, seleceted_files.Count);

                foreach (var f in export_files)
                    Assert.IsTrue(exportedSelectedList.FindIndex((l) => l.Path == f) != -1);


                Title("Extract only seleceted wrong files and compare");
                var wrong_selected = export_files.ToList();
                for (int i = 0; i < wrong_selected.Count; i++)
                    wrong_selected[i] = wrong_selected[i] + "WrongFile";

                Assert.IsTrue(PCKActions.ExtractPCKRun(testPCK, exportTestSelectedWrongPath, true, wrong_selected));
                Assert.IsFalse(Directory.Exists(exportTestSelectedWrongPath));

                Title("Extract empty list");
                Assert.IsFalse(PCKActions.ExtractPCKRun(testPCK, exportTestSelectedWrongPath, true, new string[] { }));
            }


            Title("Extract without overwrite");
            {
                Assert.IsTrue(PCKActions.ExtractPCKRun(testPCK, overwritePath, true));
                var files = Directory.GetFiles(overwritePath);
                File.Delete(files[0]);
                File.WriteAllText(files[0], "Test");
                File.Delete(files[1]);

                Assert.IsTrue(PCKActions.ExtractPCKRun(testPCK, overwritePath, false));

                Assert.AreEqual("Test", File.ReadAllText(files[0]));
                Assert.IsTrue(File.Exists(files[1]));
            }


            string ver = "";
            {
                Title("Get original version");
                string console_output = "";
                using (var output = new ConsoleOutputRedirect())
                {
                    Assert.IsTrue(PCKActions.InfoPCKRun(testPCK));
                    console_output = output.GetOuput();
                    var lines = console_output.Replace("\r", "").Split('\n');
                    ver = lines[lines.Length - 2].Split(':')[1];
                }
                Console.WriteLine(console_output);
                Console.WriteLine($"Found version: {ver}");
            }


            {
                Title("Pack new PCK");

                Assert.IsTrue(PCKActions.PackPCKRun(exportTestPath, newPckPath, ver));

                if (!Utils.IsRunningOnMono())
                {
                    Title("Locked file");
                    string locked_file = Path.Combine(exportTestPath, "out.lock");
                    using (var f = new LockedFile(locked_file))
                        Assert.IsFalse(PCKActions.PackPCKRun(exportTestPath, locked_file, ver));
                }

                Title("Wrong version and directory");
                Assert.IsFalse(PCKActions.PackPCKRun(exportTestPath, newPckPath, "1234"));
                Assert.IsFalse(PCKActions.PackPCKRun(exportTestPath, newPckPath, "123.33.2.1"));
                Assert.IsFalse(PCKActions.PackPCKRun(exportTestPath, newPckPath, "-1.0.2.1"));
                Assert.IsFalse(PCKActions.PackPCKRun(exportTestPath + "WrongPath", newPckPath, ver));

                // Compare new PCK content with alredy existing trusted list of files 'list_of_files'
                Title("Compare files to original list");
                {
                    var pck = new PCKReader();
                    Assert.IsTrue(pck.OpenFile(testPCK));
                    foreach (var f in pck.Files.Keys)
                        Assert.IsTrue(list_of_files.FindIndex((l) => l.Path == f) != -1);

                    pck.Close();
                }
            }

            {
                Title("Pack embedded");

                File.Copy(testEXE, testEmbedPack);

                Assert.IsTrue(PCKActions.PackPCKRun(exportTestPath, testEmbedPack, ver, true));
                Assert.IsTrue(File.Exists(Path.ChangeExtension(testEmbedPack, Exe(".old"))));

                Title("Pack embedded again");
                Assert.IsFalse(PCKActions.PackPCKRun(exportTestPath, testEmbedPack, ver, true));
                Assert.IsFalse(File.Exists(Path.ChangeExtension(testEmbedPack, Exe(".old"))));
            }


            {
                Title("Pack only selected files");

                Assert.IsTrue(PCKActions.PackPCKRun(seleceted_files, selectedFilesPck, ver));

                Title("Compare selected to pack content with new pck");
                {
                    var pck = new PCKReader();
                    Assert.IsTrue(pck.OpenFile(selectedFilesPck));
                    Assert.AreEqual(pck.Files.Count, seleceted_files.Count);

                    foreach (var f in pck.Files.Keys)
                        Assert.IsTrue(seleceted_files.FindIndex((l) => l.Path == f) != -1);

                    pck.Close();
                }
            }

            Title("Good run");

            File.Copy(testEXE, out_exe);

            using (var r = new RunAppWithOutput(out_exe, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));

            // test embed pack
            using (var r = new RunAppWithOutput(testEmbedPack, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));

            Title("Run without PCK");
            if (File.Exists(newPckPath))
                File.Delete(newPckPath);

            using (var r = new RunAppWithOutput(out_exe, "", 1000))
                Assert.IsTrue(r.GetConsoleText().Contains(pck_error));
        }


        [Test]
        public void TestMergePCK()
        {
            string testEXE = Path.Combine(binaries, Exe("Test"));
            string testPCK = Path.Combine(binaries, "Test.pck");
            string newEXE = Path.Combine(binaries, Exe("TestMerge"));
            string newEXE1Byte = Path.Combine(binaries, Exe("TestMerge1Byte"));
            string newEXE_old = Path.Combine(binaries, Exe("TestMerge.old"));

            File.Copy(testEXE, newEXE);

            Title("Merge");
            Assert.IsTrue(PCKActions.MergePCKRun(testPCK, newEXE));
            Assert.IsTrue(File.Exists(newEXE_old));

            Title("Again");
            Assert.IsFalse(PCKActions.MergePCKRun(testPCK, newEXE));

            File.Delete(newEXE);
            File.Copy(testEXE, newEXE);

            Title("Merge without backup");
            Assert.IsTrue(PCKActions.MergePCKRun(testPCK, newEXE, true));
            Assert.IsFalse(File.Exists(newEXE_old));

            File.Delete(newEXE);
            File.Copy(testEXE, newEXE);

            if (!Utils.IsRunningOnMono())
            {
                Title("Locked backup");
                // creates new (old + ExecutableExtension) 0kb
                using (var l = new LockedFile(newEXE_old, false))
                    Assert.IsFalse(PCKActions.MergePCKRun(testPCK, newEXE, true));

                Title("Locked pck file");
                using (var l = new LockedFile(testPCK, false))
                    Assert.IsFalse(PCKActions.MergePCKRun(testPCK, newEXE));
            }

            Title("Wrong Files");
            Assert.IsFalse(PCKActions.MergePCKRun(testPCK + "Wrong", newEXE));
            Assert.IsFalse(PCKActions.MergePCKRun(testPCK, newEXE + "Wrong", true));

            Title("Same File");
            Assert.IsFalse(PCKActions.MergePCKRun(testPCK, testPCK, true));

            Title("-1 Byte");
            var nf = new BinaryWriter(File.OpenWrite(newEXE1Byte));
            var o = new BinaryReader(File.OpenRead(testEXE));
            nf.Write(o.ReadBytes((int)o.BaseStream.Length - 1), 0, (int)o.BaseStream.Length - 1);
            nf.Close();
            o.Close();

            // The result is good... but thats not 64bit multiple :/
            Assert.IsTrue(PCKActions.MergePCKRun(testPCK, newEXE1Byte, true));

            Title("Bad run");
            using (var r = new RunAppWithOutput(newEXE, "", 1000))
                Assert.IsTrue(r.GetConsoleText().Contains(pck_error));

            Title("Good runs");
            File.Delete(newEXE);
            File.Copy(testEXE, newEXE);
            Assert.IsTrue(PCKActions.MergePCKRun(testPCK, newEXE));
            using (var r = new RunAppWithOutput(newEXE, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));

            using (var r = new RunAppWithOutput(newEXE1Byte, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));
        }

        [Test]
        public void TestRipPCK()
        {
            string new_exe = Path.Combine(binaries, Exe("TestRip"));
            string new_exe_old = Path.Combine(binaries, Exe("TestRip.old"));
            string new_pck = Path.Combine(binaries, "TestRip.pck");
            string locked_exe_str = Path.Combine(binaries, Exe("TestLockedRip"));

            File.Copy(Path.Combine(binaries, Exe("TestEmbedded")), new_exe);

            Title("Rip embedded");
            Assert.IsTrue(PCKActions.RipPCKRun(new_exe, new_pck));

            Title("Rip wrong files");
            Assert.IsFalse(PCKActions.RipPCKRun(Path.Combine(binaries, Exe("Test")), new_pck));
            Assert.IsFalse(PCKActions.RipPCKRun(new_pck, new_pck));

            if (!Utils.IsRunningOnMono())
            {
                Title("Locked file");
                string locked_file = Path.Combine(binaries, "test.lock");
                using (var f = new LockedFile(locked_file))
                    Assert.IsFalse(PCKActions.RipPCKRun(new_exe, locked_file));
            }

            Title("Rip PCK from exe");
            Assert.IsTrue(PCKActions.RipPCKRun(new_exe, null, true));
            Assert.IsFalse(File.Exists(new_exe_old));

            Title("Rip PCK from PCK");
            Assert.IsFalse(PCKActions.RipPCKRun(new_pck, null, true));

            Title("Good run");
            using (var r = new RunAppWithOutput(new_exe, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));

            Title("Run without PCK");
            if (File.Exists(new_pck))
                File.Delete(new_pck);
            using (var r = new RunAppWithOutput(new_exe, "", 1000))
                Assert.IsTrue(r.GetConsoleText().Contains(pck_error));

            Title("Rip locked");

            File.Copy(Path.Combine(binaries, Exe("TestEmbedded")), locked_exe_str);

            using (var locked_exe = File.OpenWrite(locked_exe_str))
            {
                Assert.IsFalse(PCKActions.RipPCKRun(locked_exe_str));
            }

            Title("Rip and remove .old");
            Assert.IsTrue(PCKActions.RipPCKRun(locked_exe_str, null, true));
            Assert.IsFalse(File.Exists(Path.ChangeExtension(locked_exe_str, Exe(".old"))));
        }

        [Test]
        public void TestSplitPCK()
        {
            string exe = Path.Combine(binaries, Exe("TestSplit"));
            string pck = Path.Combine(binaries, "TestSplit.pck");
            string new_exe = Path.Combine(binaries, "SplitFolder", Exe("Split"));
            string new_pck = Path.Combine(binaries, "SplitFolder", "Split.pck");

            File.Copy(Path.Combine(binaries, Exe("TestEmbedded")), exe);

            Title("Split with custom pair name and check files");
            Assert.IsTrue(PCKActions.SplitPCKRun(exe, new_exe));
            Assert.IsTrue(File.Exists(new_exe));
            Assert.IsTrue(File.Exists(new_pck));
            Assert.IsFalse(File.Exists(Path.ChangeExtension(new_exe, Exe(".old"))));

            Title("Can't copy with same name");
            Assert.IsFalse(PCKActions.SplitPCKRun(exe, exe));

            Title("Split with same name");
            Assert.IsTrue(PCKActions.SplitPCKRun(exe));
            Assert.IsTrue(File.Exists(exe));
            Assert.IsTrue(File.Exists(pck));
            Assert.IsFalse(File.Exists(Path.ChangeExtension(new_exe, Exe(".old"))));

            Title("Already splitted");
            Assert.IsFalse(PCKActions.SplitPCKRun(exe));

            Title("Good runs");
            using (var r = new RunAppWithOutput(exe, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));

            using (var r = new RunAppWithOutput(new_exe, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));

            Title("Bad runs");
            foreach (var f in new string[] { pck, new_pck })
                if (File.Exists(f))
                    File.Delete(f);

            using (var r = new RunAppWithOutput(exe, "", 1000))
                Assert.IsTrue(r.GetConsoleText().Contains(pck_error));

            using (var r = new RunAppWithOutput(new_exe, "", 1000))
                Assert.IsTrue(r.GetConsoleText().Contains(pck_error));

            if (!Utils.IsRunningOnMono())
            {
                Title("Split with locked output");
                foreach (var f in new string[] { new_exe, new_pck })
                    if (File.Exists(f))
                        File.Delete(f);
                File.Copy(Path.Combine(binaries, "Test.pck"), new_pck);

                using (var l = new LockedFile(new_pck, false))
                    Assert.IsFalse(PCKActions.SplitPCKRun(Path.Combine(binaries, Exe("TestEmbedded")), new_exe));

                Assert.IsFalse(File.Exists(new_exe));
                Assert.IsTrue(File.Exists(new_pck));
            }
        }

        [Test]
        public void TestChangePCKVersion()
        {
            string exe = Path.Combine(binaries, Exe("TestVersion"));
            string pck = Path.Combine(binaries, "TestVersion.pck");
            string exeEmbedded = Path.Combine(binaries, Exe("TestVersionEmbedded"));

            File.Copy(Path.Combine(binaries, Exe("Test")), exe);
            File.Copy(Path.Combine(binaries, "Test.pck"), pck);
            File.Copy(Path.Combine(binaries, Exe("TestEmbedded")), exeEmbedded);

            Func<string, PCKVersion> getVersion = (s) =>
            {
                Console.WriteLine($"Getting version");
                string console_output = "";
                PCKVersion ver;
                using (var output = new ConsoleOutputRedirect())
                {
                    Assert.IsTrue(PCKActions.InfoPCKRun(s));
                    console_output = output.GetOuput();
                    var lines = console_output.Replace("\r", "").Split('\n');
                    ver = new PCKVersion(lines[lines.Length - 2].Split(':')[1]);
                }
                Console.WriteLine($"Got version: {ver}");
                return ver;
            };

            var origVersion = getVersion(pck);
            var newVersion = origVersion;
            newVersion.Major += 1;
            newVersion.Minor += 1;
            newVersion.Revision += 2;

            Title("Regular pck test runs");

            Assert.IsTrue(PCKActions.ChangePCKVersion(pck, newVersion.ToString()));
            Assert.AreEqual(newVersion, getVersion(pck));

            using (var r = new RunAppWithOutput(exe, "", 1000))
                Assert.IsTrue(r.GetConsoleText().Contains(pck_error));

            Assert.IsTrue(PCKActions.ChangePCKVersion(pck, origVersion.ToString()));
            Assert.AreEqual(origVersion, getVersion(pck));

            using (var r = new RunAppWithOutput(exe, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));

            Title("Embedded test runs");

            Assert.IsTrue(PCKActions.ChangePCKVersion(exeEmbedded, newVersion.ToString()));
            Assert.AreEqual(newVersion, getVersion(exeEmbedded));

            using (var r = new RunAppWithOutput(exeEmbedded, "", 1000))
                Assert.IsTrue(r.GetConsoleText().Contains(pck_error));

            Assert.IsTrue(PCKActions.ChangePCKVersion(exeEmbedded, origVersion.ToString()));
            Assert.AreEqual(origVersion, getVersion(exeEmbedded));

            using (var r = new RunAppWithOutput(exeEmbedded, "", 1000))
                Assert.IsFalse(r.GetConsoleText().Contains(pck_error));
        }
    }
}
