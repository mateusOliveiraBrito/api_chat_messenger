namespace api_chat_messenger.Models {
    public class Usuario {
        public Usuario() { }

        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public bool isOnline { get; set; }
        public string ConnectionId { get; set; }
    }
}