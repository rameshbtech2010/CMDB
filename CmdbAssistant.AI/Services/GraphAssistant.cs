using CmdbAssistant.KnowledgeGraph;
using CmdbAssistant.Models;
using CmdbAssistant.Core.Services;

namespace CmdbAssistant.AI.Services;

public sealed class GraphAssistant(IKnowledgeGraph graph) : ICmdbChatAssistant
{
    private static readonly HashSet<string> TraversalRelationships =
        ["RealizedBy", "Supports", "DependsOn", "HostedOn", "StoredOn", "LocatedAt"];

    public ChatResponse Ask(string question)
    {
        var intent = Classify(question);
        var mentioned = FindMentionedNodes(question);
        var evidence = new List<CiNode>();
        var recommendations = new List<string>();
        string answer;
        double confidence = 0.9;

        switch (intent)
        {
            case "impact-analysis":
                answer = AnswerImpact(mentioned.FirstOrDefault(), evidence, recommendations);
                break;
            case "technology-stack":
                answer = AnswerStack(mentioned.FirstOrDefault(), evidence);
                break;
            case "relationship-path":
                answer = AnswerRelationships(mentioned, evidence);
                break;
            case "hypervisor-hosting":
                answer = AnswerHypervisors(evidence);
                break;
            case "service-ownership-trace":
                answer = AnswerServiceTrace(evidence);
                break;
            case "applications-without-service":
                answer = AnswerApplicationsWithoutService(evidence);
                break;
            case "critical-infrastructure-count":
                answer = AnswerInfrastructureCounts(evidence);
                break;
            case "pager-routing":
                answer = AnswerPagerRouting(mentioned.FirstOrDefault(), evidence);
                break;
            case "person-portfolio":
                answer = AnswerPersonPortfolio(mentioned.FirstOrDefault(), evidence);
                break;
            case "support-group-inventory":
                answer = AnswerSupportGroupInventory(evidence);
                break;
            case "single-points-of-failure":
                answer = AnswerSinglePointsOfFailure(evidence, recommendations);
                break;
            case "service-locations":
                answer = AnswerServiceLocations(evidence);
                break;
            case "non-operational-dependencies":
                answer = AnswerNonOperationalDependencies(evidence, recommendations);
                break;
            case "server-operating-systems":
                answer = AnswerOperatingSystems(evidence);
                break;
            default:
                answer = "I can answer CMDB questions about service impact, technology stacks, relationships, hosting, ownership, support, risk, locations, and operating systems. Try naming a CI or service in your question.";
                confidence = 0.2;
                break;
        }

        if (answer.StartsWith("Which CI", StringComparison.Ordinal))
        {
            confidence = 0.35;
        }

        return new ChatResponse(
            answer,
            intent,
            confidence,
            evidence.DistinctBy(node => node.Id)
                .Select(node => new EvidenceResponse(node.Id, node.Name, node.CiClass, node.Status))
                .ToArray(),
            recommendations,
            DateTimeOffset.UtcNow);
    }

    public Task<ChatResponse> AskAsync(string question, CancellationToken cancellationToken = default) =>
        Task.FromResult(Ask(question));

    public IReadOnlyList<InfrastructureInsight> GetInsights()
    {
        var insights = new List<InfrastructureInsight>();
        var singlePoints = graph.Nodes.Where(node => node.IsSinglePointOfFailure).ToArray();
        foreach (var node in singlePoints)
        {
            var affected = FindAncestors(node.Id)
                .Where(candidate => candidate.CiClass == "BusinessService")
                .Select(candidate => candidate.Name)
                .ToArray();
            insights.Add(new InfrastructureInsight(
                "high",
                $"Single point of failure: {node.Name}",
                $"{node.Name} has no modeled redundant counterpart.",
                affected,
                "Add a redundant instance or validate the recovery design."));
        }

        foreach (var node in graph.Nodes.Where(candidate => candidate.Status is "Degraded" or "Down"))
        {
            var affected = FindAncestors(node.Id)
                .Where(candidate => candidate.IsLive)
                .Select(candidate => candidate.Name)
                .Distinct()
                .ToArray();
            if (affected.Length == 0)
            {
                continue;
            }

            insights.Add(new InfrastructureInsight(
                node.Status == "Down" ? "critical" : "medium",
                $"{node.Status} dependency: {node.Name}",
                $"Live services depend on {node.Name}, which is {node.Status.ToLowerInvariant()}.",
                affected,
                "Review the dependency health and confirm the service owner has an active remediation plan."));
        }

        return insights;
    }

