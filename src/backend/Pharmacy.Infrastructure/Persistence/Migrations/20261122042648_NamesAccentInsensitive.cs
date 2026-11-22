using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharmacy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NamesAccentInsensitive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recreating the unique indexes fails if two names only differ in case or accents.
            // Check first and stop with a readable message listing the conflicting names.
            migrationBuilder.Sql("""
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
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                collation: "Modern_Spanish_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PaymentMethods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                collation: "Modern_Spanish_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                collation: "Modern_Spanish_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Branches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Modern_Spanish_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldCollation: "Modern_Spanish_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PaymentMethods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldCollation: "Modern_Spanish_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldCollation: "Modern_Spanish_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Branches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Modern_Spanish_100_CI_AI");
        }
    }
}
