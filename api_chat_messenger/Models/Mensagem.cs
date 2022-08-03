using System;

namespace api_chat_messenger.Models {
    public class Mensagem {
        public int Id { get; set; }
        public string NomeGrupo { get; set; }
        public string Usuario { get; set; }
        public string Texto { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}