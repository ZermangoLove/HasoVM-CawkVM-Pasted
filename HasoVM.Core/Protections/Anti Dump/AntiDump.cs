using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HasoVM.Core.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HasoVM.Core.Protections.Anti_Dump
{
    class AntiDump : IProtection
    {
        public override void Run(ModuleDefMD module)
        {
            var assembly2 = AssemblyDef.Load(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "HasoVM.Runtime.dll");
            var type2 = assembly2.ManifestModule.Find("Runtime.AntiDump", false);

            TypeDef type1 = new TypeDefUser("", "", module.CorLibTypes.Object.TypeDefOrRef);
            type1.Name = Utils.Rename(1);
            type1.Attributes = dnlib.DotNet.TypeAttributes.Public | dnlib.DotNet.TypeAttributes.AutoLayout |
            dnlib.DotNet.TypeAttributes.Class | dnlib.DotNet.TypeAttributes.AnsiClass;
            module.Types.Add(type1);

            IEnumerable<IDnlibDef> members = InjectHelper.Inject(type2, type1, module);
            MethodDef init = (MethodDef)members.Single(method => method.Name == "InitAntiDump");
            MethodDef init2 = (MethodDef)members.Single(method => method.Name == "VirtualProtect");
            init.Name = Utils.Rename(1);
            init2.Name = Utils.Rename(1);
            module.GlobalType.FindOrCreateStaticConstructor().Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));

            foreach (var param in init2.Parameters)
            {
                param.Name = string.Empty;
            }

            foreach (var md in module.GlobalType.Methods)
            {
                if (md.Name != ".ctor") continue;
                module.GlobalType.Remove(md);
                break;
            }
        }
    }
}
