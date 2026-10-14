using System.Linq.Expressions;

namespace Ecommerce.Domain.Models.Contracts.Specifications
{
    public interface ISpecification<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        Expression<Func<TEntity, bool>>       Criteria      { get; }
        List<Expression<Func<TEntity, object>>> Includes    { get; }
        Expression<Func<TEntity, object>>     OrderBy       { get; }
        Expression<Func<TEntity, object>>     OrderByDesc   { get; }
        int  take         { get; }
        int  skip         { get; }
        bool isPaginable  { get; set; }
    }
}
