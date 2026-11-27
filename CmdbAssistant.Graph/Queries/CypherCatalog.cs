namespace CmdbAssistant.Graph.Queries;

public static class CypherCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Queries =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["impact-analysis"] = "MATCH (ci {name: $ci})<-[:DEPENDS_ON|RUNS_ON*1..8]-(svc) WHERE svc:ApplicationService OR svc:BusinessService RETURN DISTINCT svc.name AS name, labels(svc)[0] AS class, svc.criticality AS criticality",
            ["technology-stack"] = "MATCH p=(:ApplicationService {name: $service})-[:DEPENDS_ON|RUNS_ON|HAS_SERVICE*1..8]->(ci) RETURN DISTINCT ci.name AS name, labels(ci)[0] AS class, min(length(p)) AS depth ORDER BY depth",
            ["relationship-path"] = "MATCH p=shortestPath((s {name: $service})-[*..10]-(ci {name: $ci})) RETURN [n IN nodes(p) | n.name] AS nodes, [r IN relationships(p) | type(r)] AS relationships",
            ["hypervisor-hosting"] = "MATCH (server:Server)-[:RUNS_ON*1..3]->(host:HypervisorHost) RETURN server.name AS server, host.name AS hypervisor ORDER BY server.name",
            ["service-ownership-trace"] = "MATCH (bs:BusinessService)-[:DEPENDS_ON]->(app:BusinessApplication) OPTIONAL MATCH (app)-[:HAS_SERVICE]->(service:ApplicationService) OPTIONAL MATCH (app)<-[:OWNS]-(owner:Person) RETURN bs.name AS businessService, app.name AS application, collect(DISTINCT service.name) AS applicationServices, owner.name AS owner ORDER BY bs.name",
            ["applications-without-service"] = "MATCH (app:BusinessApplication) WHERE NOT (app)-[:HAS_SERVICE]->(:ApplicationService) RETURN app.name AS application ORDER BY application",
            ["critical-infrastructure-count"] = "MATCH (service:BusinessService {criticality: 'Critical'})-[:DEPENDS_ON|RUNS_ON|HAS_SERVICE*1..8]->(ci) RETURN service.name AS service, count(DISTINCT ci) AS ciCount ORDER BY ciCount DESC",
            ["pager-routing"] = "MATCH (service {name: $service})-[:ON_CALL]->(group:SupportGroup)<-[:MEMBER_OF]-(person:Person) RETURN group.name AS group, collect(person.name) AS people",
            ["person-portfolio"] = "MATCH (person:Person {name: $person})-[relationship:OWNS|MANAGES|SUPPORTED_BY]-(ci) RETURN type(relationship) AS relationship, labels(ci)[0] AS class, collect(ci.name) AS items",
            ["support-group-inventory"] = "MATCH (group:SupportGroup)<-[:SUPPORTED_BY]-(ci) RETURN group.name AS group, labels(ci)[0] AS class, count(ci) AS total ORDER BY group.name, total DESC",
            ["single-points-of-failure"] = "MATCH (service:ApplicationService)-[:DEPENDS_ON|RUNS_ON*1..8]->(ci) WHERE NOT (ci)-[:REDUNDANT_WITH]-() WITH ci, count(DISTINCT service) AS dependents RETURN ci.name AS name, labels(ci)[0] AS class, dependents ORDER BY dependents DESC",
            ["service-locations"] = "MATCH (service:ApplicationService)-[:DEPENDS_ON|RUNS_ON*0..8]->(ci)-[:LOCATED_IN]->(location:Location) RETURN service.name AS service, collect(DISTINCT location.name) AS locations ORDER BY service.name",
            ["non-operational-dependencies"] = "MATCH (service:ApplicationService {operationalStatus: 'Live'})-[:DEPENDS_ON|RUNS_ON*1..8]->(ci) WHERE ci.operationalStatus <> 'Operational' RETURN service.name AS service, ci.name AS ci, ci.operationalStatus AS status ORDER BY service.name",
            ["server-operating-systems"] = "MATCH (server:Server) RETURN server.name AS server, server.os AS os, server.osVersion AS version ORDER BY server.os, server.osVersion, server.name",
            ["insights-single-points"] = "MATCH (service:ApplicationService)-[:DEPENDS_ON|RUNS_ON*1..8]->(ci) WHERE NOT (ci)-[:REDUNDANT_WITH]-() WITH ci, collect(DISTINCT service.name) AS services RETURN ci.name AS name, labels(ci)[0] AS class, services, size(services) AS dependentServices",
            ["insights-non-operational"] = "MATCH (service:ApplicationService)-[:DEPENDS_ON|RUNS_ON*1..8]->(ci) WHERE service.operationalStatus = 'Live' AND ci.operationalStatus <> 'Operational' RETURN service.name AS service, ci.name AS ci, ci.operationalStatus AS status",
            ["insights-orphaned-applications"] = "MATCH (app:BusinessApplication) WHERE NOT (app)-[:HAS_SERVICE]->(:ApplicationService) RETURN app.name AS application",
            ["insights-unsupported-servers"] = "MATCH (server:Server) WHERE server.osEndOfSupport IS NOT NULL AND date(server.osEndOfSupport) < date() RETURN server.name AS server, server.os AS os, server.osVersion AS version, server.osEndOfSupport AS endOfSupport",
            ["insights-unassigned-cis"] = "MATCH (ci) WHERE NOT (ci)-[:SUPPORTED_BY]->(:SupportGroup) AND NOT ci:Person AND NOT ci:SupportGroup AND NOT ci:Location RETURN ci.name AS name, labels(ci)[0] AS class"
        };

    public static bool TryGet(string name, out string cypher) => Queries.TryGetValue(name, out cypher!);
}