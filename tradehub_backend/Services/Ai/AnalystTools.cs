using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Enums;

namespace TradeHub.Services;

// The read-only "tools" the AI may call. Each one reads the live database for ONE user and returns business data only.
//
// Safety rules (the AI never touches the database itself):
//  - The user id is passed in by the server from the login token. It is not a tool parameter, so the AI cannot ask for
//    anyone else's data.
//  - Every query starts from "this user's categories / posts / orders".
//  - Only SELECTs, always projected to a few columns. No phone numbers, pickup places, payment numbers or transaction ids.
//  - Rows sent to the AI are capped (MaxRows) and the tool says so when it cut the list ("truncated").
public class AnalystTools
{
    private const int MaxRows = 100;    // rows sent back to the AI per tool call
    private const int MaxFetch = 3000;  // rows read from the database per tool call

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TradeHubDbContext _db;

    public AnalystTools(TradeHubDbContext db)
    {
        _db = db;
    }

    // What the AI is told about the tools (Gemini "function declarations").
    public const string DeclarationsJson = """
    [
      {
        "functionDeclarations": [
          {
            "name": "get_categories",
            "description": "Lists the user's own store categories, each with how many products it holds."
          },
          {
            "name": "search_products",
            "description": "Lists the user's own products, with optional filters. Use it for questions like 'all products of category X', 'products cheaper than 50', 'products that are out of stock', 'products with a discount'. Each row has name, category, list price, discount percent, final price (after the discount, rounded down to whole dollars, never below $1), stock quantity or availability. Also returns how many products match.",
            "parameters": {
              "type": "OBJECT",
              "properties": {
                "category": { "type": "STRING", "description": "Only products of the category whose name contains this text." },
                "name_contains": { "type": "STRING", "description": "Only products whose name contains this text." },
                "min_price": { "type": "NUMBER", "description": "Only products priced at least this much." },
                "max_price": { "type": "NUMBER", "description": "Only products priced at most this much." },
                "price_type": { "type": "STRING", "enum": ["list", "final"], "description": "Which price min_price / max_price are compared with: 'list' = price before discount (default), 'final' = price after the discount, rounded down to whole dollars and never below $1." },
                "availability": { "type": "STRING", "enum": ["in_stock", "out_of_stock", "any"], "description": "A product with a stock count is in stock when the count is above 0; a product without a count is in stock when its status is Available. Default: any." },
                "max_quantity": { "type": "INTEGER", "description": "Only products that count their stock and have at most this many left (use it to find low stock)." },
                "has_discount": { "type": "BOOLEAN", "description": "true = only discounted products, false = only products without a discount." },
                "sort_by": { "type": "STRING", "enum": ["name", "price_asc", "price_desc", "quantity_asc", "discount_desc"], "description": "Default: name." },
                "limit": { "type": "INTEGER", "description": "How many rows to return, at most 100. Default 50." }
              }
            }
          },
          {
            "name": "get_orders",
            "description": "Lists orders, newest first, at most 100, plus the total number of matching orders and the money of the accepted ones. 'received' = orders buyers placed on the user's products (the user is the seller). 'placed' = orders the user placed on other people's products (the user is the buyer). Each row has product, category, quantity, total price, status (PENDING, ACCEPTED, REJECTED), payment method, the other person's name and the date.",
            "parameters": {
              "type": "OBJECT",
              "properties": {
                "role": { "type": "STRING", "enum": ["received", "placed"], "description": "Default: received." },
                "status": { "type": "STRING", "enum": ["PENDING", "ACCEPTED", "REJECTED"], "description": "Only orders with this status. Leave out for all." },
                "product_name": { "type": "STRING", "description": "Only orders whose product name contains this text." },
                "from_date": { "type": "STRING", "description": "Start date, format yyyy-MM-dd. Default: 30 days ago." },
                "to_date": { "type": "STRING", "description": "End date (included), format yyyy-MM-dd. Default: today." }
              }
            }
          },
          {
            "name": "get_sales_summary",
            "description": "Summary of the orders the user RECEIVED as a seller in a date range: number of orders per status, money from ACCEPTED orders, and the best selling products and categories (by accepted quantity and money).",
            "parameters": {
              "type": "OBJECT",
              "properties": {
                "from_date": { "type": "STRING", "description": "Start date, format yyyy-MM-dd. Default: 30 days ago." },
                "to_date": { "type": "STRING", "description": "End date (included), format yyyy-MM-dd. Default: today." }
              }
            }
          },
          {
            "name": "get_my_posts",
            "description": "Lists the user's own social posts with like count, comment count, the product the post promotes (if any) and the date. Use it to find the most liked or most commented posts.",
            "parameters": {
              "type": "OBJECT",
              "properties": {
                "sort_by": { "type": "STRING", "enum": ["recent", "likes", "comments"], "description": "Default: recent." },
                "limit": { "type": "INTEGER", "description": "How many posts to return, at most 50. Default 20." }
              }
            }
          }
        ]
      }
    ]
    """;

