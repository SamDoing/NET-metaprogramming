using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public partial class Builder
    {
        private MethodInfo GetDbSetGenericGetter(Type contextType) =>
                    contextType
                    .GetProperties().Where(p => p.PropertyType.GenericTypeArguments.First() == genericType)
                    .First()
                    .GetGetMethod() ?? throw new Exception();

        private MethodInfo GetMethod(Type typeName, string name)
        {
            var meth = typeName.GetMethods().Where(m => m.Name == name).First();
            return meth.IsGenericMethod ? meth.MakeGenericMethod(genericType) : meth;
        }

        private MethodInfo GetCancellationTokenGetter() =>
            typeof(CancellationToken).GetProperty(nameof(CancellationToken.None))?.GetGetMethod() ?? throw new Exception();

        private MethodInfo GetSaveChangesAsyncMethod() =>
            typeof(DbContext)
            .GetMethods()
            .Where(m => m.Name == nameof(DbContext.SaveChangesAsync))
            .First(m => m.GetParameters().Length == 1) ?? throw new Exception();

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
