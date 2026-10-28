namespace TummlyBackend.Models
{
    public class AdminAuditEvent
    {
        public Guid Id { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public string Action { get; set; } = string.Empty;

        public int? ActorAdminUserId { get; set; }

        public string ActorIdentity { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public string TargetId { get; set; } = string.Empty;

        public int? RestaurantId { get; set; }

        public string? DetailJson { get; set; }

        public bool Succeeded { get; set; } = true;

        /// <summary>
        /// Request trace id (HttpContext.TraceIdentifier) when the event was
        /// written during an HTTP request. Null for background jobs.
        /// </summary>
        public string? CorrelationId { get; set; }
    }

    public static class AdminAuditActions
    {
        public const string TrialApprove = "trial.approve";
        public const string TrialDecline = "trial.decline";
        public const string TrialRequestMoreInfo = "trial.request_more_info";
        public const string TrialResendInvite = "trial.resend_invite";
        public const string TrialPurge = "trial.purge";
        public const string OperatorExtendActivation = "operator.extend_activation";
        public const string OperatorExtendPilot = "operator.extend_pilot";
        public const string CreditAdjust = "credit.adjust";
        public const string CreditReverse = "credit.reverse";
        public const string PaymentRefund = "payment.refund";
        public const string ShopPreparationStarted = "shop.preparation_started";
        public const string ShopProductionStarted = "shop.production_started";
        public const string ShopDispatched = "shop.dispatched";
        public const string ShopDelivered = "shop.delivered";
        public const string ShopDispatchEvidenceRecorded =
            "shop.dispatch_evidence_recorded";
        public const string ShopStatusCorrected = "shop.status_corrected";
        public const string ShopForceCancel = "shop.force_cancel";
        public const string ShopCatalogAvailableInShop =
            "shop.catalog_available_in_shop";
        public const string ShopCatalogCreate = "shop.catalog_create";
        public const string ShopCatalogUpdate = "shop.catalog_update";
        public const string ShopCatalogPublish = "shop.catalog_publish";
        public const string ShopCatalogUnpublish = "shop.catalog_unpublish";
        public const string ShopCatalogDelete = "shop.catalog_delete";
        public const string ShopCatalogImageChange = "shop.catalog_image_change";
        public const string ShopCatalogTagChange = "shop.catalog_tag_change";
        public const string ShopInterestPromote = "shop.interest_promote";
        public const string ShopInterestDemote = "shop.interest_demote";
        public const string ShopPrintAssetApproved = "shop.print_asset_approved";
        public const string ShopInterestRegistrationRemoved =
            "shop.interest_registration_removed";
        public const string ShopPrintAssetRetried = "shop.print_asset_retried";
        public const string RetentionGuestPurge = "retention.guest_purge";
        public const string GuestDelete = "guest.delete";
        public const string GuestDataExport = "guest.data_export";
        public const string GuestIdentityCorrection = "guest.identity_correction";
        public const string OperatorActivationAssetRead =
            "operator.activation_asset_read";
        public const string OperatorPrintMaterialsRead =
            "operator.print_materials_read";
        public const string OperatorPrintMaterialDownload =
            "operator.print_material_download";
        public const string ShopOrdersExport = "shop.orders_export";
        public const string ShopOrdersRead = "shop.orders_read";
        public const string TrialRequestsRead = "trial.requests_read";
        public const string OperatorPilotAccountsRead = "operator.pilot_accounts_read";
        public const string AdminAuditEventsRead = "audit.events_read";
        public const string PrivacyArchivedHistoryRead =
            "privacy_consent.archived_history_read";
        public const string SupportQueriesRead = "support.queries_read";
        public const string SupportQueryRead = "support.query_read";
        public const string CommercialCatalogPublish = "commercial_catalog.publish";
        public const string CommercialCatalogDraftSave = "commercial_catalog.draft_save";
        public const string BillingScheduledChangeGateFailed =
            "billing.scheduled_change_gate_failed";
        public const string BillingCustomSignupCheckoutFailed =
            "billing.custom_signup_checkout_failed";
        public const string BillingPaidSignupCheckoutFailed =
            "billing.paid_signup_checkout_failed";
        public const string BillingPendingPaymentRetryFailed =
            "billing.pending_payment_retry_failed";
        public const string LocationPremisesFlagged = "location.premises_flagged";
        public const string LocationPremisesCleared = "location.premises_cleared";
        public const string LocationPremisesRejected = "location.premises_rejected";
        public const string TrustIndicatorDraftEdit = "trust_indicator.draft_edit";
        public const string TrustIndicatorSubmit = "trust_indicator.submit";
        public const string TrustIndicatorPublish = "trust_indicator.publish";
        public const string TrustIndicatorRefused = "trust_indicator.refused";
        public const string StaffPasswordChange = "staff.password_change";
        public const string StaffCreate = "staff.create";
        public const string StaffDisable = "staff.disable";
        public const string StaffEnable = "staff.enable";
        public const string StaffUnlock = "staff.unlock";
        public const string StaffPasswordReset = "staff.password_reset";
    }

    public static class AdminAuditTargetTypes
    {
        public const string TrialRequest = "trial_request";
        public const string OperatorUser = "operator_user";
        public const string Restaurant = "restaurant";
        public const string LocationGuest = "location_guest";
        public const string PaymentOrder = "payment_order";
        public const string ShopOrder = "shop_order";
        public const string ShopMaterialSku = "shop_material_sku";
        public const string ShopInterestRegistration = "shop_interest_registration";
        public const string CommercialCatalogVersion = "commercial_catalog_version";
        public const string HelpCentreQuery = "help_centre_query";
        public const string AdminAuditEvent = "admin_audit_event";
        public const string RestaurantLocation = "restaurant_location";
        public const string ShopMaterialTag = "shop_material_tag";
        public const string ShopInterestProduct = "shop_interest_product";
        public const string TrustIndicatorItem = "trust_indicator_item";
        public const string TrustIndicatorDraft = "trust_indicator_draft";
        public const string TrustIndicatorPublication = "trust_indicator_publication";
        public const string StaffAccount = "staff_account";
    }
}
