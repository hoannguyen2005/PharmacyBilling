# Sử dụng base image .NET SDK để build và publish ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file csproj và thực hiện restore các gói Nuget
COPY ["PharmacyBillingService.csproj", "./"]
RUN dotnet restore "./PharmacyBillingService.csproj"

# Copy toàn bộ mã nguồn còn lại và biên dịch
COPY . .
RUN dotnet build "PharmacyBillingService.csproj" -c Release -o /app/build

# Publish ứng dụng ra thư mục đầu ra
FROM build AS publish
RUN dotnet publish "PharmacyBillingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Sử dụng runtime image gọn nhẹ cho môi trường Production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Mở cổng 8080 (Cổng mặc định của .NET 8) để kết nối bên ngoài
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Chạy ứng dụng khi container khởi động
ENTRYPOINT ["dotnet", "PharmacyBillingService.dll"]
