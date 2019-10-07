FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build-env
# Set your workdir
WORKDIR /app

# copy everything else and build
COPY . ./
RUN dotnet restore
RUN dotnet test 
RUN dotnet publish -c Release -o out

# build runtime image /aspnet -> ASP.NET /runtime -> .NET Core
FROM mcr.microsoft.com/dotnet/core/runtime:3.0
WORKDIR /app

# Copy from build
COPY --from=build-env /app/out .

# Run your app (dll name has to be changed)
ENTRYPOINT ["dotnet", "HonoIntegrationTest.dll"]