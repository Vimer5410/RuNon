using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Serilog;

namespace RuNon_Client.Components.Pages;

public partial class VoiceChat: ChatBase
{
    // из тестового войc чата
    private DotNetObjectReference<VoiceChat> dotNetRef;
    private string errorMessage = "";
    private string roomId = "";

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
            Log.Debug("[C#] Пользователь присоединился: {userId}", userId);
            await InvokeAsync(StateHasChanged);
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleUserJoined", userId, dotNetRef);
        });
                
        hubConnection.On<string, string>("ReceiveOffer", async (offer, fromUserId) =>
        {
            Log.Debug("[C#] Получен Offer от {fromUserId}", fromUserId);
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleOffer", offer, fromUserId, dotNetRef);
        });
                
        hubConnection.On<string>("ReceiveAnswer", async (answer) =>
        {
            Log.Debug("[C#] Получен Answer");
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleAnswer", answer, "", dotNetRef);
        });
                
        hubConnection.On<string>("ReceiveIceCandidate", async (candidate) =>
        {
            Log.Debug("[C#] Получен ICE");
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleIce", candidate, "", dotNetRef);
        });
                
        hubConnection.On<string>("UserLeft", async (userId) =>
        {
            Log.Debug("[C#] Пользователь вышел: {userId}", userId);
            await InvokeAsync(StateHasChanged);
            await JSRuntime.InvokeVoidAsync("VoiceChat.handleUserLeft", userId);
        });

        Log.Information("[C#] Подключён к Hub! ID: {hubConnection.ConnectionId}",
            hubConnection.ConnectionId);
        
        StateHasChanged();
        await hubConnection.InvokeAsync("GetUserIp");

    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await UsersOnline();
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
        
        errorMessage = "";
        StateHasChanged();
        
        try
        {
            Log.Debug("[C#] Вход в комнату...");
            
            // Передаём dotNetRef в JS
            await JSRuntime.InvokeVoidAsync("VoiceChat.joinRoom", dotNetRef);
            Log.Debug("[C#] Успешно вошел в комнату!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C#] ОШИБКА входа в комнату: {ex.Message}");
            errorMessage = $"Не удалось войти в комнату: {ex.Message}";
        }
    }

    private string GetUniqueRoomId(string userA, string userB)
    {
        return string.Compare(userA, userB) < 0 
            ? $"{userA}__{userB}" 
            : $"{userB}__{userA}";
    }
    
    
    [JSInvokable]
    public async Task JoinRoomOnServer()
    {
        Log.Debug("[C#] JoinRoomOnServer вызван. ID: {hubConnection?.ConnectionId}",
            hubConnection?.ConnectionId);
        
        if (hubConnection == null || hubConnection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException($"Hub не подключён. State: {hubConnection?.State}");
        }
        
        await hubConnection.InvokeAsync("JoinRoom", roomId);
        Log.Debug("[C#] JoinRoom успешно вызван на сервере");
    }
    
    [JSInvokable]
    public async Task SendOfferToRoom(string offer)
    {
        Log.Debug("[C#] Отправка Offer в комнату");
        await hubConnection!.InvokeAsync("SendOfferToRoom", roomId, offer);
    }
    
    [JSInvokable]
    public async Task SendAnswer(string targetId, string answer)
    {
        Log.Debug("[C#] Отправка Answer к {targetId}", targetId);
        await hubConnection!.InvokeAsync("SendAnswerToUser", targetId, answer);
    }
    
    [JSInvokable]
    public async Task SendIce(string targetId, string candidate)
    {
        Log.Debug("[C#] Отправка ICE к {targetId}", targetId);
        await hubConnection!.InvokeAsync("SendIceCandidateToUser", targetId, candidate);
    }
}