using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace SignalRServer.Services
{
    public class ConnectedClient
    {
        public int Id { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public interface IClientRepository
    {
        Task AddClientAsync(ConnectedClient client);
        Task RemoveClientAsync(string connectionId);
        Task<IReadOnlyList<ConnectedClient>> GetClientsAsync();
    }

    /// <summary>
    /// Lightweight SQLite-backed repository for tracking connected SignalR clients.
    /// </summary>
    public class SqliteClientRepository : IClientRepository
    {
        private readonly string _connectionString;

        public SqliteClientRepository(string connectionString)
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
                @"CREATE TABLE IF NOT EXISTS ConnectedClients (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConnectionId TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                  );";
            command.ExecuteNonQuery();
        }

        public async Task AddClientAsync(ConnectedClient client)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                @"INSERT OR REPLACE INTO ConnectedClients (ConnectionId, Name, CreatedAt)
                  VALUES ($connectionId, $name, $createdAt);";
            command.Parameters.AddWithValue("$connectionId", client.ConnectionId);
            command.Parameters.AddWithValue("$name", client.Name);
            command.Parameters.AddWithValue("$createdAt", client.CreatedAt.ToString("O"));

            await command.ExecuteNonQueryAsync();
        }

        public async Task RemoveClientAsync(string connectionId)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM ConnectedClients WHERE ConnectionId = $connectionId;";
            command.Parameters.AddWithValue("$connectionId", connectionId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlyList<ConnectedClient>> GetClientsAsync()
        {
            var result = new List<ConnectedClient>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                @"SELECT Id, ConnectionId, Name, CreatedAt
                  FROM ConnectedClients
                  ORDER BY CreatedAt;";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ConnectedClient
                {
                    Id = reader.GetInt32(0),
                    ConnectionId = reader.GetString(1),
                    Name = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3), null, DateTimeStyles.RoundtripKind)
                });
            }

            return result;
        }
    }
}
