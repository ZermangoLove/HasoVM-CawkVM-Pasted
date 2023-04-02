using System;
using System.Linq;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System.Collections.Generic;
using HasoVM.Core.Helper;
using HasoVM.Core.Properties;
using HasoVM.Core.Protections.Mutations;
using HasoVM.Core.Protections.JIT;
using HasoVM.Core.Protections.ControlFlow;
using HasoVM.Core.Protections.StringEncryption;
using HasoVM.Core.Protections;
using HasoVM.Core.Protections.Anti_Dump;
using dnlib.DotNet.Emit;
using System.Collections;
using System.Windows.Forms.Design;
using System.Reflection.Emit;
using System.Text;

namespace HasoVM.Core
{
   
    public class Context
    {   
        private static List<MethodDef> mds = new List<MethodDef>();   
       
        static IProtection[] Proc =
        {
            new StringEncryption(),
            new Mutations(),
            new ControlFlow()
        };
      
        static IProtection[] Before =
        {
            new AntiDump()
        };
       
        public static void Main(string[] args)
        {
            Config.JRT86 = Utils.CompressLZMA(Resources.HasoHook86); //JIT
            Config.JRT64 = Utils.CompressLZMA(Resources.HasoHook64); //JIT
            Config.RT86 = Utils.CompressLZMA(Resources.RT86); //VM
            Config.RT64 = Utils.CompressLZMA(Resources.RT64); //VM

            Console.Title = "HasoVM Revolution " + Config.Version;
            Console.ForegroundColor = ConsoleColor.Yellow;

            string path = args[0];

            // for fix metadatas  
            byte[] assemblyData = File.ReadAllBytes(path);
            ModuleDefMD defMD = ModuleDefMD.Load(assemblyData);

            Config.logger = new ConsoleLogger();
            Config.Maximum = true; // Encrypt all methods with JIT
           // Config.Normal = true; // Only encrypt runtime methods with JIT
            
            foreach (var p in Before)
                    p.Run(defMD);
        
            MemoryStream memorystream = new MemoryStream();
            ModuleWriterOptions writer = new ModuleWriterOptions(defMD);
         
            writer.MetadataOptions.Flags = MetadataFlags.PreserveAll;
            writer.MetadataLogger = DummyLogger.NoThrowInstance;
            defMD.Write(memorystream, writer);
         
            byte[] ByteArray = new byte[memorystream.Length];
            memorystream.Position = 0;
            memorystream.Read(ByteArray, 0, (int)memorystream.Length);
         
            Config.moduleDefMD = ModuleDefMD.Load(ByteArray);
            Config.ExePath = path;
            FindMethod();
            Run(mds);
        }
      
        private static void Run(List<MethodDef> mds)
        {
            Config.logger.Log("PATH", Config.ExePath);
            Config.name = Guid.NewGuid().ToString().ToUpper().Replace("-", string.Empty);
           
            ResPhase();
            Conversion.Execute(mds);
          
            foreach (var p in Proc)
                p.Run(Config.moduleDefMD);
           
            RenameAssembly();
            Utils.Watermark();

            SaveModule();

            Config.jitDefMD = ModuleDefMD.Load(Config.mem.ToArray());
            new JitHookProtection(Config.jitDefMD);
        }

        private static void SaveModule()
        {
            ModuleWriterOptions modOpts = new ModuleWriterOptions(Config.moduleDefMD);
            modOpts.MetadataOptions.Flags = MetadataFlags.PreserveRids;
            modOpts.MetadataLogger = DummyLogger.NoThrowInstance;
            try
            {
                Config.moduleDefMD.Write(Config.mem, modOpts);
            }
            catch (Exception ex)
            {
                Config.logger.Error("ERROR", ex.Message);
                Console.ReadKey();
            }
        }

