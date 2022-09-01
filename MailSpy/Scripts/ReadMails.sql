USE {0}

SELECT TOP 200
	Mail.MailGenerated_ID									AS [Id],
	Mail.MailGenerated_Sender								AS [Sender],
	Mail.MailGenerated_Recipient							AS [Recipient],
	Mail.MailGenerated_HiddenCopy							AS [HiddenCopy],
	Mail.MailGenerated_Subject								AS [Subject],
	Mail.MailGenerated_Body									AS [Body],
	Mail.MailGenerated_DateAdded							AS [DateAdded],
	Mail.MailGenerated_ShipDate								AS [ShipDate],
	
	(
	SELECT COUNT(*)
	FROM fwk.t_ntf_AttachMailGenerated Attachments
	WHERE Attachments.MailGenerated_ID = Mail.MailGenerated_ID
	)														AS [Attachments],
	
	MailState.MailState_Name								AS [MailState]

FROM [fwk].[t_ntf_MailGenerated] Mail
INNER JOIN [fwk].[t_ntf_MailState]			MailState		ON MailState.MailState_ID = Mail.MailState_ID

WHERE
	(@dateFrom IS NULL OR CAST(Mail.MailGenerated_DateAdded AS DATE) >= CAST(@dateFrom AS DATE))
AND (@dateTo IS NULL OR CAST(Mail.MailGenerated_DateAdded AS DATE) <= CAST(@dateTo AS DATE))