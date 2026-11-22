BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122041433_NamesAccentInsensitive'
)
BEGIN
    DROP INDEX [IX_Roles_Name] ON [Roles];
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'Name');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Roles] ALTER COLUMN [Name] nvarchar(50) COLLATE Modern_Spanish_100_CI_AI NOT NULL;
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122041433_NamesAccentInsensitive'
)
BEGIN
    DROP INDEX [IX_PaymentMethods_Name] ON [PaymentMethods];
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaymentMethods]') AND [c].[name] = N'Name');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [PaymentMethods] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [PaymentMethods] ALTER COLUMN [Name] nvarchar(50) COLLATE Modern_Spanish_100_CI_AI NOT NULL;
    CREATE UNIQUE INDEX [IX_PaymentMethods_Name] ON [PaymentMethods] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122041433_NamesAccentInsensitive'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122041433_NamesAccentInsensitive', N'10.0.15');
END;

COMMIT;
GO

