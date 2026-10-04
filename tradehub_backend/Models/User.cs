
namespace TradeHub.Models;

public class User
{
    
public Guid Id {get;set;}
public string Name {get;set;} = string.Empty;
public string UniqueName { get; set; } = string.Empty;
public string Password {get;set;} = string.Empty;
public string? Avatar { get; set; } = string.Empty;
public string? Location { get; set; } = string.Empty;
public string? PaymentNumber { get; set; } = string.Empty;
public string? Email { get; set; } = string.Empty;
public string? Phone { get; set; } = string.Empty;
public string? NecessaryInfo { get; set; } = string.Empty;


}