    private string AnswerImpact(CiNode? target, List<CiNode> evidence, List<string> recommendations)
    {
        if (target is null)
        {
            return "Which CI should I check for impact? Include its name, for example: 'If orders-db-primary fails, which services are affected?'";
        }

        evidence.Add(target);
        var impacted = FindAncestors(target.Id)
            .Where(node => node.CiClass is "BusinessService" or "ApplicationService")
            .ToArray();
        evidence.AddRange(impacted);
        if (impacted.Length == 0)
        {
            return $"No dependent application or business services are modeled for {target.Name}.";
        }

        recommendations.Add("Validate the dependency edges and confirm the listed service owners are notified.");
        return $"If {target.Name} fails, the modeled affected services are {JoinNames(impacted)}.";
    }

    private string AnswerStack(CiNode? service, List<CiNode> evidence)
    {
        if (service is null)
        {
            return "Which service should I trace? Include a service or CI name, for example: 'Show the full technology stack beneath Order Fulfillment.'";
        }

        var stack = FindDescendants(service.Id);
        evidence.Add(service);
        evidence.AddRange(stack);
        return stack.Count == 0
            ? $"No downstream technology stack is modeled for {service.Name}."
            : $"The modeled stack beneath {service.Name} is {JoinNames(stack)}.";
    }

    private string AnswerRelationships(IReadOnlyList<CiNode> mentioned, List<CiNode> evidence)
    {
        if (mentioned.Count == 0)
        {
            return "Which service and CI should I trace? Include both names so I can show the connecting relationships.";
        }

        var source = mentioned[0];
        evidence.Add(source);
        if (mentioned.Count > 1)
        {
            var target = mentioned[1];
            evidence.Add(target);
            var path = FindPath(source.Id, target.Id);
            if (path.Count == 0)
            {
                return $"No directed dependency path is modeled from {source.Name} to {target.Name}.";
            }

            evidence.AddRange(path.SelectMany(step => new[] { step.Source, step.Target }));
            return $"{source.Name} connects to {target.Name} through {string.Join("; ", path.Select(step => $"{step.Source.Name} -[{step.Relationship.Type}]-> {step.Target.Name}"))}.";
        }

        var connections = graph.Relationships
            .Where(edge => edge.SourceId == source.Id || edge.TargetId == source.Id)
            .Select(edge => (Edge: edge, Other: GetNode(edge.SourceId == source.Id ? edge.TargetId : edge.SourceId)))
            .Where(item => item.Other is not null)
            .ToArray();
        evidence.AddRange(connections.Select(item => item.Other!));
        return connections.Length == 0
            ? $"No relationships are modeled for {source.Name}."
            : $"{source.Name} is connected by {string.Join("; ", connections.Select(item => $"{item.Edge.Type} {item.Other!.Name}"))}.";
    }

    private string AnswerHypervisors(List<CiNode> evidence)
    {
        var servers = graph.Nodes.Where(node => node.CiClass == "ApplicationServer").ToArray();
        evidence.AddRange(servers);
        var rows = new List<string>();
        foreach (var server in servers)
        {
            var host = graph.Relationships
                .Where(edge => edge.SourceId == server.Id && edge.Type == "HostedOn")
                .Select(edge => GetNode(edge.TargetId))
                .FirstOrDefault(node => node is not null);
            if (host is not null)
            {
                evidence.Add(host);
                rows.Add($"{server.Name} runs on {host.Name}");
            }
        }

        return rows.Count == 0 ? "No application-server-to-hypervisor relationships are modeled." : string.Join("; ", rows) + ".";
    }

