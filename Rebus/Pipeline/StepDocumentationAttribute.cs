using System;

namespace Rebus.Pipeline;

/// <summary>
/// Documents the purpose of an <see cref="IIncomingStep"/> or <see cref="IOutgoingStep"/> which can then be used by tools to generate nice docs
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StepDocumentationAttribute : Attribute
{
    /// <summary>
    /// Creates the attribute with the given documentation text. Will be included in the output
    /// when logging the message pipelines at startup, which is done by calling
    /// <code>.Options(o => o.LogPipeline(verbose: true|false))</code>
    /// </summary>
    public StepDocumentationAttribute(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Gets the documentation text
    /// </summary>
    public string Text { get; private set; }
}