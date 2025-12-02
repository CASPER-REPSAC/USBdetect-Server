using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalRServer.Services;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SignalRServer.Hubs
{
    /// <summary>
    /// SignalR hub that handles client connections and USB reports.
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IClientRepository _clientRepository;
        private readonly IUsbEventRepository _usbEventRepository;

        public ChatHub(ILogger<ChatHub> logger, IClientRepository clientRepository, IUsbEventRepository usbEventRepository)
        {
            _logger = logger;
            _clientRepository = clientRepository;
            _usbEventRepository = usbEventRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var name = httpContext?.Request.Query["username"].ToString();
            var remoteIp = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (string.IsNullOrWhiteSpace(name))
            {
                name = "anonymous";
            }

            await _clientRepository.AddClientAsync(new ConnectedClient
            {
                ConnectionId = Context.ConnectionId,
                Name = name,
                RemoteIp = remoteIp,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Client connected. Connection ID: {ConnectionId}, Name: {Name}, RemoteIp: {RemoteIp}", Context.ConnectionId, name, remoteIp);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _clientRepository.RemoveClientAsync(Context.ConnectionId);
            _logger.LogWarning("Client disconnected. Connection ID: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessageToClient(string targetConnectionId, string user, string message)
        {
            _logger.LogInformation("Direct message - From: {User}, To: {Target}, Message: {Message}", user, targetConnectionId, message);
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", user, message);
        }

        public Task<IReadOnlyList<ConnectedClient>> GetConnectedClients()
        {
            return _clientRepository.GetClientsAsync();
        }

        public async Task ReportUsbDevices(List<UsbDeviceInfoDto> devices)
        {
            if (devices == null || devices.Count == 0)
            {
                _logger.LogInformation("USB device report request was empty. Connection ID: {ConnectionId}", Context.ConnectionId);
                return;
            }

            var eventsToSave = new List<UsbEvent>(devices.Count);
            var detectedAt = DateTime.UtcNow;

            foreach (var device in devices)
            {
                if (device == null)
                {
                    continue;
                }

                eventsToSave.Add(new UsbEvent
                {
                    ConnectionId = Context.ConnectionId,
                    DeviceIndex = device.DeviceIndex,
                    VendorId = device.VendorId,
                    ProductId = device.ProductId,
                    SerialNumber = (device.SerialNumber ?? string.Empty).Trim(),
                    ProductString = (device.ProductString ?? string.Empty).Trim(),
                    ManufacturerString = (device.ManufacturerString ?? string.Empty).Trim(),
                    IsBlocked = device.IsBlocked,
                    DetectedAt = detectedAt
                });
            }

            if (eventsToSave.Count == 0)
            {
                _logger.LogInformation("No valid USB device report entries; skipped saving. Connection ID: {ConnectionId}", Context.ConnectionId);
                return;
            }

            await _usbEventRepository.AddEventsAsync(eventsToSave);
            _logger.LogInformation("Saved {Count} USB devices from ReportUsbDevices. Connection ID: {ConnectionId}", eventsToSave.Count, Context.ConnectionId);
        }

        /// <summary>
        /// Compatible with the WinForms client's InvokeAsync(\"SendDeviceList\", jsonPayload).
        /// Accepts the raw JSON payload and stores it into the USB events table.
        /// </summary>
        public async Task SendDeviceList(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                _logger.LogWarning("Received empty device list payload. Connection ID: {ConnectionId}", Context.ConnectionId);
                return;
            }

            ClientDeviceListMessage? payload;
            try
            {
                // Allow clients to send camelCase or PascalCase JSON keys
                payload = JsonSerializer.Deserialize<ClientDeviceListMessage>(
                    jsonPayload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse device list JSON. Connection ID: {ConnectionId}", Context.ConnectionId);
                return;
            }

            if (payload?.Data == null || payload.Data.Count == 0)
            {
                _logger.LogInformation("Device list payload is empty. Connection ID: {ConnectionId}", Context.ConnectionId);
                return;
            }

            var detectedAt = DateTime.UtcNow;
            var eventsToSave = new List<UsbEvent>(payload.Data.Count);

            foreach (var device in payload.Data)
            {
                if (device == null)
                {
                    continue;
                }

                eventsToSave.Add(new UsbEvent
                {
                    ConnectionId = Context.ConnectionId,
                    DeviceIndex = device.DeviceIndex,
                    VendorId = device.VendorId,
                    ProductId = device.ProductId,
                    SerialNumber = string.Empty,
                    ProductString = device.FriendlyName ?? string.Empty,
                    ManufacturerString = device.HardwareId ?? string.Empty,
                    IsBlocked = !device.IsWhitelisted,
                    DetectedAt = detectedAt
                });
            }

            if (eventsToSave.Count == 0)
            {
                _logger.LogInformation("Parsed device list contained no valid entries. Connection ID: {ConnectionId}", Context.ConnectionId);
                return;
            }

            await _usbEventRepository.AddEventsAsync(eventsToSave);
            _logger.LogInformation("Saved {Count} USB devices reported via SendDeviceList. Connection ID: {ConnectionId}", eventsToSave.Count, Context.ConnectionId);
        }

        private class ClientDeviceListMessage
        {
            public string? Type { get; set; }
            public List<ClientUsbDeviceInfo>? Data { get; set; }
        }

        private class ClientUsbDeviceInfo
        {
            public uint DeviceIndex { get; set; }
            public ushort VendorId { get; set; }
            public ushort ProductId { get; set; }
            public string? HardwareId { get; set; }
            public string? FriendlyName { get; set; }
            public bool IsWhitelisted { get; set; }
        }
    }
}
