using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HasoVM.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HasoVM.Core.Protections
{
	class Class1
	{
		private static Dictionary<Local, Local> _varToArrayVar;
		private static Dictionary<Local, int> _varToElementIndex;
		private static Local _tempObjectVar;
        private static Random _random = new Random();
   
		public static void ConvertCode(MethodDef m)
		{
			m.Body.MaxStack = 65535;
			m.Body.SimplifyMacros(m.Parameters);	  
			m.Body.SimplifyBranches();
		  
			List<Instruction> list = new List<Instruction>();  
			Dictionary<Instruction, Instruction> dictionary = new Dictionary<Instruction, Instruction>();
		
			foreach (Instruction instruction in m.Body.Instructions)
			{
				List<Instruction> list2 = CreateInstructionCopy(instruction, m);
				dictionary.Add(instruction, list2.First<Instruction>());
				list.AddRange(list2);
			}
		  
			m.Body.SetNewInstructions(list, dictionary);
		}
	
		private static List<Instruction> CreateInstructionCopy2(Instruction instruction, MethodDef m)
		{
			switch (instruction.OpCode.Code)
			{
				case Code.Ldloc:
					{
						List<Instruction> list = null;
						Local key = (Local)instruction.Operand;
						Local variable;
						if (_varToArrayVar.TryGetValue(key, out variable))
						{
							int value = _varToElementIndex[key];
							list = new List<Instruction>
					{
						 Instruction.Create(OpCodes.Ldloc, variable),
					     Instruction.Create(OpCodes.Ldc_I4, value),
						 Instruction.Create(OpCodes.Ldelem_Ref)
					};
						}
						if (list != null)
						{
							return list;
						}
						break;
					}
				case Code.Ldloca:
					{
						List<Instruction> list2 = null;
						Local key2 = (Local)instruction.Operand;
						Local variable2;
						if (_varToArrayVar.TryGetValue(key2, out variable2))
						{
							int value2 = _varToElementIndex[key2];
							list2 = new List<Instruction>
					{
						 Instruction.Create(OpCodes.Ldloc, variable2),
						 Instruction.Create(OpCodes.Ldc_I4, value2),
						 Instruction.Create(OpCodes.Ldelema, m.Module.CorLibTypes.Object)
					};
						}
						if (list2 != null)
						{
							return list2;
						}
						break;
					}
				case Code.Stloc:
					{
						List<Instruction> list3 = null;
						Local key3 = (Local)instruction.Operand;
						Local variable3;
						if (_varToArrayVar.TryGetValue(key3, out variable3))
						{
							int value3 = _varToElementIndex[key3];
							list3 = new List<Instruction>
					{
						 Instruction.Create(OpCodes.Stloc, _tempObjectVar),
						 Instruction.Create(OpCodes.Ldloc, variable3),
						 Instruction.Create(OpCodes.Ldc_I4, value3),
						 Instruction.Create(OpCodes.Ldloc, _tempObjectVar),
						 Instruction.Create(OpCodes.Stelem_Ref)
					};
						}
						if (list3 != null)
						{
							return list3;
						}
						break;
					}
			}
			return new List<Instruction>
			{
				instruction
			};
		}

		private static List<Instruction> CreateInstructionCopy(Instruction instruction, MethodDef m)
		{
			Code code = instruction.OpCode.Code;
			if (code == Code.Ldc_I4)
			{
				List<Instruction> list = null;
				uint num = (uint)((int)instruction.Operand);
			
					uint num2 = (uint)(1 + _random.Next(1073741823));
					uint value = num2 ^ num;
					list = new List<Instruction>
					{
						 Instruction.Create(OpCodes.Ldc_I4, (int)value),
						 Instruction.Create(OpCodes.Ldc_I4, (int)num2),
						 Instruction.Create(OpCodes.Xor)
					};
				if (list != null)
				{
					return list;
				}
			}
			return new List<Instruction>
			{
				instruction
			};
		}
	}
}
