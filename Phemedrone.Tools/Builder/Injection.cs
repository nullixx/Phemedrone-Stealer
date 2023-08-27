using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Phemedrone.Tools.Builder
{
    public static class Injection
    {
        private static TypeDefUser Clone(TypeDef origin)
        {
            var ret = new TypeDefUser(origin.Namespace, origin.Name)
            {
                Attributes = origin.Attributes
            };

            if (origin.ClassLayout != null)
                ret.ClassLayout = new ClassLayoutUser(origin.ClassLayout.PackingSize, origin.ClassSize);

            foreach (var genericParam in origin.GenericParameters)
                ret.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags, "-"));

            return ret;
        }
        
        private static MethodDefUser Clone(MethodDef origin)
        {
            var ret = new MethodDefUser(origin.Name, null, origin.ImplAttributes, origin.Attributes);

            foreach (var genericParam in origin.GenericParameters)
                ret.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags, "-"));

            return ret;
        }
        
        private static FieldDefUser Clone(FieldDef origin)
        {
            var ret = new FieldDefUser(origin.Name, null, origin.Attributes);
            return ret;
        }
        
        private static TypeDef PopulateContext(TypeDef typeDef, InjectContext ctx)
        {
            var ret = ctx.Map(typeDef)?.ResolveTypeDef();
            if (ret is null)
            {
                ret = Clone(typeDef);
                ctx.DefMap[typeDef] = ret;
            }

            foreach (var nestedType in typeDef.NestedTypes)
                ret.NestedTypes.Add(PopulateContext(nestedType, ctx));

            foreach (var method in typeDef.Methods)
                ret.Methods.Add((MethodDef)(ctx.DefMap[method] = Clone(method)));

            foreach (var field in typeDef.Fields)
                ret.Fields.Add((FieldDef)(ctx.DefMap[field] = Clone(field)));

            return ret;
        }
        
        private static void CopyTypeDef(TypeDef typeDef, InjectContext ctx)
        {
            var newTypeDef = ctx.Map(typeDef)?.ResolveTypeDefThrow();
            if (newTypeDef == null) return;
            
            newTypeDef.BaseType = ctx.Importer.Import(typeDef.BaseType);

            foreach (var iFace in typeDef.Interfaces)
                newTypeDef.Interfaces.Add(new InterfaceImplUser(ctx.Importer.Import(iFace.Interface)));
        }
        
        private static void CopyMethodDef(MethodDef methodDef, InjectContext ctx)
        {
            var newMethodDef = ctx.Map(methodDef)?.ResolveMethodDefThrow();
            if (newMethodDef == null) return;
            
            newMethodDef.Signature = ctx.Importer.Import(methodDef.Signature);
            newMethodDef.Parameters.UpdateParameterTypes();

            if (methodDef.ImplMap != null)
                newMethodDef.ImplMap = new ImplMapUser(new ModuleRefUser(ctx.TargetModule, methodDef.ImplMap.Module.Name), methodDef.ImplMap.Name, methodDef.ImplMap.Attributes);

            foreach (var ca in methodDef.CustomAttributes)
                newMethodDef.CustomAttributes.Add(new CustomAttribute((ICustomAttributeType)ctx.Importer.Import(ca.Constructor)));

            if (!methodDef.HasBody) return;
            
            newMethodDef.Body = new CilBody(methodDef.Body.InitLocals, new List<Instruction>(), new List<ExceptionHandler>(), new List<Local>())
                {
                    MaxStack = methodDef.Body.MaxStack
                };

            var bodyMap = new Dictionary<object, object>();

            foreach (var local in methodDef.Body.Variables)
            {
                var newLocal = new Local(ctx.Importer.Import(local.Type));
                newMethodDef.Body.Variables.Add(newLocal);
                newLocal.Name = local.Name;

                bodyMap[local] = newLocal;
            }

            foreach (var instr in methodDef.Body.Instructions)
            {
                var newInstr = new Instruction(instr.OpCode, instr.Operand)
                {
                    SequencePoint = instr.SequencePoint
                };

                switch (newInstr.Operand)
                {
                    case IType type:
                        newInstr.Operand = ctx.Importer.Import(type);
                        break;
                    case IMethod method:
                        newInstr.Operand = ctx.Importer.Import(method);
                        break;
                    case IField field:
                        newInstr.Operand = ctx.Importer.Import(field);
                        break;
                }

                newMethodDef.Body.Instructions.Add(newInstr);
                bodyMap[instr] = newInstr;
            }

            foreach (var instr in newMethodDef.Body.Instructions)
            {
                if (instr.Operand != null && bodyMap.ContainsKey(instr.Operand))
                    instr.Operand = bodyMap[instr.Operand];
                else if (instr.Operand is Instruction[] instructions)
                    instr.Operand = instructions.Select(target => (Instruction)bodyMap[target]).ToArray();
            }

            foreach (var eh in methodDef.Body.ExceptionHandlers)
                newMethodDef.Body.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType)
                {
                    CatchType = eh.CatchType == null ? null : ctx.Importer.Import(eh.CatchType),
                    TryStart = (Instruction)bodyMap[eh.TryStart],
                    TryEnd = (Instruction)bodyMap[eh.TryEnd],
                    HandlerStart = (Instruction)bodyMap[eh.HandlerStart],
                    HandlerEnd = (Instruction)bodyMap[eh.HandlerEnd],
                    FilterStart = eh.FilterStart == null ? null : (Instruction)bodyMap[eh.FilterStart]
                });

            newMethodDef.Body.SimplifyMacros(newMethodDef.Parameters);
        }
        
        private static void CopyFieldDef(FieldDef fieldDef, InjectContext ctx)
        {
            var newFieldDef = ctx.Map(fieldDef).ResolveFieldDefThrow();

            newFieldDef.Signature = ctx.Importer.Import(fieldDef.Signature);
        }
        
        private static void Copy(TypeDef typeDef, InjectContext ctx, bool copySelf)
        {
            if (copySelf)
                CopyTypeDef(typeDef, ctx);

            foreach (var nestedType in typeDef.NestedTypes)
                Copy(nestedType, ctx, true);

            foreach (var method in typeDef.Methods)
                CopyMethodDef(method, ctx);

            foreach (var field in typeDef.Fields)
                CopyFieldDef(field, ctx);
        }
        
        public static TypeDef Inject(TypeDef typeDef, ModuleDef target)
        {
            var ctx = new InjectContext(target);
            var result = PopulateContext(typeDef, ctx);
            Copy(typeDef, ctx, true);
            return result;
        }
        
        public static MethodDef Inject(MethodDef methodDef, ModuleDef target)
        {
            var ctx = new InjectContext(target);
            MethodDef result;
            ctx.DefMap[methodDef] = result = Clone(methodDef);
            CopyMethodDef(methodDef, ctx);
            return result;
        }
        
        public static IEnumerable<IDnlibDef> Inject(TypeDef typeDef, TypeDef newType, ModuleDef target)
        {
            var ctx = new InjectContext(target)
            {
                DefMap =
                {
                    [typeDef] = newType
                }
            };
            PopulateContext(typeDef, ctx);
            Copy(typeDef, ctx, false);
            return ctx.DefMap.Values.Except(new[] { newType }).OfType<IDnlibDef>();
        }
        
        private class InjectContext : ImportMapper
        {
            public readonly Dictionary<IMemberRef, IMemberRef> DefMap = new Dictionary<IMemberRef, IMemberRef>();

            public readonly ModuleDef TargetModule;
            
            public InjectContext(ModuleDef target)
            {
                TargetModule = target;
                Importer = new Importer(target, ImporterOptions.TryToUseTypeDefs, new GenericParamContext(), this);
            }
            
            public Importer Importer { get; }
            
            public override ITypeDefOrRef Map(ITypeDefOrRef source)
            {
                if (DefMap.TryGetValue(source, out var mappedRef))
                    return mappedRef as ITypeDefOrRef;

                if (!(source is TypeRef sourceRef)) return null;
                
                var targetAssemblyRef = TargetModule.GetAssemblyRef(sourceRef.DefinitionAssembly.Name);
                if (targetAssemblyRef is null || string.Equals(targetAssemblyRef.FullName,
                        source.DefinitionAssembly.FullName, StringComparison.Ordinal)) return null;
                
                var fixedTypeRef = new TypeRefUser(sourceRef.Module, sourceRef.Namespace, sourceRef.Name, targetAssemblyRef);
                return Importer.Import(fixedTypeRef);
            }
            
            public override IMethod Map(MethodDef source)
            {
                if (DefMap.TryGetValue(source, out var mappedRef))
                    return mappedRef as IMethod;
                return null;
            }
            
            public override IField Map(FieldDef source)
            {
                if (DefMap.TryGetValue(source, out var mappedRef))
                    return mappedRef as IField;
                return null;
            }

            public override MemberRef Map(MemberRef source)
            {
                if (DefMap.TryGetValue(source, out var mappedRef))
                    return mappedRef as MemberRef;
                return null;
            }
        }
    }
}