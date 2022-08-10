using api_chat_messenger.Database;
using api_chat_messenger.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

            usuarioLogado.isOnline = true;
            _databaseContext.Usuarios.Update(usuarioLogado);
            await _databaseContext.SaveChangesAsync();
            await Clients.Caller.SendAsync("ConfirmarLogin", true, usuarioLogado, string.Empty);

            var usuarios = await _databaseContext.Usuarios.ToListAsync();
            await Clients.All.SendAsync("ReceberListaDeUsuarios", usuarios);
        }

        public async Task RealizarLogout(Usuario usuarioLogout) {
            var usuarioLogado = _databaseContext.Usuarios.FirstOrDefault(usuario => usuario.Id == usuarioLogout.Id);

            usuarioLogado.isOnline = false;
            _databaseContext.Usuarios.Update(usuarioLogado);
            await _databaseContext.SaveChangesAsync();

            await RemoverConnectionIdDoUsuario(usuarioLogout);

            var usuarios = await _databaseContext.Usuarios.ToListAsync();
            await Clients.All.SendAsync("ReceberListaDeUsuarios", usuarios);
        }

        public async Task AdicionarConnectionIdDoUsuario(Usuario usuario) {
            var usuarioDoBanco = _databaseContext.Usuarios.First(u => u.Id == usuario.Id);

            if (usuarioDoBanco.ConnectionId?.Length > 0) {
                var connectionIdsBanco = JsonConvert.DeserializeObject<List<string>>(usuarioDoBanco.ConnectionId);

                var connectionIdAtual = Context.ConnectionId;
                if (!connectionIdsBanco.Contains(connectionIdAtual)) {
                    connectionIdsBanco.Add(connectionIdAtual);

                    usuarioDoBanco.ConnectionId = JsonConvert.SerializeObject(connectionIdsBanco);

                    _databaseContext.Usuarios.Update(usuarioDoBanco);
                    await _databaseContext.SaveChangesAsync();
                }

                return;
            }

            var connectionIds = new List<string>() { Context.ConnectionId };

            usuarioDoBanco.ConnectionId = JsonConvert.SerializeObject(connectionIds);

            _databaseContext.Usuarios.Update(usuarioDoBanco);
            await _databaseContext.SaveChangesAsync();
        }

        public async Task RemoverConnectionIdDoUsuario(Usuario usuario) {
            var usuarioDoBanco = _databaseContext.Usuarios.First(u => u.Id == usuario.Id);

            if (usuarioDoBanco.ConnectionId.Length > 0) {
                var connectionIds = JsonConvert.DeserializeObject<List<string>>(usuarioDoBanco.ConnectionId);

                var connectionIdAtual = Context.ConnectionId;
                if (connectionIds.Contains(connectionIdAtual)) {
                    connectionIds.Remove(connectionIdAtual);

                    usuarioDoBanco.ConnectionId = JsonConvert.SerializeObject(connectionIds);
                    _databaseContext.Usuarios.Update(usuarioDoBanco);
                    await _databaseContext.SaveChangesAsync();
                }
            }
        }

        public async Task ObterListaDeUsuarios() {
            var usuarios = await _databaseContext.Usuarios.ToListAsync();
            await Clients.Caller.SendAsync("ReceberListaDeUsuarios", usuarios);
        }

        public async Task CriarOuAbrirGrupo(string emailUsuarioLogado, string emailUsuarioSelecionado) {
            var nomeGrupo = CriarNomeGrupo(emailUsuarioLogado, emailUsuarioSelecionado);
            var grupoExistente = await _databaseContext.Grupos.FirstOrDefaultAsync(grupo => grupo.Nome == nomeGrupo);

            var usuarioLogado = await _databaseContext.Usuarios.FirstAsync(usuario => usuario.Email == emailUsuarioLogado);
            var usuarioSelecionado = await _databaseContext.Usuarios.FirstAsync(usuario => usuario.Email == emailUsuarioSelecionado);

            if (grupoExistente == null) {
                var novoGrupo = new Grupo() {
                    Nome = nomeGrupo,
                    Usuarios = JsonConvert.SerializeObject(new List<Usuario>() {
                        usuarioLogado,
                        usuarioSelecionado
                    })
                };

                await _databaseContext.Grupos.AddAsync(novoGrupo);
                await _databaseContext.SaveChangesAsync();
            }
        }

        private string CriarNomeGrupo(string emailUsuarioLogado, string emailUsuarioSelecionado) {
            var emails = new List<string>() { emailUsuarioLogado, emailUsuarioSelecionado };
            var emailsOrdenados = emails.OrderBy(email => email).ToList();

            return emailsOrdenados[0] + emailsOrdenados[1];
        }
    }
}
