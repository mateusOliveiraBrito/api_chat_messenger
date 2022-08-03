using Microsoft.EntityFrameworkCore;

namespace api_chat_messenger.Database {
    public class ChatMessengerDatabaseContext : DbContext {

        public ChatMessengerDatabaseContext(DbContextOptions<ChatMessengerDatabaseContext> options) : base(options) {

        }
    }
}
