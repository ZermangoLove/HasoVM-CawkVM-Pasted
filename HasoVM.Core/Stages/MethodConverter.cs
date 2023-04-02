using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HasoVM.Core.Stages
{
    internal class MethodConverter
    {
        private BinaryWriter _binWriter;
        public byte[] ConvertedBytes;
        private readonly MethodDef _methods;
        public bool Successful;
      
        public MethodConverter(MethodDef methodDef)
        {
            methodDef.Body.OptimizeBranches();
            methodDef.Body.OptimizeMacros();
            methodDef.Body.SimplifyBranches();
            methodDef.Body.SimplifyMacros(methodDef.Parameters);
            _methods = methodDef;
            ConvertedBytes = null;
            Successful = false;
        }

        public void Execute()
        {
            _binWriter = new BinaryWriter(new MemoryStream());
            _binWriter.Write(_methods.Body.Instructions
             .Count);

            num = new Random().Next(1, int.MaxValue);
              
            foreach (var ins in _methods.Body.Instructions)
            {
                OpcodeWriter(ins.OpCode);
                var opType = ins.OpCode.OperandType;
              
                switch (opType)
                {
                   
                    case OperandType.InlineNone:
                        InlineNone(ins);
                        continue;
                  
                    case OperandType.InlineMethod:
                        InlineMethod(ins);
                        continue;
                   
                    case OperandType.InlineString:
                        InlineString(ins);
                        continue;
                   
                    case OperandType.InlineI:
                        InlineI(ins);
                        continue;
                   
                    case OperandType.ShortInlineVar:
                        ShortInlineVar(ins);
                        continue;
                   
                    case OperandType.InlineField:
                        InlineField(ins);
                        continue;
                   
                    case OperandType.InlineType:
                        InlineType(ins);
                        continue;
                   
                    case OperandType.ShortInlineBrTarget:
                        ShortInlineBrTarget(_methods.Body.Instructions, ins);
                        continue;
                   
                    case OperandType.ShortInlineI:                  
                        ShortInlineI(ins);
                        continue;
                   
                    case OperandType.InlineSwitch:
                        InlineSwitch(_methods.Body.Instructions, ins);
                        continue;
                   
                    case OperandType.InlineBrTarget:
                        InlineBrTarget(_methods.Body.Instructions, ins);
                        continue;
                   
                    case OperandType.InlineTok:
                        InlineTok(ins);
                        continue;
                  
                    case OperandType.InlineVar:
                        InlineVar(ins);
                        continue;
                  
                    case OperandType.ShortInlineR:
                        ShortInlineR(ins);
                        break;
                   
                    case OperandType.InlineR:
                        InlineR(ins);
                        break;
                   
                    case OperandType.InlineI8:
                        InlineI8(ins);
                        break;
                   
                    default:
                        throw new Exception(string.Format("OperandType {0} Not Supported", opType));
                }
            }
            Successful = true;
            var buffer = new byte[_binWriter.BaseStream.Length];
            _binWriter.BaseStream.Position = 0;
            _binWriter.BaseStream.Read(buffer, 0, buffer.Length);
            ConvertedBytes = buffer;
        }

        private void OpcodeWriter(OpCode opcode)
        {      
            _binWriter.Write((short)opcode.Value);
        }

        int num;
        private void InlineNone(Instruction instruction)
        {
            _binWriter.Write((byte)num);
        }

        private void InlineMethod(Instruction instruction)
        {
            _binWriter.Write((byte)num);
     
            if (instruction.Operand is MethodSpec)
            {
                var methodSpec = instruction.Operand as MethodSpec;
                if (methodSpec == null)
                    throw new Exception("Check the instruction. This should not happen");
                _binWriter.Write(methodSpec.MDToken.Raw);
            }
         
            if(instruction.Operand is IMethodDefOrRef)
            {
                var methodDeforRef = instruction.Operand as IMethodDefOrRef;
                if (methodDeforRef == null)
                    throw new Exception("Check the instruction. This should not happen");
                _binWriter.Write(methodDeforRef.MDToken.Raw);
            }
        }
      
        private void InlineString(Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var operand = GetXorString(instruction.Operand.ToString());
            _binWriter.Write(operand);
        }
        private string GetXorString(string s)
        {
            var original = new StringBuilder(s);
            var encrypted = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
                encrypted.Append((char)(original[i] ^ 29));
            return encrypted.ToString();
        }
      
        private void InlineI(Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var operand = instruction.GetLdcI4Value();
            _binWriter.Write(operand);
        }

        private void ShortInlineVar(Instruction instruction)
        {
            _binWriter.Write((byte)num);

            if (instruction.Operand is Local)
            {
                var loc = instruction.Operand as Local;
                _binWriter.Write(loc.Index);
                _binWriter.Write((byte)num);
            }
         
            else if (instruction.Operand is Parameter)
            {
                var par = instruction.Operand as Parameter;
                _binWriter.Write(par.Index);
                _binWriter.Write((byte)num);
            }
        }

        private void InlineField(Instruction instruction)
        {
            _binWriter.Write((byte)num);
          
            if (instruction.Operand is MemberRef)
            {
                var memberRef = instruction.Operand as MemberRef;
                if (memberRef == null)
                    throw new Exception("Check the instruction. This should not happen");
                _binWriter.Write(memberRef.MDToken.Raw);
            }
           
            else
            {
                var memberRef = instruction.Operand as FieldDef;
                if (memberRef == null)
                    throw new Exception("Check the instruction. This should not happen");
                _binWriter.Write(memberRef.MDToken.Raw);
            }
        }
 
        private void InlineType(Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var typeDeforRef = instruction.Operand as ITypeDefOrRef;
            if (typeDeforRef == null)
                throw new Exception("Check the instruction. This should not happen");
            _binWriter.Write(typeDeforRef.MDToken.Raw);
        }

        private void ShortInlineBrTarget(IList<Instruction> allInstructions, Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var index = allInstructions
             .IndexOf((Instruction)instruction.Operand);
            _binWriter.Write(index);
        }

        private void ShortInlineI(Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var operand = instruction.GetLdcI4Value();
            _binWriter.Write((byte)operand);
        }

        private void InlineSwitch(IList<Instruction> allInstructions, Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var allLocations = instruction.Operand as Instruction[];
            _binWriter.Write(allLocations.Count());
            foreach (var switchLocation in allLocations)
            {
                var index = allInstructions
                .IndexOf(switchLocation);
                _binWriter.Write(index);
            }
        }

        private void InlineBrTarget(IList<Instruction> allInstructions, Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var index = allInstructions
                .IndexOf((Instruction)instruction.Operand);
            _binWriter.Write(index);
        }

        private void InlineTok(Instruction instruction)
        {
            _binWriter.Write((byte)num);
          
            if (instruction.Operand is FieldDef)
            {
                var fieldDef = instruction.Operand as FieldDef;
                _binWriter.Write(fieldDef.MDToken.Raw);
                _binWriter.Write((byte)0);
            }
            
            else if (instruction.Operand is ITypeDefOrRef)
            {
                var typeDeforRef = instruction.Operand as ITypeDefOrRef;
                _binWriter.Write(typeDeforRef.MDToken.Raw);
                _binWriter.Write((byte)1);
            }
            
            else if (instruction.Operand is IMethodDefOrRef)
            {
                var methoDefOrRef = instruction.Operand as IMethodDefOrRef;
                _binWriter.Write(methoDefOrRef.MDToken.Raw);
                _binWriter.Write((byte)2);
            }
            
            else
            {
                throw new Exception("Check the instruction. This should not happen");
            }
        }
        private void InlineVar(Instruction instruction)
        {
            _binWriter.Write((byte)num);
          
            if (instruction.Operand is Local)
            {
                var loc = instruction.Operand as Local;
                _binWriter.Write(loc.Index);
                _binWriter.Write((byte)num);
            }
            
            else if (instruction.Operand is Parameter)
            {
                var par = instruction.Operand as Parameter;
                _binWriter.Write(par.Index);
                _binWriter.Write((byte)num);
            }
            
            else
            {
                _binWriter.Write(num);
                _binWriter.Write((byte)num);
            }
        }
        private void ShortInlineR(Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var operand = instruction.Operand;
            _binWriter.Write((float)operand);
        }
        private void InlineR(Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var operand = instruction.Operand;
            _binWriter.Write((double)operand);
        }
        private void InlineI8(Instruction instruction)
        {
            _binWriter.Write((byte)num);
            var operand = instruction.Operand;
            _binWriter.Write((long)operand);
        }
    }
}