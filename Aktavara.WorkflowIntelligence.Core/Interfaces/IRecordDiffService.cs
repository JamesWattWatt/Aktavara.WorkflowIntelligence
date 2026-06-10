using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

/// <summary>
/// Service for computing attribute-level differences between record snapshots.
/// Compares before and after states to identify what changed.
/// </summary>
public interface IRecordDiffService
{
    /// <summary>
    /// Computes the differences between before and after record snapshots.
    /// </summary>
    /// <param name="before">The record state before changes.</param>
    /// <param name="after">The record state after changes.</param>
    /// <returns>A read-only list of changed attributes. Empty if no changes detected.</returns>
    IReadOnlyList<ChangedAttribute> Diff(AktaRecordSnapshot before, AktaRecordSnapshot after);

    /// <summary>
    /// Computes the differences using custom options.
    /// </summary>
    /// <param name="before">The record state before changes.</param>
    /// <param name="after">The record state after changes.</param>
    /// <param name="options">Configuration for diffing behavior.</param>
    /// <returns>A read-only list of changed attributes. Empty if no changes detected.</returns>
    IReadOnlyList<ChangedAttribute> Diff(AktaRecordSnapshot before, AktaRecordSnapshot after, DiffOptions options);
}
