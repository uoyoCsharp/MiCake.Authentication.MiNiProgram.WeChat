using MiCake.Authentication.MiniProgram.WeChat;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using WeChatAuthentication.Sample.Services;

namespace WeChatAuthentication.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<IUserManager, UserManager>();
            services.AddScoped<AssociateWeChatUser>();

            services.AddDistributedMemoryCache();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Audience = Configuration["JwtConfig:Audience"];
                    options.ClaimsIssuer = Configuration["JwtConfig:Issuer"];
                })
                .AddWeChatMiniProgram(options =>
                {
                    options.WeChatAppId = Configuration["WeChatMiniProgram:appid"];
                    options.WeChatSecret = Configuration["WeChatMiniProgram:secret"];
                    options.SaveSessionToCache = true;
                    options.Events.OnWeChatSessionObtained += RedirectToGiveToken;   //��Ӱ䷢JwtToken�Ĳ���
                    options.Events.OnRemoteFailure += HandleFailure;  //��Ӵ��������쳣��Ϣ��װΪ��ʽ���Ķ���
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public Task RedirectToGiveToken(WeChatSessionObtainedContext context)
        {
            context.HttpContext.Response.WriteAsJsonAsync(new { data = context.SessionCacheKey });
            return Task.CompletedTask;
        }

        public Task HandleFailure(RemoteFailureContext context)
        {
            context.HttpContext.Response.StatusCode = 500;
            context.HttpContext.Response.WriteAsJsonAsync(new { errorMsg = context.Failure.Message });

            context.HandleResponse();   // ��Response�Ѿ�Write������ʱ�����������仰
            return Task.CompletedTask;
        }
    }
}
