<p align="center">
  <img src="PrimeBakes/PrimeBakes.Web/wwwroot/images/logo_full.png" alt="Prime Bakes Logo" width="400"/>
</p>

<h1 align="center">🧁 Prime Bakes</h1>

<p align="center">
  <strong>Enterprise-Grade Restaurant, Store, Inventory & Accounts Management System</strong>
</p>

<p align="center">
  <em>A comprehensive business management solution for Salasar Foods Guwahati</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen?style=for-the-badge" alt="Build"/>
  <img src="https://img.shields.io/badge/.NET-10.0-purple?style=for-the-badge&logo=dotnet" alt=".NET"/>
  <img src="https://img.shields.io/badge/C%23-14-239120?style=for-the-badge&logo=csharp" alt="C#"/>
  <img src="https://img.shields.io/badge/Blazor-Hybrid-blueviolet?style=for-the-badge&logo=blazor" alt="Blazor"/>
  <img src="https://img.shields.io/badge/MAUI-Cross_Platform-green?style=for-the-badge&logo=dotnet" alt="MAUI"/>
  <img src="https://img.shields.io/badge/Azure-SQL_&_Blob-0089D6?style=for-the-badge&logo=microsoft-azure" alt="Azure"/>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%20|%20Android%20|%20iOS%20|%20macOS%20|%20Web-lightgrey?style=flat-square" alt="Platforms"/>
  <img src="https://img.shields.io/badge/Syncfusion-32.2.7-blue?style=flat-square" alt="Syncfusion"/>
  <img src="https://img.shields.io/badge/License-Proprietary-red?style=flat-square" alt="License"/>
  <img src="https://img.shields.io/badge/Version-1.1.1.4-orange?style=flat-square" alt="Version"/>
