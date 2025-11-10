using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Data.Sqlite;

namespace RuNon_Client.Components.Pages;




public partial class Home:ComponentBase
{
 
    [Inject] public NavigationManager Navigation
    {
        get;
        set;
    }

    [Inject] public ProtectedLocalStorage _protectedLocalStorage
    {
        get;
        set;
    }
    
    private HubConnection? hubConnection;   
    public List<(string message, bool isMine)> receiveUserMessage = new List<(string message, bool isMine)>();
    public string yourClientID;
    public string interviewerСlientID;
    public string messagetext;
    public byte[]? publicKeyFromServer;
    public bool isUserBanned;
    public bool ShowToast;
    public string userIp = "";
    
    List<TimeSpan> reconnectTime = new List<TimeSpan>();
    RSA _rsa;
    
    protected override async Task OnInitializedAsync()
    {
        
    for (int i = 0; i < 3; i++)
    {
        reconnectTime.Add(TimeSpan.FromSeconds(i*5));
    }
    
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
        .WithAutomaticReconnect(reconnectTime.ToArray())
        .Build();
    
    _rsa = RSA.Create(1024);
    
    await hubConnection.StartAsync();
    
    hubConnection.On<byte[], byte[], byte[]>("ReceiveMessage", (encryptedMessage, encryptedAesKey, aesIV) => 
    {
        InvokeAsync(() =>
        {
            try
            {
                var aesKey = _rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);
                var decryptedMessage = Decrypt(encryptedMessage, aesKey, aesIV);
                if (!String.IsNullOrEmpty(decryptedMessage))
                {
                    receiveUserMessage.Add((decryptedMessage,false));
                }
                Console.WriteLine($"Received: {decryptedMessage}");
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decrypting message: {ex.Message}");
            }
        }); 
    });

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
    
    hubConnection.On<string>("ReceivePublicKey", (publicKey) => 
    {
        InvokeAsync(() =>
        {
            publicKeyFromServer = Convert.FromBase64String(publicKey);
            Console.WriteLine($"Received public key");
            StateHasChanged();
            }); 
        });
    
    interviewerСlientID= hubConnection.ConnectionId;
    yourClientID = hubConnection.ConnectionId;
    
    await hubConnection.InvokeAsync("GetPublicKey");
    await hubConnection.InvokeAsync("GetUserIp");
    
    var rsaPublicKey = _rsa.ExportRSAPublicKey();
    await hubConnection.InvokeAsync("ReceiveRSAkey", rsaPublicKey);
        
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Task.Delay(3000);
            Console.WriteLine($"После задержки, userIp = {userIp}");
            await CreateOrIdentify();
            StateHasChanged();
        }
    }
    
    private void EnterKeyDown(KeyboardEventArgs obj)
    {
        
        if (obj.Key=="Enter")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            SendTestMessage();
            Console.WriteLine("ВЫ НАЖАЛИ ENTER");
            messagetext = "";
        }
    }

    private async Task SendTestMessage()
    {
        if (!String.IsNullOrEmpty(messagetext))
        {
            receiveUserMessage.Add((messagetext,true));
        }
        
        
        if (hubConnection?.State == HubConnectionState.Connected && !isUserBanned)
        {
            try
            {
                using (Aes aes=Aes.Create())
                {
                    var AesKey = aes.Key;
                    var AesIV = aes.IV;
                    var EncryptMsg = Encrypt(messagetext, AesKey, AesIV);
                    
                    while (publicKeyFromServer==null)
                    {
                        await Task.Delay(100);
                    }
                    using (RSA rsa = RSA.Create())
                    {
                        rsa.ImportRSAPublicKey(publicKeyFromServer,  out _);
                        var AesKeyEncryptedbyRsa = rsa.Encrypt(AesKey, RSAEncryptionPadding.OaepSHA256);
                        // смс образа --> encryptMSG AESkey AESIV
                        await hubConnection.InvokeAsync("SendToClient", interviewerСlientID, EncryptMsg, AesKeyEncryptedbyRsa, AesIV);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        else
        {
            await hubConnection.DisposeAsync();
        }
        messagetext = "";
    }
    
    
    
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
        using (SqliteCommand BannedUsersSupporCreateTable = new SqliteCommand(sqlCreateBannedUsersSupporTable, BannedUsersSupporConnection))
        {
            await BannedUsersSupporCreateTable.ExecuteNonQueryAsync();
        }

        using (SqliteCommand sqlInsertBannedUserCommand = new SqliteCommand(sqlInsertBannedUser, BannedUsersSupporConnection))
        {
            sqlInsertBannedUserCommand.Parameters.AddWithValue("@ip_address", userIp);
            sqlInsertBannedUserCommand.Parameters.AddWithValue("@guid", userGuidLocalStorage.Value ?? "uknow_guid");
            sqlInsertBannedUserCommand.Parameters.AddWithValue("@date", DateTime.Now.ToString("G"));
            sqlInsertBannedUserCommand.ExecuteNonQuery();
        }
    }
    
    
    
    private async Task BanReboot_Btn()
    {
        Navigation.NavigateTo("/");
    }

    private async Task BanSendTechSupport_Btn()
    {
        ShowToast = true;
        StateHasChanged();
        await Task.Delay(2000);
        ShowToast = false;
        await BanTechSupport();
    }
    
    
    
    
    public static byte[] Encrypt(string text, byte[] key, byte[] iv)
    {
        using (Aes aes=Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            using (ICryptoTransform encryptor= aes.CreateEncryptor(aes.Key,aes.IV))
            {
                using (MemoryStream ms=new MemoryStream())
                {
                    using (CryptoStream cs=new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw=new StreamWriter(cs))
                        {
                            sw.Write(text);
                        }
                    }
                    return ms.ToArray();
                }
            }
        }
    }
    
    public static string Decrypt(byte[] text, byte[] key, byte[] iv)
    {
        using (Aes aes=Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            using (ICryptoTransform decryptor=aes.CreateDecryptor(aes.Key,aes.IV))
            {
                using (MemoryStream ms=new MemoryStream(text))
                {
                    using (CryptoStream cs=new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr=new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}