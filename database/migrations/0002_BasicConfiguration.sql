BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Users] ADD [CreatedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Users] ADD [UpdatedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Roles] ADD [CreatedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Roles] ADD [UpdatedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Branches] ADD [CreatedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Branches] ADD [UpdatedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE TABLE [CompanySettings] (
        [Id] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [TaxId] nvarchar(25) NULL,
        [Address] nvarchar(150) NULL,
        [Phone] nvarchar(50) NULL,
        [Email] nvarchar(100) NULL,
        [Logo] varbinary(max) NULL,
        [LogoContentType] varchar(50) NULL,
        [LogoUpdatedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_CompanySettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CompanySettings_SingleRow] CHECK ([Id] = 1),
        CONSTRAINT [FK_CompanySettings_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_CompanySettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE TABLE [PaymentMethods] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentMethods_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PaymentMethods_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Description', N'Module', N'Name') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Code], [Description], [Module], [Name])
    VALUES (''settings.manage'', N''Editar los datos y el logo de la empresa, y administrar las formas de pago.'', N''Configuración'', N''Administrar configuración''),
    (''settings.view'', N''Consultar las formas de pago.'', N''Configuración'', N''Ver configuración'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Description', N'Module', N'Name') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_Users_CreatedByUserId] ON [Users] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_Users_UpdatedByUserId] ON [Users] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_Roles_CreatedByUserId] ON [Roles] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_Roles_UpdatedByUserId] ON [Roles] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_Branches_CreatedByUserId] ON [Branches] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_Branches_UpdatedByUserId] ON [Branches] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_CompanySettings_CreatedByUserId] ON [CompanySettings] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_CompanySettings_UpdatedByUserId] ON [CompanySettings] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_PaymentMethods_CreatedByUserId] ON [PaymentMethods] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentMethods_Name] ON [PaymentMethods] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    CREATE INDEX [IX_PaymentMethods_UpdatedByUserId] ON [PaymentMethods] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Branches] ADD CONSTRAINT [FK_Branches_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Branches] ADD CONSTRAINT [FK_Branches_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Roles] ADD CONSTRAINT [FK_Roles_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Roles] ADD CONSTRAINT [FK_Roles_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122031718_BasicConfiguration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122031718_BasicConfiguration', N'10.0.15');
END;

COMMIT;
GO

