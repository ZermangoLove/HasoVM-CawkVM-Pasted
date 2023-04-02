using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using HasoVM.Core.Helper;
namespace HasoVM.Core.Stages
{
    class Injection
    {
        public static MethodDef RunVM;
        public static MethodDef Starter;

        public static List<IDnlibDef> InjectRuntime()
        {       
            TypeDef type1 = new TypeDefUser("haso", "VM", Config.moduleDefMD.CorLibTypes.Object.TypeDefOrRef);
            type1.Attributes = dnlib.DotNet.TypeAttributes.Public | dnlib.DotNet.TypeAttributes.AutoLayout |
            dnlib.DotNet.TypeAttributes.Class | dnlib.DotNet.TypeAttributes.AnsiClass;
            Config.moduleDefMD.Types.Add(type1);

            TypeDef rtType = GetRuntimeType("Runtime.VM");
            IEnumerable<IDnlibDef> members = InjectHelper.Inject(rtType, type1, Config.moduleDefMD);
            List<IDnlibDef> membersList = members.ToList();
       
            foreach (MethodDef m in rtType.Methods)
            {
              RunVM = (MethodDef)membersList.Single(method => method.Name == "RunVM");      
              Starter = (MethodDef)membersList.Single(method => method.Name == "Starter");
            }

            return membersList;
        }
    
        private static TypeDef GetRuntimeType(string v)
        {
            Module module = typeof(Injection).Assembly.ManifestModule;
            string rtPath = "HasoVM.Runtime.dll";
            if (module.FullyQualifiedName[0] != '<')
                rtPath = Path.Combine(Path.GetDirectoryName(module.FullyQualifiedName), rtPath);
            ModuleDefMD rtModule = ModuleDefMD.Load(rtPath, new ModuleCreationOptions() { TryToLoadPdbFromDisk = true });
            rtModule.EnableTypeDefFindCache = true;
            return rtModule.Find(v, true);
        }

        private static Random rnd = new Random(DateTime.Now.Millisecond);
     
        private static TypeDef CreateDelegateType(ModuleDef ctx, MethodDef qeqenzi)
        {
            TypeDef ret;
            var typeSys = ctx.CorLibTypes;
            ret = new TypeDefUser(Utils.Rename(1), ctx.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
            ret.Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.NestedPrivate;

            var ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(ctx.CorLibTypes.Void, ctx.CorLibTypes.Object, ctx.CorLibTypes.IntPtr));
            ctor.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
            ctor.ImplAttributes = MethodImplAttributes.Runtime;
            ret.Methods.Add(ctor);

            var type = qeqenzi.MethodSig.Params.ToList();
            
            var typehaso = qeqenzi.MethodSig.RetType;
            var returnType = qeqenzi.ReturnType;
            var parameters = qeqenzi.Parameters.Select(p => p.Type).ToArray();

             var invoke = new MethodDefUser("Invoke", MethodSig.CreateInstance(returnType, parameters), MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask);
            invoke.ImplAttributes |= MethodImplAttributes.Runtime;
            for (int i = 1; i <= parameters.Length; i++)
            {
                 invoke.Parameters[i].CreateParamDef();
            }
            ret.Methods.Add(invoke);
            
            var beginInvoke = new MethodDefUser("BeginInvoke", MethodSig.CreateInstance(qeqenzi.Module.Import(typeof(IAsyncResult)).ToTypeSig(),
                 parameters.Concat(new[] { qeqenzi.Module.Import(typeof(AsyncCallback)).ToTypeSig(), typeSys.Object }).ToArray()),
             MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask);
            beginInvoke.ImplAttributes |= MethodImplAttributes.Runtime;
            for (int i = 0; i < parameters.Length; i++)
            {
                beginInvoke.Parameters[i + 1].CreateParamDef();
            }
            beginInvoke.Parameters[beginInvoke.Parameters.Count - 2].CreateParamDef();
            beginInvoke.Parameters[beginInvoke.Parameters.Count - 2].ParamDef.Name = "callback";
            beginInvoke.Parameters[beginInvoke.Parameters.Count - 1].CreateParamDef();
            beginInvoke.Parameters[beginInvoke.Parameters.Count - 1].ParamDef.Name = "object";
            ret.Methods.Add(beginInvoke);
           
            var endInvoke = new MethodDefUser("EndInvoke", MethodSig.CreateInstance(typeSys.Void, qeqenzi.Module.Import(typeof(IAsyncResult)).ToTypeSig()),
            MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask);
            endInvoke.ImplAttributes |= MethodImplAttributes.Runtime;
            endInvoke.Parameters[1].CreateParamDef();
            endInvoke.Parameters[1].ParamDef.Name = "result";


            ret.Methods.Add(endInvoke);
            qeqenzi.DeclaringType.NestedTypes.Add(ret); 
            return ret;
        }

