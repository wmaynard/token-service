FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY bin/Release/net5.0/ .
ENTRYPOINT ["dotnet", "token-service.dll"]
