using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
namespace RuNon_Client.Components.Pages;

public partial class VoiceChat: ChatBase
{
    // из тестового войc чата
    private DotNetObjectReference<VoiceChat> dotNetRef;
    private bool inRoom = false;
    private bool isConnecting = false;
    private int participantCount = 1;
    private string errorMessage = "";


    protected override async Task OnInitializedAsync()
    {

        await InitHubConnection();
                
        hubConnection.On("LeaveFromRoom", () => 
        {
            InvokeAsync(async () =>
            {
                await DisconnectFromPartner();
            
            }); 
        });
                
                
        // методы signalR хаба, которые касаются работы с WebRTC
        hubConnection.On<string>("UserJoined", async (userId) =>
        {
            Console.WriteLine($"[C#] Пользователь присоединился: {userId}");
            participantCount++;
            await InvokeAsync(StateHasChanged);
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleUserJoined", userId, dotNetRef);
        });
                
        hubConnection.On<string, string>("ReceiveOffer", async (offer, fromUserId) =>
        {
            Console.WriteLine($"[C#] Получен Offer от {fromUserId}");
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleOffer", offer, fromUserId, dotNetRef);
        });
                
        hubConnection.On<string>("ReceiveAnswer", async (answer) =>
        {
            Console.WriteLine("[C#] Получен Answer");
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleAnswer", answer, "", dotNetRef);
        });
                
        hubConnection.On<string>("ReceiveIceCandidate", async (candidate) =>
        {
            Console.WriteLine("[C#] Получен ICE");
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleIce", candidate, "", dotNetRef);
        });
                
        hubConnection.On<string>("UserLeft", async (userId) =>
        {
            Console.WriteLine($"[C#] Пользователь вышел: {userId}");
            participantCount = Math.Max(1, participantCount - 1);
            await InvokeAsync(StateHasChanged);
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleUserLeft", userId);
        });
                
        Console.WriteLine($"[C#] Подключён к Hub! ID: {hubConnection.ConnectionId}");
        StateHasChanged();
        await hubConnection.InvokeAsync("GetUserIp");
        
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dotNetRef = DotNetObjectReference.Create(this);
            await Task.Delay(5000);
            Console.WriteLine($"После задержки, userIp = {userIp}");
            StateHasChanged();
            if (isUserBanned)
            {
                StopSearch();
                DisconnectFromPartner();
            }
        }
    }


    private async Task JoinRoom()
    {
        if (hubConnection == null || hubConnection.State != HubConnectionState.Connected)
        {
            errorMessage = "Нет подключения к серверу. Обновите страницу.";
            StateHasChanged();
            return;
        }
        
        isConnecting = true;
        errorMessage = "";
        StateHasChanged();
        
        try
        {
            Console.WriteLine("[C#] Вход в комнату...");
            
            
            // Передаём dotNetRef в JS
            await JSRuntime.InvokeVoidAsync("VoiceChat.joinRoom", dotNetRef);
            inRoom = true;
            Console.WriteLine("[C#] Успешно вошли в комнату!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C#] ОШИБКА входа в комнату: {ex.Message}");
            errorMessage = $"Не удалось войти в комнату: {ex.Message}";
        }
        finally
        {
            isConnecting = false;
            StateHasChanged();
        }
    }
    
    private void LeaveRoom()
    {
        inRoom = false;
        participantCount = 1;
    }
    
    
    [JSInvokable]
    public async Task JoinRoomOnServer()
    {
        Console.WriteLine($"[C#] JoinRoomOnServer вызван. ID: {hubConnection?.ConnectionId}");
        
        if (hubConnection == null || hubConnection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException($"Hub не подключён. State: {hubConnection?.State}");
        }
        
        await hubConnection.InvokeAsync("JoinRoom");
        Console.WriteLine("[C#] JoinRoom успешно вызван на сервере");
    }
    
    [JSInvokable]
    public async Task SendOfferToRoom(string offer)
    {
        Console.WriteLine("[C#] Отправка Offer в комнату");
        await hubConnection!.InvokeAsync("SendOfferToRoom", offer);
    }
    
    [JSInvokable]
    public async Task SendAnswer(string targetId, string answer)
    {
        Console.WriteLine($"[C#] Отправка Answer к {targetId}");
        await hubConnection!.InvokeAsync("SendAnswerToUser", targetId, answer);
    }
    
    [JSInvokable]
    public async Task SendIce(string targetId, string candidate)
    {
        Console.WriteLine($"[C#] Отправка ICE к {targetId}");
        await hubConnection!.InvokeAsync("SendIceCandidateToUser", targetId, candidate);
    }
    
}
