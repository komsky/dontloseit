# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is "Flea Market by Beloved Websites" - a private flea market web application built with ASP.NET Core 8.0 and Entity Framework. Users can create listings for items they want to sell, with features for reservations, search functionality, and photo management. The production URL will be fleamarket.beloved-websites.co.uk

## Technology Stack

- **Framework**: ASP.NET Core 8.0 Web Application
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **UI**: Razor Pages/Views with Bootstrap

## Project Structure

```
src/FleaMarket/FleaMarket.FrontEnd/
├── Controllers/           # MVC Controllers (HomeController, ItemsController)
├── Models/               # Domain models (Item, ItemImage, Reservation, ViewModels)
├── Views/                # Razor views and layouts
├── Areas/Identity/       # Identity UI customizations
├── Data/                 # Entity Framework DbContext and migrations
├── wwwroot/              # Static files (CSS, JS, images)
└── appsettings.json      # Configuration including RegistrationPassword
```

## Key Domain Models

- **Item**: Core marketplace item with Name, Description, Price, and status flags (IsArchived, IsReserved, IsSold)
- **ItemImage**: Photo attachments for items (stored as files with FileName references)
- **Reservation**: System for users to reserve items before purchasing
- **ApplicationDbContext**: EF Core context managing Items, ItemImages, and Reservations

## Common Development Commands

**Build and Run:**
```bash
# Navigate to the project directory
cd src/FleaMarket/FleaMarket.FrontEnd

# Build the project
dotnet build

# Run the application
dotnet run

# Run with hot reload for development
dotnet watch run
```

**Database Operations:**
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database with migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

**Project Management:**
```bash
# Restore NuGet packages
dotnet restore

# Clean build artifacts
dotnet clean
```

## Configuration Notes

- **User Secrets**: This project uses Visual Studio User Secrets to override appsettings.json entries
- **Configuration Priority**: User Secrets > appsettings.Development.json > appsettings.json
- **appsettings.json**: Contains empty reference values showing what configuration is needed
- **Required Settings**: 
  - `ConnectionStrings:DefaultConnection` - SQL Server connection string
  - `RegistrationPassword` - Shared password for new account registration
  - `Authentication:Google:ClientId` - Google OAuth Client ID
  - `Authentication:Google:ClientSecret` - Google OAuth Client Secret
  - `Authentication:Facebook:AppId` - Facebook App ID
  - `Authentication:Facebook:AppSecret` - Facebook App Secret

### Setting Up User Secrets

```bash
# Navigate to the project directory
cd src/FleaMarket/FleaMarket.FrontEnd

# Set connection string (replace with your actual connection string)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=aspnet-FleaMarket.FrontEnd-066e6f2f-7909-47a4-89d5-58624ccd7525;Trusted_Connection=True;MultipleActiveResultSets=true"

# Set registration password
dotnet user-secrets set "RegistrationPassword" "your-secret-password"

# Set Google OAuth credentials
dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"

# Set Facebook OAuth credentials
dotnet user-secrets set "Authentication:Facebook:AppId" "your-facebook-app-id"
dotnet user-secrets set "Authentication:Facebook:AppSecret" "your-facebook-app-secret"

# List all user secrets
dotnet user-secrets list

# Remove a user secret
dotnet user-secrets remove "RegistrationPassword"

# Clear all user secrets
dotnet user-secrets clear
```

### Google OAuth Setup

**Where to get Google OAuth credentials:**

1. **Go to Google Cloud Console**: https://console.cloud.google.com/
2. **Create or select a project**
3. **Enable Google+ API**:
   - Go to "APIs & Services" > "Library"
   - Search for "Google+ API" or "Google Identity"
   - Click "Enable"
4. **Create OAuth 2.0 credentials**:
   - Go to "APIs & Services" > "Credentials"
   - Click "Create Credentials" > "OAuth 2.0 Client IDs"
   - Choose "Web application"
   - Set Name: "Flea Market Authentication"
   - **Authorized redirect URIs**: Add these URLs:
     - `https://localhost:5001/signin-google` (for HTTPS development)
     - `http://localhost:5000/signin-google` (for HTTP development)
     - Add your production domain when deployed: `https://fleamarket.beloved-websites.co.uk/signin-google`
5. **Copy credentials**:
   - Copy the "Client ID" 
   - Copy the "Client secret"
   - Add them to user secrets using the commands above

**Important Notes:**
- The redirect URI must match exactly (including https/http)
- For local development, use `https://localhost:5001/signin-google`
- Google requires HTTPS in production

### Facebook OAuth Setup

**Where to get Facebook OAuth credentials:**

1. **Go to Facebook Developers**: https://developers.facebook.com/
2. **Create an App**:
   - Click "Create App"
   - Choose "Consumer" or "Business" (Consumer is fine for most cases)
   - Enter App Display Name: "Flea Market Authentication"
   - Enter App Contact Email
3. **Add Facebook Login**:
   - In your app dashboard, click "Add Product"
   - Find "Facebook Login" and click "Set Up"
   - Choose "Web" platform
4. **Configure OAuth Settings**:
   - Go to "Facebook Login" > "Settings" in left sidebar
   - **Valid OAuth Redirect URIs**: Add these URLs:
     - `https://localhost:5001/signin-facebook` (for HTTPS development)
     - `http://localhost:5000/signin-facebook` (for HTTP development)  
     - Add your production domain when deployed: `https://fleamarket.beloved-websites.co.uk/signin-facebook`
5. **Get App Credentials**:
   - Go to "Settings" > "Basic" in left sidebar
   - Copy the "App ID"
   - Copy the "App Secret" (click "Show" to reveal it)
   - Add them to user secrets using the commands above

**Important Notes:**
- Facebook requires your app to be in "Live" mode for production use
- During development, you can use "Development" mode
- Only users with roles (Admin, Developer, Tester) can login in Development mode
- The redirect URI must match exactly (including https/http)
- For local development, use `https://localhost:5001/signin-facebook`

## Authentication & Security

- Users must confirm their email accounts (`RequireConfirmedAccount = true`)
- Registration requires a shared password set in configuration
- Uses standard ASP.NET Core Identity for user management

## Development Workflow

1. Make changes to models, controllers, or views
2. Add Entity Framework migrations if data model changes
3. Test locally with `dotnet run`
4. Update database with `dotnet ef database update`

No automated tests are currently configured in this project.