using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public partial class Builder
    {
        private void InitializeMethodsIL()
        {
            methodsIL.Add("SelectAllAsync", (il) => ToListAsyncIL(il));

            Action<ILGenerator> Where = (il) =>
            {
                GenericQueriableIL(il, nameof(Queryable.Where));
                ToListAsyncIL(il);
            };
            methodsIL.Add("SelectWhereAsync", Where);

            Action<ILGenerator> Take = (il) =>
            {
                GenericQueriableIL(il, nameof(Queryable.Take));
                ToListAsyncIL(il);
            };
            methodsIL.Add("SelectNAsync", Take);

            Action<ILGenerator> FirstOrDefault = (il) =>
            {
                FirstOrDefaultAsyncIL(il);
            };
            methodsIL.Add("SelectFirstAsync", FirstOrDefault);

            Action<ILGenerator> Update = (il) =>
            {
                GenericDbSetIL(il, nameof(DbSet<object>.Update));
                il.Emit(OpCodes.Pop);
            };
            methodsIL.Add("Update", Update);

            Action<ILGenerator> Remove = (il) =>
            {
                GenericDbSetIL(il, nameof(DbSet<object>.Remove));
                il.Emit(OpCodes.Pop);
            };
            methodsIL.Add("Remove", Remove);

            Action<ILGenerator> SaveChangesAsync = (il) =>
            {
                SaveChangesAsyncIL(il);
            };
            methodsIL.Add("SaveChangesAsync", SaveChangesAsync);

            Action<ILGenerator> Add = (il) =>
            {
                AddDbSetIL(il);
            };
            methodsIL.Add("Add", Add);
        }

        private void AddDbSetIL(ILGenerator iLGenerator)
        {
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Callvirt, GetMethod(typeof(DbSet<>).MakeGenericType(genericType), nameof(DbSet<object>.Add)));
        }

        private void GenericQueriableIL(ILGenerator iLGenerator, string methName)
        {
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Call, GetMethod(typeof(Queryable), methName));
        }

        private void FirstOrDefaultAsyncIL(ILGenerator iLGenerator)
        {
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Call, GetCancellationTokenGetter());
            iLGenerator.Emit(OpCodes.Call, GetFirstOrDefaultAsyncMethod());
        }

        private void ToListAsyncIL(ILGenerator iLGenerator)
        {
            iLGenerator.Emit(OpCodes.Call, GetCancellationTokenGetter());
            iLGenerator.Emit(OpCodes.Call, GetToListAsyncMethod());
        }
        private void SaveChangesAsyncIL(ILGenerator iLGenerator)
        {
            iLGenerator.Emit(OpCodes.Call, GetCancellationTokenGetter());
            iLGenerator.Emit(OpCodes.Call, GetSaveChangesAsyncMethod());
        }

        private void GenericDbContextIL(ILGenerator iLGenerator, string methName)
        {
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Call, GetCancellationTokenGetter());
            iLGenerator.Emit(OpCodes.Callvirt, GetMethod(typeof(DbContext), methName));
        }


        private void GenericDbSetIL(ILGenerator iLGenerator, string methName)
        {
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Callvirt, GetMethod(typeof(DbSet<>).MakeGenericType(genericType), methName));
        }
    }
}
