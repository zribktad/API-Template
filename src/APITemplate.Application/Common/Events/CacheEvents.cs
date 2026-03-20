namespace APITemplate.Application.Common.Events;

/// <summary>Published when any product data changes, triggering invalidation of the Products cache region.</summary>
public sealed record ProductsChangedNotification : ICacheInvalidationEvent
{
    public string CacheTag => "Products";
}

/// <summary>Published when any category data changes, triggering invalidation of the Categories cache region.</summary>
public sealed record CategoriesChangedNotification : ICacheInvalidationEvent
{
    public string CacheTag => "Categories";
}

/// <summary>Published when product reviews change, triggering invalidation of the Reviews cache region.</summary>
public sealed record ProductReviewsChangedNotification : ICacheInvalidationEvent
{
    public string CacheTag => "Reviews";
}

/// <summary>Published when product-data attachments change, triggering invalidation of the ProductData cache region.</summary>
public sealed record ProductDataChangedNotification : ICacheInvalidationEvent
{
    public string CacheTag => "ProductData";
}

/// <summary>Published when tenant data changes, triggering invalidation of the Tenants cache region.</summary>
public sealed record TenantsChangedNotification : ICacheInvalidationEvent
{
    public string CacheTag => "Tenants";
}

/// <summary>Published when tenant invitations change, triggering invalidation of the TenantInvitations cache region.</summary>
public sealed record TenantInvitationsChangedNotification : ICacheInvalidationEvent
{
    public string CacheTag => "TenantInvitations";
}

/// <summary>Published when user data changes, triggering invalidation of the Users cache region.</summary>
public sealed record UsersChangedNotification : ICacheInvalidationEvent
{
    public string CacheTag => "Users";
}
