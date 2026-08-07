/* ============================================================================
   WebsiteInfoCard - shared content table for the basket-page sidebar widgets
   (sale banner, "Free Next Day Delivery", "Trusted By 25,000+",
   "Exclusive Trade Pricing", and any similar card added later).

   One table, discriminated by Category. Managed by the Intranet project;
   read directly by the Ecommerce site (both already point at the same
   "ngmd" database via their own Database-First EF model, exactly like the
   existing cmsEntry/cmsSection tables).

   Run this against the ngmd database, then see the "NEXT STEPS" comment
   at the bottom of this file.
   ============================================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WebsiteInfoCard' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.WebsiteInfoCard
    (
        WebsiteInfoCardId   INT IDENTITY(1,1)   NOT NULL,
        WebsiteId           INT                 NOT NULL,
        Category            NVARCHAR(50)        NOT NULL,   -- 'Banner', 'Delivery', 'Trust', 'TradePricing', ...
        IconClass           NVARCHAR(50)        NULL,       -- e.g. 'fa fa-truck' - not used by 'Banner'
        Title               NVARCHAR(200)       NULL,       -- card header text - not used by 'Banner'
        BodyText            NVARCHAR(500)       NULL,       -- short teaser shown before "Find Out More"
        FindOutMoreContent  NVARCHAR(MAX)       NULL,       -- HTML shown when "Find Out More" is expanded
        ImageUrl            NVARCHAR(500)       NULL,       -- banner image (or any card image, if ever needed)
        LinkUrl             NVARCHAR(500)       NULL,       -- optional click-through, e.g. banner -> sale page
        DisplayOrder        INT                 NOT NULL CONSTRAINT DF_WebsiteInfoCard_DisplayOrder DEFAULT (0),
        IsActive            BIT                 NOT NULL CONSTRAINT DF_WebsiteInfoCard_IsActive DEFAULT (1),
        CreatedDate         DATETIME            NOT NULL CONSTRAINT DF_WebsiteInfoCard_CreatedDate DEFAULT (GETDATE()),
        CreatedBy           NVARCHAR(100)       NULL,
        ModifiedDate        DATETIME            NULL,
        ModifiedBy          NVARCHAR(100)       NULL,

        CONSTRAINT PK_WebsiteInfoCard PRIMARY KEY CLUSTERED (WebsiteInfoCardId),
        CONSTRAINT FK_WebsiteInfoCard_Website FOREIGN KEY (WebsiteId) REFERENCES dbo.Website (WebsiteID)
    );

    CREATE NONCLUSTERED INDEX IX_WebsiteInfoCard_Website_Category
        ON dbo.WebsiteInfoCard (WebsiteId, Category, IsActive, DisplayOrder);
END
GO

/* ----------------------------------------------------------------------------
   Seed data - matches what's currently hardcoded in BasketDetails.cshtml,
   plus one inactive placeholder row for the sale banner (activate it once
   you have a real image/link to put in).

   IMPORTANT: verify the WebsiteName below actually matches your TonerGiant
   row before running (SELECT WebsiteID, WebsiteName, FriendlyName FROM
   dbo.Website to check) - swap to FriendlyName or Abbreviation if needed.
   ---------------------------------------------------------------------------- */
DECLARE @WebsiteId INT = (SELECT WebsiteID FROM dbo.Website WHERE WebsiteName = 'TonerGiant');

IF @WebsiteId IS NULL
BEGIN
    PRINT 'Could not find a Website row named ''TonerGiant'' - skipping seed data. Check dbo.Website and re-run the INSERTs below manually with the correct WebsiteId.';
END
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.WebsiteInfoCard WHERE WebsiteId = @WebsiteId)
BEGIN
    INSERT INTO dbo.WebsiteInfoCard
        (WebsiteId, Category, IconClass, Title, BodyText, FindOutMoreContent, ImageUrl, LinkUrl, DisplayOrder, IsActive, CreatedBy)
    VALUES
    (@WebsiteId, 'Banner', NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, 'Migration'),

    (@WebsiteId, 'Delivery', 'fa fa-truck', 'Free Next Day Delivery',
        'Order before <strong>5:30 Mon-Thu</strong>, &amp; <strong>5pm Fri</strong> for next working day delivery.',
        '<p>We dispatch all in-stock orders placed before the cut-off time on the same working day. Orders placed after the cut-off will be dispatched the following working day.</p><p>Delivery is available Monday to Friday excluding public holidays.</p>',
        NULL, NULL, 1, 1, 'Migration'),

    (@WebsiteId, 'Trust', 'fa fa-star-o', 'Trusted By 25,000+',
        'Thousands of businesses trust us for office supplies, printer cartridges and workplace essentials.',
        '<p>We dispatch all in-stock orders placed before the cut-off time on the same working day. Orders placed after the cut-off will be dispatched the following working day.</p><p>Delivery is available Monday to Friday excluding public holidays.</p>',
        NULL, NULL, 2, 1, 'Migration'),

    (@WebsiteId, 'TradePricing', 'fa fa-gbp', 'Exclusive Trade Pricing',
        'Register for a trade account and receive exclusive business pricing.',
        '<p>We dispatch all in-stock orders placed before the cut-off time on the same working day. Orders placed after the cut-off will be dispatched the following working day.</p><p>Delivery is available Monday to Friday excluding public holidays.</p>',
        NULL, NULL, 3, 1, 'Migration');
END
GO

/* ============================================================================
   NEXT STEPS (manual, in Visual Studio - not something this script can do)

   1. Run this script against the ngmd database.

   2. In BOTH projects that model this database, refresh the Database-First
      EF model so the new table becomes an entity class:
        - NetGiant Ecommerce: Class Libraries/DataAccess/DataAccess/EntityFramework/Ngmd.edmx
        - Intranet:           netgiant.Intranet.BusinessLayer (its Ngmd/NetgiantMasterData model)
      Open the .edmx designer -> right-click -> "Update Model from Database" ->
      Add -> Tables -> WebsiteInfoCard -> Finish. This generates a
      "WebsiteInfoCard" POCO class (matching the ProductAddon.cs pattern
      already in the Ecommerce project) in each project - nothing else to
      hand-write for the entity itself.

   3. Add a menu entry in the Intranet's actionLink table so
      "Website Info Cards" shows up in the admin nav next to CMS. Find the
      existing CMS menu's parentLevelID first:
        SELECT * FROM dbo.actionLink WHERE controllerName = 'CMS';
      then insert a sibling row, e.g.:
        INSERT INTO dbo.actionLink
            (actionLinkDesc, actionLinkLevel, parentLevelID, actionLinkURL, actionName, controllerName, active, dateLastUpdate, roles, area)
        VALUES
            ('Website Info Cards', 2, <parentLevelID from above>, '/WebsiteInfoCard/Index', 'Index', 'WebsiteInfoCard', 1, GETDATE(), 'IntranetAdmin, PMSAdmin, SEO', '');
      Adjust actionLinkLevel/parentLevelID/area to match the sibling CMS row
      exactly (the query above will show you its real values).
   ============================================================================ */
