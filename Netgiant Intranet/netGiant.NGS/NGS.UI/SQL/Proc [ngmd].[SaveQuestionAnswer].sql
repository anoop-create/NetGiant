USE [netgiantMasterData]
GO

CREATE PROC [ngmd].[SaveQuestionAnswer] 
(
	@QuestionAnswerID INT, 
	@Question VARCHAR(8000), 
	@Answer VARCHAR(8000), 
	@Email VARCHAR(255),
	@AskedDate DATETIME = NULL,
	@ShowOnAllSites TINYINT,
	@GranularityID INT, 
	@UserID VARCHAR(255) = NULL,
	@WebsiteID INT,
	@ProductID INT,
	@AltRef VARCHAR(20)
)
AS

DECLARE @NewQuestionAnswerID INT

IF @QuestionAnswerID > 0
BEGIN
	UPDATE	ngmd.qa_Main
	SET		Question = @Question,
			Answer = @Answer,
			Email = @Email,
			AskedDate = ISNULL(@AskedDate, GETDATE()),
			ShowOnAllSites = @ShowOnAllSites,
			GranularityFK = @GranularityID,
			RepliedDate = CASE WHEN LEN(LTRIM(RTRIM(@Answer))) > 0 THEN GETDATE() ELSE NULL END,
			UserFK = CASE WHEN @Answer IS NOT NULL THEN ISNULL(@UserID, '') ELSE '' END,
			SourceWebsiteID = @WebsiteID,
			AltRef = @AltRef,
			ProductID = @ProductID
	WHERE	QuestionAnswerID = @QuestionAnswerID;
END
ELSE
BEGIN
	INSERT	INTO ngmd.qa_Main(Question, Answer, Email, AskedDate, RepliedDate, ShowOnAllSites, GranularityFK, UserFK, SourceWebsiteID,
			AltRef, ProductID)
	SELECT	@Question [Question], 
			@Answer [Answer], 
			@Email [Email], 
			ISNULL(@AskedDate, GETDATE()) [AskedDate], 
			CASE WHEN LEN(LTRIM(RTRIM(@Answer))) > 0 THEN GETDATE() ELSE NULL END [RepliedDate], 
			@ShowOnAllSites [ShowOnAllSites], 
			@GranularityID [GranularityFK],
			ISNULL(@UserID, ''),
			@WebsiteID [SourceWebsiteID],
			@AltRef [AltRef],
			@ProductID [ProductID];
	
	SET @NewQuestionAnswerID = CONVERT(INT, SCOPE_IDENTITY());
	
	IF @WebsiteID > 0 AND @ShowOnAllSites = 0 AND @NewQuestionAnswerID > 0
	BEGIN
		INSERT INTO ngmd.qa_WebsiteMapping(WebsiteFK, QuestionAnswerFK)
		VALUES (@WebsiteID, @NewQuestionAnswerID);
	END
END

SELECT	qa.QuestionAnswerID, qa.Question, qa.Answer, qa.Email, qa.AskedDate, qa.RepliedDate, qa.ShowOnAllSites,
		g.GranularityID, qa.UserFK, qa.SourceWebsiteID, qa.ProductID, qa.AltRef
FROM	ngmd.qa_Main qa
		INNER JOIN ngmd.qa_Granularity g
			ON qa.GranularityFK = g.GranularityID
WHERE	qa.QuestionAnswerID = CASE WHEN @QuestionAnswerID > 0 THEN @QuestionAnswerID ELSE @NewQuestionAnswerID END;
