using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HasoVM.Core.Helper
{
    public class Data
    {
        public MethodDef Method;
        public byte[] EncryptedBytes;
        public int ID;
        public string name;
        public Data(MethodDef methods)
        {
            Method = methods;
        }
    }
}
