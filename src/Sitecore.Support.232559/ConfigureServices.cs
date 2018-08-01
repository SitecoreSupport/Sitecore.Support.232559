namespace Sitecore.Support
{
  using DependencyInjection;
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore.Modules.EmailCampaign.Services;
  using System.Linq;
  using Modules.EmailCampaign.Services;

  public class CustomServiceConfigurator:IServicesConfigurator
  {
    public void Configure(IServiceCollection serviceCollection)
    {
      var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IExmCampaignService));
      serviceCollection.Remove(descriptor);
      serviceCollection.AddSingleton<IExmCampaignService, SupportExmCampaignService>();
    }
  }
}