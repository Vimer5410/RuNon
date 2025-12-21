using System.Security.Cryptography;
using System.Text;
using RuNon_Client.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Serilog;


namespace RuNon_Client.Hubs;
public class SimpleHub : Hub
{
    private readonly EncryptionService _encryptionService;
    
    private static readonly ConcurrentDictionary<string, byte[]> _clientPublicKeys = new();
    
    public SimpleHub(EncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }
    
    public async Task GetPublicKey()
    {
        string publicKey = _encryptionService.GetPublicKey();
        await Clients.Caller.SendAsync("ReceivePublicKey", publicKey);
    }

    public async Task SendToClient(string interviewerСlientID, byte[] EncryptedMessage, byte[] AesKeyEncrypted, byte[] AesIV)
    {
        var DecryptedAesKey = _encryptionService.DecryptAESKey(Convert.ToBase64String(AesKeyEncrypted));
        var DecryptedMessage = Decrypt(EncryptedMessage, DecryptedAesKey, AesIV);
        
        
        using (Aes aes = Aes.Create())
        {
            var newEncryptedMessage = EncryptionService.Encrypt(DecryptedMessage, aes.Key, aes.IV);
            
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(_clientPublicKeys[interviewerСlientID], out _);
                var encryptedAesKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
                
                await Clients.Client(interviewerСlientID).SendAsync("ReceiveMessage", newEncryptedMessage, encryptedAesKey, aes.IV);
            }
            Console.WriteLine($"Send to: {interviewerСlientID} || Encode msg: {Convert.ToBase64String(newEncryptedMessage)} || Decode msg: {DecryptedMessage}");
        }
    }
    
    public async Task GetUserIp()
    {
        var clientIP = Context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString();
        await Clients.Caller.SendAsync("ReceiveUserIp", clientIP);
    }

    public async Task ReceiveRSAkey(byte[] rsaPublicKey)
    {
        _clientPublicKeys[Context.ConnectionId] = rsaPublicKey;
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
    
        
    public async Task JoinRoom(string roomId)
    {
        // добавляем текущего юзера в конкретную группу SignalR
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        Log.Information("[Hub] {Context.ConnectionId} вошел в комнату {roomId}",
            Context.ConnectionId, roomId);
        
        await Clients.OthersInGroup(roomId).SendAsync("UserJoined", Context.ConnectionId);
    }
        
    public async Task SendOfferToRoom(string roomId,string offer)
    {
        Log.Debug("[Hub] {Context.ConnectionId} отправил Offer в комнату {roomId}",
            Context.ConnectionId, roomId);
        
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveOffer", offer, Context.ConnectionId);
    }
        
    public async Task SendAnswerToUser(string targetId, string answer)
    {
        Log.Debug("[Hub] {Context.ConnectionId} отправил Answer к {targetId}",
            Context.ConnectionId, targetId);
        
        await Clients.Client(targetId).SendAsync("ReceiveAnswer", answer);
    }
        
    public async Task SendIceCandidateToUser(string targetId, string candidate)
    {
        Log.Debug("[Hub] {Context.ConnectionId} отправил ICE к {targetId}",
            Context.ConnectionId, targetId );
        
        await Clients.Client(targetId).SendAsync("ReceiveIceCandidate", candidate);
    }
    
    
    // апдейтим лог при отключении нового клиента    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Log.Information("[Hub] {Context.ConnectionId} отключился", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    
    // апдейтим лог при подключении нового клиента
    public override Task OnConnectedAsync()
    {
        Log.Information("[Hub] {Context.ConnectionId} подключился", Context.ConnectionId);
        return base.OnConnectedAsync();
    }
    
    
    public async Task NotifyOfLeave(string partnerConnectionId)
    {
        await Clients.Client(partnerConnectionId).SendAsync("LeaveFromRoom");
    }
}
    
    
