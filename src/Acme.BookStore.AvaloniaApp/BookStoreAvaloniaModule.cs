using Acme.BookStore.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace Acme.BookStore.AvaloniaApp;

[DependsOn(typeof(AbpAutofacModule),
    typeof(BookStoreApplicationModule),
    typeof(BookStoreEntityFrameworkCoreModule))]
public class BookStoreAvaloniaModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options => options.IsEnabled = false);
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
        Configure<AbpBackgroundWorkerOptions>(options => options.IsEnabled = false);

        // Disable authorization for desktop app (no user authentication)
        context.Services.AddAlwaysAllowAuthorization();
    }
}
