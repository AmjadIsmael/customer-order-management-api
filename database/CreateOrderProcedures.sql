-- ============================================================================
-- Stored procedures: GetCustomerOrderSummary, SearchOrders
--
-- Run in pgAdmin's Query Tool (or psql) against the application database.
-- Safe to re-run: CREATE OR REPLACE PROCEDURE overwrites the previous
-- definition as long as the parameter list is unchanged.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- GetCustomerOrderSummary
--
-- Returns one row of aggregate stats for a single customer's order history.
-- Called with:
--   CALL "GetCustomerOrderSummary"('<customer-guid>', NULL, NULL, NULL, NULL);
-- The four NULL placeholders are the OUT parameters; pgAdmin's result grid
-- fills them in and displays them as columns of a single returned row.
-- ----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE "GetCustomerOrderSummary"(
    IN  p_customer_id   uuid,
    OUT total_orders    integer,
    OUT total_spent     numeric(18,2),
    OUT pending_orders  integer,
    OUT last_order_date timestamptz
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT
        COUNT(*)::integer,
        COALESCE(SUM(o."TotalAmount"), 0),
        COUNT(*) FILTER (WHERE o."Status" = 0)::integer,  -- 0 = OrderStatus.Pending
        MAX(o."CreatedDate")
    INTO total_orders, total_spent, pending_orders, last_order_date
    FROM "Orders" o
    WHERE o."CustomerId" = p_customer_id
      AND NOT o."IsDeleted";
END;
$$;

-- ----------------------------------------------------------------------------
-- SearchOrders
--
-- Returns a variable-length set of orders matching optional filters
-- (any parameter left NULL is ignored). Because a true PROCEDURE can't
-- hand back a result set directly, it opens a REFCURSOR that the caller
-- fetches from within the same transaction.
--
-- Must be run as one batch (all statements selected together), not one
-- line at a time, since the cursor only lives for the transaction that
-- opened it:
--
--   BEGIN;
--   CALL "SearchOrders"(NULL, NULL, NULL, NULL, 'search_orders_cursor');
--   FETCH ALL FROM "search_orders_cursor";
--   COMMIT;
--
-- Example: only pending orders (status = 0) placed in the last 30 days:
--   BEGIN;
--   CALL "SearchOrders"(NULL, 0, now() - interval '30 days', NULL, 'search_orders_cursor');
--   FETCH ALL FROM "search_orders_cursor";
--   COMMIT;
-- ----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE "SearchOrders"(
    IN    p_customer_id uuid,
    IN    p_status      integer,
    IN    p_from_date   timestamptz,
    IN    p_to_date     timestamptz,
    INOUT p_cursor      refcursor DEFAULT 'search_orders_cursor'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN p_cursor FOR
        SELECT
            o."Id",
            o."CustomerId",
            c."FirstName",
            c."LastName",
            o."Status",
            o."TotalAmount",
            o."ShippingAddress",
            o."CreatedDate"
        FROM "Orders" o
        JOIN "Customers" c ON c."Id" = o."CustomerId"
        WHERE NOT o."IsDeleted"
          AND (p_customer_id IS NULL OR o."CustomerId" = p_customer_id)
          AND (p_status IS NULL OR o."Status" = p_status)
          AND (p_from_date IS NULL OR o."CreatedDate" >= p_from_date)
          AND (p_to_date IS NULL OR o."CreatedDate" <= p_to_date)
        ORDER BY o."CreatedDate" DESC;
END;
$$;
