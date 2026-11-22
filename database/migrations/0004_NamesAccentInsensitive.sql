BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122042648_NamesAccentInsensitive'
)
BEGIN
    DECLARE @duplicateNames nvarchar(max);
    SELECT @duplicateNames = COALESCE(@duplicateNames + N'; ', N'') + d.Entity + N': ' + d.Name
    FROM (
        SELECT N'Rol' AS Entity, MIN([Name]) AS Name FROM [Roles]
        GROUP BY [Name] COLLATE Modern_Spanish_100_CI_AI HAVING COUNT(*) > 1
        UNION ALL
        SELECT N'Forma de pago', MIN([Name]) FROM [PaymentMethods]
        GROUP BY [Name] COLLATE Modern_Spanish_100_CI_AI HAVING COUNT(*) > 1
        UNION ALL
        SELECT N'Categoría', MIN([Name]) FROM [Categories]
        GROUP BY [Name] COLLATE Modern_Spanish_100_CI_AI HAVING COUNT(*) > 1
        UNION ALL
        SELECT N'Sucursal', MIN([Name]) FROM [Branches]
        GROUP BY [Name] COLLATE Modern_Spanish_100_CI_AI HAVING COUNT(*) > 1
    ) AS d;
    IF @duplicateNames IS NOT NULL
    BEGIN
        DECLARE @duplicateNamesMessage nvarchar(2048) = LEFT(
            N'No se aplicó la migración NamesAccentInsensitive: hay nombres repetidos que solo se '
            + N'diferencian en mayúsculas o tildes. Renómbrelos y vuelva a ejecutar el script. '
            + @duplicateNames, 2048);
        THROW 50001, @duplicateNamesMessage, 1;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122042648_NamesAccentInsensitive'
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
    WHERE [MigrationId] = N'20261122042648_NamesAccentInsensitive'
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
    WHERE [MigrationId] = N'20261122042648_NamesAccentInsensitive'
)
BEGIN
    DROP INDEX [IX_Categories_Name] ON [Categories];
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Categories]') AND [c].[name] = N'Name');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Categories] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Categories] ALTER COLUMN [Name] nvarchar(50) COLLATE Modern_Spanish_100_CI_AI NOT NULL;
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122042648_NamesAccentInsensitive'
)
BEGIN
    DROP INDEX [IX_Branches_Name] ON [Branches];
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'Name');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Branches] ALTER COLUMN [Name] nvarchar(100) COLLATE Modern_Spanish_100_CI_AI NOT NULL;
    CREATE UNIQUE INDEX [IX_Branches_Name] ON [Branches] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122042648_NamesAccentInsensitive'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122042648_NamesAccentInsensitive', N'10.0.15');
END;

COMMIT;
GO

