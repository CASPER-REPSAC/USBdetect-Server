using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace SignalRServer.Services
{
    public class UsbEvent
    {
        public int Id { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public uint DeviceIndex { get; set; }
        public ushort VendorId { get; set; }
        public ushort ProductId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string ProductString { get; set; } = string.Empty;
        public string ManufacturerString { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    public interface IUsbEventRepository
    {
        Task AddEventsAsync(IEnumerable<UsbEvent> usbEvents);
    }

    public class SqliteUsbEventRepository : IUsbEventRepository
    {
        private readonly string _connectionString;

        public SqliteUsbEventRepository(string connectionString)
        {
            _connectionString = connectionString;
            Initialize();
        }

        private void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                @"CREATE TABLE IF NOT EXISTS UsbEvents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConnectionId TEXT NOT NULL,
                    DeviceIndex INTEGER NOT NULL,
                    VendorId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    SerialNumber TEXT NOT NULL,
                    ProductString TEXT NOT NULL,
                    ManufacturerString TEXT NOT NULL,
                    IsBlocked INTEGER NOT NULL,
                    DetectedAt TEXT NOT NULL
                  );";
            command.ExecuteNonQuery();
        }

        public async Task AddEventsAsync(IEnumerable<UsbEvent> usbEvents)
        {
            if (usbEvents == null)
            {
                return;
            }

            var eventsToSave = new List<UsbEvent>();
            foreach (var usbEvent in usbEvents)
            {
                if (usbEvent != null)
                {
                    eventsToSave.Add(usbEvent);
                }
            }

            if (eventsToSave.Count == 0)
            {
                return;
            }

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText =
                @"INSERT INTO UsbEvents (
                    ConnectionId, DeviceIndex, VendorId, ProductId,
                    SerialNumber, ProductString, ManufacturerString,
                    IsBlocked, DetectedAt
                  ) VALUES (
                    $connectionId, $deviceIndex, $vendorId, $productId,
                    $serialNumber, $productString, $manufacturerString,
                    $isBlocked, $detectedAt
                  );";

            var connectionIdParam = command.CreateParameter();
            connectionIdParam.ParameterName = "$connectionId";
            command.Parameters.Add(connectionIdParam);

            var deviceIndexParam = command.CreateParameter();
            deviceIndexParam.ParameterName = "$deviceIndex";
            command.Parameters.Add(deviceIndexParam);

            var vendorIdParam = command.CreateParameter();
            vendorIdParam.ParameterName = "$vendorId";
            command.Parameters.Add(vendorIdParam);

            var productIdParam = command.CreateParameter();
            productIdParam.ParameterName = "$productId";
            command.Parameters.Add(productIdParam);

            var serialNumberParam = command.CreateParameter();
            serialNumberParam.ParameterName = "$serialNumber";
            command.Parameters.Add(serialNumberParam);

            var productStringParam = command.CreateParameter();
            productStringParam.ParameterName = "$productString";
            command.Parameters.Add(productStringParam);

            var manufacturerStringParam = command.CreateParameter();
            manufacturerStringParam.ParameterName = "$manufacturerString";
            command.Parameters.Add(manufacturerStringParam);

            var isBlockedParam = command.CreateParameter();
            isBlockedParam.ParameterName = "$isBlocked";
            command.Parameters.Add(isBlockedParam);

            var detectedAtParam = command.CreateParameter();
            detectedAtParam.ParameterName = "$detectedAt";
            command.Parameters.Add(detectedAtParam);

            foreach (var usbEvent in eventsToSave)
            {
                connectionIdParam.Value = usbEvent.ConnectionId ?? string.Empty;
                deviceIndexParam.Value = (long)usbEvent.DeviceIndex;
                vendorIdParam.Value = (int)usbEvent.VendorId;
                productIdParam.Value = (int)usbEvent.ProductId;
                serialNumberParam.Value = usbEvent.SerialNumber ?? string.Empty;
                productStringParam.Value = usbEvent.ProductString ?? string.Empty;
                manufacturerStringParam.Value = usbEvent.ManufacturerString ?? string.Empty;
                isBlockedParam.Value = usbEvent.IsBlocked ? 1 : 0;
                detectedAtParam.Value = usbEvent.DetectedAt.ToString("O");

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
    }
}
