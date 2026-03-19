namespace APITemplate.Application.Common.Events;

/// <summary>Published when any product data changes, triggering invalidation of the Products cache region.</summary>
public sealed record ProductsChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Products";
}

/// <summary>Published when any category data changes, triggering invalidation of the Categories cache region.</summary>
public sealed record CategoriesChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Categories";
}

/// <summary>Published when product reviews change, triggering invalidation of the Reviews cache region.</summary>
public sealed record ProductReviewsChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Reviews";
}

/// <summary>Published when product-data attachments change, triggering invalidation of the ProductData cache region.</summary>
public sealed record ProductDataChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "ProductData";
}

/// <summary>Published when tenant data changes, triggering invalidation of the Tenants cache region.</summary>
public sealed record TenantsChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Tenants";
}

/// <summary>Published when tenant invitations change, triggering invalidation of the TenantInvitations cache region.</summary>
public sealed record TenantInvitationsChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "TenantInvitations";
}

/// <summary>Published when user data changes, triggering invalidation of the Users cache region.</summary>
public sealed record UsersChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Users";
}
