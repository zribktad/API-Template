using Ardalis.Specification;
using APITemplate.Application.DTOs;
using APITemplate.Domain.Entities;

namespace APITemplate.Application.Specifications;

public sealed class ProductSpecification : Specification<Product, ProductResponse>
{
    public ProductSpecification(ProductFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Name))
            Query.Where(p => p.Name.Contains(filter.Name));

        if (!string.IsNullOrWhiteSpace(filter.Description))
            Query.Where(p => p.Description != null && p.Description.Contains(filter.Description));

        if (filter.MinPrice.HasValue)
            Query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            Query.Where(p => p.Price <= filter.MaxPrice.Value);

        if (filter.CreatedFrom.HasValue)
            Query.Where(p => p.CreatedAt >= filter.CreatedFrom.Value);

        if (filter.CreatedTo.HasValue)
            Query.Where(p => p.CreatedAt <= filter.CreatedTo.Value);

        Query.OrderByDescending(p => p.CreatedAt)
             .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.CreatedAt));
    }
}
