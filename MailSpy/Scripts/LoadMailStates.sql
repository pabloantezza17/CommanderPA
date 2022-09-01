USE {0}

SELECT
	MailStates.MailState_ID		AS Id,
	MailStates.MailState_Name	AS Description
FROM [fwk].[t_ntf_MailState] MailStates