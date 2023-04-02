using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HasoVM.Core.Stages;

namespace HasoVM.Core.Helper
{
    internal class Conversion
    {
        public static List<Data> AllMethods = new List<Data>();
        public static HashSet<MethodDef> SelectedMethods = new HashSet<MethodDef>();
        public static List<string> MethodName = new List<string>();

        public static void Execute(List<MethodDef> mds)
        {
            int value = 0;
            foreach (MethodDef method in mds)
            {
                MethodName.Add(method.FullName);
                Data methodData = new Data(method);
                method.Body.SimplifyMacros(method.Parameters);
                method.Body.SimplifyBranches();
                Config.logger.Log("VM", method.FullName);
        
                var convertor = new Stages.MethodConverter(method);
                try
                {
                    convertor.Execute();
              
                    if (!convertor.Successful) continue;
                 
                    List<byte> bytes = new List<byte> { };
                    for (int i = 0; i < convertor.ConvertedBytes.Length; i++)
                    {
                        bytes.Add((byte)(convertor.ConvertedBytes[i] ^ (i % 568178 + 743919))); 
                    }
              
                    methodData.EncryptedBytes = bytes.ToArray();
                    methodData.ID = value;           
                    AllMethods.Add(methodData);
                  
                    methodData.name = Utils.Rename(3);
                    value++;
                }
                catch (Exception ex)
                {
                    Config.logger.Log("ERROR", ex.Message);
                }
            }

            List<IDnlibDef> amm = Injection.InjectRuntime();
           
            InjectMethods();
         
            var g = Config.moduleDefMD.GlobalType.FindOrCreateStaticConstructor();
            g.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Injection.Starter));
        
            foreach (var meth in AllMethods)
            {
                EmbeddedResource emb = new EmbeddedResource(meth.name, meth.EncryptedBytes);
                Config.moduleDefMD.Resources.Add(emb);
            }

        }

        private static void InjectMethods()
        {
            foreach (Data methodData in AllMethods)
            {
                Injection.InjectToMethod(methodData.Method, methodData.name, methodData.ID);
            }
        }    
    }
}