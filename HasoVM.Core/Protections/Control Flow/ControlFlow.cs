using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HasoVM.Core.Helper;

namespace HasoVM.Core.Protections.ControlFlow
{
    class ControlFlow : IProtection
	{
		public class HBlock : ICloneable
		{
			public List<Instruction> instructions;

			public void Clear()
			{
				instructions = new List<Instruction>();
			}

			public object Clone()
			{
				return MemberwiseClone();
			}
		}

		public class BBlock : ICloneable
		{
			public List<Instruction> instructions;

			public List<Instruction> branchOrRet;

			public Instruction afterInstr;

			public List<Instruction> fakeBranches;

			public void Clear()
			{
				instructions = new List<Instruction>();
				branchOrRet = new List<Instruction>();
				afterInstr = null;
				fakeBranches = new List<Instruction>();
			}

			public object Clone()
			{
				return MemberwiseClone();
			}
		}

		public class DynamicCode
		{
			private delegate int Result();

			private int intensity;

			private Random r;

			public DynamicCode(int intensity)
			{
				this.intensity = intensity;
				r = new Random();
			}

			public Instruction[] Create()
			{
				int positionValue = r.Next(0, intensity);
				List<Instruction> instructions = new List<Instruction>();
				instructions.Add(dnlib.DotNet.Emit.OpCodes.Ldc_I4.ToInstruction(r.Next()));
				instructions.Add(dnlib.DotNet.Emit.OpCodes.Ldc_I4.ToInstruction(r.Next()));
				for (int i = 0; i < intensity; i++)
				{
					instructions.Add(getRandomOperation().ToInstruction());
					if (positionValue == i)
					{
						instructions.Add(dnlib.DotNet.Emit.OpCodes.Ldarg_0.ToInstruction());
					}
					else
					{
						instructions.Add(dnlib.DotNet.Emit.OpCodes.Ldc_I4.ToInstruction(r.Next()));
					}
				}
				instructions.Add(getRandomOperation().ToInstruction());
				instructions.Add(dnlib.DotNet.Emit.OpCodes.Ret.ToInstruction());
				return instructions.ToArray();
			}

			public int RandomNumberInModule(Instruction[] instructions, int module, bool divisible)
			{
				int Rnum = module * r.Next(1, 12);
				Rnum = (divisible ? Rnum : (Rnum + 1));
				int x = 0;
				List<Instruction> instsx = new List<Instruction>();
				for (; instructions[x].OpCode != dnlib.DotNet.Emit.OpCodes.Ldarg_0; x++)
				{
					instsx.Add(instructions[x]);
				}
				instsx.Add(dnlib.DotNet.Emit.OpCodes.Ret.ToInstruction());
				int valuesx = Emulate(instsx.ToArray(), 0);
				List<Instruction> instdx = new List<Instruction>();
				instdx.Add(dnlib.DotNet.Emit.OpCodes.Ldc_I4.ToInstruction(Rnum));
				for (int i = instructions.Length - 2; i > x + 2; i -= 2)
				{
					Instruction operation = ReverseOperation(instructions[i].OpCode).ToInstruction();
					Instruction value = instructions[i - 1];
					instdx.Add(value);
					instdx.Add(operation);
				}
				instdx.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Ret));
				int valuedx = Emulate(instdx.ToArray(), 0);
				Instruction ope = ReverseOperation(instructions[x + 1].OpCode).ToInstruction();
				int finalValue = Emulate(new List<Instruction>
				{
					dnlib.DotNet.Emit.OpCodes.Ldc_I4.ToInstruction(valuedx),
					dnlib.DotNet.Emit.OpCodes.Ldc_I4.ToInstruction(valuesx),
					(ope.OpCode == dnlib.DotNet.Emit.OpCodes.Add) ? dnlib.DotNet.Emit.OpCodes.Sub.ToInstruction() : ope,
					dnlib.DotNet.Emit.OpCodes.Ret.ToInstruction()
				}.ToArray(), 0);
				if (ope.OpCode != dnlib.DotNet.Emit.OpCodes.Add)
				{
					return finalValue;
				}
				return finalValue * -1;
			}

