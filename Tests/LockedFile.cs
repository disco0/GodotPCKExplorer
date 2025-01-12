﻿using System;
using System.IO;

namespace Tests
{
    internal class LockedFile : IDisposable
    {
        private FileStream file;
        bool DeleteAfterDispose = true;

        public LockedFile(string path, bool deleteAfterDispose = true)
        {
            file = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            file.Lock(0, 0);
            DeleteAfterDispose = deleteAfterDispose;
        }

        public void Dispose()
        {
            var path = file.Name;
            file.Unlock(0, 0);
            file.Close();

            if (DeleteAfterDispose)
                File.Delete(path);
        }
    }
}
