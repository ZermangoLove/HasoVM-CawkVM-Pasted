using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HasoVM.Core.Helper
{
   public static class StringUtils
    {
		public static void SetNewInstructions(this CilBody cilBody, List<Instruction> newInstructions, Dictionary<Instruction, Instruction> oldToNewInstructions)
        {
            List<ExceptionHandler> list = new List<ExceptionHandler>();
            foreach (ExceptionHandler exceptionHandler in cilBody.ExceptionHandlers)
            {
                ExceptionHandler exceptionHandler2 = new ExceptionHandler(exceptionHandler.HandlerType);
                exceptionHandler2.CatchType = exceptionHandler.CatchType;
                if (exceptionHandler.FilterStart != null)
                {
                    exceptionHandler2.FilterStart = oldToNewInstructions[exceptionHandler.FilterStart];
                }
                if (exceptionHandler.HandlerEnd != null)
                {
                    exceptionHandler2.HandlerEnd = oldToNewInstructions[exceptionHandler.HandlerEnd];
                }
                if (exceptionHandler.HandlerStart != null)
                {
                    exceptionHandler2.HandlerStart = oldToNewInstructions[exceptionHandler.HandlerStart];
                }
                if (exceptionHandler.TryEnd != null)
                {
                    exceptionHandler2.TryEnd = oldToNewInstructions[exceptionHandler.TryEnd];
                }
                if (exceptionHandler.TryStart != null)
                {
                    exceptionHandler2.TryStart = oldToNewInstructions[exceptionHandler.TryStart];
                }
                list.Add(exceptionHandler2);
            }
            cilBody.ExceptionHandlers.Clear();
            foreach (ExceptionHandler item in list)
            {
                cilBody.ExceptionHandlers.Add(item);
            }
            newInstructions.FixJumps(oldToNewInstructions);
            cilBody.Instructions.Clear();
            foreach (Instruction item2 in newInstructions)
            {
                cilBody.Instructions.Add(item2);
            }
        }
		public static List<T> GetRandomList<T>(this List<T> baseList)
		{
			return baseList.GetRandomList(null);
		}

		public static List<T> GetRandomList<T>(this List<T> baseList, Random random)
		{
			bool flag = random == null;
			if (flag)
			{
				random = new Random();
			}
			List<T> list = new List<T>();
			List<T> list2 = new List<T>();
			list2.AddRange(baseList);
			while (list2.Count > 0)
			{
				int index = random.Next(list2.Count);
				list.Add(list2[index]);
				list2.RemoveAt(index);
			}
			return list;
		}

	public static void FixJumps(this IEnumerable<Instruction> instructions, Dictionary<Instruction, Instruction> oldToNewTargets)
		{
			if (oldToNewTargets.Count == 0)
			{
				return;
			}
			foreach (Instruction instruction in instructions)
			{
				if (instruction.Operand is Instruction)
				{
					Instruction key = (Instruction)instruction.Operand;
					Instruction operand;
					if (oldToNewTargets.TryGetValue(key, out operand))
					{
						instruction.Operand = operand;
					}
				}
				else if (instruction.Operand is Instruction[])
				{
					Instruction[] array = (Instruction[])instruction.Operand;
					Instruction[] array2 = new Instruction[array.Length];
					for (int i = 0; i < array.Length; i++)
					{
						Instruction instruction2;
						if (oldToNewTargets.TryGetValue(array[i], out instruction2))
						{
							array2[i] = instruction2;
						}
						else
						{
							array2[i] = array[i];
						}
					}
					instruction.Operand = array2;
				}
			}
		}

		public static bool AnyJumpTo(this IEnumerable<Instruction> instructions, Instruction target)
		{
			foreach (Instruction instruction in instructions)
			{
				if (instruction.Operand is Instruction)
				{
					if ((Instruction)instruction.Operand == target)
					{
						return true;
					}
				}
				else if (instruction.Operand is Instruction[])
				{
					Instruction[] array = (Instruction[])instruction.Operand;
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i] == target)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
	}
}
