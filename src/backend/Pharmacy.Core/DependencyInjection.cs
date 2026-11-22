using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Products;
using Pharmacy.Core.Purchases;
using Pharmacy.Core.Roles;
using Pharmacy.Core.Settings;
using Pharmacy.Core.Suppliers;
using Pharmacy.Core.Users;

namespace Pharmacy.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LoginLockoutOptions>(configuration.GetSection(LoginLockoutOptions.SectionName));
        services.Configure<PasswordPolicyOptions>(configuration.GetSection(PasswordPolicyOptions.SectionName));
        services.Configure<BusinessOptions>(configuration.GetSection(BusinessOptions.SectionName));

        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<PasswordPolicy>();

        services.AddScoped<UserAccessService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<RoleService>();
        services.AddScoped<BranchService>();
        services.AddScoped<CompanySettingsService>();
        services.AddScoped<PaymentMethodService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<SupplierService>();
        services.AddScoped<ProductService>();
        services.AddScoped<PurchaseService>();

        return services;
    }
}
