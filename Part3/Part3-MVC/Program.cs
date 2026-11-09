using RaceDay.Mvc.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
                            {
                                o.Cookie.Name = ".RaceDay.Mvc";
                                o.Cookie.HttpOnly = true;
                                o.Cookie.IsEssential = true;
                                o.Cookie.SameSite = SameSiteMode.Lax;
                                o.IdleTimeout = TimeSpan.FromMinutes(60);
                            });
builder.Services
    .AddHttpClient<IRaceDayApi, RaceDayApiClient>(client =>
                                                  {
                                                      client.BaseAddress =
                                                          new Uri(builder.Configuration["Api:BaseUrl"] ??
                                                                  "http://localhost:8080/");
                                                      client.Timeout = TimeSpan.FromSeconds(20);
                                                  })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler { UseCookies = false };
        // Local development only: trust the API's self-signed dev certificate.
        if (builder.Environment.IsDevelopment())
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    });
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
public partial class Program
{
}
