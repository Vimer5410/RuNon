using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;




namespace RuNon_Client.Components.Pages;

public partial class VoiceChat : ComponentBase
{
    private HubConnection? hubConnection;   
    public List<(string message, bool isMine)> receiveUserMessage = new List<(string message, bool isMine)>();
    public string yourClientID;
    public string interviewerClientID;
    public string messagetext;
    public byte[]? publicKeyFromServer;
    public bool isUserBanned;
    public bool ShowToast;
    public string userIp = "";
    

    
    
    // из тестового войc чата
    private DotNetObjectReference<VoiceChat> dotNetRef;
    private bool inRoom = false;
    private bool isConnecting = false;
    private int participantCount = 1;
    private string errorMessage = "";
    
    List<TimeSpan> reconnectTime = new List<TimeSpan>();

    protected override async Task OnInitializedAsync()
    {

        for (int i = 0; i < 3; i++)
        {
            reconnectTime.Add(TimeSpan.FromSeconds(i * 5));
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
                    .Build();
                
                await hubConnection.StartAsync();
                
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
                
                
                yourClientID = hubConnection.ConnectionId;
                
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
                
                await hubConnection.StartAsync();
                Console.WriteLine($"[C#] Подключён к Hub! ID: {hubConnection.ConnectionId}");
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C#] ОШИБКА подключения: {ex.Message}");
                errorMessage = "Не удалось подключиться к серверу";
                await InvokeAsync(StateHasChanged);
            }
        
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Создаём ссылку для JS
            dotNetRef = DotNetObjectReference.Create(this);
            
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
