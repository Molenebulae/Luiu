# 使用 .NET 10 SDK 進行編譯與發行
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /src

# 複製專案檔並進行套件還原 (Restore)
COPY ["API/API.csproj", "API/"]
COPY ["Luiu.Domain/Luiu.Domain.csproj", "Luiu.Domain/"]
COPY ["Luiu.Service/Luiu.Service.csproj", "Luiu.Service/"]
RUN dotnet restore "API/API.csproj"

# 複製所有剩餘的原始碼，並編譯成 Release 版本
COPY . .
RUN dotnet publish "API/API.csproj" -c Release -o out

# 建立極小化的運行環境，只包含 .NET 10 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /App

# 從編譯階段把發行好的檔案複製過來
COPY --from=build-env /src/out .

# 設定 Kestrel 伺服器在容器內監聽 80 埠
ENV ASPNETCORE_URLS=http://+:80

# 執行專案的核心 DLL（請確保大小寫與你的專案名稱完全一致）
ENTRYPOINT ["dotnet", "API.dll"]