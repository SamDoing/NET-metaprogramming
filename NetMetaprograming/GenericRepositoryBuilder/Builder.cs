using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NetMetaprograming.Data;
using Microsoft.EntityFrameworkCore.Internal;
using NetMetaprograming.GenericRepositoryBuilder.Utils;
using System.Collections;
using NetMetaprograming.Data.Models;
using System.Collections.Generic;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public partial class Builder<T>
    {
        private readonly TypeBuilder typeBuilder;
        private readonly Type genericType;
        private readonly List<MethodInfo> interfaceMethods = new();
        private static readonly Dictionary<string, Action<ILGenerator>> methodsIL = new();
        private FieldBuilder? fbDbContext;


        public Builder()
        {
            genericType = ValidateInterface();
            InitializeMethodsIL();
            typeBuilder = CreateType();
            AddAndCheckMethodsImplementation();
        }

        private TypeBuilder CreateType()
        {
            AssemblyName aName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);

            // The module name is usually the same as the assembly name.
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);

            return mb.DefineType($"{typeof(T).Name}DynamicType", TypeAttributes.Public);
        }

        private static Type ValidateInterface()
        {
            var typeOfT = typeof(T);

            var genericInterface = typeOfT.GetInterface(typeof(IGenericRepository<>).Name);
            if (!typeOfT.IsInterface || genericInterface == null)
            {
                throw new Exception($"Type needs to be interface and generic");
            }
            return genericInterface.GenericTypeArguments.Single();
        }

        private void AddAndCheckMethodsImplementation()
        {
            var typeOfT = typeof(T);
            
            interfaceMethods.AddRange(typeOfT.GetInterfaces().SelectMany(i => i.GetMethods()));
            interfaceMethods.AddRange(typeOfT.GetMethods());

            var methNotImplemented = interfaceMethods.FirstOrDefault(m => !methodsIL.ContainsKey(m.Name));
            if (methNotImplemented != null)
                throw new Exception($"{methNotImplemented.Name} not implemented");
        }

        public T Build(DbContext appDbContext)
        {
            typeBuilder.AddInterfaceImplementation(typeof(T));
            fbDbContext = typeBuilder.DefineField("DbContext", typeof(DbContext), FieldAttributes.Private);

            GenerateConstructor(fbDbContext);
            GenerateMethods(appDbContext);

            var tp = typeBuilder.CreateType() ?? throw new Exception();
            return (T)(Activator.CreateInstance(tp, appDbContext) ?? throw new Exception());
        }

        private void GenerateMethods(DbContext dbContext)
        {
            foreach (var methInterface in interfaceMethods)
            {
                GenerateMethod(methInterface, dbContext);
            }
        }

        private void GenerateMethod(MethodInfo methInterface, DbContext dbContext)
        {
            var paramTypes = methInterface.GetParameters().Select(p => p.ParameterType).ToArray();
            var methBuilder = typeBuilder.DefineMethod
            (
                methInterface.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                methInterface.ReturnType, paramTypes
            );

            typeBuilder.DefineMethodOverride(methBuilder, methInterface);

            ILGenerator iLGenerator = methBuilder.GetILGenerator();

            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldfld, fbDbContext);


            var dbSetType = GetDbSetGenericGetter(dbContext.GetType());

            iLGenerator.Emit(OpCodes.Callvirt, dbSetType);

            methodsIL[methInterface.Name].Invoke(iLGenerator);

            iLGenerator.Emit(OpCodes.Ret);
        }

        private void GenerateConstructor(FieldBuilder fbDbContext)
        {
            Type[] parameterTypes = { typeof(DbContext) };
            ConstructorBuilder constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

            ILGenerator iLGenerator = constructor.GetILGenerator();

            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Stfld, fbDbContext);
            iLGenerator.Emit(OpCodes.Ret);
        }
    }
}
