﻿using System.Collections.Generic;
using System.Web.Mvc;
using SmartStore.AmazonPay.Services;
using SmartStore.AmazonPay.Models;
using SmartStore.AmazonPay.Settings;
using SmartStore.Services.Configuration;
using SmartStore.Services.Payments;
using SmartStore.Services;
using SmartStore.Services.Stores;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Settings;
using SmartStore.Web.Framework.Security;

namespace SmartStore.AmazonPay.Controllers
{
	public class AmazonPayController : PaymentControllerBase
	{
		private readonly IAmazonPayService _apiService;
		private readonly ISettingService _settingService;
		private readonly ICommonServices _services;
		private readonly IStoreService _storeService;

		public AmazonPayController(
			IAmazonPayService apiService,
			ISettingService settingService,
			ICommonServices services,
			IStoreService storeService)
		{
			_apiService = apiService;
			_settingService = settingService;
			_services = services;
			_storeService = storeService;
		}

		[NonAction]
		public override IList<string> ValidatePaymentForm(FormCollection form)
		{
			var warnings = new List<string>();
			return warnings;
		}

		[NonAction]
		public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
		{
			var paymentInfo = new ProcessPaymentRequest();
			return paymentInfo;
		}

		[AdminAuthorize]
		public ActionResult Configure()
		{
			var model = new ConfigurationModel();
			int storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _services.WorkContext);
			var settings = _settingService.LoadSetting<AmazonPaySettings>(storeScope);

			model.Copy(settings, true);

			_apiService.SetupConfiguration(model);

			var storeDependingSettingHelper = new StoreDependingSettingHelper(ViewData);
			storeDependingSettingHelper.GetOverrideKeys(settings, model, storeScope, _settingService);

			return View(model);
		}

		[HttpPost, AdminAuthorize]
		public ActionResult Configure(ConfigurationModel model, FormCollection form)
		{
			if (!ModelState.IsValid)
				return Configure();

			var storeDependingSettingHelper = new StoreDependingSettingHelper(ViewData);
			int storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _services.WorkContext);
			var settings = _settingService.LoadSetting<AmazonPaySettings>(storeScope);

			model.Copy(settings, false);

			storeDependingSettingHelper.UpdateSettings(settings, form, storeScope, _settingService);
			_settingService.ClearCache();

			_apiService.DataPollingTaskUpdate(settings.DataFetching == AmazonPayDataFetchingType.Polling, model.PollingTaskMinutes * 60);

			NotifySuccess(_services.Localization.GetResource("Plugins.Payments.AmazonPay.ConfigSaveNote"));

			return Configure();
		}

		[HttpPost]
		[ValidateInput(false)]
		[RequireHttpsByConfigAttribute(SslRequirement.Yes)]
		public ActionResult IPNHandler()
		{
			_apiService.ProcessIpn(Request);
			return Content("OK");
		}
	}
}
