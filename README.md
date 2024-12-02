# YangSpace

Welcome to YangSpace! This repository contains both the backend and frontend for the YangSpace platform.

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
