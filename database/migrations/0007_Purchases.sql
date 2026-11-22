BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE TABLE [Purchases] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseDate] date NOT NULL,
        [BranchId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [DocumentNumber] nvarchar(30) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
        [Type] tinyint NOT NULL,
        [PaymentMethodId] int NULL,
        [PaymentDate] date NULL,
        [CreditDays] int NULL,
        [Total] decimal(10,2) NOT NULL,
        [IsCancelled] bit NOT NULL,
        [CancelledAtUtc] datetime2 NULL,
        [CancelledByUserId] int NULL,
        [CancellationReason] nvarchar(250) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Purchases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Purchases_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Purchases_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Purchases_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Purchases_Users_CancelledByUserId] FOREIGN KEY ([CancelledByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Purchases_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Purchases_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE TABLE [Stocks] (
        [BranchId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Stocks] PRIMARY KEY ([BranchId], [ProductId]),
        CONSTRAINT [FK_Stocks_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Stocks_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE TABLE [AccountsPayable] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [StartDate] date NOT NULL,
        [DueDate] date NOT NULL,
        [Total] decimal(10,2) NOT NULL,
        [Balance] decimal(10,2) NOT NULL,
        [Status] tinyint NOT NULL,
        [PaidDate] date NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_AccountsPayable] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountsPayable_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AccountsPayable_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccountsPayable_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_AccountsPayable_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE TABLE [PurchaseItems] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitCost] decimal(10,2) NOT NULL,
        [Subtotal] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_PurchaseItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseItems_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Description', N'Module', N'Name') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Code], [Description], [Module], [Name])
    VALUES (''purchases.create'', N''Registrar compras de productos a proveedores.'', N''Compras'', N''Registrar compras''),
    (''purchases.manage'', N''Editar y anular compras ya registradas.'', N''Compras'', N''Administrar compras''),
    (''purchases.view'', N''Consultar las compras y las existencias por sucursal.'', N''Compras'', N''Ver compras'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Description', N'Module', N'Name') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_AccountsPayable_CreatedByUserId] ON [AccountsPayable] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_AccountsPayable_DueDate] ON [AccountsPayable] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountsPayable_PurchaseId] ON [AccountsPayable] ([PurchaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_AccountsPayable_SupplierId] ON [AccountsPayable] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_AccountsPayable_UpdatedByUserId] ON [AccountsPayable] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_PurchaseItems_ProductId] ON [PurchaseItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseItems_PurchaseId_ProductId] ON [PurchaseItems] ([PurchaseId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_Purchases_BranchId] ON [Purchases] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_Purchases_CancelledByUserId] ON [Purchases] ([CancelledByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_Purchases_CreatedByUserId] ON [Purchases] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_Purchases_PaymentMethodId] ON [Purchases] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_Purchases_PurchaseDate] ON [Purchases] ([PurchaseDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Purchases_SupplierId_DocumentNumber] ON [Purchases] ([SupplierId], [DocumentNumber]) WHERE [IsCancelled] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_Purchases_UpdatedByUserId] ON [Purchases] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    CREATE INDEX [IX_Stocks_ProductId] ON [Stocks] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122072055_Purchases'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122072055_Purchases', N'10.0.15');
END;

COMMIT;
GO

