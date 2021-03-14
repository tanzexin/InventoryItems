using DotNetCore.CAP.Dashboard.NodeDiscovery;
using DotNetCore.CAP.Messages;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StockInterface;
using StockItemServer;
using StockModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zhaoxi.AgileFramework.Common.IOCOptions;
using Zhaoxi.AgileFramework.Core.ConsulExtend;
using Zhaoxi.AgileFramework.WebCore.FilterExtend;
using Zhaoxi.AgileFramework.WebCore.MiddlewareExtend;
using Zhaoxi.MSACommerce.Core;

namespace Zhaoxi.MSACommerce.StockMicroservice
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

            services.AddControllers(o =>
            {
                o.Filters.Add(typeof(CustomExceptionFilterAttribute));
                o.Filters.Add(typeof(LogActionFilterAttribute));
            }).AddNewtonsoftJson(options =>
            {
                //options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                //options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm";
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Zhaoxi.MSACommerce.StockMicroservice", Version = "v1" });
            });

            #region jwt校验  HS
            JWTTokenOptions tokenOptions = new JWTTokenOptions();
            Configuration.Bind("JWTTokenOptions", tokenOptions);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)//Scheme
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    //JWT有一些默认的属性，就是给鉴权时就可以筛选了
                    ValidateIssuer = true,//是否验证Issuer
                    ValidateAudience = true,//是否验证Audience
                    ValidateLifetime = false,//是否验证失效时间
                    ValidateIssuerSigningKey = true,//是否验证SecurityKey
                    ValidAudience = tokenOptions.Audience,//
                    ValidIssuer = tokenOptions.Issuer,//Issuer，这两项和前面签发jwt的设置一致
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.SecurityKey))//拿到SecurityKey

                };
            });
            #endregion

            #region 服务注入
            services.AddTransient<OrangeStockContext>();
            services.AddTransient<IStockService, StockService>();
            services.AddTransient<IStockManagerService, StockManagerService>();
            services.AddTransient<CacheClientDB>();
            #endregion

            #region 配置文件注入
            services.Configure<MySqlConnOptions>(this.Configuration.GetSection("MysqlConn"));
            services.Configure<RedisConnOptions>(this.Configuration.GetSection("RedisConn"));
            services.Configure<RabbitMQOptions>(this.Configuration.GetSection("RabbitMQOptions"));
            #endregion

            #region CAP配置
            string mysqlConn = this.Configuration["MysqlConn:url"];//数据库连接
            string rabbitMQHost = this.Configuration["RabbitMQOptions:HostName"];//RabbitMQ连接

            services.AddOptions<DotNetCore.CAP.MySqlOptions>().Configure(o =>
            {
                o.ConnectionString = mysqlConn;
            });

            services.AddCap(x =>
            {
                x.UseMySql(mysqlConn);
                x.UseRabbitMQ(rabbitMQHost);
                x.FailedRetryCount = 30;
                x.FailedRetryInterval = 60;//second
                x.FailedThresholdCallback = failed =>
                {
                    var logger = failed.ServiceProvider.GetService<ILogger<Startup>>();
                    logger.LogError($@"MessageType {failed.MessageType} 失败了， 重试了 {x.FailedRetryCount} 次, 
                        消息名称: {failed.Message.GetName()}");//do anything
                };

                #region 注册Consul可视化
                x.UseDashboard();
                DiscoveryOptions discoveryOptions = new DiscoveryOptions();
                this.Configuration.Bind(discoveryOptions);
                x.UseDiscovery(d =>
                {
                    d.DiscoveryServerHostName = discoveryOptions.DiscoveryServerHostName;
                    d.DiscoveryServerPort = discoveryOptions.DiscoveryServerPort;
                    d.CurrentNodeHostName = discoveryOptions.CurrentNodeHostName;
                    d.CurrentNodePort = discoveryOptions.CurrentNodePort;
                    d.NodeId = discoveryOptions.NodeId;
                    d.NodeName = discoveryOptions.NodeName;
                    d.MatchPath = discoveryOptions.MatchPath;
                });
                #endregion
            });
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (true || env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zhaoxi.MSACommerce.StockMicroservice v1"));
            }
            #region Consul-HealthCheck
            app.UseHealthCheckMiddleware("/Health");
            #endregion

            #region Options预请求处理
            app.UsePreOptionsRequest();
            #endregion

            #region jwt 
            app.UseAuthentication();
            #endregion

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            #region Consul注册
            app.UseConsulConfiguration(this.Configuration).Wait();
            #endregion

            #region MyRegion
            var managerService = app.ApplicationServices.GetService<IStockManagerService>();
            managerService.InitRedisStock();
            #endregion
        }
    }
}
