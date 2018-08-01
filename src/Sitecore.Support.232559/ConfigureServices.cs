namespace Sitecore.Support
{
  using DependencyInjection;
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore.Modules.EmailCampaign.Services;
  using System.Linq;

  public class CustomServiceConfigurator:IServicesConfigurator
  {
    public void Configure(IServiceCollection serviceCollection)
    {
      serviceCollection.AddSingleton<IExmCampaignService, ExmCampaignService>();
      var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IExmCampaignService));
      serviceCollection.Remove(descriptor);
      serviceCollection.AddSingleton<IExmCampaignService, ExmCampaignService>();
    }
  }
}