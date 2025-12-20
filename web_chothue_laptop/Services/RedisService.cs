using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace web_chothue_laptop.Services
{
    public class RedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private const int ConversationTTLHours = 2;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = redis.GetDatabase();
        }

        public async Task SaveMessageAsync(string conversationId, ChatMessage message)
        {
            try
            {
                var key = $"chat:conversation:{conversationId}";
                var messages = await GetMessagesAsync(conversationId);
                messages.Add(message);

                var json = JsonSerializer.Serialize(messages, JsonOptions);
                await _database.StringSetAsync(key, json, TimeSpan.FromHours(ConversationTTLHours));
            }
            catch (Exception)
            {
                // Silently fail if Redis is not available - messages will still be sent via SignalR
                // In production, you might want to log this
            }
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(string conversationId)
        {
            try
            {
                var key = $"chat:conversation:{conversationId}";
                var value = await _database.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                {
                    return new List<ChatMessage>();
                }

                return JsonSerializer.Deserialize<List<ChatMessage>>(value!, JsonOptions) ?? new List<ChatMessage>();
            }
            catch
            {
                // Return empty list if Redis is not available
                return new List<ChatMessage>();
            }
        }

        public async Task AddActiveCustomerAsync(long customerId, string customerName)
        {
            try
            {
                var key = "chat:active_customers";
                var customerInfo = new ActiveCustomer
                {
                    CustomerId = customerId,
                    CustomerName = customerName,
                    LastActivity = DateTime.UtcNow
                };
                var json = JsonSerializer.Serialize(customerInfo, JsonOptions);
                await _database.HashSetAsync(key, customerId.ToString(), json);
                await _database.KeyExpireAsync(key, TimeSpan.FromHours(ConversationTTLHours));
            }
            catch
            {
                // Silently fail if Redis is not available
            }
        }

        public async Task<List<ActiveCustomer>> GetActiveCustomersAsync()
        {
            try
            {
                var key = "chat:active_customers";
                var hash = await _database.HashGetAllAsync(key);
                var customers = new List<ActiveCustomer>();

                foreach (var item in hash)
                {
                    try
                    {
                        var customer = JsonSerializer.Deserialize<ActiveCustomer>(item.Value!, JsonOptions);
                        if (customer != null)
                        {
                            customers.Add(customer);
                        }
                    }
                    catch
                    {
                        // Skip invalid entries
                    }
                }

                return customers.OrderByDescending(c => c.LastActivity).ToList();
            }
            catch
            {
                // Return empty list if Redis is not available
                return new List<ActiveCustomer>();
            }
        }

        public async Task RemoveActiveCustomerAsync(long customerId)
        {
            var key = "chat:active_customers";
            await _database.HashDeleteAsync(key, customerId.ToString());
        }

        // AI Chat methods
        public async Task SaveAIChatMessageAsync(string conversationId, AIChatMessage message)
        {
            try
            {
                var key = $"ai_chat:messages:{conversationId}";
                var messages = await GetAIChatMessagesAsync(conversationId);
                messages.Add(message);

                var json = JsonSerializer.Serialize(messages, JsonOptions);
                await _database.StringSetAsync(key, json, TimeSpan.FromHours(ConversationTTLHours));
            }
            catch (Exception)
            {
                // Silently fail if Redis is not available
            }
        }

        public async Task<List<AIChatMessage>> GetAIChatMessagesAsync(string conversationId)
        {
            try
            {
                var key = $"ai_chat:messages:{conversationId}";
                var value = await _database.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                {
                    return new List<AIChatMessage>();
                }

                return JsonSerializer.Deserialize<List<AIChatMessage>>(value!, JsonOptions) ?? new List<AIChatMessage>();
            }
            catch
            {
                return new List<AIChatMessage>();
            }
        }

        public async Task SaveAIChatContextAsync(string conversationId, AIChatService.ConversationContext context)
        {
            try
            {
                var key = $"ai_chat:context:{conversationId}";
                var json = JsonSerializer.Serialize(context, JsonOptions);
                await _database.StringSetAsync(key, json, TimeSpan.FromHours(ConversationTTLHours));
            }
            catch (Exception)
            {
                // Silently fail if Redis is not available
            }
        }

        public async Task<AIChatService.ConversationContext?> GetAIChatContextAsync(string conversationId)
        {
            try
            {
                var key = $"ai_chat:context:{conversationId}";
                var value = await _database.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<AIChatService.ConversationContext>(value!, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task ClearAIChatHistoryAsync(string conversationId)
        {
            try
            {
                var messagesKey = $"ai_chat:messages:{conversationId}";
                var contextKey = $"ai_chat:context:{conversationId}";

                // Xóa messages và context
                await _database.KeyDeleteAsync(messagesKey);
                await _database.KeyDeleteAsync(contextKey);
            }
            catch (Exception)
            {
                // Silently fail if Redis is not available
            }
        }
    }

    public class ChatMessage
    {
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty; // "customer" or "staff"
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ActiveCustomer
    {
        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
    }

    public class AIChatMessage
    {
        public string Message { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty; // "user" or "assistant"
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}