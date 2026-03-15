namespace APITemplate.Application.Common.Events;

public sealed record ProductsChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Products";
}

public sealed record CategoriesChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Categories";
}

public sealed record ProductReviewsChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Reviews";
}

public sealed record ProductDataChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "ProductData";
}

public sealed record TenantsChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Tenants";
}

public sealed record TenantInvitationsChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "TenantInvitations";
}

public sealed record UsersChangedNotification : ICacheInvalidationNotification
{
    public string CacheTag => "Users";
}
