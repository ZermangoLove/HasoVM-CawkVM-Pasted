using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HasoVM.Core.Helper
{
    static class ControlFlowUtils
    {
		public static System.Reflection.Emit.OpCode ToReflectionOp(this dnlib.DotNet.Emit.OpCode op)
		{
			Code code = op.Code;
			if (code <= Code.Ldc_I4)
			{
				if (code == Code.Ldarg_0)
				{
					return System.Reflection.Emit.OpCodes.Ldarg_0;
				}
				if (code == Code.Ldc_I4)
				{
					return System.Reflection.Emit.OpCodes.Ldc_I4;
				}
			}
			else
			{
				if (code == Code.Ret)
				{
					return System.Reflection.Emit.OpCodes.Ret;
				}
				switch (code)
				{
					case Code.Add:
						return System.Reflection.Emit.OpCodes.Add;
					case Code.Sub:
						return System.Reflection.Emit.OpCodes.Sub;
					case Code.Mul:
						return System.Reflection.Emit.OpCodes.Mul;
					case Code.And:
						return System.Reflection.Emit.OpCodes.And;
					case Code.Or:
						return System.Reflection.Emit.OpCodes.Or;
					case Code.Xor:
						return System.Reflection.Emit.OpCodes.Xor;
				}
			}
			throw new NotImplementedException();
		}
	}
}
