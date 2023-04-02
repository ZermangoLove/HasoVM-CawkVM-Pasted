using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HasoVM.Core.Protections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HasoVM.Core.Helper
{
    class Utils
    {
        public static string Rename(int a)
        {
            string renamer = null;
            switch (a)
            {
                case 0:
                   renamer = Guid.NewGuid().ToString().ToUpper().Substring(0, 8);
                   break;
           
                case 1:
                    renamer = Guid.NewGuid().ToString().ToUpper().Replace("-", string.Empty);
                    break;
              
                case 2:
                   IncrementNameId();
                   renamer = EncodeString(nameId, reflectionCharset);
                    break;
            
                case 3:
                   byte[] buff2= SHA1.Create().ComputeHash(Encoding.Unicode.GetBytes(Guid.NewGuid().ToString())).Take(4)
                 .ToArray<byte>();
                   renamer = EncodeString(buff2, unicodeCharset);
                    break;      
            }
            return renamer;
        }

        #region Charsets

        static readonly char[] asciiCharset = Enumerable.Range(32, 95)
            .Select(ord => (char)ord)
            .Except(new[] { '.' })
            .ToArray();

        static readonly char[] reflectionCharset = asciiCharset.Except(new[] { ' ', '[', ']' }).ToArray();

        static readonly char[] letterCharset = Enumerable.Range(0, 26)
            .SelectMany(ord => new[] { (char)('a' + ord), (char)('A' + ord) })
            .ToArray();

        static readonly char[] alphaNumCharset = Enumerable.Range(0, 26)
            .SelectMany(ord => new[] { (char)('a' + ord), (char)('A' + ord) })
            .Concat(Enumerable.Range(0, 10).Select(ord => (char)('0' + ord)))
            .ToArray();

        // Especially chosen, just to mess with people.
        // Inspired by: http://xkcd.com/1137/ :D
        static readonly char[] unicodeCharset = new char[] { }
            .Concat(Enumerable.Range(0x200b, 5).Select(ord => (char)ord))
            .Concat(Enumerable.Range(0x2029, 6).Select(ord => (char)ord))
            .Concat(Enumerable.Range(0x206a, 6).Select(ord => (char)ord))
            .Except(new[] { '\u2029' })
            .ToArray();

        #endregion

        static readonly byte[] nameId = new byte[8];
  
        private static void IncrementNameId()
        {
            for (int i = nameId.Length - 1; i >= 0; i--)
            {
                nameId[i]++;
                if (nameId[i] != 0)
                    break;
            }
        }
      
        private static string EncodeString(byte[] buff, char[] charset)
        {
            int i = (int)buff[0];
            StringBuilder stringBuilder = new StringBuilder();
            for (int j = 1; j < buff.Length; j++)
            {
                for (i = (i << 8) + (int)buff[j]; i >= charset.Length; i /= charset.Length)
                {
                    stringBuilder.Append(charset[i % charset.Length]);
                }
            }
            if (i != 0)
            {
                stringBuilder.Append(charset[i % charset.Length]);
            }
            return stringBuilder.ToString();
        }

        public static void Watermark()
        {
            TypeRef attr = Config.moduleDefMD.CorLibTypes.GetTypeRef("System", "Attribute");
            var attr2 = new TypeDefUser("", "0x29A", attr);
            Config.moduleDefMD.Types.Add(attr2);            
            MemberRefUser ctor2 = new MemberRefUser(Config.moduleDefMD, ".ctor", MethodSig.CreateInstance(Config.moduleDefMD.CorLibTypes.Void), attr2);   
           
           
            var ctor = new MethodDefUser(
            ".ctor",
            MethodSig.CreateInstance(Config.moduleDefMD.CorLibTypes.Void, Config.moduleDefMD.CorLibTypes.String),
            dnlib.DotNet.MethodImplAttributes.Managed,
            dnlib.DotNet.MethodAttributes.HideBySig | dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.SpecialName | dnlib.DotNet.MethodAttributes.RTSpecialName);
         
            ctor.Body = new CilBody();
            ctor.Body.MaxStack = 1;
            ctor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            ctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Config.moduleDefMD, ".ctor", MethodSig.CreateInstance(Config.moduleDefMD.CorLibTypes.Void), attr)));
            ctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction()); 
            attr2.Methods.Add(ctor);

            foreach (var type in Config.moduleDefMD.GetTypes())
            {
                if (type == Config.moduleDefMD.GlobalType) continue;
                if (type.Name != Config.TypeName) continue;
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    if (method.IsConstructor) continue;
                    var attr3 = new CustomAttribute(ctor);
                    attr3.ConstructorArguments.Add(new CAArgument(Config.moduleDefMD.CorLibTypes.String, "HasoVM Revolution " + Config.Version));
                    method.CustomAttributes.Add(attr3);
                }
            }

            var attr_3 = new CustomAttribute(ctor);
            attr_3.ConstructorArguments.Add(new CAArgument(Config.moduleDefMD.CorLibTypes.String, "HasoVM Revolution " + Config.Version));
            Config.moduleDefMD.CustomAttributes.Add(attr_3);
          
            var attr_4 = new CustomAttribute(ctor);
            attr_4.ConstructorArguments.Add(new CAArgument(Config.moduleDefMD.CorLibTypes.String, "0x29A#5107"));
            Config.moduleDefMD.CustomAttributes.Add(attr_4);
           
            TypeRef attrRef = Config.moduleDefMD.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "SuppressIldasmAttribute");
            var ctorRef = new MemberRefUser(Config.moduleDefMD, ".ctor", MethodSig.CreateInstance(Config.moduleDefMD.CorLibTypes.Void), attrRef);

            var attr5 = new CustomAttribute(ctorRef);
            Config.moduleDefMD.CustomAttributes.Add(attr5);
        }

        public static byte[] CompressLZMA(byte[] toCompress)
        {
            SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();

            using (MemoryStream input = new MemoryStream(toCompress))
            using (MemoryStream output = new MemoryStream())
            {

                coder.WriteCoderProperties(output);

                for (int i = 0; i < 8; i++)
                {
                    output.WriteByte((byte)(input.Length >> (8 * i)));
                }

                coder.Code(input, output, -1, -1, null);
                return output.ToArray();
            }
        }
    }
}
