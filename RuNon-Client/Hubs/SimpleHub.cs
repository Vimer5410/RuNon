using System.Security.Cryptography;
using System.Text;
using RuNon_Client.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;


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

    private const string ROOM_NAME = "VoiceRoom"; //   единая комната
        
    public async Task JoinRoom()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ROOM_NAME);
        Console.WriteLine($"[Hub] {Context.ConnectionId} присоединился к комнате");
            
        // закидываю уведы
        await Clients.OthersInGroup(ROOM_NAME).SendAsync("UserJoined", Context.ConnectionId);
    }
        
    public async Task SendOfferToRoom(string offer)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId} отправил Offer в комнату");
        
        await Clients.OthersInGroup(ROOM_NAME).SendAsync("ReceiveOffer", offer, Context.ConnectionId);
    }
        
    public async Task SendAnswerToUser(string targetId, string answer)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId} отправил Answer к {targetId}");
        await Clients.Client(targetId).SendAsync("ReceiveAnswer", answer);
    }
        
    public async Task SendIceCandidateToUser(string targetId, string candidate)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId} отправил ICE к {targetId}");
        await Clients.Client(targetId).SendAsync("ReceiveIceCandidate", candidate);
    }
        
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId} отключился");
        await Clients.OthersInGroup(ROOM_NAME).SendAsync("UserLeft", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
    
    
