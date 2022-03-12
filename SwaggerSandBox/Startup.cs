using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwaggerSandBox
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


            services.AddVersionedApiExplorer(setupAction =>
            {
                setupAction.GroupNameFormat = "'v'VV";
            });

            services.AddApiVersioning(setupAction =>
            {
                setupAction.AssumeDefaultVersionWhenUnspecified = true;
                setupAction.DefaultApiVersion = new ApiVersion(1, 0);
                setupAction.ReportApiVersions = true;
                //Custom options to api version header thigs:
                //   setupAction.ApiVersionReader = new HeaderApiVersionReader("api-version");
                //    setupAction.ApiVersionReader = new MediaTypeApiVersionReader();
            });

            //Asegurarse de invocar esto despues de  AddApiVersioning
            var apiVersionDescriptionProvider =
                services.BuildServiceProvider().GetService<IApiVersionDescriptionProvider>();

            services.AddSwaggerGen(setupAction =>
            {
                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    setupAction.SwaggerDoc($"YourProject_OPENAPISpecification{description.GroupName}",
                new Microsoft.OpenApi.Models.OpenApiInfo()
                {
                    Title = "ProjectName API",
                    Version = description.ApiVersion.ToString(),
                    Description = "With this app you can do miracles",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                    {
                        Email = "email@gmail.com",
                        Name = "Someone's Name",
                        Url = new Uri("https://www.google.com.mx"),
                        //  Extensions - With this you can add address, headers, logos, and custom information that did come by default on openapi
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense()
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    },
                    TermsOfService = new Uri("https://www.aliveidea.com.mx")

                });

                }

                //Define customstrategy for selecting actions(select a certain action for a certain version of the specification)
                setupAction.DocInclusionPredicate((documentName, apiDescription) =>
                {
                    var actionApiVersionModel = apiDescription.ActionDescriptor
                    .GetApiVersionModel(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);

                    if (actionApiVersionModel == null)
                    {
                        return true;
                    }

                    if (actionApiVersionModel.DeclaredApiVersions.Any())
                    {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v =>
                        $"YourProject_OPENAPISpecificationv{v.ToString()}" == documentName);
                    }
                    return actionApiVersionModel.ImplementedApiVersions.Any(v =>
                        $"YourProject_OPENAPISpecificationv{v.ToString()}" == documentName);
                });

                ////Add open api specification
                ////Test if it works :https://localhost:44302/swagger/YourProject_OPENAPISpecification/swagger.json
                //services.AddSwaggerGen(setupAction =>
                //{
                //    setupAction.SwaggerDoc("YourProject_OPENAPISpecification",
                //       new Microsoft.OpenApi.Models.OpenApiInfo()
                //       {
                //           Title = "ProjectName API",
                //           Version = "1",
                //       });
                //});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider apiVersionDescriptionProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseSwagger();
            //http://localhost:59807/swagger/index.html
            app.UseSwaggerUI(setupAction =>
            {

                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    setupAction.SwaggerEndpoint($"/swagger/" +
                      $"YourProject_OPENAPISpecification{description.GroupName}/swagger.json",
                      description.GroupName.ToUpperInvariant());

                }
                setupAction.RoutePrefix = "";//Documentation available at the root

                setupAction.DefaultModelExpandDepth(2);//show fully expanded
                setupAction.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);//Controls what we see in the example part of the UI (The model or the example)
                setupAction.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                setupAction.DisplayOperationId();//Name of the method
                setupAction.DisplayRequestDuration();
                setupAction.EnableFilter();
                setupAction.EnableDeepLinking();//Search by tag(Controller name) and operational id to scroll automatically
            });
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
