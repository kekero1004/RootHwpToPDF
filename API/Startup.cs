using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using HwpToPdf.Controllers;
using HwpToPdf.Services;

namespace HwpToPdf.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            // API 문서화를 위한 Swagger 설정
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "HWP to PDF 변환 API", 
                    Version = "v1",
                    Description = "한글(HWP) 문서를 PDF로 변환하는 REST API"
                });
            });
            
            // 서비스 등록
            services.AddSingleton<IHwpService, HwpComInteropService>();
            services.AddSingleton<IPdfPostProcessingService, ITextPdfService>();
            services.AddSingleton<ConversionQueueService>();
            services.AddSingleton<ConversionController>();
            
            // CORS 설정
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HWP to PDF API v1"));

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("DefaultPolicy");
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
} 