﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GodotPCKExplorer
{
    class GUIConfig
    {
        static public GUIConfig Instance { get; private set; } = null;

        static string SaveFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "settings.json");

        public PCKVersion PackedVersion { get; set; } = new PCKVersion(1, 3, 4, 0);
        public bool EmbedPCK { get; set; } = false;
        public bool OverwriteExported { get; set; } = true;
        public List<string> RecentOpenedFiles { get; set; } = new List<string>();

        GUIConfig()
        {
            if (Instance == null)
                Instance = this;
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(SaveFile, Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static public void Load()
        {
            try
            {
                if (File.Exists(SaveFile))
                    Instance = Newtonsoft.Json.JsonConvert.DeserializeObject<GUIConfig>(File.ReadAllText(SaveFile));

                if (Instance == null)
                    Instance = new GUIConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}