			public static int Emulate(Instruction[] code, int value)
			{
				DynamicMethod emulatore = new DynamicMethod("MER ? BUULHE", typeof(int), null);
				ILGenerator il = emulatore.GetILGenerator();
				foreach (Instruction instr in code)
				{
					if (instr.OpCode == dnlib.DotNet.Emit.OpCodes.Ldarg_0)
					{
						il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, value);
					}
					else if (instr.Operand != null)
					{
						il.Emit(instr.OpCode.ToReflectionOp(), Convert.ToInt32(instr.Operand));
					}
					else
					{
						il.Emit(instr.OpCode.ToReflectionOp());
					}
				}
				return ((Result)emulatore.CreateDelegate(typeof(Result)))();
			}

			private dnlib.DotNet.Emit.OpCode getRandomOperation()
			{
				dnlib.DotNet.Emit.OpCode operation = null;
				switch (r.Next(0, 3))
				{
					case 0:
						operation = dnlib.DotNet.Emit.OpCodes.Add;
						break;
					case 1:
						operation = dnlib.DotNet.Emit.OpCodes.Sub;
						break;
					case 2:
						operation = dnlib.DotNet.Emit.OpCodes.Xor;
						break;
				}
				return operation;
			}

			private dnlib.DotNet.Emit.OpCode ReverseOperation(dnlib.DotNet.Emit.OpCode operation)
			{

				Code code = operation.Code;
				if (code == Code.Add)
				{
					return dnlib.DotNet.Emit.OpCodes.Sub;
				}
				if (code == Code.Sub)
				{
					return dnlib.DotNet.Emit.OpCodes.Add;
				}
				if (code != Code.Xor)
				{
					throw new NotImplementedException();
				}
				return dnlib.DotNet.Emit.OpCodes.Xor;
			}
		}

		public static Dictionary<MethodDef, Tuple<int[], int[]>> obfMethods;

		public override void Run(ModuleDefMD module)
		{
			obfMethods = CreateMethods(module);
			foreach (TypeDef type in module.Types.Where(t => t.Name == "VM"))
			{
				foreach (MethodDef method in type.Methods.Where(
					m => m.Name != "extractResource"))
				{
					if (!method.HasBody || !method.Body.HasInstructions || method.IsNative) continue;
					CilBody body = method.Body;
					body.SimplifyBranches();
					body.MaxStack++;
					List<Instruction> instructions = body.Instructions.ToList();
					new List<HBlock>();
					
					List<HBlock> obj = new List<HBlock> { ObfuscateHBlock(new HBlock
		        	{
			         
						instructions = instructions
			       
					}, isHBlock: false) };
					
					body.Instructions.Clear();
					
					foreach (HBlock item in obj)
					{
						foreach (Instruction instr in item.instructions)
						{
							body.Instructions.Add(instr);
						}
					}
				
					body.UpdateInstructionOffsets();
					body.SimplifyBranches();
				}
			}
		}
		public static void DoControlFlow_MD(MethodDef method, ModuleDef module)
		{
			obfMethods = CreateMethods(module);
			if (!method.HasBody || !method.Body.HasInstructions || method.IsNative)
			{
				return;
			}
			CilBody body = method.Body;
			body.SimplifyBranches();
			body.MaxStack++;
			List<Instruction> instructions = body.Instructions.ToList();
			new List<HBlock>();
			List<HBlock> obj = new List<HBlock> { ObfuscateHBlock(new HBlock
			{
				instructions = instructions
			}, isHBlock: false) };
			body.Instructions.Clear();
			foreach (HBlock item in obj)
			{
				foreach (Instruction instr in item.instructions)
				{
					body.Instructions.Add(instr);
				}
			}
			body.UpdateInstructionOffsets();
			body.SimplifyBranches();
		}
		public static void DoControlFlow_(ModuleDef module)
		{
			obfMethods = CreateMethods(module);
			foreach (TypeDef type in module.Types)
			{
				foreach (MethodDef method in type.Methods)
				{
					if (!method.HasBody || !method.Body.HasInstructions || method.IsNative) continue;
					CilBody body = method.Body;
					body.SimplifyBranches();
					body.MaxStack++;
					List<Instruction> instructions = body.Instructions.ToList();
					new List<HBlock>();

					List<HBlock> obj = new List<HBlock> { ObfuscateHBlock(new HBlock
					{

						instructions = instructions

					}, isHBlock: false) };

					body.Instructions.Clear();

					foreach (HBlock item in obj)
					{
						foreach (Instruction instr in item.instructions)
						{
							body.Instructions.Add(instr);
						}
					}

					body.UpdateInstructionOffsets();
					body.SimplifyBranches();
				}
			}

		}
		public static HBlock ObfuscateHBlock(HBlock HB, bool isHBlock)
		{
			List<BBlock> bBlocks = new List<BBlock>();
			List<Instruction> instructions = HB.instructions;
			Instruction firstBr = Instruction.Create(dnlib.DotNet.Emit.OpCodes.Br, instructions[0]);
			BBlock mainBlock = new BBlock
			{
				instructions = new List<Instruction>(),
				fakeBranches = new List<Instruction>(),
				branchOrRet = new List<Instruction>()
			};
			int stack = 0;
			for (int i = 0; i < instructions.Count; i++)
			{
				Instruction instr = instructions[i];
				instr.CalculateStackUsage(out var push, out var pop);
				stack += push - pop;
				if (instr.OpCode == dnlib.DotNet.Emit.OpCodes.Ret)
				{
					mainBlock.branchOrRet.Add(instr);
					bBlocks.Add((BBlock)mainBlock.Clone());
					mainBlock.Clear();
				}
				else if (stack == 0 && instr.OpCode.OpCodeType != dnlib.DotNet.Emit.OpCodeType.Prefix)
				{
					MethodDef obfMethod = obfMethods.Keys.ToArray()[new Random().Next(0, 4)];
					mainBlock.instructions.Add(instr);
					if (new Random().Next(0, 2) == 0)
					{
						mainBlock.branchOrRet.Add(Instruction.CreateLdcI4(obfMethods[obfMethod].Item2[new Random().Next(0, 4)]));
						mainBlock.branchOrRet.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Call, obfMethod));
						mainBlock.branchOrRet.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Brfalse, instructions[i + 1]));
					}
					else
					{
						mainBlock.branchOrRet.Add(Instruction.CreateLdcI4(obfMethods[obfMethod].Item1[new Random().Next(0, 4)]));
						mainBlock.branchOrRet.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Call, obfMethod));
						mainBlock.branchOrRet.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Brtrue, instructions[i + 1]));
					}
					bBlocks.Add((BBlock)mainBlock.Clone());
					mainBlock.Clear();
				}
				else
				{
					mainBlock.instructions.Add(instr);
				}
			}
			bBlocks = Shuffle(bBlocks, out var position);
			int index = Array.IndexOf(position, position.Length - 1);
			BBlock lastB = bBlocks[position.Length - 1];
			BBlock tempB = bBlocks[index];
			bBlocks[index] = lastB;
			bBlocks[position.Length - 1] = tempB;
			if (isHBlock)
			{
				int index2 = Array.IndexOf(position, 0);
				BBlock firstB = bBlocks[0];
				BBlock tempB2 = bBlocks[index2];
				bBlocks[index2] = firstB;
				bBlocks[0] = tempB2;
			}
			foreach (BBlock block in bBlocks)
			{
				if (block.branchOrRet[0].OpCode != dnlib.DotNet.Emit.OpCodes.Ret)
				{
					MethodDef obfMethod2 = obfMethods.Keys.ToArray()[new Random().Next(0, 4)];
					int rr = new Random().Next(0, bBlocks.Count);
					while (bBlocks[rr].instructions.Count == 0)
					{
						rr = new Random().Next(0, bBlocks.Count);
					}
					if (new Random().Next(0, 2) == 0)
					{
						block.fakeBranches.Add(Instruction.CreateLdcI4(obfMethods[obfMethod2].Item1[new Random().Next(0, 4)]));
						block.fakeBranches.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Call, obfMethod2));
						block.fakeBranches.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Brfalse, bBlocks[rr].instructions[0]));
					}
					else
					{
						block.fakeBranches.Add(Instruction.CreateLdcI4(obfMethods[obfMethod2].Item2[new Random().Next(0, 4)]));
						block.fakeBranches.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Call, obfMethod2));
						block.fakeBranches.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Brtrue, bBlocks[rr].instructions[0]));
					}
				}
			}
			List<Instruction> bInstrs = new List<Instruction>();
			foreach (BBlock B in bBlocks)
			{
				bInstrs.AddRange(B.instructions);
				if (new Random().Next(0, 2) == 0)
				{
					if (B.branchOrRet.Count != 0)
					{
						bInstrs.AddRange(B.branchOrRet);
					}
					if (B.fakeBranches.Count != 0)
					{
						bInstrs.AddRange(B.fakeBranches);
					}
				}
				else
				{
					if (B.fakeBranches.Count != 0)
					{
						bInstrs.AddRange(B.fakeBranches);
					}
					if (B.branchOrRet.Count != 0)
					{
						bInstrs.AddRange(B.branchOrRet);
					}
				}
				if (B.afterInstr != null)
				{
					bInstrs.Add(B.afterInstr);
				}
			}
			if (!isHBlock)
			{
				bInstrs.Insert(0, firstBr);
			}
			return new HBlock
			{
				instructions = bInstrs
			};
		}

		public static List<T> Shuffle<T>(List<T> array, out int[] position)
		{
			Random rand = new Random();
			List<KeyValuePair<int, T>> list = new List<KeyValuePair<int, T>>();
			foreach (T s in array)
			{
				list.Add(new KeyValuePair<int, T>(rand.Next(), s));
			}
			IOrderedEnumerable<KeyValuePair<int, T>> orderedEnumerable = list.OrderBy(delegate (KeyValuePair<int, T> item)
			{
				KeyValuePair<int, T> keyValuePair = item;
				return keyValuePair.Key;
			});
			T[] result = new T[array.Count];
			int index = 0;
			foreach (KeyValuePair<int, T> item in orderedEnumerable)
			{
				result[index] = item.Value;
				index++;
			}
			List<int> positions = new List<int>();
			for (int i = 0; i < array.Count; i++)
			{
				positions.Add(Array.IndexOf(array.ToArray(), result[i]));
			}
			position = positions.ToArray();
			return result.ToList();
		}

		public static Dictionary<MethodDef, Tuple<int[], int[]>> CreateMethods(ModuleDef loadedMod)
		{
			DynamicCode code = new DynamicCode(3);
			int[] modules = new int[4];
			for (int l = 0; l < modules.Length; l++)
			{
				modules[l] = new Random().Next(2, 25);
			}
			Instruction[,] methods = new Instruction[4, 10];
			for (int k = 0; k < 4; k++)
			{
				Instruction[] methodBody = code.Create();
				for (int y = 0; y < methodBody.Length; y++)
				{
					methods[k, y] = methodBody[y];
				}
			}
			List<Tuple<Instruction[], Tuple<int, Tuple<int[], int[]>>>> InstrToInt = new List<Tuple<Instruction[], Tuple<int, Tuple<int[], int[]>>>>();
			for (int j = 0; j < 4; j++)
			{
				List<Instruction> instr = new List<Instruction>();
				int[] numbersTrue = new int[5];
				int[] numbersFalse = new int[5];
				for (int y4 = 0; y4 < 10; y4++)
				{
					instr.Add(methods[j, y4]);
				}
				for (int y3 = 0; y3 < 5; y3++)
				{
					numbersTrue[y3] = code.RandomNumberInModule(instr.ToArray(), modules[j], divisible: true);
				}
				for (int y2 = 0; y2 < 5; y2++)
				{
					numbersFalse[y2] = code.RandomNumberInModule(instr.ToArray(), modules[j], divisible: false);
				}
				InstrToInt.Add(Tuple.Create(instr.ToArray(), Tuple.Create(modules[j], Tuple.Create(numbersTrue, numbersFalse))));
			}
			Dictionary<MethodDef, Tuple<int[], int[]>> final = new Dictionary<MethodDef, Tuple<int[], int[]>>();
			MethodAttributes methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
			MethodImplAttributes methImplFlags = MethodImplAttributes.IL;
			for (int i = 0; i < 4; i++)
			{
				MethodDef methodDefs1 = new MethodDefUser(Utils.Rename(1), MethodSig.CreateStatic(loadedMod.CorLibTypes.Boolean, loadedMod.CorLibTypes.Int32), methImplFlags, methFlags);
				methodDefs1.Name = Utils.Rename(1);
				methodDefs1.Body = new CilBody();
				methodDefs1.ParamDefs.Add(new ParamDefUser(Utils.Rename(1)));
				List<Instruction> list = new List<Instruction>(InstrToInt[i].Item1);
				int module = InstrToInt[i].Item2.Item1;
				list.Insert(list.Count - 1, Instruction.CreateLdcI4(module));
				list.Insert(list.Count - 1, dnlib.DotNet.Emit.OpCodes.Rem.ToInstruction());
				list.Insert(list.Count - 1, Instruction.CreateLdcI4(0));
				list.Insert(list.Count - 1, Instruction.Create(dnlib.DotNet.Emit.OpCodes.Ceq));

				foreach (Instruction item in list)
				{
					methodDefs1.Body.Instructions.Add(item);
				}
				final.Add(methodDefs1, InstrToInt[i].Item2.Item2);

			}

			foreach (var md in loadedMod.GlobalType.Methods)
			{
				if (md.Name != ".ctor") continue;
				loadedMod.GlobalType.Remove(md);
				break;
			}

			TypeDef type1 = new TypeDefUser("Runtime", Utils.Rename(1), loadedMod.CorLibTypes.Object.TypeDefOrRef);
			type1.Attributes = dnlib.DotNet.TypeAttributes.Public | dnlib.DotNet.TypeAttributes.AutoLayout |
			dnlib.DotNet.TypeAttributes.Class | dnlib.DotNet.TypeAttributes.AnsiClass;
			loadedMod.Types.Add(type1);
			
			foreach (KeyValuePair<MethodDef, Tuple<int[], int[]>> item2 in final)
			{
			   type1.Methods.Add(item2.Key);
			}

			return final;
		}
	}
}
