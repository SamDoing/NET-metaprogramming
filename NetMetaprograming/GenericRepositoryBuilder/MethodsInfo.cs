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
        private MethodInfo GetDbSetGenericGetter(Type contextType) =>
                    contextType
                    .GetProperties().Where(p => p.PropertyType.GenericTypeArguments.First() == genericType)
                    .First()
                    .GetGetMethod() ?? throw new Exception();

        private MethodInfo GetGenericMethod(Type typeName, string name) =>
            typeName.GetMethods().Where(m => m.Name == name).First().MakeGenericMethod(genericType);

        private MethodInfo GetCancellationTokenGetter() =>
            typeof(CancellationToken).GetProperty(nameof(CancellationToken.None))?.GetGetMethod() ?? throw new Exception();

        private MethodInfo GetToListAsyncMethod() =>
            typeof(EntityFrameworkQueryableExtensions)
            .GetMethod(nameof(EntityFrameworkQueryableExtensions.ToListAsync))?
            .MakeGenericMethod(genericType) ?? throw new Exception();

        private MethodInfo GetFirstOrDefaultAsyncMethod() =>
            typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.FirstOrDefaultAsync) && m.GetParameters().Length == 3)
            .First()
            .MakeGenericMethod(genericType) ?? throw new Exception();
    }
}
