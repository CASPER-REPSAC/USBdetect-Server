namespace SignalRServer.Services
{
    public class UsbDeviceInfoDto
    {
        public uint DeviceIndex { get; set; }
        public ushort VendorId { get; set; }
        public ushort ProductId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string ProductString { get; set; } = string.Empty;
        public string ManufacturerString { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
    }
}
