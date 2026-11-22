namespace Pharmacy.Core.Auth;

public sealed record PermissionDefinition(string Code, string Name, string Module, string Description);

/// <summary>
/// Catalog of the permissions known by the application. Each module adds its own permissions here;
/// a new EF migration then seeds them into the database.
/// </summary>
public static class Permissions
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";
    public const string BranchesView = "branches.view";
    public const string BranchesManage = "branches.manage";
    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";
    public const string CatalogView = "catalog.view";
    public const string CatalogManage = "catalog.manage";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(UsersView, "Ver usuarios", "Usuarios", "Consultar el listado y el detalle de usuarios."),
        new(UsersManage, "Administrar usuarios", "Usuarios", "Crear, editar, activar/desactivar usuarios y restablecer contraseñas."),
        new(RolesView, "Ver roles", "Roles", "Consultar roles y sus permisos."),
        new(RolesManage, "Administrar roles", "Roles", "Crear, editar, activar/desactivar y eliminar roles."),
        new(BranchesView, "Ver sucursales", "Sucursales", "Consultar el catálogo de sucursales."),
        new(BranchesManage, "Administrar sucursales", "Sucursales", "Crear, editar y activar/desactivar sucursales."),
        new(SettingsView, "Ver configuración", "Configuración", "Consultar las formas de pago."),
        new(SettingsManage, "Administrar configuración", "Configuración", "Editar los datos y el logo de la empresa, y administrar las formas de pago."),
        new(CatalogView, "Ver catálogo", "Catálogo", "Consultar las categorías del catálogo de productos."),
        new(CatalogManage, "Administrar catálogo", "Catálogo", "Crear, editar y activar/desactivar categorías del catálogo de productos."),
    ];

    public static IReadOnlySet<string> AllCodes { get; } = All.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
}
