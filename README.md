# TradeHub

TradeHub is a social-media-style marketplace. Every user has a personal store and is both a buyer and a seller: you post your products to your friends' feed, discover other people's posts and stores, and place orders. A built-in AI assistant answers questions about your own store and explains how to use the app.

There is no in-app chat and no online payment gateway. A seller shows a bKash / Nogod number, and the buyer either pays before delivery (transaction ID or screenshot) or chooses cash on delivery.

## Table of Contents

- [Repositories](#repositories)
- [Features](#features)
- [Screenshots](#screenshots)
- [Backend Architecture](#backend-architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [1. Run the backend](#1-run-the-backend)
  - [2. Run the frontend](#2-run-the-frontend)
- [Future Work](#futurework)
- [Author](#author)

## Repositories

| Part | Repository | Stack |
| --- | --- | --- |
| Frontend | [shuvo37/TradeHub](https://github.com/shuvo37/TradeHub) | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS 4 |
| Backend | [shuvo37/TradeHub_Backend](https://github.com/shuvo37/TradeHub_Backend) (API in `tradehub_backend/`) | ASP.NET Core Web API (.NET 10), EF Core, PostgreSQL |

External services: Google Gemini (AI assistant), Cloudinary (images), Gmail SMTP (verification and password-reset emails).

## Features

**Account**
- Sign up and log in with a unique name and password (JWT access token plus refresh token)
- Contact email verification with a 6-digit code (valid 2 minutes)
- Forgot password by email code

**Store and posts**
- My Store: categories and products (price, discount percent, image, description, stock count or Available / Unavailable)
- Posts with text, photo and/or an attached product; edit, delete, and revive a post (back to the top of friends' feeds, once every 24 hours)
- Likes and comments (edit, delete, paging, "x ago" time)

**Social**
- Friends: send, confirm and delete requests, friends list
- Home feed: your posts and your accepted friends' posts, newest first
- Discover: people you may know, other people's posts, and search for products and categories
- Search people by name or unique name

**Orders and notifications**
- Buyers order a product in two steps: quantity, then pickup location, phone and payment (cash on delivery, or bKash / Nogod with transaction ID or screenshot)
- Sellers see incoming orders (with a pending-order badge), then accept or reject them
- Bell notifications for friend requests, comments on your posts, and accepted / rejected orders; the buyer can open a read-only order summary

**AI assistant** (sparkle icon in the top bar)
- Answers questions about your own store using live data, for example "which products are low on stock?", "what sold best this month?" or "which of my posts got the most likes?"
- Explains how to use the app step by step ("how do I order something?", "how do I accept an order?")
- Read-only: it can look at your data and give advice, but it cannot change anything
- Only sees your own business data, never other users' contact or payment details
- Powered by Google Gemini with function calling; the server runs read-only tools against the database, and the chat history is kept in the browser, not saved on the server
- Rate limited to 15 questions per minute per user

## Screenshots

<table>
<tr><td align="center" valign="top"><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20112603.png" width="190" alt="AI assistant"><br><sub>AI assistant</sub></td><td align="center" valign="top"><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20112621.png" width="190" alt="Orders list"><br><sub>Orders list with filters</sub></td><td align="center" valign="top"><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20112639.png" width="190" alt="Order details"><br><sub>Order details (seller)</sub></td><td align="center" valign="top"><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20113238.png" width="190" alt="Product details"><br><sub>Product details</sub></td></tr>
<tr><td align="center" valign="top"><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20113335.png" width="190" alt="Edit product"><br><sub>Edit product</sub></td><td align="center" valign="top"><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20112714.png" width="150" alt="Profile"><br><sub>Profile</sub></td><td align="center" valign="top"><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20112735.png" width="150" alt="My Store"><br><sub>My Store</sub></td><td align="center" valign="top"><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20112757.png" width="190" alt="Post with product"><br><sub>Post with attached product</sub><br><br><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20112828.png" width="190" alt="Post box"><br><sub>Post box</sub><br><br><img src="https://raw.githubusercontent.com/shuvo37/tradehub-assests/main/Screenshot%202026-10-29%20112700.png" width="190" alt="Notifications"><br><sub>Notifications</sub></td></tr>
</table>

## Backend Architecture

    Model -> Repository -> Service -> Controller

- DTOs per feature, so the API never returns raw database entities
- Secrets (JWT key, Gemini key, Gmail app password, Cloudinary keys) are kept in user-secrets, never in git

## Getting Started

### Prerequisites

- Node.js (LTS)
- .NET 10 SDK and the `dotnet-ef` tool
- PostgreSQL
- A Google Gemini API key, a Cloudinary account, and a Gmail app password

### 1. Run the backend

    git clone https://github.com/shuvo37/TradeHub_Backend.git
    cd TradeHub_Backend/tradehub_backend

1. In `appsettings.json`, set `ConnectionStrings:DefaultConnection` to your own PostgreSQL connection.
2. Set the secrets:

        dotnet user-secrets set "Jwt:Key" "<a long random string>"
        dotnet user-secrets set "Gemini:ApiKey" "<your Gemini key>"
        dotnet user-secrets set "Email:Username" "<your gmail address>"
        dotnet user-secrets set "Email:Password" "<your Google app password>"
        dotnet user-secrets set "Cloudinary:CloudName" "<cloud name>"
        dotnet user-secrets set "Cloudinary:ApiKey" "<api key>"
        dotnet user-secrets set "Cloudinary:ApiSecret" "<api secret>"

3. Create the database tables:

        dotnet ef database update

4. Start the API (it runs on http://localhost:5108):

        dotnet run

### 2. Run the frontend

    git clone https://github.com/shuvo37/TradeHub.git
    cd TradeHub
    npm install

Create a `.env.local` file in the project folder:

    NEXT_PUBLIC_API_URL=http://localhost:5108

Then start the app and open http://localhost:3000:

    npm run dev

The backend only accepts requests from http://localhost:3000.

## Future Work

- More Smarter recommendations (friends and content)

## Author

Built by [shuvo37](https://github.com/shuvo37).
