
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Runtime
{
        public class VM
        {
            private static readonly Dictionary<int, Delegate> cache = new Dictionary<int, Delegate>();
            private static Module module = typeof(VM).Assembly.ManifestModule;
            private static dynamic iLGenerator;
            private static Dictionary<int, dynamic> labels; //  private static Dictionary<int, System.Reflection.Emit.Label> labels;
            private static List<dynamic> locals; //  private static List<LocalBuilder> locals;
            private static dynamic binreader;
            private static dynamic opc2;
            private static dynamic[] oneByteOpCodes = new dynamic[256];
            private static dynamic[] twoByteOpCodes = new dynamic[256];
      
            [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
            unsafe delegate void Invoke_(IntPtr A_0, int A_1);
         
            private static Invoke_ Run;

            [DllImport("kernel32", CharSet = CharSet.Auto, EntryPoint = "GetModuleHandle")]
            private static extern IntPtr a(string A_0);

            [DllImport("kernel32", CharSet = CharSet.Auto, EntryPoint = "LoadLibrary")]
            private static extern IntPtr b(string A_0);
            [DllImport("kernel32", CharSet   = CharSet.Ansi, EntryPoint = "GetProcAddress", ExactSpelling = true)]
            private static extern IntPtr c(IntPtr A_0, string A_1);
            public static Delegate RunVM(RuntimeTypeHandle A_0, int A_1, int A_2, int A_3, string A_4 )
            {
            Type typefrom = Type.GetTypeFromHandle(A_0);
            Delegate _dynamicMethod = null;

           
            if (cache.TryGetValue(A_3, out _dynamicMethod))   
            {
                return _dynamicMethod;
            }
            MethodBase methodBase;
            object methodFromHandle = MethodBase.GetMethodFromHandle(module.ModuleHandle.ResolveMethodHandle(A_1), module.ModuleHandle.ResolveTypeHandle(A_2));
            methodBase = (MethodBase)methodFromHandle;
          
            var callingMethod = methodBase;    
       
            var methodBody = callingMethod.GetMethodBody();
            binreader = new BinaryReader(new MemoryStream(extractResource(A_4)));
            
            Type[] parameterTypes = parametersArray2(callingMethod);
             
            DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, (callingMethod is ConstructorInfo) ? null : ((MethodInfo)callingMethod).ReturnType, parameterTypes, callingMethod.DeclaringType, true);
        
            iLGenerator = dynamicMethod.GetILGenerator();

            var locs = methodBody.LocalVariables;
                locals = new List<dynamic>();
          
            foreach (var localVariableInfo in locs)
                    locals.Add(iLGenerator.DeclareLocal(localVariableInfo.LocalType));

            var instructionCount = binreader.ReadInt32();

                labels = new Dictionary<int, dynamic>();

                for (var u = 0; u < instructionCount; u++)
                {
                    labels.Add(u, iLGenerator.DefineLabel());
                }

               for (var i = 0; i < instructionCount; i++)
                {
                var opcode = binreader.ReadInt16();
                if (opcode >= 0 && opcode < oneByteOpCodes.Length)
                {
                    opc2 = oneByteOpCodes[opcode];
                }
                else
                {
                    var b2 = (byte)(opcode | 0xFE00);
                    opc2 = twoByteOpCodes[b2];
                }

                iLGenerator.MarkLabel(labels[i]);
                binreader.ReadByte();
                   
                HandleOpc();
       
                }
           
                Delegate _delegate = dynamicMethod.CreateDelegate(typefrom);
                Dictionary<int, Delegate> _obj3 = cache;
                lock (_obj3)
                {
                    if (!cache.ContainsKey(A_3))
                    {
                        cache.Add(A_3, _delegate);
                    }
                }

                  return _delegate;
            }
   
         private static void HandleOpc()
         {

                switch (opc2.OperandType)
                {
                    case OperandType.InlineNone:
                        iLGenerator.Emit(opc2);
                        break;

                   case OperandType.InlineMethod:
                        dynamic mdtoken = new HasoReader().Read32();//binreader.ReadInt32();
                        dynamic resolvedMethodBase = module.ResolveMethod(mdtoken);  
                        dynamic methodInfo = resolvedMethodBase as MethodInfo;
                        dynamic constructorInfo = resolvedMethodBase as ConstructorInfo;
             
                     if (methodInfo != null)
                     {
                         iLGenerator.Emit(opc2, methodInfo);
                         return;
                     }

                     if (constructorInfo != null)
                     {
                        iLGenerator.Emit(opc2, constructorInfo);
                     }
                  
                    break;
                  
                    case OperandType.InlineString:
                     dynamic readString = binreader.ReadString();
                     var original = new StringBuilder(readString);    
                     var encrypted = new StringBuilder(readString.Length);                
                    for (int iam = 0; iam < readString.Length; iam++)
                         encrypted.Append((char)(original[iam] ^ 29)); 
                    dynamic decrypted = encrypted.ToString();
                  
                    iLGenerator.Emit(opc2, decrypted);
                        break;

                    case OperandType.InlineI:               
                    dynamic readInt32 = binreader.ReadInt32();
                      
                    iLGenerator.Emit(opc2, readInt32);
                        break;

                    case OperandType.InlineField:                   
                    dynamic mdtoken1 = binreader.ReadInt32();                  
                    dynamic fieldInfo = module.ResolveField(mdtoken1);
                      
                    iLGenerator.Emit(opc2, fieldInfo);
                        break;

                    case OperandType.InlineType:                    
                    dynamic mdtoken2 = binreader.ReadInt32();       
                    dynamic type = module.ResolveType(mdtoken2);
                      
                    iLGenerator.Emit(opc2, type);
                        break;

                    case OperandType.ShortInlineBrTarget:                    
                    dynamic index = binreader.ReadInt32();                   
                    dynamic location = labels[index];
                    
                    iLGenerator.Emit(opc2, location);
                        break;

                    case OperandType.ShortInlineI:            
                    dynamic b = binreader.ReadByte();
                      
                    iLGenerator.Emit(opc2, b);
                        break;

                    case OperandType.InlineSwitch:
                        dynamic count = binreader.ReadInt32();

                    System.Reflection.Emit.Label[] allLabels = new System.Reflection.Emit.Label[count];
                        for (int i2 = 0; i2 < count; i2++)
                        {
                            allLabels[i2] = labels[binreader.ReadInt32()];
                        }
                        iLGenerator.Emit(opc2, allLabels);
                        break;

                    case OperandType.InlineBrTarget:
                        dynamic index1 = binreader.ReadInt32();
                        dynamic location1 = labels[index1];
                        iLGenerator.Emit(opc2, location1);
                        break;

                   case OperandType.InlineTok:
                        dynamic mdtoken3 = binreader.ReadInt32();
                        dynamic type3 = binreader.ReadByte();
                        if (type3 == 0)
                        {
                            dynamic fieldinfo = module.ResolveField(mdtoken3);
                            iLGenerator.Emit(opc2, fieldinfo);
                        }
                        else if (type3 == 1)
                        {
                            dynamic typeInfo = module.ResolveType(mdtoken3);
                            iLGenerator.Emit(opc2, typeInfo);
                        }
                        else if (type3 == 2)
                        {
                            dynamic methodinfo = module.ResolveMethod(mdtoken3);

                            if (methodinfo is MethodInfo)
                                iLGenerator.Emit(opc2, (MethodInfo)methodinfo);
                            else if (methodinfo is ConstructorInfo)
                                iLGenerator.Emit(opc2, (ConstructorInfo)methodinfo);
                        }
                        break;
                 
                case OperandType.InlineVar:       
                    dynamic index2 = binreader.ReadInt32();                       
                    dynamic parOrloc = binreader.ReadByte();
                      
                    if (parOrloc == 0)
                        {
                            var label = locals[index2];
                            dynamic labelh = label;
                            iLGenerator.Emit(opc2, labelh);
                        }
                    else
                        {
                            iLGenerator.Emit(opc2, index2);
                        }
                        break;
               
                case OperandType.ShortInlineVar:
                    dynamic index666 = binreader.ReadInt32();
                    dynamic parOrloc666 = binreader.ReadByte();

                    if (parOrloc666 == 0)
                    {
                        var label = locals[index666];
                        dynamic labelh = label;
                        iLGenerator.Emit(opc2, labelh);
                    }
                    else
                    {
                        iLGenerator.Emit(opc2, index666);
                    }
                    break;
                
                case OperandType.ShortInlineR:           
                    dynamic value = binreader.ReadBytes(4);       
                    dynamic myFloat = BitConverter.ToSingle(value, 0);
                       
                    iLGenerator.Emit(opc2, myFloat);
                        break;

                    case OperandType.InlineR:             
                    dynamic value4 = binreader.ReadDouble();
                      
                    iLGenerator.Emit(opc2, value4);
                        break;

                    case OperandType.InlineI8:                      
                    dynamic value5 = binreader.ReadInt64();
                      
                    iLGenerator.Emit(opc2, value5);
                        break;

                    default:
                    MessageBox.Show(string.Format("OperandType {0} is not supported!", opc2.OperandType));
                    break;

            }
           
        }

        private static byte[] extractResource(string A_0)
        {

            using (dynamic manifestResourceStream = module.Assembly.GetManifestResourceStream(A_0))
            {
                using (new StreamReader(manifestResourceStream))
                {
                    dynamic bytes = new byte[manifestResourceStream.Length];
                    manifestResourceStream.Read(bytes, 0, bytes.Length);

                    if (bytes.Length == 0)
                        return bytes;
                    dynamic arrSize = Marshal.SizeOf(bytes[0]) * bytes.Length;
                    dynamic decryptedBytes = new byte[bytes.Length];
                    dynamic ptr = Marshal.AllocHGlobal(arrSize);
                    Marshal.Copy(bytes, 0, ptr, bytes.Length);
                    Run(ptr, arrSize);
                    Marshal.Copy(ptr, decryptedBytes, 0, bytes.Length);
                    Marshal.FreeHGlobal(ptr);
                    return decryptedBytes;
                }
            }
        }

        public static void Starter()
        {
            var typeFromHandle = typeof(OpCode);
            var typeFromHandle2 = typeof(OpCodes);

            foreach (var fieldInfo in typeFromHandle2.GetFields())
                if (fieldInfo.FieldType == typeFromHandle)
                {
                    var opCode = (OpCode)fieldInfo.GetValue(null);
                    var num = (ushort)opCode.Value;
                    if (opCode.Size == 1)
                    {
                        var b = (byte)num;
                        oneByteOpCodes[b] = opCode;
                    }
                    else
                    {
                        var b2 = (byte)(num | 65024);
                        twoByteOpCodes[b2] = opCode;
                    }
                }

            byte[] array;
            using (Stream manifestResourceStream2 = module.Assembly.GetManifestResourceStream((IntPtr.Size == 4) ? "_HasoRT" : "_HasoRT64"))
            {
                array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
            }

            byte[] dec = Lzma.Decompress(array);
            string tempFileName = Path.GetTempFileName();
            File.Delete(tempFileName);
            Directory.CreateDirectory(tempFileName);
            string text1 = System.Guid.NewGuid().ToString() + ".dll";
            string text2 = Path.Combine(tempFileName, text1);
            File.WriteAllBytes(text2, dec);
            IntPtr intPtr = b(text2);
            IntPtr addr = c(intPtr, "_0x29A");
            Run = (Invoke_)Marshal.GetDelegateForFunctionPointer(addr, typeof(Invoke_));

        }

        private static Type[] parametersArray2(MethodBase A_0)
        {
                ParameterInfo[] parameters = A_0.GetParameters();
                int num = parameters.Length;
                if (!A_0.IsStatic)
                {
                    num++;
                }
                Type[] array = new Type[num];
                int  num2 = 0;
                if (!A_0.IsStatic)
                {
                    if (A_0.DeclaringType.IsValueType)
                    {
                        array[0] = A_0.DeclaringType.MakeByRefType();
                    }
                    else
                    {
                        array[0] = A_0.DeclaringType;
                    }
                    num2++;
                }
            int i = 0;
                while (i < parameters.Length)
                {
                    array[num2] = parameters[i].ParameterType;
                    i++;
                    num2++;
                }
                return array;
         }


        internal static class Lzma
        {
            const uint kNumStates = 12;

            const int kNumPosSlotBits = 6;

            const uint kNumLenToPosStates = 4;

            const uint kMatchMinLen = 2;

            const int kNumAlignBits = 4;
            const uint kAlignTableSize = 1 << kNumAlignBits;

            const uint kStartPosModelIndex = 4;
            const uint kEndPosModelIndex = 14;

            const uint kNumFullDistances = 1 << ((int)kEndPosModelIndex / 2);

            const int kNumPosStatesBitsMax = 4;
            const uint kNumPosStatesMax = (1 << kNumPosStatesBitsMax);

            const int kNumLowLenBits = 3;
            const int kNumMidLenBits = 3;
            const int kNumHighLenBits = 8;
            const uint kNumLowLenSymbols = 1 << kNumLowLenBits;
            const uint kNumMidLenSymbols = 1 << kNumMidLenBits;

            public static byte[] Decompress(byte[] data)
            {
                LzmaDecoder coder = new LzmaDecoder();

                using (MemoryStream input = new MemoryStream(data))
                using (MemoryStream output = new MemoryStream())
                {

                    // Read the decoder properties
                    byte[] properties = new byte[5];
                    input.Read(properties, 0, 5);


                    // Read in the decompress file size.
                    byte[] fileLengthBytes = new byte[8];
                    input.Read(fileLengthBytes, 0, 8);
                    long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

                    coder.SetDecoderProperties(properties);
                    coder.Code(input, output, input.Length, fileLength);

                    return output.ToArray();


                    /*var s = new MemoryStream(data);
                    var decoder = new LzmaDecoder();
                    var prop = new byte[5];
                    s.Read(prop, 0, 5);
                    decoder.SetDecoderProperties(prop);
                    long outSize = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int v = s.ReadByte();
                        outSize |= ((long)(byte)v) << (8 * i);
                    }
                    var b = new byte[(int)outSize];
                    var z = new MemoryStream(b, true);
                    long compressedSize = s.Length - 13;
                    decoder.Code(s, z, compressedSize, outSize);
                    return b;
                */
                }

            }

            struct BitDecoder
            {
                public const int kNumBitModelTotalBits = 11;
                public const uint kBitModelTotal = (1 << kNumBitModelTotalBits);
                const int kNumMoveBits = 5;

                uint Prob;

                public void Init()
                {
                    Prob = kBitModelTotal >> 1;
                }

                public uint Decode(Decoder rangeDecoder)
                {
                    uint newBound = (rangeDecoder.Range >> kNumBitModelTotalBits) * Prob;
                    if (rangeDecoder.Code < newBound)
                    {
                        rangeDecoder.Range = newBound;
                        Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
                        if (rangeDecoder.Range < Decoder.kTopValue)
                        {
                            rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                            rangeDecoder.Range <<= 8;
                        }
                        return 0;
                    }
                    rangeDecoder.Range -= newBound;
                    rangeDecoder.Code -= newBound;
                    Prob -= (Prob) >> kNumMoveBits;
                    if (rangeDecoder.Range < Decoder.kTopValue)
                    {
                        rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                        rangeDecoder.Range <<= 8;
                    }
                    return 1;
                }
            }

            struct BitTreeDecoder
            {
                readonly BitDecoder[] Models;
                readonly int NumBitLevels;

                public BitTreeDecoder(int numBitLevels)
                {
                    NumBitLevels = numBitLevels;
                    Models = new BitDecoder[1 << numBitLevels];
                }

                public void Init()
                {
                    for (uint i = 1; i < (1 << NumBitLevels); i++)
                        Models[i].Init();
                }

                public uint Decode(Decoder rangeDecoder)
                {
                    uint m = 1;
                    for (int bitIndex = NumBitLevels; bitIndex > 0; bitIndex--)
                        m = (m << 1) + Models[m].Decode(rangeDecoder);
                    return m - ((uint)1 << NumBitLevels);
                }

                public uint ReverseDecode(Decoder rangeDecoder)
                {
                    uint m = 1;
                    uint symbol = 0;
                    for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
                    {
                        uint bit = Models[m].Decode(rangeDecoder);
                        m <<= 1;
                        m += bit;
                        symbol |= (bit << bitIndex);
                    }
                    return symbol;
                }

                public static uint ReverseDecode(BitDecoder[] Models, UInt32 startIndex,
                                                 Decoder rangeDecoder, int NumBitLevels)
                {
                    uint m = 1;
                    uint symbol = 0;
                    for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
                    {
                        uint bit = Models[startIndex + m].Decode(rangeDecoder);
                        m <<= 1;
                        m += bit;
                        symbol |= (bit << bitIndex);
                    }
                    return symbol;
                }
            }

            class Decoder
            {
                public const uint kTopValue = (1 << 24);
                public uint Code;
                public uint Range;
                public Stream Stream;

                public void Init(Stream stream)
                {
                    // Stream.Init(stream);
                    Stream = stream;

                    Code = 0;
                    Range = 0xFFFFFFFF;
                    for (int i = 0; i < 5; i++)
                        Code = (Code << 8) | (byte)Stream.ReadByte();
                }

                public void ReleaseStream()
                {
                    Stream = null;
                }

                public void Normalize()
                {
                    while (Range < kTopValue)
                    {
                        Code = (Code << 8) | (byte)Stream.ReadByte();
                        Range <<= 8;
                    }
                }

                public uint DecodeDirectBits(int numTotalBits)
                {
                    uint range = Range;
                    uint code = Code;
                    uint result = 0;
                    for (int i = numTotalBits; i > 0; i--)
                    {
                        range >>= 1;
                        /*
                        result <<= 1;
                        if (code >= range)
                        {
                            code -= range;
                            result |= 1;
                        }
                        */
                        uint t = (code - range) >> 31;
                        code -= range & (t - 1);
                        result = (result << 1) | (1 - t);

                        if (range < kTopValue)
                        {
                            code = (code << 8) | (byte)Stream.ReadByte();
                            range <<= 8;
                        }
                    }
                    Range = range;
                    Code = code;
                    return result;
                }
            }

            class LzmaDecoder
            {
                readonly BitDecoder[] m_IsMatchDecoders = new BitDecoder[kNumStates << kNumPosStatesBitsMax];
                readonly BitDecoder[] m_IsRep0LongDecoders = new BitDecoder[kNumStates << kNumPosStatesBitsMax];
                readonly BitDecoder[] m_IsRepDecoders = new BitDecoder[kNumStates];
                readonly BitDecoder[] m_IsRepG0Decoders = new BitDecoder[kNumStates];
                readonly BitDecoder[] m_IsRepG1Decoders = new BitDecoder[kNumStates];
                readonly BitDecoder[] m_IsRepG2Decoders = new BitDecoder[kNumStates];

                readonly LenDecoder m_LenDecoder = new LenDecoder();

                readonly LiteralDecoder m_LiteralDecoder = new LiteralDecoder();
                readonly OutWindow m_OutWindow = new OutWindow();
                readonly BitDecoder[] m_PosDecoders = new BitDecoder[kNumFullDistances - kEndPosModelIndex];
                readonly BitTreeDecoder[] m_PosSlotDecoder = new BitTreeDecoder[kNumLenToPosStates];
                readonly Decoder m_RangeDecoder = new Decoder();
                readonly LenDecoder m_RepLenDecoder = new LenDecoder();
                bool _solid = false;

                uint m_DictionarySize;
                uint m_DictionarySizeCheck;
                BitTreeDecoder m_PosAlignDecoder = new BitTreeDecoder(kNumAlignBits);

                uint m_PosStateMask;

                public LzmaDecoder()
                {
                    m_DictionarySize = 0xFFFFFFFF;
                    for (int i = 0; i < kNumLenToPosStates; i++)
                        m_PosSlotDecoder[i] = new BitTreeDecoder(kNumPosSlotBits);
                }

                void SetDictionarySize(uint dictionarySize)
                {
                    if (m_DictionarySize != dictionarySize)
                    {
                        m_DictionarySize = dictionarySize;
                        m_DictionarySizeCheck = Math.Max(m_DictionarySize, 1);
                        uint blockSize = Math.Max(m_DictionarySizeCheck, (1 << 12));
                        m_OutWindow.Create(blockSize);
                    }
                }

                void SetLiteralProperties(int lp, int lc)
                {
                    m_LiteralDecoder.Create(lp, lc);
                }

                void SetPosBitsProperties(int pb)
                {
                    uint numPosStates = (uint)1 << pb;
                    m_LenDecoder.Create(numPosStates);
                    m_RepLenDecoder.Create(numPosStates);
                    m_PosStateMask = numPosStates - 1;
                }

                void Init(Stream inStream, Stream outStream)
                {
                    m_RangeDecoder.Init(inStream);
                    m_OutWindow.Init(outStream, _solid);

                    uint i;
                    for (i = 0; i < kNumStates; i++)
                    {
                        for (uint j = 0; j <= m_PosStateMask; j++)
                        {
                            uint index = (i << kNumPosStatesBitsMax) + j;
                            m_IsMatchDecoders[index].Init();
                            m_IsRep0LongDecoders[index].Init();
                        }
                        m_IsRepDecoders[i].Init();
                        m_IsRepG0Decoders[i].Init();
                        m_IsRepG1Decoders[i].Init();
                        m_IsRepG2Decoders[i].Init();
                    }

                    m_LiteralDecoder.Init();
                    for (i = 0; i < kNumLenToPosStates; i++)
                        m_PosSlotDecoder[i].Init();
                    // m_PosSpecDecoder.Init();
                    for (i = 0; i < kNumFullDistances - kEndPosModelIndex; i++)
                        m_PosDecoders[i].Init();

                    m_LenDecoder.Init();
                    m_RepLenDecoder.Init();
                    m_PosAlignDecoder.Init();
                }

                public void Code(Stream inStream, Stream outStream,
                                 Int64 inSize, Int64 outSize)
                {
                    Init(inStream, outStream);

                    var state = new State();
                    state.Init();
                    uint rep0 = 0, rep1 = 0, rep2 = 0, rep3 = 0;

                    UInt64 nowPos64 = 0;
                    var outSize64 = (UInt64)outSize;
                    if (nowPos64 < outSize64)
                    {
                        m_IsMatchDecoders[state.Index << kNumPosStatesBitsMax].Decode(m_RangeDecoder);
                        state.UpdateChar();
                        byte b = m_LiteralDecoder.DecodeNormal(m_RangeDecoder, 0, 0);
                        m_OutWindow.PutByte(b);
                        nowPos64++;
                    }
                    while (nowPos64 < outSize64)
                    {
                        // UInt64 next = Math.Min(nowPos64 + (1 << 18), outSize64);
                        // while(nowPos64 < next)
                        {
                            uint posState = (uint)nowPos64 & m_PosStateMask;
                            if (m_IsMatchDecoders[(state.Index << kNumPosStatesBitsMax) + posState].Decode(m_RangeDecoder) == 0)
                            {
                                byte b;
                                byte prevByte = m_OutWindow.GetByte(0);
                                if (!state.IsCharState())
                                    b = m_LiteralDecoder.DecodeWithMatchByte(m_RangeDecoder,
                                                                             (uint)nowPos64, prevByte, m_OutWindow.GetByte(rep0));
                                else
                                    b = m_LiteralDecoder.DecodeNormal(m_RangeDecoder, (uint)nowPos64, prevByte);
                                m_OutWindow.PutByte(b);
                                state.UpdateChar();
                                nowPos64++;
                            }
                            else
                            {
                                uint len;
                                if (m_IsRepDecoders[state.Index].Decode(m_RangeDecoder) == 1)
                                {
                                    if (m_IsRepG0Decoders[state.Index].Decode(m_RangeDecoder) == 0)
                                    {
                                        if (m_IsRep0LongDecoders[(state.Index << kNumPosStatesBitsMax) + posState].Decode(m_RangeDecoder) == 0)
                                        {
                                            state.UpdateShortRep();
                                            m_OutWindow.PutByte(m_OutWindow.GetByte(rep0));
                                            nowPos64++;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        UInt32 distance;
                                        if (m_IsRepG1Decoders[state.Index].Decode(m_RangeDecoder) == 0)
                                        {
                                            distance = rep1;
                                        }
                                        else
                                        {
                                            if (m_IsRepG2Decoders[state.Index].Decode(m_RangeDecoder) == 0)
                                                distance = rep2;
                                            else
                                            {
                                                distance = rep3;
                                                rep3 = rep2;
                                            }
                                            rep2 = rep1;
                                        }
                                        rep1 = rep0;
                                        rep0 = distance;
                                    }
                                    len = m_RepLenDecoder.Decode(m_RangeDecoder, posState) + kMatchMinLen;
                                    state.UpdateRep();
                                }
                                else
                                {
                                    rep3 = rep2;
                                    rep2 = rep1;
                                    rep1 = rep0;
                                    len = kMatchMinLen + m_LenDecoder.Decode(m_RangeDecoder, posState);
                                    state.UpdateMatch();
                                    uint posSlot = m_PosSlotDecoder[GetLenToPosState(len)].Decode(m_RangeDecoder);
                                    if (posSlot >= kStartPosModelIndex)
                                    {
                                        var numDirectBits = (int)((posSlot >> 1) - 1);
                                        rep0 = ((2 | (posSlot & 1)) << numDirectBits);
                                        if (posSlot < kEndPosModelIndex)
                                            rep0 += BitTreeDecoder.ReverseDecode(m_PosDecoders,
                                                                                 rep0 - posSlot - 1, m_RangeDecoder, numDirectBits);
                                        else
                                        {
                                            rep0 += (m_RangeDecoder.DecodeDirectBits(
                                                numDirectBits - kNumAlignBits) << kNumAlignBits);
                                            rep0 += m_PosAlignDecoder.ReverseDecode(m_RangeDecoder);
                                        }
                                    }
                                    else
                                        rep0 = posSlot;
                                }
                                if (rep0 >= nowPos64 || rep0 >= m_DictionarySizeCheck)
                                {
                                    if (rep0 == 0xFFFFFFFF)
                                        break;
                                }
                                m_OutWindow.CopyBlock(rep0, len);
                                nowPos64 += len;
                            }
                        }
                    }
                    m_OutWindow.Flush();
                    m_OutWindow.ReleaseStream();
                    m_RangeDecoder.ReleaseStream();
                }

                public void SetDecoderProperties(byte[] properties)
                {
                    int lc = properties[0] % 9;
                    int remainder = properties[0] / 9;
                    int lp = remainder % 5;
                    int pb = remainder / 5;
                    UInt32 dictionarySize = 0;
                    for (int i = 0; i < 4; i++)
                        dictionarySize += ((UInt32)(properties[1 + i])) << (i * 8);
                    SetDictionarySize(dictionarySize);
                    SetLiteralProperties(lp, lc);
                    SetPosBitsProperties(pb);
                }

                static uint GetLenToPosState(uint len)
                {
                    len -= kMatchMinLen;
                    if (len < kNumLenToPosStates)
                        return len;
                    return unchecked((kNumLenToPosStates - 1));
                }

                class LenDecoder
                {
                    readonly BitTreeDecoder[] m_LowCoder = new BitTreeDecoder[kNumPosStatesMax];
                    readonly BitTreeDecoder[] m_MidCoder = new BitTreeDecoder[kNumPosStatesMax];
                    BitDecoder m_Choice = new BitDecoder();
                    BitDecoder m_Choice2 = new BitDecoder();
                    BitTreeDecoder m_HighCoder = new BitTreeDecoder(kNumHighLenBits);
                    uint m_NumPosStates;

                    public void Create(uint numPosStates)
                    {
                        for (uint posState = m_NumPosStates; posState < numPosStates; posState++)
                        {
                            m_LowCoder[posState] = new BitTreeDecoder(kNumLowLenBits);
                            m_MidCoder[posState] = new BitTreeDecoder(kNumMidLenBits);
                        }
                        m_NumPosStates = numPosStates;
                    }

                    public void Init()
                    {
                        m_Choice.Init();
                        for (uint posState = 0; posState < m_NumPosStates; posState++)
                        {
                            m_LowCoder[posState].Init();
                            m_MidCoder[posState].Init();
                        }
                        m_Choice2.Init();
                        m_HighCoder.Init();
                    }

                    public uint Decode(Decoder rangeDecoder, uint posState)
                    {
                        if (m_Choice.Decode(rangeDecoder) == 0)
                            return m_LowCoder[posState].Decode(rangeDecoder);
                        uint symbol = kNumLowLenSymbols;
                        if (m_Choice2.Decode(rangeDecoder) == 0)
                            symbol += m_MidCoder[posState].Decode(rangeDecoder);
                        else
                        {
                            symbol += kNumMidLenSymbols;
                            symbol += m_HighCoder.Decode(rangeDecoder);
                        }
                        return symbol;
                    }
                }

                class LiteralDecoder
                {
                    Decoder2[] m_Coders;
                    int m_NumPosBits;
                    int m_NumPrevBits;
                    uint m_PosMask;

                    public void Create(int numPosBits, int numPrevBits)
                    {
                        if (m_Coders != null && m_NumPrevBits == numPrevBits &&
                            m_NumPosBits == numPosBits)
                            return;
                        m_NumPosBits = numPosBits;
                        m_PosMask = ((uint)1 << numPosBits) - 1;
                        m_NumPrevBits = numPrevBits;
                        uint numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
                        m_Coders = new Decoder2[numStates];
                        for (uint i = 0; i < numStates; i++)
                            m_Coders[i].Create();
                    }

                    public void Init()
                    {
                        uint numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
                        for (uint i = 0; i < numStates; i++)
                            m_Coders[i].Init();
                    }

                    uint GetState(uint pos, byte prevByte)
                    {
                        return ((pos & m_PosMask) << m_NumPrevBits) + (uint)(prevByte >> (8 - m_NumPrevBits));
                    }

                    public byte DecodeNormal(Decoder rangeDecoder, uint pos, byte prevByte)
                    {
                        return m_Coders[GetState(pos, prevByte)].DecodeNormal(rangeDecoder);
                    }

                    public byte DecodeWithMatchByte(Decoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
                    {
                        return m_Coders[GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte);
                    }

                    struct Decoder2
                    {
                        BitDecoder[] m_Decoders;

                        public void Create()
                        {
                            m_Decoders = new BitDecoder[0x300];
                        }

                        public void Init()
                        {
                            for (int i = 0; i < 0x300; i++) m_Decoders[i].Init();
                        }

                        public byte DecodeNormal(Decoder rangeDecoder)
                        {
                            uint symbol = 1;
                            do
                                symbol = (symbol << 1) | m_Decoders[symbol].Decode(rangeDecoder); while (symbol < 0x100);
                            return (byte)symbol;
                        }

                        public byte DecodeWithMatchByte(Decoder rangeDecoder, byte matchByte)
                        {
                            uint symbol = 1;
                            do
                            {
                                uint matchBit = (uint)(matchByte >> 7) & 1;
                                matchByte <<= 1;
                                uint bit = m_Decoders[((1 + matchBit) << 8) + symbol].Decode(rangeDecoder);
                                symbol = (symbol << 1) | bit;
                                if (matchBit != bit)
                                {
                                    while (symbol < 0x100)
                                        symbol = (symbol << 1) | m_Decoders[symbol].Decode(rangeDecoder);
                                    break;
                                }
                            } while (symbol < 0x100);
                            return (byte)symbol;
                        }
                    }
                };
            }

            class OutWindow
            {
                byte[] _buffer;
                uint _pos;
                Stream _stream;
                uint _streamPos;
                uint _windowSize;

                public void Create(uint windowSize)
                {
                    if (_windowSize != windowSize)
                    {
                        _buffer = new byte[windowSize];
                    }
                    _windowSize = windowSize;
                    _pos = 0;
                    _streamPos = 0;
                }

                public void Init(Stream stream, bool solid)
                {
                    ReleaseStream();
                    _stream = stream;
                    if (!solid)
                    {
                        _streamPos = 0;
                        _pos = 0;
                    }
                }

                public void ReleaseStream()
                {
                    Flush();
                    _stream = null;
                    Buffer.BlockCopy(new byte[_buffer.Length], 0, _buffer, 0, _buffer.Length);
                }

                public void Flush()
                {
                    uint size = _pos - _streamPos;
                    if (size == 0)
                        return;
                    _stream.Write(_buffer, (int)_streamPos, (int)size);
                    if (_pos >= _windowSize)
                        _pos = 0;
                    _streamPos = _pos;
                }

                public void CopyBlock(uint distance, uint len)
                {
                    uint pos = _pos - distance - 1;
                    if (pos >= _windowSize)
                        pos += _windowSize;
                    for (; len > 0; len--)
                    {
                        if (pos >= _windowSize)
                            pos = 0;
                        _buffer[_pos++] = _buffer[pos++];
                        if (_pos >= _windowSize)
                            Flush();
                    }
                }

                public void PutByte(byte b)
                {
                    _buffer[_pos++] = b;
                    if (_pos >= _windowSize)
                        Flush();
                }

                public byte GetByte(uint distance)
                {
                    uint pos = _pos - distance - 1;
                    if (pos >= _windowSize)
                        pos += _windowSize;
                    return _buffer[pos];
                }
            }

            struct State
            {
                public uint Index;

                public void Init()
                {
                    Index = 0;
                }

                public void UpdateChar()
                {
                    if (Index < 4) Index = 0;
                    else if (Index < 10) Index -= 3;
                    else Index -= 6;
                }

                public void UpdateMatch()
                {
                    Index = (uint)(Index < 7 ? 7 : 10);
                }

                public void UpdateRep()
                {
                    Index = (uint)(Index < 7 ? 8 : 11);
                }

                public void UpdateShortRep()
                {
                    Index = (uint)(Index < 7 ? 9 : 11);
                }

                public bool IsCharState()
                {
                    return Index < 7;
                }
            }
        }

        public class HasoReader
        {

            public HasoReader()
            {

            }


            public int Read32()
            {
                return binreader.ReadInt32();
            }


        }
    
    
    
    }
}
