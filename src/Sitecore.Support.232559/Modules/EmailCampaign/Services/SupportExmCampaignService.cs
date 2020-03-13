namespace Sitecore.Support.Modules.EmailCampaign.Services
{
  using System;
  using System.Globalization;
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Data.Managers;
  using Sitecore.EmailCampaign.Model;
  using Sitecore.Framework.Conditions;
  using Sitecore.Globalization;
  using Sitecore.Links;
  using Sitecore.Marketing.Core.Extensions;
  using Sitecore.Marketing.Definitions;
  using Sitecore.Marketing.Definitions.Campaigns;
  using Sitecore.Modules.EmailCampaign;
  using Sitecore.Modules.EmailCampaign.Core;
  using Sitecore.Modules.EmailCampaign.Factories;
  using Sitecore.Modules.EmailCampaign.Messages;
  using Sitecore.Modules.EmailCampaign.Services;
  using Sitecore.Tasks;

  public class SupportExmCampaignService:IExmCampaignService
  {
      private readonly TypeResolver _typeResolver;
      private readonly ItemUtilExt _itemUtil;
      private readonly IDefinitionManager<ICampaignActivityDefinition> _campaignDefinitionManager;
      private readonly ICultureProvider _cultureProvider;

      public SupportExmCampaignService(
          [NotNull] ITypeResolverFactory typeResolverFactory,
          [NotNull] ItemUtilExt itemUtil,
          [NotNull] IDefinitionManager<ICampaignActivityDefinition> campaignDefinitionManager,
          [NotNull] ICultureProvider cultureProvider)
      {
        Condition.Requires(typeResolverFactory, nameof(typeResolverFactory)).IsNotNull();
        Condition.Requires(itemUtil, nameof(itemUtil)).IsNotNull();
        Condition.Requires(campaignDefinitionManager, nameof(campaignDefinitionManager)).IsNotNull();
        Condition.Requires(cultureProvider, nameof(cultureProvider)).IsNotNull();

        _typeResolver = typeResolverFactory.GetTypeResolver();
        _itemUtil = itemUtil;
        _campaignDefinitionManager = campaignDefinitionManager;
        _cultureProvider = cultureProvider;
      }

      public MessageItem GetMessageItem(Guid messageId)
      {
        Condition.Requires(messageId, nameof(messageId)).IsNotEmptyGuid();

        Item item = _itemUtil.GetItem(messageId);

        return _typeResolver.GetCorrectMessageObject(item);
      }

      public MessageItem GetMessageItem(Guid messageId, string language)
      {
        Condition.Requires(messageId, nameof(messageId)).IsNotEmptyGuid();

        Language lang = LanguageManager.GetLanguage(language);

        Item item = _itemUtil.GetItem(ID.Parse(messageId), lang, false);

        return _typeResolver.GetCorrectMessageObject(item);
      }

      public MessageItem GetMessageItem(Item item)
      {
        return _typeResolver.GetCorrectMessageObject(item);
      }

      public bool IsMessageItem(Item item)
      {
        if (ABTestMessage.IsCorrectMessageItem(item))
        {
          return true;
        }

        if (WebPageMail.IsCorrectMessageItem(item))
        {
          return true;
        }

        if (HtmlMail.IsCorrectMessageItem(item))
        {
          return true;
        }

        if (TextMail.IsCorrectMessageItem(item))
        {
          return true;
        }

        return false;
      }

      public ItemLink[] GetItemReferrers(Item item)
      {
        return Factory.GetLinkDatabase().GetItemReferrers(item, false);
      }

      public ICampaignActivityDefinition GetMessageCampaign(Guid campaignId)
      {
        Condition.Requires(campaignId, nameof(campaignId)).IsNotNull();
        return _campaignDefinitionManager.Get(campaignId, CultureInfo.InvariantCulture, true);
    }
      public ICampaignActivityDefinition GetMessageCampaign(MessageItem message)
      {
        Condition.Requires(message, nameof(message)).IsNotNull();

        ID messageCampaignId = message.CampaignId;
        if (ID.IsNullOrEmpty(messageCampaignId))
        {
          messageCampaignId = GetMessageItem(message.MessageId).CampaignId;
        }

        return GetMessageCampaign(messageCampaignId.Guid);
      }

      public Item GetItem(string itemPath)
      {
        return _itemUtil.GetItemByPath(itemPath);
      }

      public ScheduleItem GetTaskItem(Guid messageId, Guid branchId)
      {
        return _itemUtil.GetTaskItem(messageId, branchId);
      }

      public ScheduleItem GetSendingTaskItem(string messageId)
      {
        return GetTaskItem(Guid.Parse(messageId), Guid.Parse(ItemIds.SendMessageTaskBranch));
      }
    }
  }
