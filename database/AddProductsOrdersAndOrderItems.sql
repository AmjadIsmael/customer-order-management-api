START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813093328_AddProductsOrdersAndOrderItems') THEN
    CREATE TABLE "Orders" (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "ShippingAddress" text,
        "TotalAmount" numeric(18,2) NOT NULL,
        "CreatedDate" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedDate" timestamp with time zone,
        "UpdatedBy" text,
        "IsActive" boolean NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Orders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Orders_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813093328_AddProductsOrdersAndOrderItems') THEN
    CREATE TABLE "Products" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "Price" numeric(18,2) NOT NULL,
        "StockQuantity" integer NOT NULL,
        "CreatedDate" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedDate" timestamp with time zone,
        "UpdatedBy" text,
        "IsActive" boolean NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Products" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813093328_AddProductsOrdersAndOrderItems') THEN
    CREATE TABLE "OrderItems" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Quantity" integer NOT NULL,
        "UnitPrice" numeric(18,2) NOT NULL,
        "CreatedDate" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedDate" timestamp with time zone,
        "UpdatedBy" text,
        "IsActive" boolean NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_OrderItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813093328_AddProductsOrdersAndOrderItems') THEN
    CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813093328_AddProductsOrdersAndOrderItems') THEN
    CREATE INDEX "IX_OrderItems_ProductId" ON "OrderItems" ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813093328_AddProductsOrdersAndOrderItems') THEN
    CREATE INDEX "IX_Orders_CustomerId" ON "Orders" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813093328_AddProductsOrdersAndOrderItems') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260813093328_AddProductsOrdersAndOrderItems', '10.0.10');
    END IF;
END $EF$;
COMMIT;

