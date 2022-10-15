using NetMetaprograming.Data.Models;
using NetMetaprograming.GenericRepositoryBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMetaprograming.Data
{
    public interface IProductRepository : IGenericRepository<Product>
    {
    }
}
