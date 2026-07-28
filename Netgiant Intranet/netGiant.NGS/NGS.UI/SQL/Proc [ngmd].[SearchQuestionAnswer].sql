USE [netgiantMasterData]
GO

ALTER PROC [ngmd].[SearchQuestion] (@AltRef VARCHAR(20), @SearchText VARCHAR(MAX), @UnAnsweredQuestion TINYINT)
AS

IF @UnAnsweredQuestion = 0
BEGIN
	SELECT	qa.QuestionAnswerID, qa.Question, qa.Answer, qa.Email, qa.AskedDate, qa.RepliedDate, qa.ShowOnAllSites,
			g.GranularityID, qa.UserFK, qa.SourceWebsiteID, qa.ProductID, qa.AltRef
	FROM	ngmd.qa_Main qa  
			INNER JOIN ngmd.qa_Granularity g  
				ON qa.GranularityFK = g.GranularityID
	WHERE	qa.AltRef LIKE '%' + @AltRef + '%'
			AND qa.Question LIKE '%' + @SearchText + '%';
END
ELSE IF @UnAnsweredQuestion = 1
BEGIN
	SELECT	qa.QuestionAnswerID, qa.Question, qa.Answer, qa.Email, qa.AskedDate, qa.RepliedDate, qa.ShowOnAllSites,
			g.GranularityID, qa.UserFK, qa.SourceWebsiteID, qa.ProductID, qa.AltRef
	FROM	ngmd.qa_Main qa  
			INNER JOIN ngmd.qa_Granularity g  
				ON qa.GranularityFK = g.GranularityID
	WHERE	qa.AltRef LIKE '%' + @AltRef + '%'
			AND qa.Question LIKE '%' + @SearchText + '%'
			AND ISNULL(qa.Answer, '') = ''
			AND ISNULL(qa.RepliedDate, '') = ''
END
ELSE IF @UnAnsweredQuestion = 2
BEGIN
	SELECT	qa.QuestionAnswerID, qa.Question, qa.Answer, qa.Email, qa.AskedDate, qa.RepliedDate, qa.ShowOnAllSites,
			g.GranularityID, qa.UserFK, qa.SourceWebsiteID, qa.ProductID, qa.AltRef
	FROM	ngmd.qa_Main qa  
			INNER JOIN ngmd.qa_Granularity g  
				ON qa.GranularityFK = g.GranularityID 
			WHERE	qa.AltRef LIKE '%' + @AltRef + '%'
				AND qa.Question LIKE '%' + @SearchText + '%'
				AND ISNULL(qa.Answer, '') != ''
				AND ISNULL(qa.RepliedDate, '') != ''
				AND g.Granularity != 'All products'

	UNION

	SELECT	qa.QuestionAnswerID, qa.Question, qa.Answer, qa.Email, qa.AskedDate, qa.RepliedDate, qa.ShowOnAllSites,
			g.GranularityID, qa.UserFK, qa.SourceWebsiteID, qa.ProductID, qa.AltRef
	FROM	ngmd.qa_Main qa  
			INNER JOIN ngmd.qa_Granularity g  
				ON qa.GranularityFK = g.GranularityID 
	WHERE	g.Granularity = 'All Products'
			AND qa.Answer != ''
			AND qa.RepliedDate IS NOT NULL
			AND qa.AltRef = ''
			AND qa.Question LIKE '%' + @SearchText + '%'
END