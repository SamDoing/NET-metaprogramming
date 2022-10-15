using NetMetaprograming.Data;
using NetMetaprograming.Data.Models;
using NetMetaprograming.GenericRepositoryBuilder;

using(var db = new AppDbContext())
{
    db.Database.EnsureCreated();

    var repoBuilder = new GenericRepoBuilder<IGenericRepository<Product>, Product>();
    var productRepository = repoBuilder.Build(db);
    
    var products = await productRepository.SelectAll();
    products.ForEach(p => Console.WriteLine($"{p.Name} is a product"));
}
