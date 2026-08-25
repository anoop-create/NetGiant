/* ============================================================================
   WebsiteInfoCard - "Payment" category seed data (2026-08-25)

   Makes the basket-page sidebar's "Payment Cards Accepted" box editable from
   the Intranet's Website Info Cards admin screen instead of being three
   hardcoded <img> tags in BasketDetails.cshtml. No schema change needed -
   this reuses the existing dbo.WebsiteInfoCard table and its already-present
   "Payment" Category value (added in an earlier pass to the Categories list
   in WebsiteInfoCardViewModel.cs, but never previously wired up to actually
   render as card logo images anywhere).

   One row per payment card logo:
     - Title       -> the logo's alt text (e.g. "Visa")
     - ImageUrl    -> the logo image path (same images already used today)
     - DisplayOrder -> left-to-right order in the "Payment Cards Accepted" box
     - IsActive    -> uncheck in the admin to hide a logo without deleting it

   Run this against the ngmd database AFTER WebsiteInfoCard_Schema.sql (i.e.
   the table must already exist). Safe to re-run - skips seeding if this
   website already has any "Payment" category rows.

   IMPORTANT: verify the WebsiteName below actually matches your TonerGiant
   row before running (SELECT WebsiteID, WebsiteName, FriendlyName FROM
   ngmd.Website to check) - swap to FriendlyName or Abbreviation if needed,
   exactly as noted in WebsiteInfoCard_Schema.sql's own seed section.
   ============================================================================ */

DECLARE @WebsiteId INT = (SELECT WebsiteID FROM ngmd.Website WHERE WebsiteName = 'TonerGiant');

IF @WebsiteId IS NULL
BEGIN
    PRINT 'Could not find a Website row named ''TonerGiant'' - skipping seed data. Check ngmd.Website and re-run the INSERTs below manually with the correct WebsiteId.';
END
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.WebsiteInfoCard WHERE WebsiteId = @WebsiteId AND Category = 'Payment')
BEGIN
    INSERT INTO dbo.WebsiteInfoCard
        (WebsiteId, Category, IconClass, Title, BodyText, FindOutMoreContent, ImageUrl, LinkUrl, DisplayOrder, IsActive, CreatedBy)
    VALUES
    (@WebsiteId, 'Payment', NULL, 'Visa',       NULL, NULL, '/Content/Images/icons/visa-card.png',   NULL, 1, 1, 'Migration'),
    (@WebsiteId, 'Payment', NULL, 'Mastercard', NULL, NULL, '/Content/Images/icons/master-card.png', NULL, 2, 1, 'Migration'),
    (@WebsiteId, 'Payment', NULL, 'Amex',       NULL, NULL, '/Content/Images/icons/amex-card.png',   NULL, 3, 1, 'Migration');
END
ELSE
BEGIN
    PRINT 'This website already has "Payment" category WebsiteInfoCard rows - skipping seed (nothing changed). Manage existing rows via the Intranet''s Website Info Cards admin screen instead.';
END
GO

/* ============================================================================
   NEXT STEPS

   1. Run this script against the ngmd database (after confirming the
      WebsiteName match above).

   2. No EF model refresh needed here - WebsiteInfoCard is already a mapped
      entity in both projects since Pass 4 (the original feature build); this
      script only adds rows to the existing table, it doesn't change the
      table's shape.

   3. Rebuild/redeploy BOTH repos before testing:
        - Intranet:  CreateEntry.cshtml (Payment now shows the Image URL /
          Link URL fields, same as Banner).
        - Ecommerce: BasketDetails.cshtml (the "Payment Cards Accepted" box
          now renders from these rows instead of the old hardcoded <img>
          tags, falling back to the same three static images if this table
          has no "Payment" rows yet on a given environment).

   4. Once live, staff manage the accepted card logos entirely from the
      Intranet: Website Info Cards -> Create/Edit -> Category "Payment" ->
      set Title (alt text), Image URL (logo path), Display Order, Active.
      No further code changes needed to add, remove, reorder, or temporarily
      hide a card logo (e.g. adding "PayPal" or "Amazon Pay" later, if a
      suitable logo image is uploaded to /Content/Images/icons/ first).
   ==========================