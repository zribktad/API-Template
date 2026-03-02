-- Function: get_product_category_stats(p_category_id UUID)
--
-- Returns aggregated statistics for a single category:
--   CategoryId    — the category UUID
--   CategoryName  — the category name
--   ProductCount  — number of products assigned to this category
--   AveragePrice  — average price of those products (0 when no products)
--   TotalReviews  — total number of reviews across all products in the category
--
-- Column names use quoted PascalCase so that EF Core can map them directly to
-- the ProductCategoryStats entity properties without additional configuration.
--
-- DROP IF EXISTS + CREATE OR REPLACE handles two cases:
--   • New install         — DROP does nothing, CREATE installs the function
--   • Update (e.g. changed return columns) — DROP removes old signature, CREATE installs new one
-- This combination is fully idempotent and safe to run on every application start.

DROP FUNCTION IF EXISTS get_product_category_stats(UUID);

CREATE OR REPLACE FUNCTION get_product_category_stats(p_category_id UUID)
RETURNS TABLE(
    "CategoryId"   UUID,
    "CategoryName" TEXT,
    "ProductCount" BIGINT,
    "AveragePrice" NUMERIC,
    "TotalReviews" BIGINT
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT
        c."Id"                      AS "CategoryId",
        c."Name"::TEXT              AS "CategoryName",
        COUNT(DISTINCT p."Id")      AS "ProductCount",
        COALESCE(AVG(p."Price"), 0) AS "AveragePrice",
        COUNT(pr."Id")              AS "TotalReviews"
    FROM "Categories" c
    LEFT JOIN "Products"       p  ON p."CategoryId" = c."Id"
    LEFT JOIN "ProductReviews" pr ON pr."ProductId"  = p."Id"
    WHERE c."Id" = p_category_id
    GROUP BY c."Id", c."Name";
END;
$$;
