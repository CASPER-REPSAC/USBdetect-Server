using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging; // 로깅을 위해 필요
using SignalRServer.Services;
using System;
using System.Threading.Tasks;

namespace SignalRServer.Hubs
{
    /// <summary>
    /// 클라이언트와의 실시간 통신을 처리하는 SignalR 허브입니다.
    /// </summary>
    public class ChatHub : Hub
    {
        // ILogger 객체를 저장하기 위한 읽기 전용 멤버 변수
        private readonly ILogger<ChatHub> _logger;
        private readonly IClientRepository _clientRepository;

        /// <summary>
        /// 생성자 주입(Constructor Injection)을 통해 ILogger 인스턴스를 받습니다.
        /// ASP.NET Core의 DI 시스템이 자동으로 ILogger<ChatHub> 객체를 생성하여 전달해 줍니다.
        /// </summary>
        public ChatHub(ILogger<ChatHub> logger, IClientRepository clientRepository)
        {
            _logger = logger;
            _clientRepository = clientRepository;
        }

        /// <summary>
        /// 클라이언트가 성공적으로 연결되었을 때 SignalR에 의해 자동으로 호출되는 메서드입니다.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var name = Context.GetHttpContext()?.Request.Query["username"].ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "알 수 없는 사용자";
            }

            await _clientRepository.AddClientAsync(new ConnectedClient
            {
                ConnectionId = Context.ConnectionId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            });

            // 콘솔(CLI)에 클라이언트 연결 성공 로그를 출력합니다.
            _logger.LogInformation("✅ 클라이언트 연결 성공. Connection ID: {ConnectionId}, Name: {Name}", Context.ConnectionId, name);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 클라이언트 연결이 끊어졌을 때 SignalR에 의해 자동으로 호출되는 메서드입니다.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _clientRepository.RemoveClientAsync(Context.ConnectionId);

            // 콘솔(CLI)에 클라이언트 연결 종료 로그를 출력합니다.
            _logger.LogWarning("❌ 클라이언트 연결 종료. Connection ID: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 클라이언트 측에서 호출할 수 있는 공개 메서드입니다.
        /// 대상 클라이언트를 선택하여 메시지를 전달합니다.
        /// </summary>
        /// <param name="targetConnectionId">메시지를 받을 대상의 ConnectionId</param>
        /// <param name="user">메시지를 보낸 사용자 이름</param>
        /// <param name="message">전달할 메시지 내용</param>
        public async Task SendMessageToClient(string targetConnectionId, string user, string message)
        {
            // 메시지를 받았다는 사실을 콘솔(CLI)에 로그로 남깁니다.
            _logger.LogInformation("📬 메시지 수신 - From: {User}, To: {Target}, Message: {Message}", user, targetConnectionId, message);

            // "ReceiveMessage" 라는 이름으로, 선택된 클라이언트에게만 사용자 이름과 메시지를 보냅니다.
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", user, message);
        }

        /// <summary>
        /// 현재 데이터베이스에 저장된 모든 연결된 클라이언트를 반환합니다.
        /// </summary>
        public Task<IReadOnlyList<ConnectedClient>> GetConnectedClients()
        {
            return _clientRepository.GetClientsAsync();
        }
    }
}
