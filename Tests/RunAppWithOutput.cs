﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    internal class RunAppWithOutput : IDisposable
    {
        Process process = null;
        System.Threading.Timer timer = null;

        public RunAppWithOutput(string name, string args, int closeDelay)
        {
            process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                Arguments = args,
                FileName = name,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            process.Start();

            timer = new System.Threading.Timer((s) =>
            {
                if (!process.HasExited)
                    Kill();
            }, null, closeDelay, -1);
        }

        void Kill()
        {
#if UNIX
            var p = Process.Start("pkill", $"-9 -P {process.Id}");
            p.WaitForExit();
#endif
            process.Kill();
        }

        public string GetConsoleText()
        {
            if (!process.HasExited)
                process.WaitForExit();
            return process.StandardOutput.ReadToEnd();
        }

        public string GetConsoleError()
        {
            if (!process.HasExited)
                process.WaitForExit();
            return process.StandardError.ReadToEnd();
        }

        public void Dispose()
        {
            timer.Dispose();
            if (!process.HasExited)
                Kill();
            process.Dispose();
        }
    }
}
