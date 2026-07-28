USE netgiantMasterData;
GO

CREATE PROC ngmd.[QAWebsites] (@WebsiteID INT, @QuestionAnswersID INT, @ShowOnAll TINYINT)
AS

IF @ShowOnAll = 0
BEGIN
	INSERT INTO ngmd.qa_WebsiteMapping (WebsiteFK, QuestionAnswerFK)
	SELECT @WebsiteID, @QuestionAnswersID;
END
ELSE
BEGIN
	DELETE FROM ngmd.qa_WebsiteMapping WHERE QuestionAnswerFK = @QuestionAnswersID;
END