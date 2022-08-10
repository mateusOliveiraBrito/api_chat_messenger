using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace api_chat_messenger.Models {
    public class Mensagem {
        public int Id { get; set; }
        public string NomeGrupo { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioJson { get; set; }
        [NotMapped]
        public Usuario Usuario { get; set; }
        public string Texto { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}