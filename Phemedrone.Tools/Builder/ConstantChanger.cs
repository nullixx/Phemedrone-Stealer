using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Phemedrone.Tools.Interface;

namespace Phemedrone.Tools.Builder
{
    public class ConstantChanger
    {
        public static void Run(ModuleDefMD module, ProgressWindow progress, Dictionary<string, object> values, Dictionary<string, object> defaultValues)
        {
            var replacedVariables = 0;
            foreach(var type in module.Types.Where(t => !t.IsGlobalModuleType))
            {
                if (type.Name != "Config") continue;

                var staticCtor = type.Methods.FirstOrDefault(x => x.IsStaticConstructor);
                if (staticCtor == null) return;
                
                var instructions = staticCtor.Body.Instructions;
                var patchedInstructions = new List<Instruction>();
                
                var startOffset = 0;

                var offsets = new List<int>();
                for (var i = 0; i < instructions.Count; i++)
                {
                    if (instructions[i].OpCode != OpCodes.Stsfld) continue;
                    
                    var operand = (FieldDef)instructions[i].Operand;

                    if (operand.FieldType.ReflectionName != "ISender")
                    {
                        offsets.Add(i+1);
                        continue;
                    }
                    
                    var senderConstructors = FindSenderConstructors(module);
                    
                    foreach (var keyValue in values.Skip(1))
                    {
                        patchedInstructions.Add(Instruction.Create(OpCodes.Ldstr, (string)keyValue.Value));
                        progress.Update("Updating variables", (decimal)++replacedVariables / (values.Count + defaultValues.Count));
                        startOffset++;
                    }
                    
                    patchedInstructions.Add(Instruction.Create(OpCodes.Newobj, 
                        senderConstructors.FirstOrDefault(c => c.DeclaringType.Name == (string)values["type"])));
                    patchedInstructions.Add(instructions[i]);

                    offsets.Add(startOffset+2);
                }

                for (var i = 0; i < offsets.Count - 1; i++)
                {
                    var endOffset = offsets[i+1]-1;
                    var value = defaultValues[((FieldDef)instructions[endOffset].Operand).Name];

                    switch (value)
                    {
                        case string s:
                            patchedInstructions.Add(Instruction.Create(OpCodes.Ldstr, s));
                            patchedInstructions.Add(instructions[endOffset]);
                            progress.Update("Updating variables", (decimal)++replacedVariables / (values.Count + defaultValues.Count));
                            break;
                        case int j:
                            patchedInstructions.Add(Instruction.CreateLdcI4(j));
                            patchedInstructions.Add(instructions[endOffset]);
                            progress.Update("Updating variables", (decimal)++replacedVariables / (values.Count + defaultValues.Count));
                            break;
                        case bool b:
                            patchedInstructions.Add(Instruction.CreateLdcI4(b ? 1 : 0));
                            patchedInstructions.Add(instructions[endOffset]);
                            progress.Update("Updating variables", (decimal)++replacedVariables / (values.Count + defaultValues.Count));
                            break;
                        case IList l:
                            var ctor = module.Assembly.ManifestModule.Import(
                                value.GetType().GetConstructor(Type.EmptyTypes));
                            var listAdd = module.Assembly.ManifestModule.Import(
                                value.GetType().GetMethod("Add"));
                            
                            patchedInstructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
                            patchedInstructions.Add(Instruction.Create(OpCodes.Dup));

                            foreach (var obj in l)
                            {
                                Instruction instr;
                                switch (obj)
                                {
                                    case string s:
                                        instr = Instruction.Create(OpCodes.Ldstr, s);
                                        break;
                                    default:
                                        instr = Instruction.Create(OpCodes.Ldnull);
                                        // if you wanna extend your config and there will be a list of any other type than string
                                        // add case statement with type of this object
                                        break;
                                }
                                
                                patchedInstructions.Add(instr);
                                patchedInstructions.Add(Instruction.Create(OpCodes.Callvirt, listAdd));
                                patchedInstructions.Add(Instruction.Create(OpCodes.Nop));
                                patchedInstructions.Add(Instruction.Create(OpCodes.Dup));
                            }
                            patchedInstructions.RemoveAt(patchedInstructions.Count - 1);
                            patchedInstructions.Add(instructions[endOffset]);
                            progress.Update("Updating variables", (decimal)++replacedVariables / (values.Count + defaultValues.Count));
                            break;
                    }
                }
                patchedInstructions.Add(Instruction.Create(OpCodes.Ret));
                
                instructions.Clear();
                foreach (var instr in patchedInstructions)
                {
                    instructions.Add(instr);
                }
            }
        }
        
        private static List<MethodDef> FindSenderConstructors(ModuleDefMD module)
        {
            var list = new List<MethodDef>();
            
            foreach (var type in module.Types.Where(t => t.Namespace.StartsWith("Phemedrone.Senders") &&
                                                         (t.Attributes & TypeAttributes.Abstract) == 0))
            {
                var ctor = type.Methods.FirstOrDefault(m => m.IsConstructor);
                list.Add(ctor);
            }

            return list;
        }
    }
}