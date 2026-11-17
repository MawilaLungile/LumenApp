using SQLite;

namespace Lumen.Models;

/// <summary>A monthly spending limit for one category.</summary>
[Table("Budgets")]
public class Budget
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string CategoryId { get; set; } = "food";

    public double Limit { get; set; }
}
