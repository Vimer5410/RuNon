using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.Sqlite;
using Microsoft.JSInterop;
using RuNon_Client.Services;
namespace RuNon_Client.Components.Pages;

public abstract class ChatBase:ComponentBase
{
    
    [Inject] private NavigationManager Navigation { get; set; }
    [Inject] protected ProtectedLocalStorage _protectedLocalStorage { get; set; }
    
    protected HubConnection? hubConnection;
    protected string yourClientID;
    protected string interviewerClientID;
    protected bool isUserBanned;
    protected bool ShowToast;
    protected string userIp = "";

    
    List<TimeSpan> reconnectTime = new List<TimeSpan>();
    
    protected virtual async Task InitHubConnection()
    {
        for (int i = 0; i < 3; i++)
        {
            reconnectTime.Add(TimeSpan.FromSeconds(i*5));
        }
        
        try
        {
            // Игнорируем SSL ошибки для самоподписанного сертификата
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
    
        
            hubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/simplehub"), options =>
                {
                    options.HttpMessageHandlerFactory = _ => httpClientHandler;
                })
                .WithAutomaticReconnect()
                .Build();
                
            await hubConnection.StartAsync();
            yourClientID = hubConnection.ConnectionId;
            // методы signalR хаба, которые касаются основной работы с войс чатом
            hubConnection.On<string>("ReceiveUserIp", (UserIp) =>
            {
                InvokeAsync(() =>
                {
                    userIp = UserIp;
                    if (userIp=="::1")
                    {
                        userIp = "127.0.0.1";
                    }
                });
            });
            
        }
        catch (Exception e)
        {
            Console.WriteLine($"[HUB] Ошибка: {e}");
            throw;
        }
    }
    
    // логика проверки забаненных пользователей
    public async Task<bool> IsUserBanned(string ipAddress, string Guid)
    {
        var ipDbConnectionString = "Data Source=Banned_Ip.db"; 
        var guidDbConnectionString = "Data Source=Banned_GUID.db";

        using (SqliteConnection sqliteConnectionIp = new SqliteConnection(ipDbConnectionString))
        {
            await sqliteConnectionIp.OpenAsync();
            var ipSelect = "SELECT COUNT(*) FROM Banned_Ip WHERE ip_address = @ip";
            
            using (var ipSelectCommand=new SqliteCommand(ipSelect, sqliteConnectionIp))
            {
                ipSelectCommand.Parameters.AddWithValue("@ip", ipAddress);
                long res= (long)await ipSelectCommand.ExecuteScalarAsync();
                
                using (SqliteConnection sqliteConnectionGuid = new SqliteConnection(guidDbConnectionString))
                {
                    await sqliteConnectionGuid.OpenAsync();
                    var guidSelect = "SELECT COUNT(*) FROM Banned_GUID WHERE guid = @guid";
                    using (var guidSelectCommand=new SqliteCommand(guidSelect,sqliteConnectionGuid))
                    {
                        guidSelectCommand.Parameters.AddWithValue("@guid", Guid);
                        res += (long)await guidSelectCommand.ExecuteScalarAsync();
                    }
                }
                return res > 0;
            }
        }
    }
    
    public async Task CreateOrIdentify()
    {
        var userGuidLocalStorage = await _protectedLocalStorage.GetAsync<string>("user_id");
        var userGuid = Guid.NewGuid().ToString();
        
        var ipDbConnectionString = "Data Source=Banned_Ip.db"; 
        var guidDbConnectionString = "Data Source=Banned_GUID.db"; 
        
        SqliteConnection sqliteConnectionIp = new SqliteConnection(ipDbConnectionString);
        SqliteConnection sqliteConnectionGuid = new SqliteConnection(guidDbConnectionString);
        
        await sqliteConnectionIp.OpenAsync();
        await sqliteConnectionGuid.OpenAsync();
        
        var sqlCreateBannedIPTable = @"
            CREATE TABLE IF NOT EXISTS Banned_Ip(
                ip_address TEXT PRIMARY KEY
                )";
        
        var sqlCreateBannedGUIDTable = @"
            CREATE TABLE IF NOT EXISTS Banned_GUID(
                guid TEXT PRIMARY KEY
                )";
        
        SqliteCommand sqlCreateBannedIPTableCommand = new SqliteCommand(sqlCreateBannedIPTable, sqliteConnectionIp);
        SqliteCommand sqlCreateBannedGUIDTableCommand = new SqliteCommand(sqlCreateBannedGUIDTable, sqliteConnectionGuid);
        
        await sqlCreateBannedIPTableCommand.ExecuteNonQueryAsync();
        await sqlCreateBannedGUIDTableCommand.ExecuteNonQueryAsync();
        
        if (String.IsNullOrEmpty(userGuidLocalStorage.Value))
        {
            _protectedLocalStorage.SetAsync("user_id",userGuid);
            Console.WriteLine($"Обнаружен новый GUID: {userGuid}");
        }
        isUserBanned = await IsUserBanned(userIp, userGuidLocalStorage.Value);
        var banNotify = isUserBanned ? "Пользователь забанен" : "Пользователь чист";
        Console.WriteLine(banNotify);
    }
    
    private async Task BanTechSupport()
    {
        // переменная для проверки(чтобы избежать повторения записей)
        int count = 0;
        
        
        var userGuidLocalStorage = await _protectedLocalStorage.GetAsync<string>("user_id");
        var BannedUsersSupportConnectionString = "Data Source=Banned_Users_Support.db";
        SqliteConnection BannedUsersSupporConnection = new SqliteConnection(BannedUsersSupportConnectionString);
        await BannedUsersSupporConnection.OpenAsync();
        var sqlCreateBannedUsersSupporTable = @"
            CREATE TABLE IF NOT EXISTS Banned_Users_Support(
                ip_address TEXT,
                guid TEXT,
                date TEXT
                )";
        
        var sqlInsertBannedUser = @"INSERT INTO Banned_Users_Support(ip_address, guid, date)
                                  VALUES (@ip_address, @guid, @date)";

        var sqlCheckIfIpExistsCommand = @"
            SELECT COUNT(*) 
            FROM Banned_Users_Support 
            WHERE ip_address = @ip_address_to_check;
            ";
        var sqlCheckIfGUIDExistsCommand = @"
            SELECT COUNT(*) 
            FROM Banned_Users_Support 
            WHERE guid = @guid_to_check;
            ";
        
        using (SqliteCommand BannedUsersSupporCreateTable = new SqliteCommand(sqlCreateBannedUsersSupporTable, BannedUsersSupporConnection))
        {
            await BannedUsersSupporCreateTable.ExecuteNonQueryAsync();
        }

        using (SqliteCommand CheckIfIpExists=new SqliteCommand(sqlCheckIfIpExistsCommand,BannedUsersSupporConnection))
        {
            CheckIfIpExists.Parameters.AddWithValue("@ip_address_to_check", userIp);
            object? res;
            res = await CheckIfIpExists.ExecuteScalarAsync();
            count = + Convert.ToInt32(res);
        }

        using (SqliteCommand CheckIfGUIDExists=new SqliteCommand(sqlCheckIfGUIDExistsCommand, BannedUsersSupporConnection))
        {
            CheckIfGUIDExists.Parameters.AddWithValue("@guid_to_check", userGuidLocalStorage.Value);
            object? res;
            res = await CheckIfGUIDExists.ExecuteScalarAsync();
            count = + Convert.ToInt32(res);
        }

        if (count>0)
        {
            Console.WriteLine($"Уже существует в бд {count}");
        }
        else
        {
            using (SqliteCommand sqlInsertBannedUserCommand = new SqliteCommand(sqlInsertBannedUser, BannedUsersSupporConnection))
            {
                sqlInsertBannedUserCommand.Parameters.AddWithValue("@ip_address", userIp);
                sqlInsertBannedUserCommand.Parameters.AddWithValue("@guid", userGuidLocalStorage.Value ?? "uknow_guid");
                sqlInsertBannedUserCommand.Parameters.AddWithValue("@date", DateTime.Now.ToString("G"));
                sqlInsertBannedUserCommand.ExecuteNonQuery();
            }
        }
    }
    
    protected async Task BanReboot_Btn()
    {
        Navigation.NavigateTo("/");
    }

    protected async Task BanSendTechSupport_Btn()
    {
        ShowToast = true;
        StateHasChanged();
        await Task.Delay(2000);
        ShowToast = false;
        await BanTechSupport();
    }
}