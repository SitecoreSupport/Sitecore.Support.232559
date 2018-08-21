namespace Sitecore.Support.Pipelines.DispatchNewsletter
{
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using SecurityModel;
    using System;
    using Analytics;
    using Analytics.Data.Items;
    using Diagnostics;
    using EmailCampaign.Cm.Pipelines.DispatchNewsletter;
    using EmailCampaign.Model;
    using EmailCampaign.Model.Message;
    using EmailCampaign.Model.Web.Settings;
    using ExM.Framework.Diagnostics;
    using Sitecore.Modules.EmailCampaign;
    using Sitecore.Modules.EmailCampaign.Core;
    using Sitecore.Modules.EmailCampaign.Core.Data;
    using Sitecore.Modules.EmailCampaign.Messages;
    using Sitecore.Modules.EmailCampaign.Services;
    using ExmConstants = Sitecore.EmailCampaign.Model.Constants;

  /// <summary>
  /// Defines the deploy analytics class.
  /// </summary>
  public class DeployAnalytics
  {

    private readonly ItemUtilExt _itemUtil;
    private readonly IExmCampaignService _exmCampaignService;
    private readonly EcmDataProvider _dataProvider;
    private readonly EcmSettings _settings;
    private readonly ILogger _logger;

    public DeployAnalytics([NotNull] EcmDataProvider dataProvider, [NotNull] EcmSettings settings, [NotNull] ILogger logger, [NotNull] ItemUtilExt util, [NotNull] IExmCampaignService exmCampaignService)
    {
      Assert.ArgumentNotNull(dataProvider, nameof(dataProvider));
      Assert.ArgumentNotNull(settings, nameof(settings));
      Assert.ArgumentNotNull(logger, nameof(logger));
      Assert.ArgumentNotNull(util, "util");
      Assert.ArgumentNotNull(exmCampaignService, nameof(exmCampaignService));

      _dataProvider = dataProvider;
      _settings = settings;
      _logger = logger;
      this._itemUtil = util;
      _exmCampaignService = exmCampaignService;
    }

    public void Process(DispatchNewsletterArgs args)
    {
      if (args.IsTestSend || args.SendingAborted || args.Queued)
      {
        return;
      }

      try
      {
        using (new SecurityDisabler())
        {
          var campaignItem = this.ProcessCampaign(args.Message);
          this.EnsureCampaignData(args.Message, campaignItem);
        }
      }
      catch (Exception e)
      {
        args.AbortSending(e, true, _logger);
        args.AbortPipeline();
      }
    }

    protected virtual Item ProcessCampaign(MessageItem message)
    {
      Assert.ArgumentNotNull(message, "message");

      var messageCampaignId = message.CampaignId;
      if (ID.IsNullOrEmpty(messageCampaignId))
      {
        messageCampaignId = _exmCampaignService.GetMessageItem(message.MessageId).CampaignId;
      }

      // Ensure campaign item in Sitecore.
      var campaignItem = this._itemUtil.GetItem(messageCampaignId);
      if (campaignItem == null || campaignItem.TemplateID != AnalyticsIds.Campaign)
      {
        campaignItem = this.AddCampaignItem(message);
      }

      campaignItem.Editing.BeginEdit();
      campaignItem[CampaignItem.FieldIDs.Data] = message.ManagerRoot.InnerItem.ID + message.ID;
      campaignItem[CampaignclassificationItem.FieldIDs.Channel] = _settings.CampaignClassificationChannel;
      campaignItem[CampaignclassificationItem.FieldIDs.Campaigngroup] = message.Source.CampaignGroup;

      campaignItem.Editing.EndEdit();

      // Deploy the campaign item to Analytics.
      this._itemUtil.ExecuteWorkflowCommandForItem(campaignItem, ItemIds.DeployAnalyticsCommand);

      return campaignItem;
    }

    protected virtual void EnsureCampaignData(MessageItem message, Item campaignItem)
    {
      _dataProvider.SaveCampaign(message, campaignItem);
    }

    private Item AddCampaignItem(MessageItem message)
    {
      Item destinationRoot = null;


      if (message.MessageType != MessageType.Undefined)
      {
        destinationRoot = this._itemUtil.GetItem(message.CampaignPosition);
      }

      if (destinationRoot == null)
      {
        destinationRoot = this._itemUtil.GetItem(ItemIds.MessageCampaignsFolder);
      }

      Util.AssertNotNull(destinationRoot);

      var destination = this._itemUtil.GetDestinationFolderItem(destinationRoot);
      Util.AssertNotNull(destination);

      destination = this._itemUtil.GetItem(destination.ID, message.TargetLanguage, false);
      Util.AssertNotNull(destination);

      var campaignItem = this._itemUtil.AddSubItemFromTemplate($"{message.InnerItem.Name}{message.MessageId:D}", message.InnerItem.DisplayName, new TemplateID(AnalyticsIds.Campaign), destination);
      Util.AssertNotNull(campaignItem);

      campaignItem.Editing.BeginEdit();
      campaignItem[new ID(FieldIds.CampaignItemTitle)] = campaignItem.Name;
      campaignItem[new ID(FieldIds.CampaignItemTrafficType)] = ExmConstants.EmailTrafficTypeId;
      if (campaignItem.Fields.Contains(new ID(FieldIds.CampaignItemChangeTrafficType)))
      {
        campaignItem[new ID(FieldIds.CampaignItemChangeTrafficType)] = ExmConstants.EmailChangeTrafficTypeId;
      }

      var startDate = DateTime.UtcNow;
      campaignItem[new ID(FieldIds.CampaignItemStartDate)] = DateUtil.ToIsoDate(startDate);

      campaignItem.Editing.EndEdit();

      message.Source.CampaignId = campaignItem.ID;
      message.Source.StartTime = startDate;

      return campaignItem;
    }

  }
}