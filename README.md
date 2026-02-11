<p align="center">
  <img src="PrimeBakes/PrimeBakes.Web/wwwroot/images/logo_full.png" alt="Prime Bakes Logo" width="400"/>
</p>

<h1 align="center">🧁 Prime Bakes</h1>

<p align="center">
  <strong>Enterprise-Grade Order, Sales, Inventory & Accounts Management System</strong>
</p>

<p align="center">
  <em>A comprehensive business management solution for Salasar Foods Guwahati</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-purple?style=for-the-badge&logo=dotnet" alt=".NET"/>
  <img src="https://img.shields.io/badge/Blazor-Hybrid-blueviolet?style=for-the-badge&logo=blazor" alt="Blazor"/>
  <img src="https://img.shields.io/badge/MAUI-Cross_Platform-green?style=for-the-badge&logo=xamarin" alt="MAUI"/>
  <img src="https://img.shields.io/badge/Azure-SQL_&_Blob-0089D6?style=for-the-badge&logo=microsoft-azure" alt="Azure"/>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%20|%20Android%20|%20iOS%20|%20macOS%20|%20Web-lightgrey?style=flat-square" alt="Platforms"/>
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
- [API Reference](#-api-reference)
- [Security](#-security)
- [License](#-license)

---

## 🎯 Overview

**Prime Bakes** is a full-featured enterprise resource planning (ERP) system designed specifically for bakery and food manufacturing businesses. Built with modern .NET technologies, it provides seamless cross-platform functionality across desktop, mobile, and web environments.

The system handles the complete business lifecycle from raw material procurement to finished goods sales, including comprehensive financial accounting and real-time inventory tracking across multiple locations.

<p align="center">
  <img src="PrimeBakes/PrimeBakes/Resources/AppIcon/logo.png" alt="App Icon" width="120"/>
</p>

---

## ✨ Key Features

### 🛒 **Sales Management**
- **Point of Sale (POS)** - Desktop and mobile-optimized interfaces
- **Order Processing** - Customer order creation and tracking
- **Sales Returns** - Complete return and refund management
- **Stock Transfers** - Inter-location inventory transfers
- **Customer Management** - Customer database with contact information

### 📦 **Inventory Management**
- **Purchase Orders** - Raw material procurement tracking
- **Kitchen Operations** - Production issue and completion tracking
- **Recipe Management** - Product recipes with ingredient mapping
- **Stock Adjustments** - Manual stock corrections and auditing
- **Multi-Location Support** - Track inventory across multiple outlets

### 💰 **Financial Accounting**
- **Double-Entry Bookkeeping** - Complete accounting system
- **Voucher Management** - Payment, receipt, and journal entries
- **Ledger Accounts** - Full chart of accounts management
- **Financial Statements** - Trial Balance, P&L, Balance Sheet
- **Multi-Year Support** - Financial year management

### 📊 **Reporting & Analytics**
- **Sales Reports** - Item-wise and transaction reports
- **Purchase Reports** - Vendor and item analysis
- **Stock Reports** - Real-time inventory levels
- **Production Reports** - Kitchen issue and production tracking
- **Accounting Reports** - Comprehensive financial analysis

### 🔔 **Push Notifications**
- Real-time order notifications
- Stock alert notifications
- Firebase Cloud Messaging integration
- Azure Notification Hubs backend

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
                     │   • Export Services         │
                     └──────────────┬──────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         │                         │                          │
         ▼                         ▼                          ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────────┐
│  Azure SQL      │     │  Azure Blob     │     │  Push Notifications │
│  Database       │     │  Storage        │     │  API                │
│  • Tables       │     │  • Documents    │     │  • Azure Notification│
│  • Stored Procs │     │  • Attachments  │     │    Hubs             │
│  • Views        │     │                 │     │  • Firebase FCM     │
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
│   │   └── Components/               # MAUI-specific components
│   │
│   ├── 🔗 PrimeBakes.Shared/         # Shared Blazor Components
│   │   ├── Pages/                    # All application pages
│   │   │   ├── Sales/                # Sales module pages
│   │   │   ├── Inventory/            # Inventory module pages
│   │   │   ├── Accounts/             # Accounting module pages
│   │   │   └── Operations/           # Admin & settings pages
│   │   ├── Components/               # Reusable UI components
│   │   ├── Layout/                   # Main layout templates
│   │   └── Services/                 # Shared service interfaces
│   │
│   └── 🌐 PrimeBakes.Web/            # Blazor Server Web App
│       ├── Services/                 # Web-specific implementations
│       └── wwwroot/                  # Static assets (images, JS, CSS)
│
├── 📚 PrimeBakesLibrary/             # Core Business Library
│   ├── Data/                         # Data access classes
│   │   ├── Sales/                    # Sales data operations
│   │   ├── Inventory/                # Inventory data operations
│   │   ├── Accounts/                 # Accounting data operations
│   │   └── Common/                   # Shared data utilities
│   ├── DataAccess/                   # Database & blob connectivity
│   ├── Models/                       # Entity models
│   │   ├── Sales/                    # Sales entities
│   │   ├── Inventory/                # Inventory entities
│   │   ├── Accounts/                 # Accounting entities
│   │   └── Operations/               # System entities
│   └── Exporting/                    # PDF & Excel export services
│
├── 🗄️ DBPrimeBakes/                  # SQL Server Database Project
│   ├── Tables/                       # Database table definitions
│   ├── StoredProcedures/             # Stored procedures
│   │   ├── LoadData/                 # Data retrieval procedures
│   │   ├── Insert/                   # Data insertion procedures
│   │   └── Delete/                   # Data deletion procedures
│   ├── Views/                        # Database views
│   └── Types/                        # Custom SQL types
│
├── 📤 PushNotificationsAPI/          # Notification Backend API
│   ├── Controllers/                  # API controllers
│   ├── Services/                     # Notification services
│   ├── Models/                       # API models
│   └── Authentication/               # API key authentication
│
└── 📊 ExcelImportProduct/            # Data Import Utility
    └── Program.cs                    # Bulk data import tool
```

---

## 🛠 Technology Stack

### **Frontend**
| Technology | Purpose |
|------------|---------|
| **.NET MAUI** | Cross-platform native apps |
| **Blazor Hybrid** | UI framework for MAUI |
| **Blazor Server** | Web application hosting |
| **Syncfusion Blazor** | Enterprise UI components |
| **Toolbelt HotKeys2** | Keyboard shortcuts |

### **Backend**
| Technology | Purpose |
|------------|---------|
| **.NET 10** | Application framework |
| **Dapper** | Micro-ORM for data access |
| **Azure SQL Database** | Cloud database |
| **Azure Blob Storage** | Document storage |

### **Services & APIs**
| Technology | Purpose |
|------------|---------|
| **ASP.NET Core Web API** | Push notification backend |
| **Azure Notification Hubs** | Cross-platform push notifications |
| **Firebase Cloud Messaging** | Android notifications |
| **MailKit/MimeKit** | Email services |

### **Export & Reporting**
| Technology | Purpose |
|------------|---------|
| **Syncfusion PDF** | PDF document generation |
| **Syncfusion XlsIO** | Excel export functionality |

### **DevOps & Tooling**
| Technology | Purpose |
|------------|---------|
| **SQL Server Data Tools** | Database project management |
| **GitHub Actions** | CI/CD and releases |

---

## 📦 Modules

### 🛍️ Sales Module

| Feature | Desktop | Mobile | Description |
|---------|:-------:|:------:|-------------|
| **Sale Entry** | ✅ | ✅ | Create sales transactions |
| **Order Entry** | ✅ | ✅ | Process customer orders |
| **Sale Return** | ✅ | ❌ | Handle product returns |
| **Stock Transfer** | ✅ | ❌ | Transfer between locations |
| **Customer Management** | ✅ | ❌ | Manage customer database |
| **Product Management** | ✅ | ❌ | Product catalog |
| **Tax Configuration** | ✅ | ❌ | GST/Tax setup |

### 📦 Inventory Module

| Feature | Description |
|---------|-------------|
| **Purchase Entry** | Record raw material purchases |
| **Purchase Return** | Return materials to suppliers |
| **Kitchen Issue** | Issue raw materials to production |
| **Kitchen Production** | Record finished goods production |
| **Raw Material Management** | Ingredient catalog |
| **Recipe Management** | Product recipes with BOM |
| **Stock Adjustment** | Manual inventory corrections |

### 💼 Accounts Module

| Feature | Description |
|---------|-------------|
| **Financial Accounting** | Voucher entry system |
| **Ledger Management** | Chart of accounts |
| **Group Management** | Account grouping |
| **Company Management** | Multi-company support |
| **Trial Balance** | Financial statement |
| **Profit & Loss** | Income statement |
| **Balance Sheet** | Financial position |

### ⚙️ Operations Module

| Feature | Description |
|---------|-------------|
| **User Management** | Role-based access control |
| **Location Management** | Multi-outlet support |
| **Settings** | System configuration |
| **Reports Dashboard** | Centralized reporting |

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
│                            SALES                                     │
├─────────────────────────────────────────────────────────────────────┤
│  Product       │  Customer       │  Sale           │  Order          │
│  ├─ Id         │  ├─ Id          │  ├─ Id          │  ├─ Id          │
│  ├─ Name       │  ├─ Name        │  ├─ Date        │  ├─ Date        │
│  ├─ Code       │  └─ Number      │  ├─ CustomerId  │  ├─ CustomerId  │
│  ├─ CategoryId │                 │  ├─ LocationId  │  ├─ LocationId  │
│  ├─ Rate       │  Tax            │  └─ Total       │  └─ Total       │
│  └─ TaxId      │  ├─ Id          │                 │                 │
│                │  ├─ Name        │  StockTransfer  │                 │
│  ProductLoc.   │  └─ Percentage  │  ├─ Id          │                 │
│  ├─ ProductId  │                 │  ├─ FromLoc     │                 │
│  ├─ LocationId │                 │  └─ ToLoc       │                 │
│  └─ Rate       │                 │                 │                 │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                          INVENTORY                                   │
├─────────────────────────────────────────────────────────────────────┤
│  RawMaterial   │  Recipe         │  Purchase       │  KitchenIssue   │
│  ├─ Id         │  ├─ Id          │  ├─ Id          │  ├─ Id          │
│  ├─ Name       │  ├─ ProductId   │  ├─ Date        │  ├─ Date        │
│  ├─ Code       │  └─ Details[]   │  ├─ SupplierId  │  ├─ KitchenId   │
│  ├─ CategoryId │                 │  └─ Total       │  └─ Details[]   │
│  ├─ Rate       │  Kitchen        │                 │                 │
│  └─ UOM        │  ├─ Id          │  ProductStock   │  RMStock        │
│                │  └─ Name        │  ├─ ProductId   │  ├─ RMId        │
│                │                 │  ├─ LocationId  │  └─ Quantity    │
│                │                 │  └─ Quantity    │                 │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                          ACCOUNTS                                    │
├─────────────────────────────────────────────────────────────────────┤
│  Ledger        │  Group          │  Voucher        │  Transaction    │
│  ├─ Id         │  ├─ Id          │  ├─ Id          │  ├─ Id          │
│  ├─ Name       │  ├─ Name        │  ├─ Name        │  ├─ VoucherId   │
│  ├─ GroupId    │  └─ TypeId      │  └─ Type        │  ├─ LedgerId    │
│  └─ OpeningBal │                 │                 │  ├─ Debit       │
│                │  AccountType    │  FinancialYear  │  └─ Credit      │
│  Company       │  ├─ Id          │  ├─ Id          │                 │
│  ├─ Id         │  └─ Name        │  ├─ StartDate   │                 │
│  └─ Name       │                 │  └─ EndDate     │                 │
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
- **AzureDBPrimeBakes.publish.xml** - Azure production database

---

## 🔐 Security

### Authentication
- **Passcode-based login** - 4-digit user authentication
- **Role-based access control** - Admin, Sales, Order, Inventory, Accounts
- **Location-based restrictions** - Users tied to specific locations

### API Security
- **API Key Authentication** - Secure notification API
- **Secure storage** - Encrypted local data storage

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

Latest Version = 1.0.9.1