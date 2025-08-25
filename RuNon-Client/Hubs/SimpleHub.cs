using RuNon_Client.Services;

namespace RuNon_Client.Hubs;
using Microsoft.AspNetCore.SignalR;


public class SimpleHub : Hub
{
    
    private readonly EncryptionService _encryptionService;
    
    public SimpleHub(EncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }
    public async Task GetPublicKey()
    {
        string publicKey = _encryptionService.GetPublicKey();
        await Clients.Caller.SendAsync("ReceivePublicKey", publicKey);
    }

    public async Task ReceiveEncodeMsg(byte[] EncryptedMessage, byte[] AesKeyEncrypted)
    {
        var DecryptedAesKey = _encryptionService.DecryptAESKey(Convert.ToBase64String(AesKeyEncrypted));
        
        Console.WriteLine($"Итого: {Convert.ToBase64String(EncryptedMessage)} {Convert.ToBase64String(DecryptedAesKey)}" );
    }
    public async Task SendToClient(string clientID,string message)
    {
        Console.WriteLine($"Sending to {clientID}: {message}");
        await Clients.Client(clientID).SendAsync("ReceiveMessage", message);
    }

    
    
}