using Ecommerce.Domain;
using Ecommerce.Domain.Models.Contracts.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Ecommerce.Presistence
{
    public static class SpecificationEvaluator
    {
        public static IQueryable<TEntity> CreateQuery<TEntity, TKey>(IQueryable<TEntity> baseQuery, ISpecification<TEntity, TKey> specification) where TEntity : BaseEntity<TKey>
        {
            var query = baseQuery;

            // Apply filtering criteria
            if (specification.Criteria != null)
            {
                query = query.Where(specification.Criteria);
            }

            // Apply ordering (must occur before pagination)
            if (specification.OrderBy != null)
            {
                query = query.OrderBy(specification.OrderBy);
            }
            else if (specification.OrderByDesc != null)
            {
                query = query.OrderByDescending(specification.OrderByDesc);
            }

            // Apply pagination if enabled
            if (specification.isPaginable)
            {
                query = query.Skip(specification.skip).Take(specification.take);
            }

            // Apply eager loading navigation properties
            if (specification.Includes != null && specification.Includes.Any())
            {
                foreach (var include in specification.Includes)
                {
                    query = query.Include(include);
                }
            }

            return query;
        }
    }
}
