namespace RuNon_Client.Hubs;
using Microsoft.AspNetCore.SignalR;


public class SimpleHub : Hub
{
    public async Task SendToClient(string clientID,string message)
    {
        Console.WriteLine($"Sending to {clientID}: {message}");
        await Clients.Client(clientID).SendAsync("ReceiveMessage", message);
    }
}