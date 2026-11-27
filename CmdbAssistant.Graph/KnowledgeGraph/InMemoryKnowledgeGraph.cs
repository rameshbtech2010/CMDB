using CmdbAssistant.Models;

namespace CmdbAssistant.KnowledgeGraph;

public sealed class InMemoryKnowledgeGraph : IKnowledgeGraph
{
    public IReadOnlyList<CiNode> Nodes { get; } =
    [
        new("svc-orders", "Order Fulfillment", "BusinessService", "Operational", true, true, SupportGroupId: "group-platform"),
        new("svc-payments", "Card Payments", "BusinessService", "Operational", true, true, SupportGroupId: "group-payments"),
        new("svc-customer", "Customer Portal", "BusinessService", "Operational", true, false, SupportGroupId: "group-platform"),
        new("app-order", "Order Management", "BusinessApplication", "Operational", SupportGroupId: "group-platform"),
        new("app-payments", "Payment Hub", "BusinessApplication", "Operational", SupportGroupId: "group-payments"),
        new("app-portal", "Customer Web", "BusinessApplication", "Operational", SupportGroupId: "group-platform"),
        new("app-legacy", "Legacy Claims", "BusinessApplication", "Operational", SupportGroupId: "group-platform"),
        new("as-order", "Order API Service", "ApplicationService", "Operational", true, SupportGroupId: "group-platform"),
        new("as-payments", "Payment Processing Service", "ApplicationService", "Operational", true, SupportGroupId: "group-payments"),
        new("as-portal", "Customer Portal Service", "ApplicationService", "Degraded", true, SupportGroupId: "group-platform"),
        new("server-order-01", "order-api-01", "ApplicationServer", "Operational", true, SupportGroupId: "group-platform", OperatingSystem: "Ubuntu", OsVersion: "22.04 LTS"),
        new("server-order-02", "order-api-02", "ApplicationServer", "Operational", true, SupportGroupId: "group-platform", OperatingSystem: "Ubuntu", OsVersion: "22.04 LTS"),
        new("server-payments-01", "payments-api-01", "ApplicationServer", "Operational", true, SupportGroupId: "group-payments", OperatingSystem: "Red Hat Enterprise Linux", OsVersion: "9.3"),
        new("server-portal-01", "portal-web-01", "ApplicationServer", "Operational", true, SupportGroupId: "group-platform", OperatingSystem: "Windows Server", OsVersion: "2022"),
        new("db-orders", "orders-db-primary", "Database", "Degraded", true, SupportGroupId: "group-platform", IsSinglePointOfFailure: true),
        new("db-payments", "payments-db-primary", "Database", "Operational", true, SupportGroupId: "group-payments", IsSinglePointOfFailure: true),
        new("db-customer", "customer-db", "Database", "Operational", true, SupportGroupId: "group-platform"),
        new("lb-orders", "orders-load-balancer", "LoadBalancer", "Operational", true, SupportGroupId: "group-platform"),
        new("storage-orders", "orders-storage", "Storage", "Operational", true, SupportGroupId: "group-platform"),
        new("hv-east", "hv-prod-east-01", "Hypervisor", "Operational", true, SupportGroupId: "group-compute"),
        new("hv-west", "hv-prod-west-01", "Hypervisor", "Operational", true, SupportGroupId: "group-compute"),
        new("loc-east", "East US", "Location", "Operational"),
        new("loc-west", "West US", "Location", "Operational"),
        new("person-aditya", "Aditya Rao", "Person", "Active"),
        new("person-nina", "Nina Patel", "Person", "Active"),
        new("person-oncall", "Platform On-call", "Person", "Active"),
        new("group-platform", "Cloud Platform Support", "SupportGroup", "Active"),
        new("group-payments", "Payments Support", "SupportGroup", "Active"),
        new("group-compute", "Compute Operations", "SupportGroup", "Active")
    ];

    public IReadOnlyList<CiRelationship> Relationships { get; } =
    [
        new("svc-orders", "as-order", "RealizedBy"),
        new("svc-payments", "as-payments", "RealizedBy"),
        new("svc-customer", "as-portal", "RealizedBy"),
        new("as-order", "app-order", "Supports"),
        new("as-payments", "app-payments", "Supports"),
        new("as-portal", "app-portal", "Supports"),
        new("svc-orders", "person-aditya", "OwnedBy"),
        new("svc-payments", "person-nina", "OwnedBy"),
        new("svc-orders", "person-oncall", "PagedTo"),
        new("svc-payments", "person-oncall", "PagedTo"),
        new("svc-customer", "person-oncall", "PagedTo"),
        new("as-order", "server-order-01", "DependsOn"),
        new("as-order", "server-order-02", "DependsOn"),
        new("as-order", "db-orders", "DependsOn"),
        new("as-order", "lb-orders", "DependsOn"),
        new("as-payments", "server-payments-01", "DependsOn"),
        new("as-payments", "db-payments", "DependsOn"),
        new("as-portal", "server-portal-01", "DependsOn"),
        new("as-portal", "db-customer", "DependsOn"),
        new("server-order-01", "hv-east", "HostedOn"),
        new("server-order-02", "hv-west", "HostedOn"),
        new("server-payments-01", "hv-east", "HostedOn"),
        new("server-portal-01", "hv-west", "HostedOn"),
        new("db-orders", "storage-orders", "StoredOn"),
        new("server-order-01", "loc-east", "LocatedAt"),
        new("server-order-02", "loc-west", "LocatedAt"),
        new("server-payments-01", "loc-east", "LocatedAt"),
        new("server-portal-01", "loc-west", "LocatedAt"),
        new("person-aditya", "svc-orders", "Manages"),
        new("person-nina", "svc-payments", "Supports")
    ];
}