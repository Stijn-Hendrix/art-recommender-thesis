using frontend.Data;
using frontend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

builder.Services.AddScoped<DbSeeder>(); //can be placed among other "AddScoped" - above: var app = builder.Build();   

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();


builder.Services.AddHttpContextAccessor();

//builder.WebHost.UseUrls("http://*:5292", "https://*:443");
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseSession();

/// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Error");
//// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();

var rewriteOptions = new RewriteOptions()
    .AddRedirectToHttpsPermanent()
    .Add(new RedirectWwwRule());

app.UseRewriter(rewriteOptions);

app.UseHttpsRedirection();


using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    dbInitializer.Seed();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();


app.Run();


public class RedirectWwwRule : IRule
{
    public void ApplyRule(RewriteContext context)
    {
        var request = context.HttpContext.Request;
        var host = request.Host;

        if (!host.Host.StartsWith("www", StringComparison.OrdinalIgnoreCase))
        {
            var newHost = new HostString("www." + host.Host, host.Port ?? 443);
            var newUrl = $"{request.Scheme}://{newHost}{request.Path}{request.QueryString}";
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status301MovedPermanently;
            response.Headers[HeaderNames.Location] = newUrl;
            context.Result = RuleResult.EndResponse; // Stop processing rules
        }
    }
}
