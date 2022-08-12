using api_chat_messenger.Database;
using api_chat_messenger.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            services.AddControllers();
            services.AddSignalR(config => {
                config.EnableDetailedErrors = true;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

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
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapHub<ChatMessengerHub>("/ChatMessengerHub");
            });
        }
    }
}