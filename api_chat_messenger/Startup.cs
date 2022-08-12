using api_chat_messenger.Database;
using api_chat_messenger.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace api_chat_messenger {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            services.AddDbContext<ChatMessengerDatabaseContext>(config => {
                config.UseSqlite("Data Source=Database\\ZapWeb.db");
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSignalR(config => {
                config.EnableDetailedErrors = true;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseHsts();
            }

            //Configuração produção
            //app.UseCors(builder => {
            //    builder.WithOrigins("https://chatmessengerwebapp.azurewebsites.net")
            //        .AllowAnyHeader()
            //        .AllowAnyMethod()
            //        .AllowCredentials();
            //});

            //Configuração local
            app.UseCors(builder => {
                builder.WithOrigins("https://localhost:5003")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            app.UseHttpsRedirection();
            app.UseMvc();
            app.UseSignalR(config => {
                config.MapHub<ChatMessengerHub>("/ChatMessengerHub");
            });
        }
    }
}