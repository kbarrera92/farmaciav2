BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122040912_CategoryNameAccentInsensitive'
)
BEGIN
    DROP INDEX [IX_Categories_Name] ON [Categories];
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Categories]') AND [c].[name] = N'Name');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Categories] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Categories] ALTER COLUMN [Name] nvarchar(50) COLLATE Modern_Spanish_100_CI_AI NOT NULL;
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122040912_CategoryNameAccentInsensitive'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122040912_CategoryNameAccentInsensitive', N'10.0.15');
END;

COMMIT;
GO