    private string AnswerServiceTrace(List<CiNode> evidence)
    {
        var services = graph.Nodes.Where(node => node.CiClass == "BusinessService").ToArray();
        foreach (var service in services)
        {
            var related = FindDescendants(service.Id);
            var owner = graph.Relationships
                .Where(edge => edge.SourceId == service.Id && edge.Type == "OwnedBy")
                .Select(edge => GetNode(edge.TargetId))
                .FirstOrDefault(node => node is not null);
            evidence.Add(service);
            evidence.AddRange(related.Where(node => node.CiClass is "ApplicationService" or "BusinessApplication"));
            if (owner is not null)
            {
                evidence.Add(owner);
            }
        }

        return string.Join("; ", services.Select(service =>
        {
            var descendants = FindDescendants(service.Id);
            var appServices = descendants.Where(node => node.CiClass == "ApplicationService").ToArray();
            var applications = descendants.Where(node => node.CiClass == "BusinessApplication").ToArray();
            var owner = graph.Relationships
                .Where(edge => edge.SourceId == service.Id && edge.Type == "OwnedBy")
                .Select(edge => GetNode(edge.TargetId)?.Name)
                .FirstOrDefault(name => name is not null) ?? "owner not recorded";
            return $"{service.Name} -> {(appServices.Length == 0 ? "no application service" : JoinNames(appServices))} -> {(applications.Length == 0 ? "no business application" : JoinNames(applications))}; owner: {owner}";
        })) + ".";
    }

    private string AnswerApplicationsWithoutService(List<CiNode> evidence)
    {
        var applications = graph.Nodes.Where(node => node.CiClass == "BusinessApplication").ToArray();
        var withoutService = applications.Where(application =>
            !graph.Relationships.Any(edge => edge.TargetId == application.Id && edge.Type == "Supports")).ToArray();
        evidence.AddRange(withoutService);
        return withoutService.Length == 0
            ? "Every modeled business application has an application service."
            : $"Business applications without a modeled application service: {JoinNames(withoutService)}.";
    }

    private string AnswerInfrastructureCounts(List<CiNode> evidence)
    {
        var services = graph.Nodes.Where(node => node.CiClass == "BusinessService" && node.IsBusinessCritical).ToArray();
        var summaries = services.Select(service =>
        {
            var infrastructure = FindDescendants(service.Id)
                .Where(node => node.CiClass is "ApplicationServer" or "Database" or "LoadBalancer" or "Storage" or "Hypervisor")
                .ToArray();
            evidence.Add(service);
            evidence.AddRange(infrastructure);
            return $"{service.Name}: {infrastructure.Length} infrastructure CIs";
        });
        return string.Join("; ", summaries) + ".";
    }

    private string AnswerPagerRouting(CiNode? service, List<CiNode> evidence)
    {
        var services = service is null
            ? graph.Nodes.Where(node => node.CiClass == "BusinessService").ToArray()
            : [service];
        var results = new List<string>();
        foreach (var item in services)
        {
            evidence.Add(item);
            var pagers = graph.Relationships
                .Where(edge => edge.SourceId == item.Id && edge.Type == "PagedTo")
                .Select(edge => GetNode(edge.TargetId))
                .Where(node => node is not null)
                .ToArray();
            evidence.AddRange(pagers!);
            results.Add($"{item.Name}: {(pagers.Length == 0 ? "no pager is recorded" : JoinNames(pagers!))}");
        }

        return string.Join("; ", results) + ".";
    }

    private string AnswerPersonPortfolio(CiNode? person, List<CiNode> evidence)
    {
        if (person is null || person.CiClass != "Person")
        {
            return "Which person should I look up? Include their name, for example: 'What does Aditya Rao own, manage, or support?'";
        }

        evidence.Add(person);
        var links = graph.Relationships
            .Where(edge => edge.SourceId == person.Id || edge.TargetId == person.Id)
            .Select(edge => (Edge: edge, Other: GetNode(edge.SourceId == person.Id ? edge.TargetId : edge.SourceId)))
            .Where(item => item.Other is not null)
            .ToArray();
        evidence.AddRange(links.Select(item => item.Other!));
        return links.Length == 0
            ? $"No ownership, management, or support relationships are modeled for {person.Name}."
            : $"{person.Name}: {string.Join("; ", links.Select(item => $"{item.Edge.Type} {item.Other!.Name}"))}.";
    }

