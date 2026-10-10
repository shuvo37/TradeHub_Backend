namespace TradeHub.Repositories;

// Raw facts about ONE possible suggestion, seen from the logged-in user ("me").
// The repository only counts; the service turns the counts into points.
public class SuggestionSignals
{
    public Guid UserId { get; set; }

    // Friends I have in common with this person
    public int MutualFriends { get; set; }

    // Orders (not rejected) between us
    public int OrdersIBought { get; set; }     // I ordered from them
    public int OrdersTheyBought { get; set; }  // they ordered from me

    // They liked / commented on MY posts
    public int LikesToMe { get; set; }
    public int CommentsToMe { get; set; }

    // I liked / commented on THEIR posts
    public int LikesFromMe { get; set; }
    public int CommentsFromMe { get; set; }

    // Order requests they received as a seller in the recent period (any status)
    public int SellerOrders { get; set; }
}
