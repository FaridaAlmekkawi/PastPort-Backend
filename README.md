\# PastPort Backend API



Backend API for PastPort - Virtual Reality Historical Experience Platform



\## 🚀 Technologies



\- .NET 9.0

\- ASP.NET Core Web API

\- Entity Framework Core

\- SQL Server

\- JWT Authentication

\- Redis (Caching)

\- Serilog (Logging)



\## 📋 Features



\- ✅ User Authentication \& Authorization (JWT)

\- ✅ Historical Scenes Management

\- ✅ Characters Management

\- ✅ Conversation History

\- 🔄 AI Integration Layer (In Progress)

\- 🔄 Subscription \& Payment System (In Progress)



\## 🏗️ Architecture



Clean Architecture with:

\- \*\*API Layer\*\*: Controllers \& Middlewares

\- \*\*Application Layer\*\*: Business Logic \& DTOs

\- \*\*Domain Layer\*\*: Entities \& Interfaces

\- \*\*Infrastructure Layer\*\*: Data Access \& External Services



\## ⚙️ Setup



\### Prerequisites

\- .NET 9 SDK

\- SQL Server

\- Redis (optional)



\### Installation



1\. Clone the repository

```bash

git clone https://github.com/YOUR\_USERNAME/PastPort-Backend.git

cd PastPort-Backend

```



2\. Update Connection String in `appsettings.Development.json`



3\. Run Migrations

```bash

dotnet ef database update --project PastPort.Infrastructure --startup-project PastPort.API

```



4\. Run the API

```bash

dotnet run --project PastPort.API

```



5\. Open Swagger

```

https://localhost:7xxx/swagger

```



\## 📁 Project Structure

```

PastPort/

├── PastPort.API/              # API Layer

├── PastPort.Application/      # Business Logic

├── PastPort.Domain/           # Entities \& Interfaces

├── PastPort.Infrastructure/   # Data Access

└── PastPort.Tests/            # Unit Tests

```



\## 🔐 Environment Variables



Create `appsettings.Development.json`:

```json

{

&nbsp; "ConnectionStrings": {

&nbsp;   "DefaultConnection": "YOUR\_CONNECTION\_STRING"

&nbsp; },

&nbsp; "JwtSettings": {

&nbsp;   "SecretKey": "YOUR\_SECRET\_KEY"

&nbsp; }

}

```



\## 📝 API Documentation



Available at `/swagger` when running in Development mode.



\## 👥 Team



\- \*\*Backend Developer\*\*: Omar Abo Elmaaty

\- \*\*AI Team\*\*: \[AI Engineers]

\- \*\*Unity/VR Team\*\*: \[VR Developers]

\- \*\*Flutter Team\*\*: \[Mobile Developers]



\## 📄 License



Private Project - All Rights Reserved



\## 🔗 Links



\- \[Project Proposal](link-to-proposal)

\- \[API Documentation](link-to-docs)

