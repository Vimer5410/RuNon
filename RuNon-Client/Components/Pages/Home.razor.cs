using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Data.Sqlite;

namespace RuNon_Client.Components.Pages;


public partial class Home:ChatBase
{
 
    [Inject] public NavigationManager Navigation {get; set;}

    [Inject] public ProtectedLocalStorage _protectedLocalStorage {get; set;}
    
    RSA _rsa;
    public List<(string message, bool isMine)> receiveUserMessage = new List<(string message, bool isMine)>();
    public string messagetext;
    public byte[]? publicKeyFromServer;
    
    
    protected override async Task OnInitializedAsync()
    {
        await InitHubConnection();
        
        _rsa = RSA.Create(1024);
    
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

    
        hubConnection.On("LeaveFromRoom", () => 
        {
            InvokeAsync(async () =>
            {
                await DisconnectFromPartner();
            
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
    
        await hubConnection.InvokeAsync("GetPublicKey");
        await hubConnection.InvokeAsync("GetUserIp");
    
        var rsaPublicKey = _rsa.ExportRSAPublicKey();
        await hubConnection.InvokeAsync("ReceiveRSAkey", rsaPublicKey);
        
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Task.Delay(5000);
            Console.WriteLine($"После задержки, userIp = {userIp}");
            await CreateOrIdentify();
            StateHasChanged();
            if (isUserBanned)
            {
                StopSearch();
                DisconnectFromPartner();
            }
        }
    }
    
    // отправляем сообщение из messagetext при нажатии Enter
    private void EnterKeyDown(KeyboardEventArgs obj)
    {
        if (obj.Key=="Enter")
        {
            SendTestMessage();
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
                        await hubConnection.InvokeAsync("SendToClient", interviewerClientID, EncryptMsg, AesKeyEncryptedbyRsa, AesIV);
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