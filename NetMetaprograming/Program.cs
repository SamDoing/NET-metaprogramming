﻿using NetMetaprograming.Data;
using NetMetaprograming.Data.Models;
using NetMetaprograming.GenericRepositoryBuilder;

using(var db = new AppDbContext())
{
    db.Database.EnsureCreated();

    var repoBuilder = new GenericRepoBuilder<IGenericRepository<Product>>();
    var productRepository = repoBuilder.Build(db);
    
    var products = productRepository.SelectAll();
    products.ForEach(p => Console.WriteLine($"{p.Nome} is a product"));
}