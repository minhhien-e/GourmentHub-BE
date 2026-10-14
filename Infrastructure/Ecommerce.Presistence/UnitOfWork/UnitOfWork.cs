using Ecommerce.Domain;
using Ecommerce.Domain.Models.Contracts.Repository.GenericReposatory;
using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Presistence.Contexts;
using Ecommerce.Presistence.Repository;
using System.Collections.Concurrent;

namespace Ecommerce.Presistence.UnitOfWork
{
    public class UnitOfWork(StoreDbContext context) : IUnitOfWork
    {
        private readonly ConcurrentDictionary<string, object> _repositories = new();

        public IGenericRepo<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>
        {
            var typeName = typeof(TEntity).Name;

            return (IGenericRepo<TEntity, TKey>)_repositories.GetOrAdd(typeName, _ => new GenericRepo<TEntity, TKey>(context));
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }
    }
}