		public static void InjectToMethod(MethodDef meth, string name, int id)
        {
            var containsOut = false;
            if (meth.Body.HasExceptionHandlers) { meth.Body.ExceptionHandlers.Clear(); }
            meth.Body.Instructions.Clear();
            var rrr = meth.Parameters.Where(i => i.Type.FullName.EndsWith("&"));
            if (rrr.Count() != 0)
                containsOut = true;

            var rrg = Config.moduleDefMD.CorLibTypes.Object.ToSZArraySig();
            var loc = new Local(Config.moduleDefMD.CorLibTypes.Object);
            var loc2 = new Local(new SZArraySig(Config.moduleDefMD.CorLibTypes.Object));
            var cli = new CilBody();
         
            foreach (var bodyVariable in meth.Body.Variables)
                cli.Variables.Add(bodyVariable);
            cli.Variables.Add(loc);
            cli.Variables.Add(loc2);
            var outParams = new List<Local>();
            var testerDictionary = new Dictionary<Parameter, Local>();
                if (containsOut)
                foreach (var parameter in rrr)
                {
                    var locf = new Local(parameter.Type.Next);
                    testerDictionary.Add(parameter, locf);
                    cli.Variables.Add(locf);
                }

            bool flag = meth.ReturnType.ElementType != ElementType.Void;

            int key1 = rnd.Next(100000, int.MaxValue);
            int key2 = rnd.Next(1000, int.MaxValue);
            TypeDef delegate_ = CreateDelegateType(meth.Module, meth);

            Instruction tryStart;
            Local variable = null;
            if (flag)
            {
                cli.InitLocals = true;
                cli.Variables.Add(variable = new Local(meth.Module.Import(meth.ReturnType)));
            }     

            cli.Instructions.Add(tryStart = Instruction.Create(OpCodes.Ldtoken, delegate_));
            cli.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, meth.MDToken.ToInt32()));
            cli.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, meth.DeclaringType.MDToken.ToInt32()));
            cli.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, key1));
            cli.Instructions.Add(Instruction.Create(OpCodes.Ldstr, name));   
            cli.Instructions.Add(Instruction.Create(OpCodes.Call, RunVM));
            cli.Instructions.Add(Instruction.Create(OpCodes.Castclass, delegate_));
      
            foreach (var arg in meth.Parameters)
            {
                arg.CreateParamDef();
                cli.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, arg));
            }
        
            cli.Instructions.Add(Instruction.Create(OpCodes.Callvirt, delegate_.FindMethods("Invoke").First()));

            Instruction instruction;

            if (flag)
            {
                cli.Instructions.Add(Instruction.Create(OpCodes.Stloc, variable));
                cli.Instructions.Add(Instruction.Create(OpCodes.Leave_S, instruction = Instruction.Create(OpCodes.Ldloc, variable)));
            }
            else
            {
                cli.Instructions.Add(Instruction.Create(OpCodes.Leave_S, instruction = Instruction.Create(OpCodes.Ret)));
            }

            Instruction instruction2;
            cli.Instructions.Add(instruction2 = Instruction.Create(OpCodes.Pop));
            cli.Instructions.Add(Instruction.Create(OpCodes.Rethrow));
            cli.Instructions.Add(instruction);
            if (flag)
            {
               cli.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            ExceptionHandler exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Catch);
            exceptionHandler.TryStart = tryStart;
            exceptionHandler.TryEnd = instruction2;
            exceptionHandler.HandlerStart = instruction2;
            exceptionHandler.HandlerEnd = instruction;
            exceptionHandler.CatchType = meth.Module.Import(typeof(object));
            cli.ExceptionHandlers.Add(exceptionHandler);

            meth.Body = cli;
            meth.Body.UpdateInstructionOffsets();
            meth.Body.MaxStack += 10;

            Protections.Class1.ConvertCode(meth);     
        }
    }
}
