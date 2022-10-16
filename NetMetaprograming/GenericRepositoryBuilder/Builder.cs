using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Reflection.Emit;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public partial class Builder
    {
        private readonly Type interfaceType;
        private readonly TypeBuilder typeBuilder;
        private readonly Type genericType;
        private readonly List<MethodInfo> interfaceMethods = new();
        private static readonly Dictionary<string, Action<ILGenerator>> methodsIL = new();
        private FieldBuilder fbDbContext;


        public Builder(Type interfaceType)
        {
            this.interfaceType = interfaceType;
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

            return mb.DefineType($"{interfaceType.Name}DynamicType", TypeAttributes.Public);
        }

        private Type ValidateInterface()
        {
            var genericInterface = interfaceType.GetInterface(typeof(IGenericRepository<>).Name);
            if (!interfaceType.IsInterface || genericInterface == null)
            {
                throw new Exception($"Type needs to be interface and generic");
            }
            return genericInterface.GenericTypeArguments.Single();
        }

        private void AddAndCheckMethodsImplementation()
        {   
            interfaceMethods.AddRange(interfaceType.GetInterfaces().SelectMany(i => i.GetMethods()));
            interfaceMethods.AddRange(interfaceType.GetMethods());

            var methNotImplemented = interfaceMethods.FirstOrDefault(m => !methodsIL.ContainsKey(m.Name));
            if (methNotImplemented != null)
                throw new Exception($"{methNotImplemented.Name} not implemented");
        }

        public object Build(DbContext appDbContext)
        {
            typeBuilder.AddInterfaceImplementation(interfaceType);
            fbDbContext = typeBuilder.DefineField("dbContext", typeof(DbContext), FieldAttributes.Private);

            GenerateConstructor(fbDbContext);
            GenerateMethods(appDbContext);

            var tp = typeBuilder.CreateType() ?? throw new Exception();
            return Activator.CreateInstance(tp, appDbContext) ?? throw new Exception();
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


            if(methInterface.Name != "SaveChangesAsync")
            {
                var dbSetType = GetDbSetGenericGetter(dbContext.GetType());
                iLGenerator.Emit(OpCodes.Callvirt, dbSetType);
            }

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