</p>

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Key Features](#-key-features)
- [Architecture](#-architecture)
- [Project Structure](#-project-structure)
- [Technology Stack](#-technology-stack)
- [Modules](#-modules)
- [Database Schema](#-database-schema)
- [Getting Started](#-getting-started)
- [Deployment](#-deployment)
- [Security](#-security)
- [Platform Support](#-platform-support)
- [License](#-license)

---

## 🎯 Overview

**Prime Bakes** is a full-featured enterprise resource planning (ERP) system designed specifically for bakery and food manufacturing businesses. Built with modern .NET technologies, it provides seamless cross-platform functionality across desktop, mobile, and web environments.

The system handles the complete business lifecycle from raw material procurement to finished goods sales, including restaurant dine-in billing, comprehensive financial accounting, and real-time inventory tracking across multiple locations.

<p align="center">
  <img src="PrimeBakes/PrimeBakes/Resources/AppIcon/logo.png" alt="App Icon" width="120"/>
</p>

---

## ✨ Key Features

### 🍽️ **Restaurant Management**
- **Dine-In Billing** - Desktop and mobile-optimized POS with table management
- **Dining Areas & Tables** - Area/table configuration for dine-in operations
- **KOT (Kitchen Order Ticket)** - Thermal printing to kitchen printers
- **Bill Thermal Printing** - Instant receipt printing via Bluetooth/USB
- **Mobile Billing** - Cart, payment, and confirmation flow for mobile devices

### 🛒 **Store Management**
- **Point of Sale (POS)** - Desktop and mobile-optimized sales interfaces
- **Order Processing** - Customer order creation, tracking, and mobile ordering
- **Sales Returns** - Complete return and refund management
- **Stock Transfers** - Inter-location inventory transfers with dual-location tracking
- **Outlet Summary** - Multi-outlet consolidated reporting
- **Product Catalog** - Products, categories, KOT categories, location-specific pricing
- **Customer Management** - Customer database with contact information
- **Tax Configuration** - GST/Tax setup with product-level tax mapping

### 📦 **Inventory Management**
- **Purchase Entry** - Raw material procurement with supplier tracking
- **Purchase Returns** - Return materials to suppliers
- **Kitchen Issue** - Issue raw materials to production kitchens
- **Kitchen Production** - Record finished goods production output
- **Raw Material Management** - Ingredient catalog with categories and UoM
- **Recipe Management** - Product recipes with Bill of Materials (BOM)
- **Product Stock Adjustment** - Manual finished goods stock corrections
- **Raw Material Stock Adjustment** - Manual raw material stock corrections
- **Multi-Location Stock** - Track inventory across multiple outlets

### 💰 **Financial Accounting**
- **Double-Entry Bookkeeping** - Complete voucher entry system
- **Ledger Management** - Full chart of accounts with groups and account types
- **Company Management** - Multi-company support
- **Voucher Types** - Payment, receipt, journal, contra entries
- **Financial Year Management** - Multi-year period support
- **State/UT Configuration** - State and union territory master data
- **Nature & Account Types** - Hierarchical account classification
- **Auto-Posting** - Automatic accounting entries from sales and bills

### 📊 **Reporting & Analytics**
- **Sales Reports** - Transaction-level and item-wise with summary/detailed views
- **Sale Return Reports** - Transaction-level and item-wise breakdowns
- **Order Reports** - Transaction-level and item-wise analysis
- **Stock Transfer Reports** - Transaction-level and item-wise tracking
- **Bill Reports** - Restaurant billing transaction and item reports
- **Purchase Reports** - Vendor and item-wise purchase analysis
- **Purchase Return Reports** - Transaction-level and item-wise breakdowns
- **Kitchen Issue Reports** - Issue transaction and item reports
- **Kitchen Production Reports** - Production transaction and item reports
- **Product Stock Reports** - Opening, closing, purchase, sale stock with valuation
- **Raw Material Stock Reports** - Comprehensive material stock analysis
- **Outlet Summary Report** - Multi-outlet consolidated performance
- **Financial Accounting Report** - Voucher-wise accounting transactions
- **Accounting Ledger Report** - Ledger-wise transaction details
- **Trial Balance** - Company-wise financial statement
- **Profit & Loss** - Income statement
- **Balance Sheet** - Financial position statement
- **PDF & Excel Export** - All reports exportable in both formats
- **Invoice Generation** - PDF invoices for all transaction types
- **Email Integration** - Send invoices and reports via email

### 🔔 **Push Notifications**
- Real-time order and transaction notifications
- Stock alert and production notifications
- Firebase Cloud Messaging integration (Android)
- Azure Notification Hubs backend
- Local notification support on all platforms

### 🖨️ **Printing**
- **Thermal Printing** - Sale, bill, and KOT receipt printing
- **Bluetooth Printing** - Wireless printer support for mobile devices
- **PDF Invoices** - Formatted invoices for purchase, sale, bill, order, stock transfer, kitchen issue, kitchen production, and accounting

### 🔄 **Auto-Updates**
- Automatic update detection
- Seamless in-app update installation
- Version management via GitHub releases

---

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT APPLICATIONS                             │
├─────────────────┬─────────────────┬─────────────────┬───────────────────────┤
│  📱 Android     │  🍎 iOS/macOS   │  🖥️ Windows     │  🌐 Web Browser       │
│  MAUI Blazor    │  MAUI Blazor    │  MAUI Blazor    │  Blazor Server        │
└────────┬────────┴────────┬────────┴────────┬────────┴───────────┬───────────┘
         │                 │                 │                    │
         └─────────────────┴─────────────────┴────────────────────┘
                                    │
                     ┌──────────────┴──────────────┐
                     │   PrimeBakes.Shared         │
                     │   (Blazor Components)       │
                     │   • Pages & Layouts         │
                     │   • UI Components           │
                     │   • Services Interfaces     │
                     └──────────────┬──────────────┘
                                    │
                     ┌──────────────┴──────────────┐
                     │   PrimeBakesLibrary         │
                     │   (Business Logic)          │
                     │   • Data Access Layer       │
                     │   • Models & Entities       │
                     │   • Export & Print Services  │
                     │   • Notification Services   │
                     └──────────────┬──────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         │                         │                          │
         ▼                         ▼                          ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────────┐
│  Azure SQL      │     │  Azure Blob     │     │  Push Notifications │
│  Database       │     │  Storage        │     │  API                │
│  • 40+ Tables   │     │  • Documents    │     │  • Azure Notification│
│  • Stored Procs │     │  • Attachments  │     │    Hubs             │
│  • 25+ Views    │     │                 │     │  • Firebase FCM     │
└─────────────────┘     └─────────────────┘     └─────────────────────┘
```

---

## 📁 Project Structure

```
PrimeBakes/
│
├── 📂 PrimeBakes/                    # Main Application Folder
│   ├── 📱 PrimeBakes/                # MAUI Client (Android/iOS/Windows/Mac)
│   │   ├── Platforms/                # Platform-specific code
│   │   ├── Resources/                # App icons, splash, fonts
│   │   ├── Services/                 # Native service implementations
│   │   │   ├── Notifications        # Push & local notification handling
│   │   │   ├── Bluetooth             # Bluetooth printer services
│   │   │   ├── Sound & Vibration     # Audio & haptic feedback
│   │   │   └── Updates               # Auto-update service
│   │   └── Components/               # MAUI-specific components
│   │
│   ├── 🔗 PrimeBakes.Shared/         # Shared Blazor Components
│   │   ├── Pages/                    # All application pages
│   │   │   ├── Store/                # Sales, orders, products, stock transfer
│   │   │   ├── Restaurant/           # Bills, dining areas/tables, mobile POS
│   │   │   ├── Inventory/            # Purchase, kitchen, stock, recipes, raw materials
│   │   │   ├── Accounts/             # Financial accounting, masters, reports
│   │   │   └── Operations/           # Users, locations, settings
│   │   ├── Components/               # Reusable UI components
│   │   │   ├── Button/               # Action buttons, date range, toggles, mobile buttons
│   │   │   ├── Card/                 # Balance info cards
│   │   │   ├── Dialog/               # Confirmations, uploads, toasts, validation
│   │   │   └── Page/                 # Header, footer, loader, mobile filters
│   │   ├── Layout/                   # Main layout templates
│   │   └── Services/                 # Shared service interfaces
│   │
│   └── 🌐 PrimeBakes.Web/            # Blazor Server Web App
│       ├── Services/                 # Web-specific implementations
│       └── wwwroot/                  # Static assets (images, JS, CSS)
│
├── 📚 PrimeBakesLibrary/             # Core Business Library
│   ├── Data/                         # Data access classes
│   │   ├── Store/                    # Sale, order, stock transfer, product, masters
│   │   ├── Restaurant/               # Bill, dining data operations
│   │   ├── Inventory/                # Purchase, kitchen, stock, recipe, raw material
│   │   ├── Accounts/                 # Financial accounting, masters
│   │   ├── Operations/               # User, location, settings
│   │   └── Common/                   # Shared utilities & helpers
│   ├── DataAccess/                   # Database & blob connectivity
│   ├── Models/                       # Entity models (80+ classes)
│   └── Exporting/                    # PDF, Excel, thermal print & email services (70+ classes)
│       └── Utils/                    # Export utilities (PDF, Excel, thermal, mailing)
│
├── 🗄️ DBPrimeBakes/                  # SQL Server Database Project (SSDT)
│   ├── Tables/                       # 40+ database table definitions
│   │   ├── Store/                    # Product, sale, order, stock transfer tables
│   │   ├── Restaurant/               # Bill, dining area/table tables
│   │   ├── Inventory/                # Purchase, kitchen, stock, recipe, raw material tables
│   │   ├── Accounts/                 # Financial accounting, ledger, group, voucher tables
│   │   └── Operations/               # User, location, settings tables
│   ├── StoredProcedures/             # Stored procedures
│   │   ├── LoadData/                 # Data retrieval procedures
│   │   ├── Insert/                   # Data insertion procedures
│   │   └── Delete/                   # Data deletion procedures
│   └── Views/                        # 25+ database views (overview & item-level)
│
├── 📤 PushNotificationsAPI/          # Notification Backend API
│   ├── Controllers/                  # API controllers
│   ├── Services/                     # Notification hub services
│   ├── Models/                       # API models
│   └── Authentication/               # API key authentication handler
│
└── 📊 ExcelImport/                   # Data Import Utility
    └── Program.cs                    # Bulk data import tool
```

---

## 🛠 Technology Stack

### **Frontend**
| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET MAUI** | 10.0 | Cross-platform native apps |
| **Blazor Hybrid** | 10.0 | UI framework for MAUI |
| **Blazor Server** | 10.0 | Web application hosting |
| **Syncfusion Blazor** | 32.2.7 | Enterprise UI components (Grid, Dropdowns, Calendars, Inputs, Notifications, Popups) |
| **Toolbelt HotKeys2** | 6.2.0 | Keyboard shortcuts |
| **Blazor.Bluetooth** | 1.0.6 | Bluetooth printer connectivity (Web) |

### **Backend**
| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET 10 / C# 14** | 10.0 | Application framework |
| **Dapper** | 2.1.72 | Micro-ORM for data access |
| **Microsoft.Data.SqlClient** | 6.1.4 | Azure SQL Database connectivity |
| **Azure.Storage.Blobs** | 12.27.0 | Document & attachment storage |
| **SkiaSharp** | 3.119.2 | Image processing for exports |

### **Services & APIs**
| Technology | Version | Purpose |
|------------|---------|---------|
| **ASP.NET Core Web API** | 10.0 | Push notification backend |
| **Azure Notification Hubs** | 4.2.0 | Cross-platform push notifications |
| **Firebase Cloud Messaging** | — | Android push notifications |
| **MailKit / MimeKit** | 4.15.1 | Email invoice & report delivery |
| **Plugin.LocalNotification** | 13.0.0 | In-app local notifications |
| **Plugin.Maui.Audio** | 4.0.0 | Notification sounds |

### **Export & Reporting**
| Technology | Version | Purpose |
|------------|---------|---------|
| **Syncfusion PDF** | 32.2.7 | PDF invoice & report generation |
| **Syncfusion XlsIO** | 32.2.7 | Excel export functionality |
| **NumericWordsConversion** | 2.1.1 | Amount-to-words on invoices |

### **DevOps & Tooling**
| Technology | Purpose |
|------------|---------|
| **SQL Server Data Tools (SSDT)** | Database project management |
| **GitHub Actions** | CI/CD, releases, and auto-updates |

---

## 📦 Modules

### 🍽️ Restaurant Module

| Feature | Desktop | Mobile | Description |
|---------|:-------:|:------:|-------------|
| **Bill Entry** | ✅ | ✅ | Create dine-in billing transactions |
| **Dining Dashboard** | ✅ | ✅ | Table status overview with quick actions |
| **Dining Area Management** | ✅ | ❌ | Configure restaurant dining areas |
| **Dining Table Management** | ✅ | ❌ | Configure tables per dining area |
| **Bill Reports** | ✅ | ❌ | Transaction-level and item-wise reports |
| **KOT Printing** | ✅ | ✅ | Kitchen order ticket thermal printing |
| **Bill Thermal Print** | ✅ | ✅ | Receipt printing via Bluetooth/USB |

### 🛍️ Store Module

| Feature | Desktop | Mobile | Description |
|---------|:-------:|:------:|-------------|
| **Sale Entry** | ✅ | ✅ | Create sales transactions with POS |
| **Order Entry** | ✅ | ✅ | Process customer orders with mobile flow |
| **Sale Return** | ✅ | ❌ | Handle product returns and refunds |
| **Stock Transfer** | ✅ | ❌ | Transfer inventory between locations |
| **Product Management** | ✅ | ❌ | Product catalog with categories |
| **Product Location Pricing** | ✅ | ❌ | Location-specific product rates |
| **KOT Category Management** | ✅ | ❌ | Kitchen order ticket categories |
| **Customer Management** | ✅ | ❌ | Customer database |
| **Tax Configuration** | ✅ | ❌ | GST/Tax setup with product mapping |
| **Sale Reports** | ✅ | ❌ | Transaction and item-wise reports |
| **Sale Return Reports** | ✅ | ❌ | Return transaction and item reports |
| **Order Reports** | ✅ | ❌ | Order transaction and item reports |
| **Stock Transfer Reports** | ✅ | ❌ | Transfer transaction and item reports |
| **Outlet Summary Report** | ✅ | ❌ | Multi-outlet consolidated performance |
| **Sale Thermal Print** | ✅ | ✅ | Receipt printing via Bluetooth/USB |

### 📦 Inventory Module

| Feature | Desktop | Mobile | Description |
|---------|:-------:|:------:|-------------|
| **Purchase Entry** | ✅ | ❌ | Record raw material purchases |
| **Purchase Return** | ✅ | ❌ | Return materials to suppliers |
| **Kitchen Issue** | ✅ | ❌ | Issue raw materials to production |
| **Kitchen Production** | ✅ | ❌ | Record finished goods output |
| **Kitchen Management** | ✅ | ❌ | Configure production kitchens |
| **Raw Material Management** | ✅ | ❌ | Ingredient catalog with categories & UoM |
| **Raw Material Categories** | ✅ | ❌ | Organize raw materials by category |
| **Recipe Management** | ✅ | ❌ | Product recipes with BOM |
| **Product Stock Adjustment** | ✅ | ❌ | Manual finished goods corrections |
| **Raw Material Stock Adjustment** | ✅ | ❌ | Manual raw material corrections |
| **Purchase Reports** | ✅ | ❌ | Transaction and item-wise reports |
| **Purchase Return Reports** | ✅ | ❌ | Return transaction and item reports |
| **Kitchen Issue Reports** | ✅ | ❌ | Issue transaction and item reports |
| **Kitchen Production Reports** | ✅ | ❌ | Production transaction and item reports |
| **Product Stock Report** | ✅ | ❌ | Opening, closing, purchase, sale stock with valuation |
| **Raw Material Stock Report** | ✅ | ❌ | Comprehensive material stock with pricing |

### 💼 Accounts Module

| Feature | Desktop | Mobile | Description |
|---------|:-------:|:------:|-------------|
| **Financial Accounting** | ✅ | ❌ | Voucher entry system (payment, receipt, journal, contra) |
| **Ledger Management** | ✅ | ❌ | Chart of accounts with opening balances |
| **Group Management** | ✅ | ❌ | Account grouping by nature |
| **Account Types** | ✅ | ❌ | Account type classification |
| **Company Management** | ✅ | ❌ | Multi-company support |
| **Voucher Management** | ✅ | ❌ | Voucher type configuration |
| **Financial Year** | ✅ | ❌ | Multi-year period management |
| **State/UT Configuration** | ✅ | ❌ | State and union territory master data |
| **Auto Posting** | ✅ | ❌ | Automatic accounting from sales & bills |
| **Financial Accounting Report** | ✅ | ❌ | Voucher-wise transaction report |
| **Accounting Ledger Report** | ✅ | ❌ | Ledger-wise transaction details |
| **Trial Balance** | ✅ | ❌ | Company-wise trial balance with opening & closing |
| **Profit & Loss** | ✅ | ❌ | Income statement by nature |
| **Balance Sheet** | ✅ | ❌ | Financial position statement |

### ⚙️ Operations Module

| Feature | Desktop | Mobile | Description |
|---------|:-------:|:------:|-------------|
| **User Management** | ✅ | ❌ | Role-based access control (Admin, Sales, Order, Inventory, Accounts) |
| **Location Management** | ✅ | ❌ | Multi-outlet configuration with ledger mapping |
| **Settings** | ✅ | ❌ | System-wide configuration |
| **Local Settings** | ✅ | ✅ | Device-local preferences |
| **Reports Dashboard** | ✅ | ❌ | Centralized access to all reports |

---

## 🗃️ Database Schema

### Core Tables Structure

```
┌─────────────────────────────────────────────────────────────────────┐
│                          OPERATIONS                                  │
├─────────────────────────────────────────────────────────────────────┤
│  User          │  Location       │  Settings                        │
│  ├─ Id         │  ├─ Id          │  ├─ Id                          │
│  ├─ Name       │  ├─ Name        │  ├─ Key                         │
│  ├─ Passcode   │  ├─ Code        │  └─ Value                       │
│  ├─ LocationId │  ├─ Discount    │                                  │
│  ├─ Sales ✓    │  └─ LedgerId    │                                  │
│  ├─ Order ✓    │                 │                                  │
│  ├─ Inventory ✓│                 │                                  │
│  ├─ Accounts ✓ │                 │                                  │
│  └─ Admin ✓    │                 │                                  │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                          RESTAURANT                                  │
├─────────────────────────────────────────────────────────────────────┤
│  DiningArea    │  DiningTable    │  Bill           │  BillDetail     │
│  ├─ Id         │  ├─ Id          │  ├─ Id          │  ├─ Id          │
│  └─ Name       │  ├─ DiningArea  │  ├─ Date        │  ├─ BillId      │
│                │  ├─ Name        │  ├─ LocationId  │  ├─ ProductId   │
│                │  └─ Capacity    │  ├─ CompanyId   │  ├─ Quantity    │
│                │                 │  ├─ Running     │  └─ Rate        │
│                │                 │  └─ Total       │                 │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                             STORE                                    │
├─────────────────────────────────────────────────────────────────────┤
│  Product       │  Customer       │  Sale           │  Order          │
│  ├─ Id         │  ├─ Id          │  ├─ Id          │  ├─ Id          │
│  ├─ Name       │  ├─ Name        │  ├─ Date        │  ├─ Date        │
│  ├─ Code       │  └─ Number      │  ├─ CustomerId  │  ├─ CustomerId  │
│  ├─ CategoryId │                 │  ├─ LocationId  │  ├─ LocationId  │
│  ├─ Rate       │  Tax            │  └─ Total       │  └─ Total       │
│  └─ TaxId      │  ├─ Id          │                 │                 │
│                │  ├─ Name        │  SaleReturn     │  StockTransfer  │
│  ProductLoc.   │  └─ Percentage  │  ├─ Id          │  ├─ Id          │
│  ├─ ProductId  │                 │  ├─ SaleId      │  ├─ FromLoc     │
│  ├─ LocationId │  KOTCategory    │  └─ Total       │  └─ ToLoc       │
│  └─ Rate       │  ├─ Id          │                 │                 │
│                │  └─ Name        │                 │                 │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                          INVENTORY                                   │
├─────────────────────────────────────────────────────────────────────┤
│  RawMaterial   │  Recipe         │  Purchase       │  KitchenIssue   │
│  ├─ Id         │  ├─ Id          │  ├─ Id          │  ├─ Id          │
│  ├─ Name       │  ├─ ProductId   │  ├─ Date        │  ├─ Date        │
│  ├─ Code       │  └─ Details[]   │  ├─ PartyId     │  ├─ KitchenId   │
│  ├─ CategoryId │                 │  └─ Total       │  └─ Details[]   │
│  ├─ Rate       │  Kitchen        │                 │                 │
│  └─ UOM        │  ├─ Id          │  PurchaseReturn │  KitchenProd.   │
│                │  └─ Name        │  ├─ Id          │  ├─ Id          │
│  RMCategory    │                 │  └─ Total       │  ├─ KitchenId   │
│  ├─ Id         │  ProductStock   │                 │  └─ Details[]   │
│  └─ Name       │  ├─ ProductId   │  RMStock        │                 │
│                │  ├─ LocationId  │  ├─ RMId        │                 │
│                │  └─ Quantity    │  └─ Quantity    │                 │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                          ACCOUNTS                                    │
├─────────────────────────────────────────────────────────────────────┤
│  Ledger        │  Group          │  Voucher        │  FinAccounting  │
│  ├─ Id         │  ├─ Id          │  ├─ Id          │  ├─ Id          │
│  ├─ Name       │  ├─ Name        │  ├─ Name        │  ├─ VoucherId   │
│  ├─ GroupId    │  ├─ NatureId    │  └─ Type        │  ├─ CompanyId   │
│  └─ OpeningBal │  └─ TypeId      │                 │  └─ Date        │
│                │                 │  FinancialYear  │                 │
│  Company       │  AccountType    │  ├─ Id          │  FinAccDetail   │
│  ├─ Id         │  ├─ Id          │  ├─ StartDate   │  ├─ LedgerId    │
│  └─ Name       │  └─ Name        │  └─ EndDate     │  ├─ Debit       │
│                │                 │                 │  └─ Credit      │
│  Nature        │  StateUT        │                 │                 │
│  ├─ Id         │  ├─ Id          │                 │                 │
│  └─ Name       │  └─ Name        │                 │                 │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** or later
- **Visual Studio 2022** (17.8+) with:
  - .NET MAUI workload
  - ASP.NET and web development
  - Data storage and processing (SQL Server Data Tools)
- **Azure Account** (for production deployment)
- **Android SDK** (for Android development)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/aadipoddar/PrimeBakes.git
   cd PrimeBakes
   ```

2. **Configure Secrets**
   
   Update `PrimeBakesLibrary/DataAccess/Secrets.cs` with your connection strings:
   ```csharp
   public static class Secrets
   {
       public static string AzureConnectionString = "your-azure-sql-connection";
       public static string AzureBlobStorageConnectionString = "your-blob-connection";
       // ... other secrets
   }
   ```

3. **Publish Database**
   - Open `DBPrimeBakes/DBPrimeBakes.sqlproj`
   - Right-click → Publish
   - Select target database profile

4. **Run the Application**
   
   **Web:**
   ```bash
   cd PrimeBakes/PrimeBakes.Web
   dotnet run
   ```
   
   **Desktop (Windows):**
   ```bash
   cd PrimeBakes/PrimeBakes
   dotnet build -f net10.0-windows10.0.19041.0
   ```
   
   **Android:**
   ```bash
   cd PrimeBakes/PrimeBakes
   dotnet build -f net10.0-android
   ```

---

## 🌐 Deployment

### Azure Resources Required

| Resource | Purpose |
|----------|---------|
| Azure SQL Database | Primary data storage |
| Azure Blob Storage | Document attachments |
| Azure App Service | Web application hosting |
| Azure Notification Hubs | Push notifications |

### Publishing Profiles

- **LocalDBPrimeBakes.publish.xml** - Local development database
- **AzurePrimeBakesTesting.publish.xml** - Azure testing database
- **AzurePrimeBakes.publish.xml** - Azure production database

---

## 🔐 Security

### Authentication
- **Passcode-based login** - User authentication
- **Role-based access control** - Admin, Sales, Order, Inventory, Accounts permissions
- **Location-based restrictions** - Users tied to specific locations

### API Security
- **API Key Authentication** - Secure notification API endpoints
- **Secure storage** - Encrypted local data storage on devices

### Data Protection
- Azure SQL with TDE (Transparent Data Encryption)
- Secure blob storage with private access
- HTTPS/TLS for all communications

---

## 📱 Platform Support

| Platform | Status | Min Version |
|----------|:------:|-------------|
| Windows 10/11 | ✅ | 10.0.17763.0 |
| Android | ✅ | API 24 (Android 7.0) |
| iOS | ✅ | iOS 15.0 |
| macOS | ✅ | macOS 15.0 |
| Web Browser | ✅ | Modern browsers |

---

## 📄 License

This project is proprietary software developed for **Salasar Foods Guwahati**.

---

## 👨‍💻 Development

<p align="center">
  <strong>Developed with ❤️ by <a href="https://aadisoft.vercel.app">AadiSoft</a></strong>
</p>

<p align="center">
  <img src="PrimeBakes/PrimeBakes.Web/wwwroot/images/logo_resized.png" alt="Prime Bakes" width="150"/>
</p>

---

Latest Version = 1.1.3.1