        private static void RenameAssembly()
        {
            Dictionary<string, string> index = new Dictionary<string, string>();
            foreach (TypeDef t2 in Config.moduleDefMD.Types.Where(t2 => t2.IsGlobalModuleType))
            {
                t2.Name = Utils.Rename(1);
            }

            foreach (TypeDef t in Config.moduleDefMD.Types.Where(t => t.Namespace == "haso"))
            {
                for (int i = 0; i < t.Methods.Count; i++)
                {
                    MethodDef m = t.Methods[i];
                    if (m.HasImplMap)
                    {
                        m.Name = Utils.Rename(1);
                    }
                    t.Name = Config.TypeName = Utils.Rename(1);
                  
                    switch (m.Name)
                    {
                          case "RunVM":
                              Config.RunVM = m.Name = Utils.Rename(1);
                              break;    
                          case "extractResource":
                              Config.ExtractResource = m.Name = Utils.Rename(1);               
                              break;
                          case "parametersArray2":
                              Config.parametersArray2 = m.Name = Utils.Rename(1);
                              break;
                          case "Starter":
                              Config.Starter = m.Name = Utils.Rename(1);
                              break;
                          case "HandleOpc":
                              Config.HandleOpc = m.Name = Utils.Rename(1);
                              break;     
                     }
                    
                    for (int j = 0; j < t.Fields.Count; j++)
                    {
                        t.Fields[j].Name = Utils.Rename(1);
                    }
                   
                    foreach (var param in m.Parameters)
                    {
                        param.Name = string.Empty;
                    }

                     var types = t.GetTypes().ToList();
                    for (int j = 0; j < types.Count(); j++)
                    {
                        types[j].Name = Utils.Rename(1);
                        foreach (var field in types[j].Fields)
                        {
                            string nameValue;
                            if (index.TryGetValue(field.Name, out nameValue))
                                field.Name = nameValue;
                            else
                            {
                                string rndname = Utils.Rename(1);
                                index.Add(field.Name, rndname);
                                field.Name = rndname;
                            }
                        }                
                    }
                }
            }
        }

        private static void FindMethod()
        {
            foreach (var typeDef in Config.moduleDefMD.GetTypes())
            {
                if (typeDef == Config.moduleDefMD.GlobalType) continue;
                if (typeDef.HasGenericParameters) continue;
                if (typeDef.CustomAttributes.Count(i => i.TypeFullName.Contains("CompilerGenerated")) != 0) continue;
                if (typeDef.IsValueType) continue;
                foreach (var method in typeDef.Methods)
                {
                    // if (method.IsConstructor) continue;
                    if (!method.HasBody) continue;
                    if (method.Body.Instructions.Count < 2) continue;
                    if (typeDef.IsGlobalModuleType && method.IsConstructor) continue;
                    if (method.HasGenericParameters) continue;
                    if (method.CustomAttributes.Count(i => i.TypeFullName.Contains("CompilerGenerated")) != 0) continue;
                    if (method.ReturnType == null) continue;
                    if (method.ReturnType.IsGenericParameter) continue;
                    if (method.Parameters.Count(i => i.Type.FullName.EndsWith("&") && i.ParamDef.IsOut == false) != 0) continue;
                    if (method.CustomAttributes.Count(i => i.NamedArguments.Count == 2 &&
                                                            i.NamedArguments[0].Value.ToString().Contains("Encrypt") &&
                                                            i.NamedArguments[1].Name.Contains("Exclude") && i.NamedArguments[1].Value
                                                            .ToString().ToLower().Contains("true")) != 0) continue;
                   mds.Add(method);
                }
            }
        }

        private static void ResPhase()
        {
            Config.moduleDefMD.Resources.Add(new EmbeddedResource("HasoRT", Config.JRT86));
            Config.moduleDefMD.Resources.Add(new EmbeddedResource("HasoRT64", Config.JRT64));
            Config.moduleDefMD.Resources.Add(new EmbeddedResource("_HasoRT", Config.RT86));
            Config.moduleDefMD.Resources.Add(new EmbeddedResource("_HasoRT64", Config.RT64));
        }
        
    }
}
