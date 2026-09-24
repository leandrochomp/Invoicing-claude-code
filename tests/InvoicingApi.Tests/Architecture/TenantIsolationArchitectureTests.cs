using System.Text.RegularExpressions;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Configuration;
using Shared.Entities;
using Shouldly;

namespace InvoicingApi.Tests.Architecture;

// Guards the rules that keep tenant isolation from being bypassed silently as the codebase grows.
public partial class TenantIsolationArchitectureTests
{
    // Admin handlers (issue #53) are the only code allowed to lift the tenant filter.
    private const string AdminFeatureFolder = "Features/Admin/";

    private static IModel CreateModel()
    {
        // Building the model needs no connection; the connection string is never opened.
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql("Host=unused")
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options, TestTenancy.None);
        return context.Model;
    }

    [Fact]
    public void Every_tenant_owned_entity_has_the_tenant_query_filter()
    {
        var model = CreateModel();
        var tenantOwnedTypes = typeof(InvoicingDbContext).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ITenantOwned).IsAssignableFrom(t))
            .ToList();

        tenantOwnedTypes.ShouldNotBeEmpty();
        foreach (var type in tenantOwnedTypes)
        {
            var entityType = model.FindEntityType(type);
            entityType.ShouldNotBeNull($"{type.Name} is ITenantOwned but not mapped.");
            entityType.GetDeclaredQueryFilters().Select(f => f.Key)
                .ShouldContain(TenantQueryFilter.Name, $"{type.Name} has no \"{TenantQueryFilter.Name}\" query filter.");
        }
    }

    [Fact]
    public void Soft_delete_filter_is_named_so_it_can_be_lifted_on_its_own()
    {
        var model = CreateModel();

        foreach (var entityType in model.GetEntityTypes().Where(t => typeof(SoftDeletableEntity).IsAssignableFrom(t.ClrType)))
        {
            entityType.GetDeclaredQueryFilters().Select(f => f.Key)
                .ShouldContain(SoftDeleteQueryFilter.Name, $"{entityType.ClrType.Name} has no named soft-delete filter.");
        }
    }

    [Fact]
    public void Query_filters_are_only_ignored_by_name_outside_the_admin_feature()
    {
        var violations = ApiSourceFiles()
            .Where(file => !file.RelativePath.StartsWith(AdminFeatureFolder, StringComparison.Ordinal))
            .SelectMany(file => file.Lines.Select((line, index) => (file.RelativePath, Line: index + 1, Text: line)))
            .Where(l => BareIgnoreQueryFilters().IsMatch(l.Text) || IgnoresTenantFilter().IsMatch(l.Text))
            .Select(l => $"{l.RelativePath}:{l.Line}: {l.Text.Trim()}")
            .ToList();

        violations.ShouldBeEmpty(
            "A bare IgnoreQueryFilters() also drops the tenant filter, and only /admin handlers may ignore the tenant filter. "
            + "Ignore a filter by name instead, for example IgnoreQueryFilters([SoftDeleteQueryFilter.Name]).");
    }

    [Fact]
    public void Source_scan_finds_the_api_code()
    {
        // Proves the scan above isn't passing vacuously because it read no files.
        ApiSourceFiles().ShouldContain(f => f.RelativePath == "Features/Invoices/PaymentQueries.cs");
    }

    private static IEnumerable<(string RelativePath, string[] Lines)> ApiSourceFiles()
    {
        var apiRoot = Path.Combine(FindRepositoryRoot(), "src", "Api", "InvoicingApi");

        return Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => (RelativePath: Path.GetRelativePath(apiRoot, path).Replace('\\', '/'), Path: path))
            .Where(f => !f.RelativePath.StartsWith("bin/", StringComparison.Ordinal)
                && !f.RelativePath.StartsWith("obj/", StringComparison.Ordinal)
                && !f.RelativePath.StartsWith("Migrations/", StringComparison.Ordinal))
            .Select(f => (f.RelativePath, File.ReadAllLines(f.Path)));
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Invoicing.Claude.Code.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the repository root (Invoicing.Claude.Code.slnx).");
    }

    [GeneratedRegex(@"IgnoreQueryFilters\s*\(\s*\)")]
    private static partial Regex BareIgnoreQueryFilters();

    [GeneratedRegex(@"IgnoreQueryFilters\s*\(.*(TenantQueryFilter|""Tenant"")")]
    private static partial Regex IgnoresTenantFilter();
}
