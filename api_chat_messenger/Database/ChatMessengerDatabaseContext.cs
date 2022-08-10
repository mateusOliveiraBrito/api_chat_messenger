using api_chat_messenger.Models;
using Microsoft.EntityFrameworkCore;

namespace api_chat_messenger.Database {
    public class ChatMessengerDatabaseContext : DbContext {

        public ChatMessengerDatabaseContext(DbContextOptions<ChatMessengerDatabaseContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Mensagem> Mensagens { get; set; }
    }
}
