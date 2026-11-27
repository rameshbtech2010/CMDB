using System.ComponentModel;
using System.Text.Json;
using CmdbAssistant.KnowledgeGraph;
using Microsoft.SemanticKernel;

namespace CmdbAssistant.AI.Plugins;

public sealed class CmdbPlugin(IGraphQueryService graph)
{
    [KernelFunction("impact_analysis"), Description("Lists business and application services affected if a CI fails.")]
    public Task<string> GetImpactedServices([Description("Exact CI name")] string ci) =>
        Run("impact-analysis", ("ci", ci));

    [KernelFunction("technology_stack"), Description("Returns the full technology stack beneath an application service.")]
    public Task<string> GetServiceStack([Description("Exact application service name")] string service) =>
        Run("technology-stack", ("service", service));

    [KernelFunction("relationship_path"), Description("Shows how a service is connected to a configuration item.")]
    public Task<string> GetRelationshipPath(
        [Description("Exact service name")] string service,
        [Description("Exact CI name")] string ci) =>
        Run("relationship-path", ("service", service), ("ci", ci));

    [KernelFunction("hypervisor_hosting"), Description("Lists the hypervisor host for every application server.")]
    public Task<string> GetHypervisorHosts() => Run("hypervisor-hosting");

    [KernelFunction("service_ownership_trace"), Description("Traces each business service through its application, application service, and owner.")]
    public Task<string> GetServiceOwnershipTrace() => Run("service-ownership-trace");

    [KernelFunction("applications_without_service"), Description("Finds business applications with no application service.")]
    public Task<string> GetApplicationsWithoutService() => Run("applications-without-service");

    [KernelFunction("critical_infrastructure_count"), Description("Counts infrastructure CIs beneath each business-critical service.")]
    public Task<string> GetCriticalInfrastructureCounts() => Run("critical-infrastructure-count");

    [KernelFunction("pager_routing"), Description("Finds the on-call group and people paged for a service.")]
    public Task<string> GetPagerRouting([Description("Exact service name")] string service) =>
        Run("pager-routing", ("service", service));

    [KernelFunction("person_portfolio"), Description("Lists the CIs a person owns, manages, or supports.")]
    public Task<string> GetPersonPortfolio([Description("Exact person name")] string person) =>
        Run("person-portfolio", ("person", person));

    [KernelFunction("support_group_inventory"), Description("Counts configuration items by support group and CI class.")]
    public Task<string> GetSupportGroupInventory() => Run("support-group-inventory");

    [KernelFunction("single_points_of_failure"), Description("Identifies infrastructure components with no modeled redundancy.")]
    public Task<string> GetSinglePointsOfFailure() => Run("single-points-of-failure");

    [KernelFunction("service_locations"), Description("Lists the locations spanned by each service stack.")]
    public Task<string> GetServiceLocations() => Run("service-locations");

    [KernelFunction("non_operational_dependencies"), Description("Finds live services depending on non-operational configuration items.")]
    public Task<string> GetNonOperationalDependencies() => Run("non-operational-dependencies");

    [KernelFunction("server_operating_systems"), Description("Lists server operating systems and versions.")]
    public Task<string> GetServerOperatingSystems() => Run("server-operating-systems");

    [KernelFunction("run_read_only_cypher"), Description("Fallback only: runs a bounded read-only Cypher query when no curated CMDB tool can answer.")]
    public async Task<string> RunReadOnlyCypher([Description("A single read-only Cypher query")] string cypher)
    {
        var rows = await graph.RunReadOnlyCypherAsync(cypher);
        return JsonSerializer.Serialize(rows);
    }

    private async Task<string> Run(string name, params (string Key, object? Value)[] parameters)
    {
        var values = parameters.ToDictionary(item => item.Key, item => item.Value);
        var rows = await graph.QueryAsync(new GraphQuery(name, values));
        return JsonSerializer.Serialize(rows);
    }
}