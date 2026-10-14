using Ecommerce.Domain.Models.Contracts.Repository.GenericReposatory;

namespace Ecommerce.Domain.Models.Contracts.UOW
{
    public interface IUnitOfWork
    {
        IGenericRepo<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>;
        Task<int> SaveChangesAsync();
    }
}
