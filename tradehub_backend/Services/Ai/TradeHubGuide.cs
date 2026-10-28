namespace TradeHub.Services;

// What the AI knows about HOW TO USE the TradeHub app (button names, pages, steps).
// It is added to the AI's instructions on every question. The AI's data (products, orders, posts) comes from the tools instead.
//
// KEEP THIS UP TO DATE: when you add or rename a feature or a button in the app, change the matching line here.
// If a line is missing, the AI says it is not sure instead of guessing.
public static class TradeHubGuide
{
    public const string Text = """
    Top bar (every page):
    - Search people box (top left): find users by name or unique name.
    - Tabs: Home, Friends, Discover.
    - Bag icon = Orders page: the order requests other people sent for MY products. The red number counts the pending ones.
    - Bell icon = notifications (friend requests, comments on my posts, my orders accepted or rejected).
    - Sparkle icon = this AI assistant.
    - My profile picture = my Profile page.
    - Three-dot menu: Profile, My friends, Contact email, Log out.

    Home: the news feed. It shows posts of my accepted friends and my own posts, newest first. The box at the top creates a post. Older posts load with the More button. Clicking the Home icon refreshes the feed.

    Creating a post: write text, add a photo with Photo, and/or attach one of my own products with Attach Product (the product must already exist in one of my categories on my Profile). A post can have text, an image, a product, or any mix.
    Post three-dot menu (my own posts): Edit (changes the text only), Revive post (moves the post back to the top of my friends' feeds, allowed once every 24 hours), Delete. Everyone can Like and Comment. I can delete my own comments, and delete any comment on my own post.

    Profile page: my store and my posts. The store has categories (add one with the + button; rename or delete from the category menu) and each category has products (name, price, description, image, discount percent, stock). Deleting a category deletes all its products. Category names are unique for me, and product names are unique inside a category. On the left I can edit my profile (picture, name, location, payment number, email, phone, necessary information for buyers).
    A product's stock is either a quantity (counts down as orders are accepted) or just Available / Unavailable. An Unavailable product cannot be ordered.

    HOW TO ORDER SOMETHING (as a buyer):
    1. Find a product. Either press Order Now on a post that has a product attached (Home or Discover), or open another person's profile (search for them, or use Discover), open a category, click the product, and press Order. Discover also has a search box that finds products and categories.
    2. Step 1: choose the quantity and continue. TradeHub calculates the price on the server (whole dollars, rounded down, at least $1).
    3. Step 2: enter the pickup location and phone number, then choose the payment: Cash on delivery, or Pay before delivery. For Pay before delivery choose bKash or Nogod, enter the payment number I paid to, and give the transaction ID or upload a screenshot of the payment.
    4. Submit. The order goes to the seller as PENDING. I cannot order my own product, and I cannot order an Unavailable or out-of-stock product.
    5. The seller accepts or rejects it. I get a bell notification ("accepted your order" / "rejected your order"), and clicking it opens a summary card with what I gave, the price, and the seller's decision.

    HOW TO HANDLE ORDERS (as a seller): open the Orders page with the bag icon. Open an order to see the buyer's pickup location, phone and payment details, then press Accept or Reject. After the decision I can Delete the order from my list (a pending order cannot be deleted). The buyer keeps their own summary.

    Friends: send a request from a person's profile or from Discover (People you may know). Requests I received are on the Friends page with Confirm / Delete. My friends list is in the three-dot menu under My friends.
    Discover: People you may know on one side, other people's posts on the other, and a search box for products and categories.

    Account: Log in with unique name and password. Forgot password? is a link on the Login page; the code is sent to my verified contact email. TradeHub asks for a contact email and a 6-digit code sent to it (valid 2 minutes) before the app can be used; it can be changed later from the three-dot menu under Contact email.
    """;
}
