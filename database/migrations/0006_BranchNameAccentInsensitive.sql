BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122041742_BranchNameAccentInsensitive'
)
BEGIN
    DROP INDEX [IX_Branches_Name] ON [Branches];
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'Name');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Branches] ALTER COLUMN [Name] nvarchar(100) COLLATE Modern_Spanish_100_CI_AI NOT NULL;
    CREATE UNIQUE INDEX [IX_Branches_Name] ON [Branches] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122041742_BranchNameAccentInsensitive'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122041742_BranchNameAccentInsensitive', N'10.0.15');
END;

COMMIT;
GO

