using SignalRServer.Hubs;
using SignalRServer.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. SignalR 서비스를 DI(Dependency Injection) 컨테이너에 추가합니다.
builder.Services.AddSignalR();

// 2. SQLite 기반의 클라이언트 저장소를 DI 컨테이너에 추가합니다.
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "clients.db");
builder.Services.AddSingleton<IClientRepository>(_ => new SqliteClientRepository($"Data Source={dbPath}"));
builder.Services.AddSingleton<IUsbEventRepository>(_ => new SqliteUsbEventRepository($"Data Source={dbPath}"));

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
    });
});

var app = builder.Build();

// 4. (추가) 기본 파일을 설정합니다. (예: index.html)
//    루트 경로로 접속 시 wwwroot 폴더의 index.html을 기본으로 보여줍니다.
app.UseDefaultFiles();

// 5. (추가) 정적 파일(html, css, js)을 사용하도록 설정합니다.
//    wwwroot 폴더에 있는 파일들을 웹에서 접근할 수 있게 해줍니다.
app.UseStaticFiles();

// 6. CORS 정책을 사용하도록 설정합니다.
app.UseCors("AllowAll");

app.UseRouting();

// 7. 클라이언트가 접속할 허브의 엔드포인트(주소)를 매핑합니다.
//    이제 클라이언트는 "서버주소/chathub"로 접속할 수 있습니다.
app.MapHub<ChatHub>("/chathub");

// API: USB 이벤트 최근 목록 조회 (기본 100개)
app.MapGet("/api/usb-events", async (IUsbEventRepository repo, int? take) =>
{
    var limit = take.HasValue && take.Value > 0 ? Math.Min(take.Value, 500) : 100;
    var events = await repo.GetRecentEventsAsync(limit);
    return Results.Ok(events);
});

// API: USB 이벤트 단일 삭제
app.MapDelete("/api/usb-events/{id:int}", async (IUsbEventRepository repo, int id) =>
{
    await repo.DeleteEventAsync(id);
    return Results.NoContent();
});

// index.html Route
app.MapGet("/", async context =>
{
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});

// talk Route
app.MapGet("/talk", async context =>
{
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "talk/talk.html"));
});

// board Route
app.MapGet("/board", async context =>
{
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "board/board.html"));
});

// 404 Route
app.MapFallback(async context =>
{
    context.Response.StatusCode = 404;
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "404/404.html"));
});

// 서버를 실행합니다.
app.Run();