    private string AnswerSupportGroupInventory(List<CiNode> evidence)
    {
        var groups = graph.Nodes.Where(node => node.CiClass == "SupportGroup").ToArray();
        var lines = groups.Select(group =>
        {
            var inventory = graph.Nodes.Where(node => node.SupportGroupId == group.Id).ToArray();
            evidence.Add(group);
            evidence.AddRange(inventory);
            var byClass = inventory.GroupBy(node => node.CiClass)
                .OrderBy(item => item.Key)
                .Select(item => $"{item.Key} {item.Count()}");
            return $"{group.Name}: {(inventory.Length == 0 ? "no assigned CIs" : string.Join(", ", byClass))}";
        });
        return string.Join("; ", lines) + ".";
    }

    private string AnswerSinglePointsOfFailure(List<CiNode> evidence, List<string> recommendations)
    {
        var nodes = graph.Nodes.Where(node => node.IsSinglePointOfFailure).ToArray();
        evidence.AddRange(nodes);
        recommendations.AddRange(nodes.Select(node => $"Review redundancy or recovery for {node.Name}."));
        return nodes.Length == 0
            ? "No single points of failure are flagged in the current graph."
            : $"Flagged single points of failure: {JoinNames(nodes)}.";
    }

    private string AnswerServiceLocations(List<CiNode> evidence)
    {
        var services = graph.Nodes.Where(node => node.CiClass == "BusinessService").ToArray();
        var lines = services.Select(service =>
        {
            var locations = FindDescendants(service.Id).Where(node => node.CiClass == "Location").ToArray();
            evidence.Add(service);
            evidence.AddRange(locations);
            return $"{service.Name}: {(locations.Length == 0 ? "no location is modeled" : JoinNames(locations))}";
        });
        return string.Join("; ", lines) + ".";
    }

    private string AnswerNonOperationalDependencies(List<CiNode> evidence, List<string> recommendations)
    {
        var results = new List<string>();
        foreach (var service in graph.Nodes.Where(node => node.CiClass == "BusinessService" && node.IsLive))
        {
            var unhealthy = FindDescendants(service.Id)
                .Where(node => node.Status is "Degraded" or "Down")
                .ToArray();
            if (unhealthy.Length == 0)
            {
                continue;
            }

            evidence.Add(service);
            evidence.AddRange(unhealthy);
            recommendations.Add($"Review {JoinNames(unhealthy)} for {service.Name} and notify its owner.");
            results.Add($"{service.Name} depends on {JoinNames(unhealthy)}");
        }

        return results.Count == 0
            ? "No live business services have a degraded or down dependency in the current graph."
            : string.Join("; ", results) + ".";
    }

    private string AnswerOperatingSystems(List<CiNode> evidence)
    {
        var servers = graph.Nodes.Where(node => node.CiClass == "ApplicationServer").ToArray();
        evidence.AddRange(servers);
        return string.Join("; ", servers.Select(server =>
            $"{server.Name}: {server.OperatingSystem ?? "OS unknown"} {server.OsVersion ?? "version unknown"}")) + ".";
    }

    private List<CiNode> FindDescendants(string nodeId)
    {
        var found = new Dictionary<string, CiNode>();
        var visited = new HashSet<string> { nodeId };
        var queue = new Queue<string>();
        queue.Enqueue(nodeId);
        while (queue.TryDequeue(out var current))
        {
            foreach (var edge in graph.Relationships.Where(edge => edge.SourceId == current && TraversalRelationships.Contains(edge.Type)))
            {
                if (!visited.Add(edge.TargetId))
                {
                    continue;
                }

                var node = GetNode(edge.TargetId);
                if (node is not null)
                {
                    found[node.Id] = node;
                    queue.Enqueue(node.Id);
                }
            }
        }

        return found.Values.ToList();
    }

