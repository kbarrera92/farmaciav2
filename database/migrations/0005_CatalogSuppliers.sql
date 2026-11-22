BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122051538_CatalogSuppliers'
)
BEGIN
    CREATE TABLE [Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
        [TaxId] nvarchar(50) NULL,
        [Address] nvarchar(150) NULL,
        [Phone] nvarchar(50) NULL,
        [Email] nvarchar(75) NULL,
        [ContactName] nvarchar(100) NULL,
        [ContactPhone] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Suppliers_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Suppliers_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122051538_CatalogSuppliers'
)
BEGIN
    EXEC(N'UPDATE [Permissions] SET [Description] = N''Crear, editar y activar/desactivar categorías, proveedores y productos del catálogo.''
    WHERE [Code] = ''catalog.manage'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122051538_CatalogSuppliers'
)
BEGIN
    EXEC(N'UPDATE [Permissions] SET [Description] = N''Consultar las categorías, proveedores y productos del catálogo.''
    WHERE [Code] = ''catalog.view'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122051538_CatalogSuppliers'
)
BEGIN
    CREATE INDEX [IX_Suppliers_CreatedByUserId] ON [Suppliers] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122051538_CatalogSuppliers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Suppliers_Name] ON [Suppliers] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122051538_CatalogSuppliers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Suppliers_TaxId] ON [Suppliers] ([TaxId]) WHERE [TaxId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122051538_CatalogSuppliers'
)
BEGIN
    CREATE INDEX [IX_Suppliers_UpdatedByUserId] ON [Suppliers] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122051538_CatalogSuppliers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122051538_CatalogSuppliers', N'10.0.15');
END;

COMMIT;
GO

