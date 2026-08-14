namespace Codify.Infrastructure.AI;

/// <summary>
/// Connection settings for Chroma Cloud. All values come from configuration
/// (appsettings / environment variables) — nothing is hardcoded here.
///
/// Region endpoints:
///   AWS us-east-1 (default): https://api.trychroma.com
///   GCP europe-west1:        https://europe-west1.gcp.trychroma.com
/// </summary>
public class ChromaCloudOptions
{
    public const string SectionName = "ChromaCloud";

    /// <summary>Base endpoint of the Chroma Cloud region, e.g. https://api.trychroma.com</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Chroma Cloud API key. Sent as a Bearer token.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Chroma tenant (from the Cloud dashboard / identity).</summary>
    public string Tenant { get; set; } = "default_tenant";

    /// <summary>Chroma database name.</summary>
    public string Database { get; set; } = "default_database";

    /// <summary>Name of the collection that stores the Codify knowledge base.</summary>
    public string CollectionName { get; set; } = "codify-knowledge-base";

    /// <summary>
    /// Minimum normalized similarity (0..1) for a hit to be considered relevant.
    /// Hits below this threshold are discarded to avoid injecting noise.
    /// </summary>
    public float SimilarityThreshold { get; set; } = 0.25f;

    /// <summary>Request timeout for Chroma Cloud calls.</summary>
    public int TimeoutSeconds { get; set; } = 20;
}
