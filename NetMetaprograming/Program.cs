using NetMetaprograming.Data;
using NetMetaprograming.GenericRepositoryBuilder;

using(var db = new AppDbContext())
{
    db.Database.EnsureCreated();

    var repoBuilder = new Builder(typeof(IProductRepository));
    var productRepository = (IProductRepository) repoBuilder.Build(db);
    
    var products = await productRepository.SelectNAsync(1);
    products.ForEach(p => Console.WriteLine($"{p.Name} is a product"));

    var product = await productRepository.SelectFirstAsync(p => p.Name.Contains("Item2"));
    Console.WriteLine($"{product.Name} is a product");
}
