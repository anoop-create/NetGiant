USE netgiantMasterData;
GO

CREATE PROC ngmd.[GetAllQuestionAnswers]
AS
BEGIN
	SELECT	qa.QuestionAnswerID, qa.Question, qa.Answer, qa.Email, qa.AskedDate, qa.RepliedDate, qa.ShowOnAllSites,
			g.GranularityID, qa.UserFK, qa.SourceWebsiteID, qa.ProductID, qa.AltRef
	FROM	ngmd.qa_Main qa  
			INNER JOIN ngmd.qa_Granularity g  
				ON qa.GranularityFK = g.GranularityID;
END