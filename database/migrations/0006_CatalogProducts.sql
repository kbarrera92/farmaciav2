BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
        [Presentation] nvarchar(100) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
        [ActiveIngredient] nvarchar(150) NULL,
        [TherapeuticAction] nvarchar(150) NULL,
        [Indications] nvarchar(150) NULL,
        [Contraindications] nvarchar(150) NULL,
        [Notes] nvarchar(250) NULL,
        [Measure] nvarchar(75) NULL,
        [Laboratory] nvarchar(100) NULL,
        [Barcode] nvarchar(25) NULL,
        [CategoryId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [Price] decimal(10,2) NOT NULL,
        [Cost] decimal(10,2) NOT NULL,
        [MinStock] int NULL,
        [IsControlled] bit NOT NULL,
        [IsBonusEligible] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Products_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    CREATE TABLE [ProductBranchSettings] (
        [ProductId] int NOT NULL,
        [BranchId] int NOT NULL,
        [Price] decimal(10,2) NULL,
        [MinStock] int NULL,
        [Shelf] nvarchar(30) NULL,
        CONSTRAINT [PK_ProductBranchSettings] PRIMARY KEY ([ProductId], [BranchId]),
        CONSTRAINT [FK_ProductBranchSettings_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductBranchSettings_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    CREATE INDEX [IX_ProductBranchSettings_BranchId] ON [ProductBranchSettings] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_Barcode] ON [Products] ([Barcode]) WHERE [Barcode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    CREATE INDEX [IX_Products_CreatedByUserId] ON [Products] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Name_Presentation] ON [Products] ([Name], [Presentation]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    CREATE INDEX [IX_Products_SupplierId] ON [Products] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    CREATE INDEX [IX_Products_UpdatedByUserId] ON [Products] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122052228_CatalogProducts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122052228_CatalogProducts', N'10.0.15');
END;

COMMIT;
GO

