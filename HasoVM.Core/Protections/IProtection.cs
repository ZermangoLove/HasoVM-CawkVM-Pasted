using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HasoVM.Core.Protections
{
	public abstract class IProtection
	{
		public abstract void Run(ModuleDefMD m);
	}
}
