namespace SharedKernel.Messaging.Topology;

public static class RabbitMqTopology
{
    public static class Exchanges
    {
        public const string Identity = "identity.events";
        public const string ProductCatalog = "product-catalog.events";
        public const string Reviews = "reviews.events";
    }

    public static class Queues
    {
        public static class Reviews
        {
            public const string ProductCreated = "reviews.product-created";
            public const string ProductDeleted = "reviews.product-deleted";
            public const string TenantDeactivated = "reviews.tenant-deactivated";
        }

        public static class Webhooks
        {
            public const string ProductCreated = "webhooks.product-created";
            public const string ProductDeleted = "webhooks.product-deleted";
            public const string ReviewCreated = "webhooks.review-created";
        }

        public static class BackgroundJobs
        {
            public const string TenantDeactivated = "background-jobs.tenant-deactivated";
        }

        public static class Notifications
        {
            public const string UserRegistered = "notifications.user-registered";
            public const string UserRoleChanged = "notifications.user-role-changed";
            public const string InvitationCreated = "notifications.invitation-created";
        }

        public static class FileStorage
        {
            public const string ProductDeleted = "file-storage.product-deleted";
        }

        public static class ProductCatalog
        {
            public const string ReviewsCascadeCompleted =
                "product-catalog.reviews-cascade-completed";
            public const string FilesCascadeCompleted = "product-catalog.files-cascade-completed";
            public const string StartProductDeletionSaga =
                "product-catalog.start-product-deletion-saga";
        }
    }
}
