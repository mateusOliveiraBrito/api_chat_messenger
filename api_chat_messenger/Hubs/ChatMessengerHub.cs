using api_chat_messenger.Database;
using api_chat_messenger.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace api_chat_messenger.Hubs {
    public class ChatMessengerHub : Hub {

        private ChatMessengerDatabaseContext _databaseContext;

        public ChatMessengerHub(ChatMessengerDatabaseContext databaseContext) {
            _databaseContext = databaseContext;
        }

        public async Task CadastrarUsuario(Usuario novoUsuario) {
            var usuarioExistente = _databaseContext.Usuarios.FirstOrDefault(usuario => usuario.Email == novoUsuario.Email);

            if (usuarioExistente != null) {
                await Clients.Caller.SendAsync("ConfirmarCadastro", false, null, "Já existe um usuário cadastrado com o email informado");
                return;
            }

            _databaseContext.Usuarios.Add(novoUsuario);
            await _databaseContext.SaveChangesAsync();

            await Clients.Caller.SendAsync("ConfirmarCadastro", true, novoUsuario, "Cadastro realizado com sucesso");
        }

        public async Task RealizarLogin(Usuario usuarioLogin) {
            var usuarioLogado = _databaseContext.Usuarios.FirstOrDefault(usuario => usuario.Email == usuarioLogin.Email && usuario.Senha == usuarioLogin.Senha);

            if (usuarioLogado == null) {
                await Clients.Caller.SendAsync("ConfirmarLogin", false, null, "Login não realizado: email e/ou senha estão incorretos");
                return;
            }

            await Clients.Caller.SendAsync("ConfirmarLogin", true, usuarioLogado, string.Empty);
        }
    }
}
