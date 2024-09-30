# InnoShop

**InnoShop** is a web-based e-commerce system that allows users to manage and purchase products. The system includes role-based access where administrators have full control over the platform, and regular users can manage their own products after email verification.

## Features

- **User roles**:
  - **Administrator**: Full control over all products and users.
  - **Regular users**: Can create, edit, and delete their own products after email verification.
  - **Non-authenticated users**: Can view products, but cannot manage or create them.

- **Product management**:
  - Regular users can add, edit, and delete their own products.
  - Administrators can manage all products.

- **Authentication and Authorization**:
  - Email confirmation is required for users before they can create a product.
  - JWT-based authentication for API access.

## Steps to Set Up

### [AuthAPI](InnoShop.Services.AuthAPI) 
1) In the **appsettings.json** file you need to configure the **"ConnectionStrings:DefaultConnection"** by adding your SQL Server connection string.
2) Also in the **appsettings.json** file you need to configure the connection via **SmtpClient**.
Example: 
```json
{
  "EmailSettings": {
    "From": "you@gmail.com",
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "email@gmail.com",
    "Password": "smtppassword"
  },
  "ConnectionStrings": {
    "DockerConnection": "Server=innoshop-master-db-1;Database=InnoShop_AuthAPI;User Id=sa;Password=;Integrated Security=False;TrustServerCertificate=True;Connection Timeout=30;",
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InnoShop_Auth;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

3) To run tests in AuthAPI, you also need to create a file **appsettings.Test.json** in which you need to add your connection string to the given SQL Server database in the **"ConnectionStrings:DefaultConnection"**.
4) To work with Docker, in the **appsettings.json file**, you need to take the connection string from the **"ConnectionStrings:DockerConnection"** section and paste it with replacement into the **"DefaultConnection"**.

### [ProductAPI](InnoShop.Services.ProductAPI) 
1) In the **appsettings.json** file you need to configure the **"ConnectionStrings:DefaultConnection"** by adding your SQL Server connection string.
2) Before running tests, make sure to set the correct connection string for your SQL Server database in the **appsettings.Test.json** file.
3) To work with Docker, in the **appsettings.json** file, you need to take the connection string from the **"ConnectionStrings":"DockerConnection"** section and paste it with replacement into the **"DefaultConnection"**.

## Initially, two users will be created.

<table style="width:100%">
  <tr>
    <td style="text-align: center;">
      <h3>1. Admin</h3>
      <p><strong>Email:</strong> admin@gmail.com<br>
      <strong>Password:</strong> Admin123*</p>
    </td>
    <td style="text-align: center;">
      <h3>2. Regular</h3>
      <p><strong>Email:</strong> regular@gmail.com<br>
      <strong>Password:</strong> Regular123*</p>
    </td>
  </tr>
</table>

## Libraries used in this project:

- [NUnit](https://github.com/nunit/nunit) and [moq](https://github.com/devlooped/moq) for testing.
- [SmtpServer](https://blog.elmah.io/how-to-send-emails-from-csharp-net-the-definitive-tutorial/) for a simple SMTP server.
- [FluentValidation](https://docs.fluentvalidation.net/en/latest/)
- [AutoMapper](https://docs.automapper.org/en/stable/Getting-started.html)
- [Newtonsoft](https://www.newtonsoft.com/json) for serialization and deserialization.


Before you begin, ensure you have the following installed:

- [.NET SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Docker](https://www.docker.com/) (if containerization is used)
