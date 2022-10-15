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
        }

        private void GenericQueriableIL(ILGenerator iLGenerator, string methName)
        {
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Call, GetGenericMethod(typeof(Queryable), methName));
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
    }
}
