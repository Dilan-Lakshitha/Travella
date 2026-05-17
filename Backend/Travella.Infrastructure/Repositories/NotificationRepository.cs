using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationRepository(IDbConnectionFactory connectionFactory, IUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        private async Task<IDbConnection> OpenConnectionAsync()
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                return _unitOfWork.Connection;
            }

            var c = _connectionFactory.CreateConnection();
            if (c.State != ConnectionState.Open)
            {
                if (c is NpgsqlConnection npg)
                {
                    await npg.OpenAsync();
                }
                else
                {
                    c.Open();
                }
            }

            return c;
        }

        private IDbTransaction? CurrentTransaction =>
            _unitOfWork.HasActiveTransaction ? _unitOfWork.CurrentTransaction : null;

        public async Task<int> CreateAsync(int userId, int? itineraryId, string type, string title, string message)
        {
            const string sql = """
                INSERT INTO tbl_notifications (user_id, itinerary_id, type, title, message, is_read, created_at)
                VALUES (@UserId, @ItineraryId, @Type, @Title, @Message, false, NOW())
                RETURNING id
                """;

            var connection = await OpenConnectionAsync();
            return await connection.ExecuteScalarAsync<int>(
                sql,
                new { UserId = userId, ItineraryId = itineraryId, Type = type, Title = title, Message = message },
                CurrentTransaction);
        }

        public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId, int limit)
        {
            const string sql = """
                SELECT
                    id AS Id,
                    user_id AS UserId,
                    itinerary_id AS ItineraryId,
                    type AS Type,
                    title AS Title,
                    message AS Message,
                    is_read AS IsRead,
                    created_at AS CreatedAt
                FROM tbl_notifications
                WHERE user_id = @UserId
                ORDER BY created_at DESC
                LIMIT @Limit
                """;

            using var connection = _connectionFactory.CreateConnection();
            if (connection is NpgsqlConnection npg)
            {
                await npg.OpenAsync();
            }
            else
            {
                connection.Open();
            }

            var rows = await connection.QueryAsync<NotificationDto>(sql, new { UserId = userId, Limit = limit });
            return rows.AsList();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            const string sql = """
                SELECT COUNT(*)::int
                FROM tbl_notifications
                WHERE user_id = @UserId AND is_read = false
                """;

            using var connection = _connectionFactory.CreateConnection();
            if (connection is NpgsqlConnection npg)
            {
                await npg.OpenAsync();
            }
            else
            {
                connection.Open();
            }

            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            const string sql = """
                UPDATE tbl_notifications
                SET is_read = true
                WHERE id = @NotificationId AND user_id = @UserId AND is_read = false
                """;

            using var connection = _connectionFactory.CreateConnection();
            if (connection is NpgsqlConnection npg)
            {
                await npg.OpenAsync();
            }
            else
            {
                connection.Open();
            }

            var affected = await connection.ExecuteAsync(sql, new { NotificationId = notificationId, UserId = userId });
            return affected > 0;
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            const string sql = """
                UPDATE tbl_notifications
                SET is_read = true
                WHERE user_id = @UserId AND is_read = false
                """;

            using var connection = _connectionFactory.CreateConnection();
            if (connection is NpgsqlConnection npg)
            {
                await npg.OpenAsync();
            }
            else
            {
                connection.Open();
            }

            await connection.ExecuteAsync(sql, new { UserId = userId });
        }
    }
}