    private List<CiNode> FindAncestors(string nodeId)
    {
        var found = new Dictionary<string, CiNode>();
        var visited = new HashSet<string> { nodeId };
        var queue = new Queue<string>();
        queue.Enqueue(nodeId);
        while (queue.TryDequeue(out var current))
        {
            foreach (var edge in graph.Relationships.Where(edge => edge.TargetId == current && TraversalRelationships.Contains(edge.Type)))
            {
                if (!visited.Add(edge.SourceId))
                {
                    continue;
                }

                var node = GetNode(edge.SourceId);
                if (node is not null)
                {
                    found[node.Id] = node;
                    queue.Enqueue(node.Id);
                }
            }
        }

        return found.Values.ToList();
    }

    private List<(CiNode Source, CiNode Target, CiRelationship Relationship)> FindPath(string sourceId, string targetId)
    {
        var queue = new Queue<string>();
        var previous = new Dictionary<string, (string Parent, CiRelationship Edge)>();
        var visited = new HashSet<string> { sourceId };
        queue.Enqueue(sourceId);
        while (queue.TryDequeue(out var current) && current != targetId)
        {
            foreach (var edge in graph.Relationships.Where(edge => edge.SourceId == current && TraversalRelationships.Contains(edge.Type)))
            {
                if (visited.Add(edge.TargetId))
                {
                    previous[edge.TargetId] = (current, edge);
                    queue.Enqueue(edge.TargetId);
                }
            }
        }

        if (!visited.Contains(targetId))
        {
            return [];
        }

        var path = new List<(CiNode, CiNode, CiRelationship)>();
        var cursor = targetId;
        while (cursor != sourceId)
        {
            var step = previous[cursor];
            path.Add((GetNode(step.Parent)!, GetNode(cursor)!, step.Edge));
            cursor = step.Parent;
        }

        path.Reverse();
        return path;
    }

    private List<CiNode> FindMentionedNodes(string question) => graph.Nodes
        .Where(node => question.Contains(node.Name, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(node => node.Name.Length)
        .ToList();

    private CiNode? GetNode(string id) => graph.Nodes.FirstOrDefault(node => node.Id == id);

    private static string JoinNames(IEnumerable<CiNode> nodes) => string.Join(", ", nodes.Select(node => node.Name));

    private static string Classify(string question)
    {
        var text = question.ToLowerInvariant();
        if (text.Contains("no application service") || text.Contains("without an application service")) return "applications-without-service";
        if (text.Contains("support group") && (text.Contains("how many") || text.Contains("count") || text.Contains("by class"))) return "support-group-inventory";
        if (text.Contains("hypervisor") || text.Contains("host does") || text.Contains("run on")) return "hypervisor-hosting";
        if (text.Contains("single point") || text.Contains("single-point") || text.Contains("spof")) return "single-points-of-failure";
        if (text.Contains("not operational") || text.Contains("degraded") || text.Contains("down dependenc")) return "non-operational-dependencies";
        if (text.Contains("location") || text.Contains("span")) return "service-locations";
        if (text.Contains("operating system") || text.Contains("os version") || text.Contains("what os")) return "server-operating-systems";
        if (text.Contains("paged") || text.Contains("who gets page") || text.Contains("on-call") || text.Contains("on call")) return "pager-routing";
        if (text.Contains("what does") && (text.Contains("own") || text.Contains("manage") || text.Contains("support"))) return "person-portfolio";
        if (text.Contains("business-critical") || text.Contains("business critical") || text.Contains("how much infrastructure")) return "critical-infrastructure-count";
        if (text.Contains("technology stack") || text.Contains("full stack") || text.Contains("beneath")) return "technology-stack";
        if (text.Contains("connected") || text.Contains("connection") || text.Contains("exactly") || text.Contains("relationship")) return "relationship-path";
        if (text.Contains("trace through") || text.Contains("business service") && text.Contains("owner")) return "service-ownership-trace";
        if (text.Contains("fail") || text.Contains("failure") || text.Contains("affected") || text.Contains("impact")) return "impact-analysis";
        return "general";
    }
}