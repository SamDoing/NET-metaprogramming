using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public interface IGenericRepository<T> where T : class
    {
        public Task<List<T>> SelectWhereAsync(Expression<Func<T, bool>> filter);
        public Task<List<T>> SelectAllAsync();
        public Task<List<T>> SelectNAsync(int n);
        public Task<T?> SelectFirstAsync(Expression<Func<T, bool>> filter);
    }
}
