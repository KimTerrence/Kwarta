namespace Kwarta.Models;

/// <summary>A table row with an auto-incremented id; zero until it is first saved.</summary>
public interface IEntity
{
    int Id { get; set; }
}
