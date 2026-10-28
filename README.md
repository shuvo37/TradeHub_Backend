# TradeHub

TradeHub is a full-stack social-commerce platform where users can create storefronts, organize products, promote them
through social posts, and trade with other users. Buyers can place orders using cash-on-delivery or prepaid payment
methods, while sellers manage inventory and accept or reject incoming orders

## Repositories

TradeHub is split into two repositories:

| Part | Repository | Stack |
| --- | --- | --- |
| Frontend | [shuvo37/TradeHub](https://github.com/shuvo37/TradeHub) (app lives in `tradehub/`) | Next.js 16 (App Router), TypeScript, Tailwind CSS |
| Backend | [shuvo37/TradeHub_Backend](https://github.com/shuvo37/TradeHub_Backend) (app lives in `tradehub_backend/`) | ASP.NET Core Web API (.NET 10), EF Core, PostgreSQL |

## Features

- Sign up and log in
- Profile page
- My Store: create categories and products
- Posts with likes and comments (comments can be edited, shown 5 per page with "x ago" time)
- Orders: buyers place orders, sellers see and accept or reject them
- Notifications: the buyer gets a bell notification when a seller accepts or rejects an order, and can open a read-only order summary

## Backend Architecture

The backend follows a layered structure:

    Model -> Repository -> Service -> Controller

DTOs are used per feature, so the API never returns raw database entities.

## Getting Started

### Prerequisites

- Node.js (LTS)
- .NET 10 SDK
- PostgreSQL

### 1. Run the backend

    git clone https://github.com/shuvo37/TradeHub_Backend.git
    cd TradeHub_Backend/tradehub_backend

1. Open `appsettings.json` and set your PostgreSQL connection string.
2. Apply the database migrations:

        dotnet ef database update

3. Start the API:

        dotnet run

### 2. Run the frontend

    git clone https://github.com/shuvo37/TradeHub.git
    cd TradeHub/tradehub
    npm install
    npm run dev

Make sure the frontend is pointing at the URL your backend is running on, then open http://localhost:3000.

## Roadmap

- Friends and discover
- Visiting other users' profiles
- Better recommendation system (friend recommendations and content recommendations)

## Author

Built by [shuvo37](https://github.com/shuvo37).
