using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public interface IGenericRepository<T> where T : class
    {
        public Task<List<T>> SelectWhere(Expression<Func<T, bool>> filter);
        public Task<List<T>> SelectAll();
    }
}
