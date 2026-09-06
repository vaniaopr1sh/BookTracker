using System.ComponentModel.DataAnnotations;

namespace BookTracker.Models;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString())[0];
        var attribute = (DisplayAttribute?)Attribute.GetCustomAttribute(member, typeof(DisplayAttribute));
        return attribute?.Name ?? value.ToString();
    }
}
