using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HasoVM.Core.Helper
{
    class Config
    {
        public static string Version = "2.6";
        public static MemoryStream mem = new MemoryStream();
        public static ModuleDefMD moduleDefMD;
        public static ModuleDefMD jitDefMD;
        public static ILogger logger;

        public static string name;
        public static string TypeName;
        public static string ExePath;
        public static byte[] RT86;
        public static byte[] RT64;
        public static byte[] JRT86;
        public static byte[] JRT64;
       
        public static bool Normal = false;
        public static bool Maximum = false;
        public static bool AntiDumpSetting = false;

        //not used
        public static string RunVM;
        public static string ExtractResource;
        public static string parametersArray2;
        public static string Starter;
        public static string HandleOpc;

    }
}
