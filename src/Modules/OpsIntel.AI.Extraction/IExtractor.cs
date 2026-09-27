using OpsIntel.AI.Extraction.WorkItems;

namespace OpsIntel.AI.Extraction;

/// <summary>
/// Entry point to the quarantined extraction pipeline (ADR-0013/ADR-0015/ADR-0019): a
/// structured-output call to an <c>IChatClient</c> with no tools, followed by deterministic
/// local evidence verification. One method per prompt schema in
/// <c>prompts/extraction/v1/schemas/</c>; <c>work_items</c> is implemented fully for Faz 0 —
/// <c>decisions</c>/<c>risks</c>/<c>phase_signal</c>/<c>project_assignment</c> follow the exact
/// same <see cref="Prompts.PromptSetLoader"/> + <see cref="EvidenceVerificationEngine"/>
/// pattern (see <c>SPIKE-FOUNDRY-LOCAL.md</c> TODOs).
/// </summary>
public interface IExtractor
{
    /// <summary>Runs the <c>work_items</c> schema call (task/commitment/request/follow_up/open_question) against one thread.</summary>
    Task<ExtractionOutcome<RawWorkItem>> ExtractWorkItemsAsync(ExtractionThread thread, CancellationToken cancellationToken = default);
}
