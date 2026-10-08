namespace TypedPrompt;

/// <summary>Who a rendered message is from.</summary>
public enum PromptRole
{
    /// <summary>Instructions for the model: the file's <c>system</c> text.</summary>
    System,

    /// <summary>The request: the file's <c>user</c> text, or the user side of an example.</summary>
    User,

    /// <summary>A model answer: the assistant side of an example from <c>[[examples]]</c>.</summary>
    Assistant,
}
