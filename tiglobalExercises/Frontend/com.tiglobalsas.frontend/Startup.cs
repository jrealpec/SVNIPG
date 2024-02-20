using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using com.tiglobalsas.frontend.Data;
using com.tiglobalsas.frontend.Filters;
using SautinSoft.Document;
using System.IO;

namespace com.tiglobalsas.frontend
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

            services.AddRazorPages(options =>
            {
                options.Conventions
                    .AddPageApplicationModelConvention("/StreamedSingleFileUploadDb",
                        model =>
                        {
                            model.Filters.Add(
                                new GenerateAntiforgeryTokenCookieAttribute());
                            model.Filters.Add(
                                new DisableFormValueModelBindingAttribute());
                        });
                options.Conventions
                    .AddPageApplicationModelConvention("/StreamedSingleFileUploadPhysical",
                        model =>
                        {
                            model.Filters.Add(
                                new GenerateAntiforgeryTokenCookieAttribute());
                            model.Filters.Add(
                                new DisableFormValueModelBindingAttribute());
                        });
            });
            // To save physical files to a path provided by configuration:
            var _targetFilePath = Configuration.GetValue<string>("StoredFilesPath");

            bool folderExists = Directory.Exists(_targetFilePath);
            if (!folderExists)
            {
                Directory.CreateDirectory(_targetFilePath);
            }
            // To list physical files from a path provided by configuration:
            var physicalProvider = new PhysicalFileProvider(_targetFilePath);

            // To list physical files in the temporary files folder, use:
            //var physicalProvider = new PhysicalFileProvider(Path.GetTempPath());

            services.AddSingleton<IFileProvider>(physicalProvider);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("InMemoryDb"));

            string serial = "03/19/24zy/o2/IjTFkpZBCP3tbIsrlg1KqTyIye55";

            DocumentCore.SetLicense(serial);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
