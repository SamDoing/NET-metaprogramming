using Microsoft.EntityFrameworkCore;

namespace NetMetaprograming.GenericRepositoryBuilder
{
    public interface IGenericRepository<T> where T : class
    {
        public List<T> SelectAll();
    }
}
