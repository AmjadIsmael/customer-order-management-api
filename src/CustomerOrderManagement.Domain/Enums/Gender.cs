namespace CustomerOrderManagement.Domain.Enums;

/// <summary>
/// A customer's gender. Serialized as its underlying integer value
/// (Unspecified = 0, Male = 1, Female = 2, Other = 3, PreferNotToSay = 4).
/// </summary>
public enum Gender
{
    Unspecified = 0,
    Male = 1,
    Female = 2,
    Other = 3,
    PreferNotToSay = 4
}
