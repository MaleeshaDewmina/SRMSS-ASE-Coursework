using SRMSS.Web.Data;
using SRMSS.Web.Models;
using SRMSS.Web.Utilities;

namespace SRMSS.Web.Services
{
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string tableName, int? recordId, string description)
        {
            int? currentUserId = _httpContextAccessor.HttpContext?.Session.GetInt32(SessionKeys.UserId);

            var auditLog = new AuditLog
            {
                AppUserId = currentUserId,
                Action = TrimText(action, 100),
                TableName = TrimText(tableName, 100),
                RecordId = recordId,
                Description = TrimText(description, 500),
                CreatedAt = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        private static string TrimText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}