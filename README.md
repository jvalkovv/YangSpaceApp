## YangSpace

YangSpace is a web application that connects service providers with users, offering a variety of services with integrated booking and management features. Developed with ASP.NET Core and Angular, it allows service providers to list services, manage bookings, and delete services, while users can browse available services and schedule appointments.

The application is deployed on Azure, providing a scalable and reliable cloud infrastructure for users to interact with the platform.
# Application URL - 
https://yangspace.azurewebsites.net/
## Features
	•	User Authentication: Secure login and role-based authorization with JWT.
	•	Service Management: Providers can create, update, or delete services they offer.
	•	Booking System: Users can view service availability and book appointments.
	•	User Profiles: Each user has a profile, and service providers can manage their listings.
	•	Dynamic UI: Fully responsive interface built with Angular for seamless user experience.

## App Functionality
    User Authentication and Authorization
	•	Users log in with their credentials, and their roles (e.g., provider or customer) are determined.
	•	Service providers can manage their listed services.
	•	JWT tokens are used for secure authentication and API interaction.

## Service Management
	•	Service providers can create, edit, and delete services.
	•	Services include detailed descriptions, available time slots, and pricing.

## Booking System
	•	Users can book services by selecting available time slots.
	•	Booking details are stored and accessible in the user profile.

## Administrative Features
	•	Providers can view all bookings and delete services if needed.
	•	Service providers can only modify or delete services they created.

## User Roles
# Client
	•	Book Services: Browse available services and schedule appointments with service providers.
	•	View Bookings: Manage current and past bookings.
username: cuser
password: cuser1A

# Service Provider
	•	Manage Services: Create, update, or delete services they offer.
	•	View Bookings: View client bookings for their services and manage availability.
	•	Admin Features: Providers can delete their services if no longer required.
usename: spuser
password: spuser1A

## Getting Started

Follow the instructions below to set up the project on your local machine.

### Prerequisites

Make sure you have the following software installed:
- [Node.js](https://nodejs.org/)
- [.NET Core SDK](https://dotnet.microsoft.com/download)
- SQL Server (or use Docker to run a SQL Server instance)
You can check if .NET Core SDK is installed by running:

```bash
dotnet --version
```

### Running the Backend
1. Ensure you have a SQL Server instance running. You can either install SQL Server locally or use Docker. For Docker, you can run:
   
   ```bash
   docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=My@Password123!' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
   ```
   
3. Open a terminal and navigate to the backend folder:

    ```bash
    cd YangSpaceBackEnd
    ```

4. Install dependencies and run the backend server:

    ```bash
    dotnet restore
    dotnet ef database update
    dotnet run
    ```

### Running the Frontend

1. Open a new terminal and navigate to the frontend folder:

    ```bash
    cd YangSpaceClient
    ```

2. Install the necessary packages:

    ```bash
    npm install
    ```

3. Start the Angular development server:

    ```bash
    ng serve
    ```

### Additional Information

- The backend server runs on `http://localhost:5290`
- The frontend server runs on `http://localhost:4200`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---
