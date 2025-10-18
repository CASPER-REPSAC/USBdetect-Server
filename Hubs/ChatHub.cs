using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging; // 로깅을 위해 필요
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

        /// <summary>
        /// 생성자 주입(Constructor Injection)을 통해 ILogger 인스턴스를 받습니다.
        /// ASP.NET Core의 DI 시스템이 자동으로 ILogger<ChatHub> 객체를 생성하여 전달해 줍니다.
        /// </summary>
        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 클라이언트가 성공적으로 연결되었을 때 SignalR에 의해 자동으로 호출되는 메서드입니다.
        /// </summary>
        public override Task OnConnectedAsync()
        {
            // 콘솔(CLI)에 클라이언트 연결 성공 로그를 출력합니다.
            _logger.LogInformation("✅ 클라이언트 연결 성공. Connection ID: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// 클라이언트 연결이 끊어졌을 때 SignalR에 의해 자동으로 호출되는 메서드입니다.
        /// </summary>
        public override Task OnDisconnectedAsync(Exception exception)
        {
            // 콘솔(CLI)에 클라이언트 연결 종료 로그를 출력합니다.
            _logger.LogWarning("❌ 클라이언트 연결 종료. Connection ID: {ConnectionId}", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 클라이언트 측에서 호출할 수 있는 공개 메서드입니다.
        /// 클라이언트로부터 메시지를 받아 연결된 모든 클라이언트에게 전달합니다.
        /// </summary>
        /// <param name="user">메시지를 보낸 사용자 이름</param>
        /// <param name="message">전달할 메시지 내용</param>
        public async Task SendMessage(string user, string message)
        {
            // 메시지를 받았다는 사실을 콘솔(CLI)에 로그로 남깁니다.
            _logger.LogInformation("📬 메시지 수신 - User: {User}, Message: {Message}", user, message);

            // "ReceiveMessage" 라는 이름으로, 연결된 모든 클라이언트에게 사용자 이름과 메시지를 보냅니다.
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
