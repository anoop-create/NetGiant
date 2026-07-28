USE netgiantMasterData;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'ngmd' AND TABLE_NAME = 'qa_Granularity')
BEGIN
	CREATE TABLE ngmd.[qa_Granularity]
	(
		GranularityID INT NOT NULL IDENTITY(1, 1),
		Granularity VARCHAR(255) NOT NULL,
		CONSTRAINT PK_Granularity_GranularityID PRIMARY KEY (GranularityID)
	);
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'ngmd' AND TABLE_NAME = 'Website')
BEGIN
	CREATE TABLE ngmd.[Website]
	(
		WebsiteID INT NOT NULL IDENTITY(1, 1),
		WebsiteName VARCHAR(100) NOT NULL,
		WebsiteURL VARCHAR(MAX) NOT NULL
		CONSTRAINT PK_Website_WebsiteID PRIMARY KEY (WebsiteID)
	);
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'ngmd' AND TABLE_NAME = 'qa_Main')
BEGIN
	CREATE TABLE ngmd.[qa_Main]
	(
		QuestionAnswerID INT NOT NULL IDENTITY(1, 1),
		Question VARCHAR(MAX) NOT NULL,
		Answer VARCHAR(MAX) NOT NULL,
		Email VARCHAR(255) NOT NULL,
		AskedDate DATETIME NOT NULL,
		RepliedDate DATETIME,
		ShowOnAllSites TINYINT NOT NULL,
		GranularityFK INT NOT NULL,
		UserFK VARCHAR(255),
		SourceWebsiteID INT NOT NULL,
		ProductID INT NOT NULL,
		AltRef VARCHAR(20) NOT NULL,
		CONSTRAINT PK_Main_QuestionAnswerID PRIMARY KEY (QuestionAnswerID),
		CONSTRAINT FK_Main_GranualityFK FOREIGN KEY (GranularityFK) REFERENCES ngmd.qa_Granularity (GranularityID)
	);
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'ngmd' AND TABLE_NAME = 'qa_WebsiteMapping')
BEGIN
	CREATE TABLE ngmd.[qa_WebsiteMapping]
	(
		WebsiteMappingID INT NOT NULL IDENTITY(1, 1),
		WebsiteFK INT NOT NULL,
		QuestionAnswerFK INT NOT NULL,
		CONSTRAINT PK_WebsiteMapping_WebsiteMappingID PRIMARY KEY (WebsiteMappingID),
		CONSTRAINT FK_WebsiteMapping_WebsiteFK FOREIGN KEY (WebsiteFK) REFERENCES ngmd.[Website] (WebsiteID) ON DELETE CASCADE,
		CONSTRAINT FK_WebsiteMapping_QuestionAnswerFK FOREIGN KEY (QuestionAnswerFK) REFERENCES ngmd.qa_Main (QuestionAnswerID) ON DELETE CASCADE
	)
END

--WEBSITES
INSERT INTO ngmd.WebSite (WebsiteName, WebsiteURL)
SELECT 'betatonergiant', 'beta.tonergiant.co.uk'
UNION
SELECT 'tonergiant', 'www.tonergiant.co.uk'
UNION
SELECT 'betacartridgemonkey', 'beta.cartridgemonkey.com'
UNION
SELECT 'cartridgemonkey', 'www.cartridgemonkey.com'
UNION
SELECT 'netgiant', 'www.netgiant.com'
UNION
SELECT 'betanetgiant', 'beta.netgiant.com'

--GRANULARITES
INSERT INTO ngmd.Granularity (Granularity)
SELECT 'All products'
UNION
SELECT 'Not assigned'
UNION
SELECT 'This manufacturer'
UNION
SELECT 'This manufacturer and this product type'
UNION
SELECT 'This product'
UNION
SELECT 'This product type'