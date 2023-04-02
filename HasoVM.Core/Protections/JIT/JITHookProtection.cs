using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using System.IO;
using dnlib.DotNet.Emit;
using System.Reflection;
using dnlib.DotNet.Writer;
using HasoVM.Core.Helper;
using System.Runtime.InteropServices;
using System.Text;

namespace HasoVM.Core.Protections.JIT
{
    class JitHookProtection
    {
        public static ModuleDefMD module;
        public static byte[] AssemblyByte = null;
        List<uint> Tokens = new List<uint>();
        List<uint> Tokens2 = new List<uint>();
        public static List<string> jittedM = new List<string>();
        public JitHookProtection(ModuleDefMD Module)
        {
            module = Module;
            InjectRuntime();
            SearchMethods();
            UpdateModule();
            ProtectMethods();
            try
            {
                string cz = Path.GetDirectoryName(Config.ExePath) + "\\" + Path.GetFileNameWithoutExtension(Config.ExePath) + "_virtualised" + "" + ".exe";
                if (File.Exists(cz)) { File.Delete(cz); }
                Console.ForegroundColor = ConsoleColor.Green;
                Config.logger.Log("OBFUSCATION", "DONE!");
                System.Threading.Thread.Sleep(1000);
                File.WriteAllBytes(cz, AssemblyByte);
            }
            catch (Exception ex)
            {
                Config.logger.Error("ERROR", ex.Message);
            }
        }

        #region RuntimeInjection   

        private static Dictionary<string, string> index = new Dictionary<string, string>();

        public void InjectRuntime()
        {

            TypeDef type1 = new TypeDefUser("Runtime", Utils.Rename(1), module.CorLibTypes.Object.TypeDefOrRef);
            type1.Attributes = dnlib.DotNet.TypeAttributes.Public | dnlib.DotNet.TypeAttributes.AutoLayout |
            dnlib.DotNet.TypeAttributes.Class | dnlib.DotNet.TypeAttributes.AnsiClass;
            module.Types.Add(type1);

            var assembly2 = AssemblyDef.Load(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "HasoVM.Runtime.dll");
            var type2 = assembly2.ManifestModule.Find("Runtime.JIT", false);

            IEnumerable<IDnlibDef> defs = InjectHelper.Inject(type2, type1, module);
            MethodDef Init2 = defs.OfType<MethodDef>().Single(method => method.Name == "Hookx");
            Init2.Name = Utils.Rename(1);
            module.GlobalType.FindOrCreateStaticConstructor().Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Init2));

            var types = type1.GetTypes().ToList();
            for (int i = 0; i < type1.Methods.Count; i++)
            {
                MethodDef m = type1.Methods[i];
                if (m.IsConstructor) continue;

                m.Name = Utils.Rename(1);
                foreach (var param in m.Parameters)
                {
                    param.Name = string.Empty;
                }
            }
            for (int j = 0; j < type1.Fields.Count; j++)
            {
                type1.Fields[j].Name = Utils.Rename(1);
            }


            for (int j = 0; j < types.Count(); j++)
            {
                types[j].Name = Utils.Rename(1);
                foreach (var field in types[j].Fields)
                {
                    string nameValue;
                    if (index.TryGetValue(field.Name, out nameValue)) { field.Name = nameValue; }
                    else { string rndname = Utils.Rename(1); index.Add(field.Name, rndname); field.Name = rndname; }
                }
            }
            
            UpdateModule();

        }
        #endregion

        #region UpdateModule

        public byte[] GetCurrentModule()
        {

            MemoryStream memorystream = new MemoryStream();
            ModuleWriterOptions writer = new ModuleWriterOptions(module);
            writer.MetadataOptions.Flags = MetadataFlags.PreserveAll;
            writer.MetadataLogger = DummyLogger.NoThrowInstance;
            module.Write(memorystream, writer);
            byte[] ByteArray = new byte[memorystream.Length];
            memorystream.Position = 0;
            memorystream.Read(ByteArray, 0, (int)memorystream.Length);
            return ByteArray;

        }

        private void UpdateModule()
        {       
            AssemblyByte = GetCurrentModule();
        }

        #endregion

        #region JIT

    
        public void SearchMethods()
        {
            foreach (TypeDef t in module.Types)
            {
               if (t.Namespace == "Runtime") { t.Namespace = string.Empty; continue; }
               if (Config.Normal == true) { if (t.Namespace != "haso") continue; } 
                foreach (MethodDef m in t.Methods)
                {
                    if (m.IsConstructor)
                        NOPMethods(m);
                        Tokens2.Add(m.MDToken.ToUInt32());
                        jittedM.Add(m.Name);   
                 
                    if (!m.HasBody) continue;
                        NOPMethods(m);
                        Tokens.Add(m.MDToken.ToUInt32());
                        jittedM.Add(m.Name);  
                    if (t.Namespace == "haso") t.Namespace = string.Empty; 
                }
            }  
        }

      
        private void NOPMethods(MethodDef method)
        {
           var processor = method.Body.Instructions;   
           for (int i = 0; i < 5; i++)
           {
               processor.Insert(0, OpCodes.Nop.ToInstruction());
           }   
        }

        private void ProtectMethods()
        {          
            Assembly asm = Assembly.Load(AssemblyByte);
            Type[] types;
            try
            {  
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
               //reflection sucks  
                types = e.Types;
            }
            foreach (var t in types.Where(t => t != null))
            {
                MethodInfo[] methods = t.GetMethods(BindingFlags.Public |  BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (ConstructorInfo item in t.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    try
                    {
                        foreach (var tokken in Tokens2)
                        {
                            if (item.MetadataToken == tokken)
                            {
                                System.Reflection.MethodBody mb = item.GetMethodBody();

                                byte[] ILbyte = mb.GetILAsByteArray();
                                int size = ILbyte.Length;
                                int start = SearchArray(AssemblyByte, ILbyte);
                                if (start == -1) continue;
                                else
                                {
                                    int position = start;
                                    Encrypt(ILbyte);
                                    position = start;
                                    for (int i = 0; i < size; i++)
                                    {
                                        AssemblyByte[position] = ILbyte[i];
                                        position++;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                foreach (var m in methods)
                {
                    {
                        foreach (var tokken in Tokens)
                        {
                            if (m.MetadataToken == tokken)
                            {
                                try
                                {
                                    byte[] ILbyte = m.GetMethodBody().GetILAsByteArray();
                                    int size = ILbyte.Length;
                                    int start = SearchArray(AssemblyByte, ILbyte);
                                    if (start == -1) continue;
                                    else
                                    {
                                        int position = start;
                                        Encrypt(ILbyte);
                                        position = start;
                                        for (int i = 0; i < size; i++)
                                        {
                                            AssemblyByte[position] = ILbyte[i];
                                            position++;
                                        }
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                    }
                }
            }
        }

        public void Encrypt(byte[] data)
        {
             byte[] key = Convert.FromBase64String("SDNJOD01RVFUNHdBdHQ9VXonYy5lWzFTdTBmTnR5bnhtZVZNTWhrd0ZIJkpvX0I1dk4=");
             if (data == null)
               throw new ArgumentNullException("data");
               for (int i = 0; i < data.Length; i++)
                 data[i] = (byte)(data[i] ^ key[i % key.Length]);
       
        
        }
 
        private int SearchArray(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        #endregion
    }
}
