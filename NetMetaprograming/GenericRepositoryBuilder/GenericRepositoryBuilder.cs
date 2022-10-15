using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NetMetaprograming.Data;
using Microsoft.EntityFrameworkCore.Internal;
using NetMetaprograming.GenericRepositoryBuilder.Utils;
using System.Collections;
using NetMetaprograming.Data.Models;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public class GenericRepoBuilder<T>
    {
        private readonly TypeBuilder typeBuilder;

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


                var methGenRetType = methInterface.ReturnParameter.ParameterType.GenericTypeArguments.First();
                if(methGenRetType.Name == typeof(List<>).Name)
                {
                    var iParams = methInterface.GetParameters();
                    if (iParams.Any())
                    {
                        iLGenerator.Emit(OpCodes.Ldarg_1);

                        var queryFilter = methInterface.Name.Substring("Select".Length);

                        iLGenerator.Emit(OpCodes.Ldstr, queryFilter);
                        iLGenerator.Emit(OpCodes.Call, GetType().GetMethod(nameof(CollectionQueryLambdaGenerator)));
                    }
                    else
                    {
                        iLGenerator.Emit(OpCodes.Call, GetType().GetMethod(nameof(ToListAsync)));
                    }
                }

                iLGenerator.Emit(OpCodes.Ret);
            }
        }

        public static Task CollectionQueryLambdaGenerator(object lambda, object dbSet, string filterName)
        {
            var lambName = nameof(Queryable.Where);
            var lambGenType = lambda.GetType().GenericTypeArguments.First();
            var lambMeth = typeof(Queryable).GetMethods().Where(m => m.Name == filterName).First();

            var query = (IQueryable) lambMeth.MakeGenericMethod(lambGenType).Invoke(null, new object[] { lambda, dbSet });
            return ToListAsync(query);
        }

        public static Task ToListAsync(object query)
        {
            var methName = nameof(EntityFrameworkQueryableExtensions.ToListAsync);
            var genType = query.GetType().GenericTypeArguments.First();
            var meth = typeof(EntityFrameworkQueryableExtensions).GetMethod(methName).MakeGenericMethod(genType);

            return (Task)meth.Invoke(null, new object[] { query, null });
        }

        public static void test2(Task<List<Product>> t)
        {
            Console.WriteLine(t.GetAwaiter().GetResult());
        }

        private void GenerateConstructor(FieldBuilder fbDbContext)
        {
            Type[] parameterTypes = { typeof(DbContext) };
            ConstructorBuilder constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

            ILGenerator iLGenerator = constructor.GetILGenerator();

            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

    //       iLGenerator.Emit(OpCodes.Ldarg_1);
   //        iLGenerator.Emit(OpCodes.Call, GetType().GetMethod(nameof(test)));

  //         iLGenerator.Emit(OpCodes.Call, GetType().GetMethod(nameof(test2)));

            //var t = MethodInfoHelper.Of((string s) => Console.WriteLine(s));
            //iLGenerator.Emit(OpCodes.Call, t);


            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Stfld, fbDbContext);
            iLGenerator.Emit(OpCodes.Ret);
        }
    }
}
