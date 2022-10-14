using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public class GenericRepoBuilder<T>
    {
        private readonly TypeBuilder typeBuilder;
        private readonly Dictionary<string, MethodTranslate> LinqTranslate = new()
        {
            { "All", new() { Name = nameof(Enumerable.ToList), MethodType = typeof(Enumerable) } }
        };

        public GenericRepoBuilder()
        {
            AssemblyName aName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);

            // The module name is usually the same as the assembly name.
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);

            typeBuilder = mb.DefineType($"{typeof(T).Name}DynamicType", TypeAttributes.Public);
        }

        public T Build(DbContext appDbContext)
        {
            var typeOfT = typeof(T);
            //if (!typeOfT.IsAssignableFrom(typeof(GenericRepositoryBase<>)) || !typeOfT.IsInterface)
            //{
            //    throw new Exception($"Type needs to be interface and implements {typeof(GenericRepositoryBase<>).Name}");
            //}

            typeBuilder.AddInterfaceImplementation(typeof(T));

            var fbDbContext = typeBuilder.DefineField("DbContext", typeof(DbContext), FieldAttributes.Private);

            GenerateConstructor(fbDbContext);
            GenerateMethods(fbDbContext, appDbContext);

            var tp = typeBuilder.CreateType();
            return (T)(Activator.CreateInstance(tp, appDbContext) ?? throw new Exception());
        }

        private void GenerateMethods(FieldBuilder fbDbContext, DbContext dbContext)
        {
            foreach (var methInterface in typeof(T).GetMethods())
            {
                var paramTypes = methInterface.GetParameters().Select(p => p.ParameterType).ToArray();
                MethodBuilder meth = typeBuilder.DefineMethod(methInterface.Name, MethodAttributes.Public | MethodAttributes.Virtual, methInterface.ReturnType, paramTypes);

                typeBuilder.DefineMethodOverride(meth, methInterface);

                ILGenerator iLGenerator = meth.GetILGenerator();

                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Ldfld, fbDbContext);


                var dbSetType = dbContext.GetType().GetProperties()
                        .Where(p => p.PropertyType.GenericTypeArguments.First() == typeof(T).GenericTypeArguments.First())
                        .First()
                        .GetGetMethod();

                iLGenerator.Emit(OpCodes.Callvirt, dbSetType);

                var info = ResolveLinq(methInterface.Name);

                iLGenerator.Emit(OpCodes.Call, info);
                iLGenerator.Emit(OpCodes.Ret);
            }
        }


        private MethodInfo ResolveLinq(string name)
        {
            var methodTranslate = LinqTranslate.Where(o => name.Contains(o.Key)).Select(o => o.Value).First();

            return methodTranslate.MethodType.GetMethod(methodTranslate.Name).MakeGenericMethod(typeof(T).GenericTypeArguments.First());
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