    public async Task<JsonNode> RunAsync(Guid userId, string name, JsonObject? args)
    {
        object result = name switch
        {
            "get_categories" => await CategoriesAsync(userId),
            "search_products" => await SearchProductsAsync(userId, args),
            "get_orders" => await OrdersAsync(userId, args),
            "get_sales_summary" => await SalesSummaryAsync(userId, args),
            "get_my_posts" => await MyPostsAsync(userId, args),
            _ => new { error = $"Unknown tool: {name}" }
        };

        return JsonSerializer.SerializeToNode(result, JsonOptions)!;
    }

    // ---------- tools ----------

    private async Task<object> CategoriesAsync(Guid userId)
    {
        var rows = await _db.Categories.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Name, Products = c.Products.Count })
            .Take(MaxRows)
            .ToListAsync();

        return new { totalCategories = rows.Count, categories = rows };
    }

    private async Task<object> SearchProductsAsync(Guid userId, JsonObject? args)
    {
        var category = Arg(args, "category");
        var nameContains = Arg(args, "name_contains");

        var query = _db.Products.AsNoTracking()
            .Where(p => p.Category!.UserId == userId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var text = category.Trim().ToLower();
            query = query.Where(p => p.Category!.Name.ToLower().Contains(text));
        }

        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            var text = nameContains.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(text));
        }

        // The price column is text ("$50", "1,200.50"), so the price filters run here in C# on this user's own products only.
        var products = await query
            .OrderBy(p => p.Name)
            .Take(MaxFetch)
            .Select(p => new
            {
                p.Name,
                Category = p.Category!.Name,
                p.Price,
                p.Discount,
                p.Quantity,
                p.Status
            })
            .ToListAsync();

        var rows = products.Select(p =>
        {
            var price = ParsePrice(p.Price);
            var discount = Math.Clamp(p.Discount ?? 0m, 0m, 100m);
            // TradeHub never uses cents: the price of ONE item after the discount is rounded DOWN to whole dollars
            // (an order of several items is rounded down once, on its total: see OrderService).
            // and never goes below $1 (a free product, price 0, stays 0).
            var finalPrice = price == null
                ? (decimal?)null
                : price.Value > 0
                    ? Math.Max(1m, Math.Floor(price.Value * (100m - discount) / 100m))
                    : price.Value;

            // Same rule as the order form: a product with a count is in stock above 0, otherwise the status decides.
            var inStock = p.Quantity != null ? p.Quantity > 0 : p.Status != StockStatus.Unavailable;

            return new ProductRow(
                p.Name,
                p.Category,
                price,
                discount,
                finalPrice,
                p.Quantity,
                p.Quantity != null ? null : (p.Status ?? StockStatus.Available).ToString(),
                inStock);
        }).ToList();

        var finalPriceType = string.Equals(Arg(args, "price_type"), "final", StringComparison.OrdinalIgnoreCase);
        decimal? Compared(ProductRow r) => finalPriceType ? r.FinalPrice : r.ListPrice;

        var minPrice = Number(args, "min_price");
        var maxPrice = Number(args, "max_price");

        if (minPrice != null)
            rows = rows.Where(r => Compared(r) != null && Compared(r) >= minPrice).ToList();
        if (maxPrice != null)
            rows = rows.Where(r => Compared(r) != null && Compared(r) <= maxPrice).ToList();

        var availability = Arg(args, "availability")?.ToLowerInvariant();
        if (availability == "in_stock")
            rows = rows.Where(r => r.InStock).ToList();
        else if (availability == "out_of_stock")
            rows = rows.Where(r => !r.InStock).ToList();

        var maxQuantity = Number(args, "max_quantity");
        if (maxQuantity != null)
            rows = rows.Where(r => r.Quantity != null && r.Quantity <= maxQuantity).ToList();

        var hasDiscount = Bool(args, "has_discount");
        if (hasDiscount == true)
            rows = rows.Where(r => r.DiscountPercent > 0).ToList();
        else if (hasDiscount == false)
            rows = rows.Where(r => r.DiscountPercent == 0).ToList();

        rows = Arg(args, "sort_by")?.ToLowerInvariant() switch
        {
            "price_asc" => rows.OrderBy(r => r.ListPrice ?? decimal.MaxValue).ThenBy(r => r.Name).ToList(),
            "price_desc" => rows.OrderByDescending(r => r.ListPrice ?? decimal.MinValue).ThenBy(r => r.Name).ToList(),
            "quantity_asc" => rows.OrderBy(r => r.Quantity ?? int.MaxValue).ThenBy(r => r.Name).ToList(),
            "discount_desc" => rows.OrderByDescending(r => r.DiscountPercent).ThenBy(r => r.Name).ToList(),
            _ => rows
        };

        var limit = Math.Clamp((int?)Number(args, "limit") ?? 50, 1, MaxRows);

        return new
        {
            totalMatching = rows.Count,
            truncated = rows.Count > limit || products.Count >= MaxFetch,
            priceComparedWith = finalPriceType ? "final price (after discount, rounded down to whole dollars, at least $1)" : "list price (before discount)",
            products = rows.Take(limit)
        };
    }

    private async Task<object> OrdersAsync(Guid userId, JsonObject? args)
    {
        var (start, endExclusive) = DateRange(args);
        var placed = string.Equals(Arg(args, "role"), "placed", StringComparison.OrdinalIgnoreCase);

        var query = _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= start && o.CreatedAt < endExclusive);

        // received: the user is the seller (orders the seller deleted from the list stay hidden, like in the Orders page)
        // placed: the user is the buyer
        query = placed
            ? query.Where(o => o.BuyerId == userId)
            : query.Where(o => o.SellerId == userId && !o.DeletedBySeller);

        var statusText = Arg(args, "status");
        if (!string.IsNullOrWhiteSpace(statusText))
        {
            if (!Enum.TryParse<OrderStatus>(statusText, ignoreCase: true, out var status))
                return new { error = "status must be PENDING, ACCEPTED or REJECTED" };

            query = query.Where(o => o.OrderStatus == status);
        }

        var productName = Arg(args, "product_name");
        if (!string.IsNullOrWhiteSpace(productName))
        {
            var text = productName.Trim().ToLower();
            query = query.Where(o => o.ProductName.ToLower().Contains(text));
        }

        var total = await query.CountAsync();
        var acceptedMoney = await query
            .Where(o => o.OrderStatus == OrderStatus.ACCEPTED)
            .SumAsync(o => (long?)o.TotalPrice) ?? 0;

        var rows = await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(MaxRows)
            .Select(o => new
            {
                o.ProductName,
                o.CategoryName,
                o.Quantity,
                o.TotalPrice,
                o.OrderStatus,
                o.PaymentMethod,
                OtherPerson = placed ? o.Seller!.Name : o.Buyer!.Name,
                o.CreatedAt,
                o.DecidedAt
            })
            .ToListAsync();

        return new
        {
            role = placed ? "placed" : "received",
            fromDate = start.ToString("yyyy-MM-dd"),
            toDate = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
            totalMatching = total,
            truncated = total > MaxRows,
            acceptedMoneyDollars = acceptedMoney,
            orders = rows.Select(o => new
            {
                o.ProductName,
                o.CategoryName,
                o.Quantity,
                o.TotalPrice,
                Status = o.OrderStatus.ToString(),
                PaymentMethod = o.PaymentMethod == PaymentMethod.ON_CASH_DELIVERY ? "Cash on delivery" : "Paid before delivery",
                OtherPerson = o.OtherPerson,
                o.CreatedAt,
                o.DecidedAt
            })
        };
    }

    private async Task<object> SalesSummaryAsync(Guid userId, JsonObject? args)
    {
        var (start, endExclusive) = DateRange(args);

        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.SellerId == userId && !o.DeletedBySeller
                        && o.CreatedAt >= start && o.CreatedAt < endExclusive)
            .OrderBy(o => o.CreatedAt)
            .Take(MaxFetch)
            .Select(o => new { o.ProductName, o.CategoryName, o.Quantity, o.TotalPrice, o.OrderStatus })
            .ToListAsync();

        var accepted = orders.Where(o => o.OrderStatus == OrderStatus.ACCEPTED).ToList();

        var topProducts = accepted
            .GroupBy(o => o.ProductName)
            .Select(g => new { Product = g.Key, UnitsSold = g.Sum(x => x.Quantity), Money = g.Sum(x => (long)x.TotalPrice) })
            .OrderByDescending(x => x.Money)
            .Take(10)
            .ToList();

        var topCategories = accepted
            .GroupBy(o => o.CategoryName)
            .Select(g => new { Category = g.Key, UnitsSold = g.Sum(x => x.Quantity), Money = g.Sum(x => (long)x.TotalPrice) })
            .OrderByDescending(x => x.Money)
            .Take(10)
            .ToList();

        return new
        {
            fromDate = start.ToString("yyyy-MM-dd"),
            toDate = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
            truncated = orders.Count >= MaxFetch,
            ordersReceived = orders.Count,
            pending = orders.Count(o => o.OrderStatus == OrderStatus.PENDING),
            accepted = accepted.Count,
            rejected = orders.Count(o => o.OrderStatus == OrderStatus.REJECTED),
            moneyFromAcceptedOrdersDollars = accepted.Sum(o => (long)o.TotalPrice),
            topProducts,
            topCategories
        };
    }

    private async Task<object> MyPostsAsync(Guid userId, JsonObject? args)
    {
        var limit = Math.Clamp((int?)Number(args, "limit") ?? 20, 1, 50);

        var query = _db.Posts.AsNoTracking().Where(p => p.UserId == userId);

        var total = await query.CountAsync();

        var ordered = Arg(args, "sort_by")?.ToLowerInvariant() switch
        {
            "likes" => query.OrderByDescending(p => p.Likes.Count).ThenByDescending(p => p.CreatedAt),
            "comments" => query.OrderByDescending(p => p.Comments.Count).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var rows = await ordered
            .Take(limit)
            .Select(p => new
            {
                Text = p.Text.Length > 120 ? p.Text.Substring(0, 120) : p.Text,
                Likes = p.Likes.Count,
                Comments = p.Comments.Count,
                Product = p.Product != null ? p.Product.Name : null,
                p.CreatedAt
            })
            .ToListAsync();

        return new { totalPosts = total, posts = rows };
    }

    // ---------- helpers ----------

    private record ProductRow(
        string Name,
        string Category,
        decimal? ListPrice,
        decimal DiscountPercent,
        decimal? FinalPrice,
        int? Quantity,
        string? Availability,
        bool InStock);

    private static string? Arg(JsonObject? args, string key) => args?[key]?.ToString();

    private static decimal? Number(JsonObject? args, string key)
    {
        var text = args?[key]?.ToString();
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static bool? Bool(JsonObject? args, string key)
    {
        var text = args?[key]?.ToString();
        return bool.TryParse(text, out var b) ? b : null;
    }

    // The product price is text such as "$50", "50" or "$1,200.50" (same rule as OrderService). null = no usable price.
    private static decimal? ParsePrice(string? price)
    {
        var match = Regex.Match(price ?? string.Empty, @"\d[\d,]*(\.\d+)?");

        return match.Success &&
               decimal.TryParse(match.Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    // from_date / to_date (yyyy-MM-dd). Defaults: the last 30 days up to and including today.
    // Returns [from, toExclusive) so the whole "to" day is included. Days are UTC days, like the stored timestamps.
    private static (DateTimeOffset Start, DateTimeOffset EndExclusive) DateRange(JsonObject? args)
    {
        var today = DateTime.UtcNow.Date;

        var start = TryDate(Arg(args, "from_date")) ?? today.AddDays(-30);
        var end = TryDate(Arg(args, "to_date")) ?? today;

        if (end < start)
        {
            (start, end) = (end, start);
        }

        return (new DateTimeOffset(start, TimeSpan.Zero), new DateTimeOffset(end.AddDays(1), TimeSpan.Zero));
    }

    private static DateTime? TryDate(string? text)
    {
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? DateTime.SpecifyKind(d.Date, DateTimeKind.Unspecified)
            : null;
    }
}
