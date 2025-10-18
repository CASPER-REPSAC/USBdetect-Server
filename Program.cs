// 1. ChatHub 클래스의 네임스페이스를 using 문으로 추가합니다.
using SignalRServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 2. SignalR 서비스를 DI(Dependency Injection) 컨테이너에 추가합니다.
builder.Services.AddSignalR();

// 3. (매우 중요) CORS 정책을 추가하여 다른 도메인(WinForm 등)의 접속을 허용합니다.
//    이 설정을 하지 않으면 클라이언트가 접속할 때 보안 오류가 발생합니다.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true) // 모든 Origin 허용
              .AllowCredentials();
            //   .WithOrigins(
            //     "null",                      // WebView2(file://)
            //     "http://localhost",
            //     "http://127.0.0.1",
            //     "http://192.168.0.50:5009"   // 서버 내부 IP (자기 IP로 변경)
            // );
    });
});

var app = builder.Build();

// 개발 환경에서는 HTTPS 리디렉션을 사용합니다.
// if (app.Environment.IsDevelopment())
// {
//     app.UseHttpsRedirection();
// }

// 4. CORS 정책을 사용하도록 설정합니다.
app.UseCors("AllowAll");

app.UseRouting();

// 5. 클라이언트가 접속할 허브의 엔드포인트(주소)를 매핑합니다.
//    이제 클라이언트는 "서버주소/chathub"로 접속할 수 있습니다.
app.MapHub<ChatHub>("/chathub");

// 서버를 실행합니다.
app.Run();