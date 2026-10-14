using Ecommerce.Domain;
using Ecommerce.Domain.Models.Contracts.Repository.GenericReposatory;
using Ecommerce.Domain.Models.Contracts.Specifications;
using Ecommerce.Presistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecommerce.Presistence.Repository
{
    public class GenericRepo<TEntity, TKey>(StoreDbContext context) : IGenericRepo<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        public async Task<IEnumerable<TEntity>> GetAllAsync()
            => await context.Set<TEntity>().ToListAsync();

        public async Task<TEntity?> GetByIdAsync(TKey id)
            => await context.Set<TEntity>().FindAsync(id);

        public void add(TEntity entity)
            => context.Set<TEntity>().Add(entity);

        public void update(TEntity entity)
            => context.Set<TEntity>().Update(entity);

        public void delete(TEntity entity)
            => context.Set<TEntity>().Remove(entity);

        public async Task<IEnumerable<TEntity>> GetAllWithSpecificatonsAsync(ISpecification<TEntity, TKey> specifications)
        {
            var baseQuery = context.Set<TEntity>().AsQueryable();
            return await SpecificationEvaluator.CreateQuery(baseQuery, specifications).ToListAsync();
        }

        public async Task<TEntity?> GetByIdWithSpecificationsAync(ISpecification<TEntity, TKey> specifications)
        {
            var baseQuery = context.Set<TEntity>().AsQueryable();
            return await SpecificationEvaluator.CreateQuery(baseQuery, specifications).FirstOrDefaultAsync();
        }

        public async Task<int> GetCountWithSpecificatonsAsync(ISpecification<TEntity, TKey> specifications)
        {
            var query = SpecificationEvaluator.CreateQuery(context.Set<TEntity>().AsQueryable(), specifications);
            return await query.CountAsync();
        }
    }
}
