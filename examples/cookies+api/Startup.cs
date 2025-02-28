﻿using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace simple_aspnet_auth
{
  public class Startup
  {
    public IConfiguration Configuration { get; set; }

    public Startup(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
    {
      Configuration = new ConfigurationBuilder()
        .SetBasePath(env.ContentRootPath)
        .AddJsonFile("appsettings.json")
        .Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
      var configSection = Configuration.GetSection(nameof(JwtSettings));
      var settings = new JwtSettings();

      configSection.Bind(settings);

      services.AddSingleton<IConfiguration>(provider => Configuration);
      services.AddTransient<ITokenService, TokenService>();
      services.AddScoped<IUserService, UserService>();
      services.AddOptions();
      services.Configure<JwtSettings>(configSection);
      services.AddAuthentication(options => 
      {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; 
      }).AddCookie().AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
      {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidIssuer = settings.Issuer,
          ValidAudience = settings.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
          ValidateIssuerSigningKey = true,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.Zero // the default for this setting is 5 minutes
        };
        options.Events = new JwtBearerEvents
        {
          OnAuthenticationFailed = context =>
          {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
              context.Response.Headers.Add("Token-Expired", true.ToString().ToLower());
            }
            return Task.CompletedTask;
          }
        };

      });
      services.AddMvc();

    }

    public void Configure(IApplicationBuilder app)
    {
      app.UseDeveloperExceptionPage();
      app.UseStaticFiles();
      app.UseRouting();
      app.UseAuthentication();
      app.UseAuthorization();
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}");
      });
    }

    public static void Main(string[] args) =>
      WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().Build().Run();

  }

}
