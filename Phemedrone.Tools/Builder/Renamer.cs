using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Phemedrone.Tools.Builder
{
    public static class Renamer
    {
        public static void Run(ModuleDef module)
        {
            var namespaces = module.Types.Select(x => x.Namespace)
                .Distinct()
                .Select(x => new { Namespace = x, Name = RandomValues.RandomString(3) })
                .ToList();
            
            foreach (var type in module.Types.Where(x => x.Namespace.Length != 0))
            {
                foreach (var method in type.Methods.Where(x => !x.IsConstructor))
                {
                    if (!method.HasBody || method.IsVirtual || method.IsSpecialName)
                    {
                        continue;
                    }

                    method.Name = RandomValues.RandomString(6);
                    
                    foreach (var param in method.Parameters)
                    {
                        param.Name = RandomValues.RandomString(8);
                    }

                    foreach (var instruction in method.Body.Instructions)
                    {
                        var name = RandomValues.RandomString(8);
                        if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Stfld)
                        {
                            switch (instruction.Operand)
                            {
                                case MemberRef m:
                                    m.Name = name;
                                    break;
                                case FieldDef f:
                                    f.Name = name;
                                    break;
                            }
                        }
                    }
                }
                
                foreach (var property in type.Properties)
                {
                    var name = RandomValues.RandomString(6);
                    property.Name = name;
                }

                foreach (var field in type.Fields.Where(x => (x.Attributes & FieldAttributes.RTSpecialName) == 0))
                {
                    if (field.HasCustomAttributes) continue;
                    field.Name = RandomValues.RandomString(8);
                }
                
                foreach (var nestedType in type.NestedTypes)
                {
                    if (nestedType.IsDelegate || nestedType.IsEnum || nestedType.IsValueType)
                    {
                        nestedType.Name = RandomValues.RandomString(7);
                    }
                }

                type.Name = RandomValues.RandomString(5);
                type.Namespace = namespaces.First(x => x.Namespace == type.Namespace).Name;
            }
            
            module.Assembly.Name = RandomValues.RandomString(5);
            module.Assembly.Version = new Version(1, 4, 4, 8);
            module.Name = "Hello";
        }
    }
}