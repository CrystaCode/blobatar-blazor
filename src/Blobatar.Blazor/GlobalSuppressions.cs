using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Scope = "type",
    Target = "~T:Blobatar.Blazor.Blobatar",
    Justification = "Every upstream adapter names its component <Blobatar>; the tag is the product's name and consumers expect it.")]
