namespace dotnet_backend_2.Data.Entities;

/// <summary>
/// Explicit node type, chosen by the admin when the category is created.
/// Stored - not derived from structure - so an empty BRANCH can exist
/// before its children are added.
/// </summary>
public enum CategoryType
{
    /// <summary>Holds products; rendered as a clickable link. Cannot have subcategories.</summary>
    Leaf = 0,

    /// <summary>Container for subcategories; rendered as an expandable group. Cannot hold products.</summary>
    Branch = 1
}
