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
            var gruposDoUsuario = await _databaseContext.Grupos.Where(grupo => grupo.Usuarios.Contains(usuarioDoBanco.Email)).ToListAsync();

            if (usuarioDoBanco.ConnectionId?.Length > 0) {
                var connectionIdsBanco = JsonConvert.DeserializeObject<List<string>>(usuarioDoBanco.ConnectionId);

                var connectionIdAtual = Context.ConnectionId;
                if (!connectionIdsBanco.Contains(connectionIdAtual)) {
                    connectionIdsBanco.Add(connectionIdAtual);

                    usuarioDoBanco.ConnectionId = JsonConvert.SerializeObject(connectionIdsBanco);

                    _databaseContext.Usuarios.Update(usuarioDoBanco);
                    await _databaseContext.SaveChangesAsync();

                    foreach (var connectionId in connectionIdsBanco) {
                        foreach (var grupo in gruposDoUsuario) {
                            await Groups.AddToGroupAsync(connectionId, grupo.Nome);
                        }
                    }
                }

                return;
            }

            var connectionIds = new List<string>() { Context.ConnectionId };
            usuarioDoBanco.ConnectionId = JsonConvert.SerializeObject(connectionIds);
            _databaseContext.Usuarios.Update(usuarioDoBanco);
            await _databaseContext.SaveChangesAsync();

            foreach (var connectionId in connectionIds) {
                foreach (var grupo in gruposDoUsuario) {
                    await Groups.AddToGroupAsync(connectionId, grupo.Nome);
                }
            }
        }

        public async Task RemoverConnectionIdDoUsuario(Usuario usuario) {
            var usuarioDoBanco = _databaseContext.Usuarios.First(u => u.Id == usuario.Id);

            if (usuarioDoBanco.ConnectionId.Length > 0) {
                var connectionIds = JsonConvert.DeserializeObject<List<string>>(usuarioDoBanco.ConnectionId);

                var connectionIdAtual = Context.ConnectionId;
                if (connectionIds.Contains(connectionIdAtual)) {
                    connectionIds.Remove(connectionIdAtual);

                    usuarioDoBanco.ConnectionId = JsonConvert.SerializeObject(connectionIds);

                    if (connectionIds.Count <= 0) {
                        usuarioDoBanco.isOnline = false;
                    }

                    _databaseContext.Usuarios.Update(usuarioDoBanco);
                    await _databaseContext.SaveChangesAsync();

                    await ObterListaDeUsuarios();
                }

                var gruposDoUsuario = await _databaseContext.Grupos.Where(grupo => grupo.Usuarios.Contains(usuarioDoBanco.Email)).ToListAsync();
                foreach (var connectionId in connectionIds) {
                    foreach (var grupo in gruposDoUsuario) {
                        await Groups.RemoveFromGroupAsync(connectionId, grupo.Nome);
                    }
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

            if (grupoExistente == null) {
                grupoExistente = new Grupo() {
                    Nome = nomeGrupo,
                    Usuarios = JsonConvert.SerializeObject(new List<string>() {
                        emailUsuarioLogado,
                        emailUsuarioSelecionado
                    })
                };

                await _databaseContext.Grupos.AddAsync(grupoExistente);
                await _databaseContext.SaveChangesAsync();
                return;
            }

            var emails = JsonConvert.DeserializeObject<List<string>>(grupoExistente.Usuarios);

            var usuarioLogado = await _databaseContext.Usuarios.FirstAsync(usuario => usuario.Email == emails[0]);
            var usuarioSelecionado = await _databaseContext.Usuarios.FirstAsync(usuario => usuario.Email == emails[1]);
            var usuarios = new List<Usuario>() { usuarioLogado, usuarioSelecionado };

            foreach (var usuario in usuarios) {
                var connectionIdsDoUsuario = JsonConvert.DeserializeObject<List<string>>(usuario.ConnectionId);

                foreach (var connectionId in connectionIdsDoUsuario) {
                    await Groups.AddToGroupAsync(connectionId, nomeGrupo);
                }
            }

            var mensagensDoGrupo = await _databaseContext.Mensagens.Where(mensagem => mensagem.NomeGrupo == nomeGrupo)
                                                                   .OrderBy(mensagem => mensagem.DataCriacao)
                                                                   .ToListAsync();

            for (int i = 0; i < mensagensDoGrupo.Count; i++) {
                mensagensDoGrupo[i].Usuario = JsonConvert.DeserializeObject<Usuario>(mensagensDoGrupo[i].UsuarioJson);
            }

            await Clients.Caller.SendAsync("AbrirGrupo", nomeGrupo, mensagensDoGrupo);
        }

        private string CriarNomeGrupo(string emailUsuarioLogado, string emailUsuarioSelecionado) {
            var emails = new List<string>() { emailUsuarioLogado, emailUsuarioSelecionado };
            var emailsOrdenados = emails.OrderBy(email => email).ToList();

            return emailsOrdenados[0] + emailsOrdenados[1];
        }

        public async Task EnviarMensagem(Usuario usuario, string conteudoMensagem, string nomeDoGrupo) {
            var grupoExistente = await _databaseContext.Grupos.FirstAsync(grupo => grupo.Nome == nomeDoGrupo);

            if (!grupoExistente.Usuarios.Contains(usuario.Email)) {
                throw new Exception("O usuário logado não pertence a este grupo");
            }

            var mensagem = new Mensagem() {
                NomeGrupo = nomeDoGrupo,
                Texto = conteudoMensagem,
                UsuarioId = usuario.Id,
                UsuarioJson = JsonConvert.SerializeObject(usuario),
                Usuario = usuario,
                DataCriacao = DateTime.Now
            };

            await _databaseContext.Mensagens.AddAsync(mensagem);
            await _databaseContext.SaveChangesAsync();

            await Clients.Group(nomeDoGrupo).SendAsync("ReceberMensagem", mensagem, nomeDoGrupo);
        }
    }
}
